import { mkdir, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { pathToFileURL } from "node:url";

export async function generateAudio() {
  const rate = 48000, seconds = 75, frames = rate * seconds;
  const file = fileURLToPath(new URL("../../artifacts/promo/audio-bed.wav", import.meta.url));
  const wav = Buffer.alloc(44 + frames * 4);
  wav.write("RIFF", 0); wav.writeUInt32LE(wav.length - 8, 4); wav.write("WAVEfmt ", 8);
  wav.writeUInt32LE(16, 16); wav.writeUInt16LE(1, 20); wav.writeUInt16LE(2, 22);
  wav.writeUInt32LE(rate, 24); wav.writeUInt32LE(rate * 4, 28); wav.writeUInt16LE(4, 32);
  wav.writeUInt16LE(16, 34); wav.write("data", 36); wav.writeUInt32LE(frames * 4, 40);
  const cues = [6, 12, 23, 33, 44, 55, 63, 69];
  let peak = 0, sum = 0;
  for (let i = 0; i < frames; i++) {
    const t = i / rate;
    const fade = Math.min(1, t / .6, (seconds - t) / 1.5);
    for (let channel = 0; channel < 2; channel++) {
      const phase = channel * .25;
      let sample = [130.8128, 164.8138, 195.9977].reduce((total, frequency, index) =>
        total + .018 * (.8 + .2 * Math.sin(t * .17 + index)) *
          (Math.sin(2 * Math.PI * frequency * t + phase) + .12 * Math.sin(2 * Math.PI * frequency * 2 * t + phase)), 0);
      for (const cue of cues) {
        const elapsed = t - cue;
        if (elapsed >= 0 && elapsed < .38) {
          const envelope = Math.sin(Math.PI * elapsed / .38) ** 2 * Math.exp(-elapsed * 8);
          sample += .045 * envelope * (Math.sin(2 * Math.PI * (440 * elapsed - 110 * elapsed * elapsed) + phase) + .18 * Math.sin(2 * Math.PI * 660 * elapsed));
        }
      }
      sample = .9 * Math.tanh(sample) * Math.max(0, fade);
      peak = Math.max(peak, Math.abs(sample)); sum += sample * sample;
      wav.writeInt16LE(Math.round(sample * 32767), 44 + i * 4 + channel * 2);
    }
  }
  if (peak <= 0 || peak >= 1 || wav.length !== 44 + 75 * 48000 * 4) throw new Error("Audio synthesis failed PCM validation.");
  await mkdir(fileURLToPath(new URL("../../artifacts/promo/", import.meta.url)), { recursive: true });
  await writeFile(file, wav);
  return { file, duration: seconds, rate, channels: 2, peakDb: 20 * Math.log10(peak), rmsDb: 20 * Math.log10(Math.sqrt(sum / (frames * 2))) };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  console.log(JSON.stringify(await generateAudio(), null, 2));
}
