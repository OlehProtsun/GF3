# GF3 finished product film

75 seconds · 1920×1080 · 30 fps · H.264/yuv420p + AAC stereo 48 kHz.
The final deliverable is `artifacts/promo/gf3-product-film-1080p.mp4`.
All employees, dates, shifts and requests are synthetic October 2026 examples.

## Existing pipeline inventory (2026-10-08)
1. Existing promo.html is retained as the independent Vite entry.
2. Existing React mount and scaled 1920×1080 stage are retained.
3. Existing GSAP context owns a single cleaned-up timeline.
4. Existing capture controller is opt-in and validates finite seeks.
5. Previous browser checks verified order-independent frame pixels.
6. Existing preview supports playback, scrub, keyboard and reduced motion.
7. Existing nine-scene 75-second storyboard is retained; its static screenshot treatment is upgraded.
8. Previous Demo* product panels were recreated illustrations, not authentic captures.
9. Existing production component mounts and E2E interception supply five synthetic authentic sources.
10. Existing H.264/AAC renderer streams PNGs; source hashes, styleframes and motion cues extend its QA.


## Authentic product sources

| Film chapter | Production source | Acquisition |
|---|---|---|
| manager | `src/entities/containers/ui/ContainerGraphMatrix.tsx` + own CSS | production-component; actual editor commits a synthetic shift |
| availability | `src/pages/employee-availability/ui/EmployeeAvailabilityPage.tsx` | actual-app-capture; real preset dialog and draft selection |
| schedule | `src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx` | actual-app-capture; published fixture, matrix/day controls |
| swap | `src/pages/employee-swap/ui/EmployeeSwapPage.tsx` | actual-app-capture; offer expansion and actual acceptance confirmation |
| more | Same production employee schedule page | actual-app-capture; actual hours summary calculated from fixture slots |

`real-ui/shot-manifest.ts` catalogs all five sources. Sanitized PNGs and
`captures/capture-provenance.json` record three states, production source hashes,
capture hashes, local route and acquisition method per feature. Fixture interception
reuses existing `e2e/swap-restoration.spec.ts`, `employee-availability-presets.spec.ts`
and `schedule-hero.spec.ts` patterns. All API calls are fulfilled inside the isolated
browser; no backend, database or real account is contacted. Availability shows a
local draft, not a fabricated server save. Swap ends at the real confirmation dialog;
no backend approval or successful transfer is claimed. Manager planning and the
previously published employee schedule are distinct states, not a fake publish action.
The old `Demo*` illustrations remain reference code and are absent from the film.

## Prerequisites and preview

```bash
cd FrontEnd
npm install
npx playwright install chromium
npm run dev
# http://localhost:5173/promo.html
# Capture-only URL: http://localhost:5173/promo.html?capture=1
```

Install FFmpeg and ffprobe separately. Windows:

```powershell
winget install --id Gyan.FFmpeg -e
# Reopen terminal, then:
ffmpeg -version
ffprobe -version
```

Ubuntu, when system installation is permitted:
`sudo apt-get update && sudo apt-get install -y ffmpeg`.
The scripts never install system software. Both executables must be on PATH.
`npm run build` retains both `index.html` and `promo.html`; the normal router and
production behavior are unchanged. `npm run preview` serves built entries.

Preview controls: Play/Pause, scrub, Space outside form controls, left/right ±1s,
Home/End 0/75. Reduced-motion preview shows static summaries and a next-scene button.
Capture ignores reduced-motion preferences, decodes all images/fonts before readiness
and exposes only duration/fps/size/seek/getTime metadata. Nonfinite seeks throw.
All nine scenes remain mounted; the timeline is the sole source of film time.

## Acquire and render

Sanitized source PNGs are included; capture again only when updating real product UI.
With Vite running, `npm run promo:capture-ui` executes real controls at a 1440×900
viewport, DPR 1, Polish locale and a fixed October date. `PROMO_APP_URL` overrides its
local origin; remote hosts are rejected. `PROMO_CAPTURE_DIR` optionally changes where
sanitized captures are written (use the default for the compositor). No production
storage state is required by the built-in synthetic fixture pathway. Never provide
real credentials or real customer datasets. Private captures/storage state are ignored.
Optional `PROMO_MANAGER_STATE`/`PROMO_EMPLOYEE_STATE` accept local demo storageState paths
only when `PROMO_DEMO_DATASET` points to a local recipe declaring `synthetic:true`.
The built-in read-only interception still supplies the coherent October fixture; state
contents are never logged, copied into media, or sent to a remote origin.

