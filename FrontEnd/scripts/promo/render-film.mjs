import { chromium } from "@playwright/test";
import { spawn, spawnSync } from "node:child_process";
import { readFile, writeFile, rename, rm, stat, mkdir } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import path from "node:path";
import { generateAudio } from "./generate-audio.mjs";
import { verifyFilm, verifyProvenance, artifactDirectory, finalOutput } from "./verify-film.mjs";

const root = fileURLToPath(new URL("../../", import.meta.url));
const partial = path.join(artifactDirectory, "gf3-product-film-1080p.partial.mp4");
let browser, encoder, ownedServer, encoderDone;
let generated = false, canceled = false, preserveForReview = false;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const cancel = () => { canceled = true; encoder?.stdin.destroy(); encoder?.kill(); void browser?.close().catch(() => {}); };
process.once("SIGINT", cancel); process.once("SIGTERM", cancel);

async function resolveServer() {
  const url = new URL(process.env.PROMO_BASE_URL || "http://localhost:5173");
  if (!["localhost", "127.0.0.1", "[::1]"].includes(url.hostname)) throw new Error("Film requires a local demonstration origin.");
  try { if ((await fetch(new URL("/promo.html", url), { signal: AbortSignal.timeout(1500) })).ok) return url; } catch { /* Start only our own server. */ }
  if (process.env.PROMO_BASE_URL) throw new Error(`Server ${url.origin} unavailable. Start npm run dev.`);
  for (const port of [5174, 5175, 5176]) {
    const candidate = new URL(`http://localhost:${port}`);
    ownedServer = spawn(process.execPath, [path.join(root, "node_modules/vite/bin/vite.js"), "--host", "localhost", "--port", String(port), "--strictPort"], { cwd: root, windowsHide: true, stdio: "ignore" });
    for (let attempt = 0; attempt < 40; attempt++) {
      if (ownedServer.exitCode !== null) break;
      try { if ((await fetch(new URL("/promo.html", candidate), { signal: AbortSignal.timeout(500) })).ok) return candidate; } catch { /* Wait for bootstrap. */ }
      await sleep(250);
    }
    ownedServer.kill(); ownedServer = undefined;
  }
  throw new Error("Could not start Vite on ports 5174–5176; set PROMO_BASE_URL to a running local Vite server.");
}

