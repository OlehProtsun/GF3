import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { mkdir, readFile, stat, writeFile, access } from "node:fs/promises";
import { createHash } from "node:crypto";
import { fileURLToPath, pathToFileURL } from "node:url";
import path from "node:path";

const execute = promisify(execFile);
export const artifactDirectory = fileURLToPath(new URL("../../artifacts/promo/", import.meta.url));
export const finalOutput = path.join(artifactDirectory, "gf3-product-film-1080p.mp4");
const frontend = fileURLToPath(new URL("../../", import.meta.url));
const command = (tool, args) => execute(tool, args, { windowsHide: true, maxBuffer: 20 * 1024 * 1024 });

export async function verifyProvenance() {
  const sourceProvenance = JSON.parse(await readFile(path.join(frontend, "src/promo/real-ui/captures/capture-provenance.json"), "utf8"));
  const authenticScenes = [], missingAuthenticScenes = [];
  for (const id of ["manager", "availability", "schedule", "swap", "more"]) {
    const shot = sourceProvenance.shots.find(shot => shot.id === id);
    if (!shot || !shot.synthetic || !["production-component", "actual-app-capture"].includes(shot.acquisition) || shot.assets.length !== 3) { missingAuthenticScenes.push(id); continue; }
    try {
      for (const source of shot.sourcePaths) {
        if (createHash("sha256").update(await readFile(path.join(frontend, source.path))).digest("hex") !== source.sha256) throw new Error("Changed source hash");
      }
      for (const asset of shot.assets) {
        const image = await readFile(path.join(frontend, "src/promo/real-ui/captures", asset.file));
        if (createHash("sha256").update(image).digest("hex") !== asset.sha256 || image.length < 1000) throw new Error("Invalid capture hash");
      }
      authenticScenes.push(id);
    } catch { missingAuthenticScenes.push(id); }
  }
  return { sourceProvenance, authenticScenes, missingAuthenticScenes };
}