```bash
npm run promo:film
# Backward-compatible alias for the same finished pipeline:
npm run promo:render
npm run promo:verify
```

`PROMO_BASE_URL` optionally overrides the local Vite origin. If no server is running,
rendering starts Vite on the first free port in 5174–5176 and stops only its own child.
It never kills unrelated processes using 5173. In PowerShell:
`$env:PROMO_BASE_URL = 'http://localhost:5173'`.

The renderer synthesizes an original quiet 75s PCM WAV locally; no downloaded samples,
music services or voice imitation. It captures exactly 2250 PNGs at i/30, streams each
with backpressure to FFmpeg, then encodes medium preset/CRF 17 H.264 and 192 kb/s AAC.
Audio has soft original harmonic intervals, eight transition accents, a 0.6s fade-in
and 1.5s fade-out. The film also remains understandable when muted.

Encoding creates `gf3-product-film-1080p.partial.mp4`; technical QA extracts stills,
poster, contact sheet and `qa-report.json`. Objective checks cover decoded frame count,
video/audio formats and duration, source hashes, black intervals, audio peak/RMS and
ending fade. **Objective measurements alone do not establish editorial/audio quality.**
The process waits up to 600 seconds for review of the actual encoded film and stills.
After checking captions, crops, sharpness, playback timing and soundtrack, record
`artifacts/promo/visual-review.json` with `status: "PASS"`, the exact `videoSha256`
printed by the renderer, reviewer and concrete findings. It never approves itself from
RMS alone. A matching reviewed hash plus passing probes yields PASS and atomic promotion
of the partial to the final MP4. `PROMO_REVIEW_TIMEOUT_SECONDS` changes the review wait.
A pending review is PROVISIONAL, never a completed video. To verify a retained partial:
`PROMO_VERIFY_FILE` can override the verifier's input file. Earlier final films are
preserved on failed exports; only newly generated failed partial output is removed.

Required outputs under ignored `artifacts/promo/`:
- `gf3-product-film-1080p.mp4`
- `gf3-product-film-poster.png` (1920×1080 at 71.5s)
- `contact-sheet.png` (nine chronological chapter centers)
- `qa-report.json` (PASS required)
- `audio-bed.wav`, `visual-review.json`, `stills/` (verification evidence)

## Validate and troubleshoot

```bash
npm run lint
npm run build
npm run test
npx playwright test e2e/promo-video.spec.ts --project=chromium
npm run promo:verify
ffprobe -v error -count_frames -show_streams -show_format -of json artifacts/promo/gf3-product-film-1080p.mp4
```

Expected: 75s (74.95–75.05), 2250 decoded frames, 1920×1080, 30/1 fps,
h264/yuv420p, AAC stereo 48000 Hz. Poster/contact sheet/provenance and final CTA must
also be verified. The supplemental summary uses actual fixture-derived hours, not
an invented performance metric. No exported MP4 or private auth state belongs in Git.

Missing Chromium: run `npx playwright install chromium`. Missing encoder: install the
system tools, reopen terminal and check their versions. Server unavailable: check the
printed local URL/PROMO_BASE_URL. Busy Vite port: let renderer choose its own free port.
PowerShell npm forwarding issues: use `npm.cmd` or `node node_modules/vite/bin/vite.js`.
Ctrl+C cancels rendering and closes owned processes; a technically valid partial
awaiting review is preserved for diagnosis. Browser/encoding/provenance failures exit
nonzero with specific diagnostics. Do not label an unreviewed partial as final.

## Current execution verification (2026-10-08)

Full Vitest: 342 tests / 74 files passed. Film Chromium: 6 tests passed.
Lint: no errors, 18 warnings in existing production files.
Final motion film export and independent technical QA passed; native encoded playback was inspected.