try {
  for (const tool of ["ffmpeg", "ffprobe"]) {
    const check = spawnSync(tool, ["-version"], { windowsHide: true, encoding: "utf8" });
    if (check.error || check.status !== 0) throw new Error(`${tool} unavailable. Windows: winget install --id Gyan.FFmpeg -e; reopen terminal. Ubuntu: sudo apt-get update && sudo apt-get install -y ffmpeg. ${check.error?.message || check.stderr}`);
  }
  const origin = await resolveServer();
  const audio = await generateAudio();
  browser = await chromium.launch({ headless: true }).catch(error => { throw new Error(`Install Chromium: npx playwright install chromium\n${error.message}`); });
  const context = await browser.newContext({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 1, colorScheme: "light", locale: "pl-PL", reducedMotion: "no-preference", serviceWorkers: "block" });
  const page = await context.newPage();
  const errors = [];
  page.on("pageerror", error => errors.push(error.message));
  page.on("console", message => { if (message.type() === "error") errors.push(message.text()); });
  await page.route("**/*", route => new URL(route.request().url()).origin === origin.origin ? route.continue() : route.abort());
  await page.goto(new URL("/promo.html?capture=1", origin).href, { timeout: 30_000, waitUntil: "networkidle" });
  await page.waitForFunction(() => window.__GF3_PROMO__?.ready === true, undefined, { timeout: 30_000 });
  await page.evaluate(async () => { await document.fonts.ready; await Promise.all([...document.images].map(image => image.decode())); });
  const contract = await page.evaluate(() => {
    const { duration, fps, width, height } = window.__GF3_PROMO__;
    const bounds = document.querySelector("[data-promo-stage]").getBoundingClientRect();
    return { duration, fps, width, height, stageWidth: bounds.width, stageHeight: bounds.height, shots: [...new Set([...document.querySelectorAll("[data-promo-real-shot]")].map(node => node.dataset.promoRealShot))] };
  });
  if (contract.duration !== 75 || contract.fps !== 30 || contract.width !== 1920 || contract.height !== 1080 || contract.stageWidth !== 1920 || contract.stageHeight !== 1080 || !["manager", "availability", "schedule", "swap", "more"].every(id => contract.shots.includes(id))) throw new Error(`Invalid film contract: ${JSON.stringify(contract)}`);
  const provenance = await verifyProvenance();
  if (provenance.authenticScenes.length !== 5 || provenance.sourceProvenance.blocked.length || provenance.missingAuthenticScenes.length) throw new Error("Authentic acquisition incomplete or source/capture hashes changed.");
  await context.setOffline(true);
  if (errors.length) throw new Error(errors.join("\n"));
  const cues = await page.locator("[data-promo-stage]").getAttribute("data-motion-cue-sheet").then(JSON.parse);
  if (cues.length !== 9 || cues[0].startSec !== 0 || cues.at(-1).endSec !== 75) throw new Error("Incomplete motion cue sheet.");
  await mkdir(path.join(artifactDirectory, "styleframes"), { recursive: true });
  await writeFile(path.join(artifactDirectory, "motion-cue-sheet.json"), JSON.stringify(cues, null, 2));
  for (const [index, cue] of cues.entries()) {
    const seconds = (cue.startSec + cue.endSec) / 2;
    await page.evaluate(async seconds => { window.__GF3_PROMO__.seek(seconds); await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))); }, seconds);
    await page.locator("[data-promo-stage]").screenshot({ path: path.join(artifactDirectory, "styleframes", `${String(index + 1).padStart(2, "0")}-${cue.sceneId}.png`), animations: "disabled", caret: "hide" });
  }
  console.log("Nine native styleframes and timeline-derived motion cue sheet ready for creative QA.");
  if (process.env.PROMO_STYLEFRAMES_ONLY === "1") {
    console.log("Internal previsualization only; no final MP4 encoded.");
  } else {
  encoder = spawn("ffmpeg", ["-y", "-f", "image2pipe", "-framerate", "30", "-vcodec", "png", "-i", "pipe:0", "-i", audio.file,
    "-map", "0:v:0", "-map", "1:a:0", "-c:v", "libx264", "-preset", "medium", "-crf", "17", "-pix_fmt", "yuv420p", "-r", "30", "-frames:v", "2250", "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2", "-t", "75", "-movflags", "+faststart", partial], { windowsHide: true, stdio: ["pipe", "ignore", "pipe"] });
  generated = true;
  let diagnostic = "", encoderFailure;
  encoder.stderr.on("data", chunk => { diagnostic = (diagnostic + chunk.toString()).slice(-20_000); });
  encoder.stdin.on("error", error => { encoderFailure = error; });
  encoderDone = new Promise((resolve, reject) => { encoder.once("error", reject); encoder.once("close", code => code === 0 ? resolve() : reject(new Error(`FFmpeg exit ${code}\n${diagnostic}`))); });
  void encoderDone.catch(error => { encoderFailure = error; });
  for (let index = 0; index < 2250; index++) {
    if (canceled) throw new Error("Film canceled.");
    if (encoderFailure) throw encoderFailure;
    await page.evaluate(async seconds => { window.__GF3_PROMO__.seek(seconds); await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))); }, index / 30);
    const png = await page.locator("[data-promo-stage]").screenshot({ type: "png", animations: "disabled", caret: "hide" });
    if (errors.length) throw new Error(errors.join("\n"));
    await new Promise((resolve, reject) => encoder.stdin.write(png, error => error ? reject(error) : resolve()));
    if (index % 150 === 0) console.log(`Rendering ${index + 1}/2250 (${(index / 30).toFixed(1)}s)`);
  }
  encoder.stdin.end(); await encoderDone;
  await browser.close(); browser = undefined;
  console.log("Encoding complete. Running frame/audio/provenance QA and extracting review artifacts.");
  let report = await verifyFilm(partial);
  if (report.status !== "PASS") {
    preserveForReview = true;
    console.log("Objective QA passed. Review the actual partial MP4 and artifacts; save visual-review.json with status PASS and videoSha256:", report.videoSha256);
    const limit = Number(process.env.PROMO_REVIEW_TIMEOUT_SECONDS || 600);
    for (let elapsed = 0; elapsed < limit; elapsed += 2) {
      if (canceled) throw new Error("Review canceled; valid partial MP4 retained.");
      await sleep(2000);
      try { const review = JSON.parse(await readFile(path.join(artifactDirectory, "visual-review.json"), "utf8")); if (review.status === "PASS" && review.videoSha256 === report.videoSha256) { report = await verifyFilm(partial); break; } } catch { /* Review is explicitly pending. */ }
    }
  }
  if (report.status !== "PASS") throw new Error("Visual/playback review pending; partial MP4 retained, final film not declared complete.");
  await rename(partial, finalOutput); generated = false;
  report.checkedFile = finalOutput;
  await writeFile(path.join(artifactDirectory, "qa-report.json"), JSON.stringify(report, null, 2));
  console.log(JSON.stringify({ outputPath: finalOutput, bytes: (await stat(finalOutput)).size, duration: report.duration, width: 1920, height: 1080, fps: 30, videoCodec: "h264", audioCodec: "aac", frameCount: report.frameCount, qa: path.join(artifactDirectory, "qa-report.json"), status: report.status }, null, 2));
  }
} catch (error) { console.error(`Film export failed: ${error.message}`); process.exitCode = 1; }
finally {
  encoder?.stdin.destroy(); if (encoder && encoder.exitCode === null) encoder.kill();
  await encoderDone?.catch(() => {}); await browser?.close().catch(() => {}); ownedServer?.kill();
  if (generated && !preserveForReview) await rm(partial, { force: true });
  process.removeListener("SIGINT", cancel); process.removeListener("SIGTERM", cancel);
}