export async function verifyFilm(file = finalOutput) {
  await mkdir(path.join(artifactDirectory, "stills"), { recursive: true });
  const failures = [], warnings = [];
  const probe = JSON.parse((await command("ffprobe", ["-v", "error", "-count_frames", "-show_streams", "-show_format", "-of", "json", file])).stdout);
  const video = probe.streams.find(stream => stream.codec_type === "video");
  const audio = probe.streams.find(stream => stream.codec_type === "audio");
  const size = (await stat(file)).size;
  const encoded = await readFile(file);
  const atoms = [];
  for (let offset = 0; offset + 8 <= encoded.length;) {
    let length = encoded.readUInt32BE(offset);
    const type = encoded.toString("ascii", offset + 4, offset + 8);
    if (length === 1) length = Number(encoded.readBigUInt64BE(offset + 8));
    if (length === 0) length = encoded.length - offset;
    if (length < 8 || offset + length > encoded.length) break;
    atoms.push(type); offset += length;
  }
  const faststart = atoms.includes("moov") && atoms.includes("mdat") && atoms.indexOf("moov") < atoms.indexOf("mdat");
  if (!faststart) failures.push("MP4 moov must precede mdat (faststart).");
  const duration = Number(probe.format.duration);
  const frameCount = Number(video?.nb_read_frames ?? video?.nb_frames);
  if (size < 1024 * 1024) failures.push("MP4 is smaller than 1 MiB.");
  if (!video || video.codec_name !== "h264" || video.width !== 1920 || video.height !== 1080 || video.pix_fmt !== "yuv420p" || video.r_frame_rate !== "30/1" || frameCount !== 2250 || duration < 74.95 || duration > 75.05) failures.push("Video contract failed.");
  if (!audio || audio.codec_name !== "aac" || audio.channels !== 2 || Number(audio.sample_rate) !== 48000 || Math.abs(Number(audio.duration) - 75) > .1) failures.push("Audio contract failed.");
  const { sourceProvenance, authenticScenes, missingAuthenticScenes } = await verifyProvenance();
  if (sourceProvenance.blocked.length || missingAuthenticScenes.length) failures.push("Authentic source provenance is incomplete.");
  const black = await command("ffmpeg", ["-hide_banner", "-i", file, "-an", "-vf", "blackdetect=d=0.20:pix_th=0.05:pic_th=0.98", "-f", "null", "-"]);
  for (const match of black.stderr.matchAll(/black_start:([\d.]+) black_end:([\d.]+) black_duration:([\d.]+)/g)) {
    if (Number(match[2]) > 6 && Number(match[3]) > .2) failures.push(`Unexpected black interval: ${match[0]}`);
  }
  const audioStats = await command("ffmpeg", ["-hide_banner", "-i", file, "-vn", "-af", "astats=metadata=0:reset=0", "-f", "null", "-"]);
  const peakDb = Number([...audioStats.stderr.matchAll(/Peak level dB: ([\d.-]+)/g)].at(-1)?.[1]);
  const rmsDb = Number([...audioStats.stderr.matchAll(/RMS level dB: ([\d.-]+)/g)].at(-1)?.[1]);
  const tail = await command("ffmpeg", ["-hide_banner", "-ss", "74.8", "-i", file, "-vn", "-af", "astats=metadata=0:reset=0", "-f", "null", "-"]);
  const tailRmsDb = Number([...tail.stderr.matchAll(/RMS level dB: ([\d.-]+)/g)].at(-1)?.[1]);
  if (!Number.isFinite(peakDb) || peakDb >= 0 || !Number.isFinite(rmsDb) || rmsDb < -65 || !Number.isFinite(tailRmsDb) || tailRmsDb >= rmsDb - 12) failures.push("Audio silence, clipping or ending fade check failed.");
  const centers = [3, 9, 17, 28, 38, 49, 59, 66, 72];
  const contactCenters = [3, 9, 17.5, 28, 38.5, 49.5, 59, 66, 72];
  const boundaries = [6, 12, 23, 33, 44, 55, 63, 69];
  for (const seconds of new Set([...centers, ...contactCenters, ...boundaries, 71.5])) {
    await command("ffmpeg", ["-v", "error", "-y", "-ss", String(seconds), "-i", file, "-frames:v", "1", path.join(artifactDirectory, "stills", `frame-${seconds}.png`)]);
  }
  await command("ffmpeg", ["-v", "error", "-y", "-ss", "71.5", "-i", file, "-frames:v", "1", path.join(artifactDirectory, "gf3-product-film-poster.png")]);
  const inputs = contactCenters.flatMap(seconds => ["-i", path.join(artifactDirectory, "stills", `frame-${seconds}.png`)]);
  const filter = centers.map((_, index) => `[${index}:v]scale=640:360[s${index}]`).join(";") + ";" + centers.map((_, index) => `[s${index}]`).join("") + "xstack=inputs=9:layout=0_0|640_0|1280_0|0_360|640_360|1280_360|0_720|640_720|1280_720[out]";
  await command("ffmpeg", ["-v", "error", "-y", ...inputs, "-filter_complex", filter, "-map", "[out]", "-frames:v", "1", path.join(artifactDirectory, "contact-sheet.png")]);
  for (const name of ["gf3-product-film-poster.png", "contact-sheet.png"]) await access(path.join(artifactDirectory, name));
  const cues = JSON.parse(await readFile(path.join(artifactDirectory, "motion-cue-sheet.json"), "utf8"));
  if (cues.length !== 9 || cues[0].startSec !== 0 || cues.at(-1).endSec !== 75 || cues.some((cue, index) => index > 0 && cue.startSec !== cues[index - 1].endSec)) failures.push("Motion cue sheet is incomplete.");
  await mkdir(path.join(artifactDirectory, "styleframes"), { recursive: true });
  for (const [index, cue] of cues.entries()) {
    // Replace previsualization PNGs with frames decoded from the final encoded film.
    await command("ffmpeg", ["-v", "error", "-y", "-ss", String((cue.startSec + cue.endSec) / 2), "-i", file, "-frames:v", "1", path.join(artifactDirectory, "styleframes", `${String(index + 1).padStart(2, "0")}-${cue.sceneId}.png`)]);
  }
  try { await access(path.join(artifactDirectory, "creative-qa.md")); } catch { failures.push("Creative QA report missing."); }
  let review;
  try { review = JSON.parse(await readFile(path.join(artifactDirectory, "visual-review.json"), "utf8")); }
  catch { warnings.push("Human visual/playback/audio review pending; objective probes cannot establish editorial quality."); }
  if (review?.status === "FAIL") failures.push("Visual/playback review failed: " + (review.findings ?? []).join("; "));
  const hash = createHash("sha256").update(await readFile(file)).digest("hex");
  const reviewed = review?.status === "PASS" && review.videoSha256 === hash;
  if (!reviewed && review) warnings.push("Review is for a different video or has not passed.");
  const report = { status: failures.length ? "FAIL" : reviewed ? "PASS" : "PROVISIONAL", duration, fps: 30, width: video?.width, height: video?.height, videoCodec: video?.codec_name, audioCodec: audio?.codec_name, frameCount, faststart, outputBytes: size, sourceProvenance, authenticScenes, missingAuthenticScenes, visualWarnings: warnings, failures, audio: { peakDb, rmsDb, tailRmsDb }, review, renderCommand: "npm run promo:film", outputPath: finalOutput, checkedFile: file, videoSha256: hash, timestamp: new Date().toISOString() };
  await writeFile(path.join(artifactDirectory, "qa-report.json"), JSON.stringify(report, null, 2));
  if (failures.length) throw new Error(failures.join("\n"));
  return report;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const report = await verifyFilm(process.env.PROMO_VERIFY_FILE || finalOutput);
  console.log(JSON.stringify({ status: report.status, duration: report.duration, frameCount: report.frameCount, audio: report.audio, outputPath: report.outputPath }, null, 2));
  if (report.status !== "PASS") process.exitCode = 1;
}