## Locked creative treatment and references

The current film preserves the existing React/GSAP entry and exact nine windows.
`storyboard.ts` supplies chapter timing, feature gestures and `MOTION_CUE_SHEET`;
`timeline.ts` implements those cues. `SHOT_DETAILS` catalogs native source rectangles.
Actual UI crops assemble, reattach and travel independently; masks reveal Polish
headlines, three workflows have a truthful cursor/state chain, the summary badge
is extracted and returned, and four final product views form a two-row composition.
Read UI in steady head-on intervals; perspective belongs to assembly transitions.
The ending holds unchanged from 72s through the last encoded frame.

Three user references are preserved as inspiration only:
- https://www.youtube.com/watch?v=SgmuplXU2iY — LangEase identity available; visual playback was not accessible in this execution environment.
- https://www.youtube.com/watch?v=jX4dLxiso6A — Doks.AI reference supplied by the plan; page fetch failed, footage was not independently viewed.
- https://www.youtube.com/watch?v=pZv7me6dFns — inaccessible/unverified; no footage descriptions are invented.

Additional required artifacts: nine 1920×1080 `styleframes/01-pain.png` through
`09-outro.png`, `motion-cue-sheet.json`, and `creative-qa.md`. Frames are captured
before encoding for QA, then replaced with frames decoded from the finished film.
Creative review covers motion grammar, authentic states, typography and composition;
objective video/audio probes alone do not establish subjective quality.

Internal previsualization only (does not deliver a movie), PowerShell:
`$env:PROMO_STYLEFRAMES_ONLY='1'; npm run promo:film; Remove-Item Env:PROMO_STYLEFRAMES_ONLY`.

When WinGet FFmpeg is already installed but absent from PATH, add its `bin` directory
to the current PowerShell `$env:Path`, then verify `ffmpeg -version` and `ffprobe -version`.
For this machine the installed directory is:
`C:\Users\Oleg\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0.2-full_build\bin`.


## Final motion-film delivery (2026-10-08)

`artifacts/promo/gf3-product-film-1080p.mp4`: 7,377,706 bytes (7.04 MiB),
75.000s, 1920×1080, 30/1 fps, 2250 decoded frames, H.264/yuv420p/faststart,
AAC stereo 48000 Hz. Final SHA-256:
`bec17ad6f72e7dde3c7f836bba98e00212c9691af2d4c84a7c5c4a71548579da`.
Five genuine product sources passed source/capture hash checks. The final technical
QA report is PASS. Poster, contact sheet, nine styleframes, motion cue sheet,
creative QA and playback evidence all correspond to the final encoded timeline.

Build passed. Full Vitest: 342 tests / 74 files; final focused promo checks: 4 tests.
Final Chromium: 6 tests passed, including repeated-seek byte equality and preview
controls. Full lint: 0 errors / 18 existing production warnings; scoped promo lint
is clean. Normal index.html and production routing remain unchanged.

The first encoded film was reviewed, its modal-corner crop was corrected, then all
2250 frames were re-encoded once. Final native 1× playback samples had zero dropped
frames; the ending played to 75s with the CTA visible. Encoded audio peak −25.51 dBFS,
RMS −35.88 dBFS, final-tail RMS −56.92 dBFS; all eight cue periods and the final
logarithmic spectrum were inspected. Subjective audio listening remains unverified
because the tools do not supply audio input; neither probes nor spectra establish
subjective sound quality. This limitation is explicit in visual-review.json and
creative-qa.md.

Changed in this execution: PromoApp.tsx, storyboard.ts, timeline.ts, Scenes.module.css,
real-ui/RealFeatureShot.tsx and .module.css, shot-manifest.ts, real-ui.test.ts,
synthetic capture PNGs/provenance, capture-product-ui.mjs, render-film.mjs,
verify-film.mjs, e2e/promo-video.spec.ts and this document. Existing package commands,
Vite entries, audio synthesis and production components were retained.

Preview: `npm run dev`, then `http://localhost:5173/promo.html`.
Reproduce: `npm run promo:film`; independent QA: `npm run promo:verify`.
