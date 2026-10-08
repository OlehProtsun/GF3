# Plan.md — GF3 Premium UI Motion Product Film (MP4, authentic GF3 visuals)

> **CODEX EXECUTION PLAN — IMPLEMENT AND DELIVER THE VIDEO FILE, NOT JUST A WEBSITE.**
> Read and execute the steps in order. Do not perform another full repository analysis, replace the existing promo implementation from scratch, or stop when `promo.html` looks good. The work is **not complete** until an actual playable MP4 exists on disk and has passed the verification gates below.

## 0. Baseline, objective, and non-negotiable distinction

- **Repository:** `https://github.com/OlehProtsun/GF3`, local working branch `DEV2`.
- **Previously verified historical commit:** `b63b35d44222b22122689bf6fe96f310e1e733a8`. The user's CODEX agent has **subsequently generated `promo.html` locally**. Those local changes are **not verifiable in the linked remote branch**; they are the implementation starting point. **Never reset, discard, or overwrite them wholesale.**
- **User goal:** a polished, high-end, cinematic **finished MP4 product pitch** using **the user's own GF3 visual components and genuinely recognizable interfaces**, with animated demonstrations of all major customer-facing workflows. The desired aesthetic is a modern premium software/product launch (restrained, editorial, bold typography, elegant motion), **not** an assertion that GF3 itself uses AI.
- **Mandatory master:** `FrontEnd/artifacts/promo/gf3-product-film-1080p.mp4`, exactly **75 seconds**, **1920×1080**, **30 fps**, H.264, `yuv420p`, standard MP4 faststart, playable without development software. **Audio:** a subtle, original, locally synthesized soundtrack + occasional interface transition sounds, encoded AAC 48 kHz stereo. All necessary content remains fully understandable when muted.
- **Additional required deliverables:** `FrontEnd/artifacts/promo/gf3-product-film-poster.png` (1920×1080), `FrontEnd/artifacts/promo/contact-sheet.png` (one frame from every scene), `FrontEnd/artifacts/promo/qa-report.json` (technical probe, scene checks, real-UI provenance, errors), and `FrontEnd/PROMO.md` with one-command reproduction and troubleshooting.
- **Working preview:** preserve and improve existing `FrontEnd/promo.html` as the editable/seekable source for producing the MP4. It is a production tool, **not the final deliverable**.
- **Language:** all on-screen marketing copy Polish, including correct diacritics. Developer docs and code comments English.
- **No invented features or metrics:** no claims of AI scheduling, instant auto-approval of swaps, automatic task allocation, percentage savings or customer counts unless demonstrably implemented. All people, shifts and messages in promotional material must be **synthetic**.
- **Privacy:** no filming production customer accounts; no real staff names, identities, schedules, JWTs, credentials or client data inside the final MP4, intermediate captures or committed repo.

### 0.1 Actual project paths/source-of-truth established from the repository

- `FrontEnd/package.json`: React + TypeScript + Vite, GSAP, `@gsap/react`, Playwright and Vitest already exist at baseline.
- `docs/design-system.json`: detailed design snapshot, metadata points at **older** commit `d8e752a` and says `runtimeSourceOfTruth: false`. Use it as a design index; the **current React/CSS implementation** wins on discrepancies.
- `FrontEnd/src/index.css`, `FrontEnd/src/shared/ui/motion.css`, source-specific CSS modules: actual styling and motion references.
- `FrontEnd/src/pages/container/ui/ContainerPage.tsx`: manager planning/workspace. Known reusable UI exports from `@entities/containers` include `ContainerProfileWorkspace`, `ContainerListCard`, etc. Avoid importing the entire stateful page into a standalone preview without its providers.
- `FrontEnd/src/pages/employee-availability/ui/EmployeeAvailabilityPage.tsx`: employee availability workflow.
- `FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx`: published schedule, view and summary.
- `FrontEnd/src/pages/employee-swap/ui/EmployeeSwapPage.tsx`: employee swap flow, confirmation, filters/history.
- `FrontEnd/src/app/router/AppRouter.tsx`: manager `/container`, `/availability`, `/employee`, `/communications`; employee `/availability`, `/schedule`, `/swap`, `/notifications` (routes are role-protected).
- `FrontEnd/src/shared/ui/PageHeader`, `FrontEnd/src/shared/ui/components/IosButton`, `FrontEnd/src/shared/ui/sections/CardSection/CardSection.tsx`: verified existing UI building-block references.
- The previous Plan.md specified `FrontEnd/src/promo/`, `FrontEnd/scripts/render-promo.mjs`, `FrontEnd/PROMO.md` and `FrontEnd/promo.html`, **but CODEX must check which of these were actually created locally**; do not assume a previous plan was implemented in full.

### 0.2 The critical new acceptance rule — real product visuals

**The previous promo plan permitted custom illustrated lookalikes (`DemoWorkspace`, `DemoSwap`, etc.). That is no longer sufficient.**

1. Primary product footage must consist of either **(A)** real production GF3 presentational React components, mounted with frozen synthetic props while preserving their own styles; or **(B)** actual screenshots/frame sequences captured from the running GF3 UI using Playwright with a completely synthetic demonstration dataset. These are **authentic GF3 pixels**, not hand-redrawn miniatures.
2. Use **A** for cleanly separable presentational components, including existing shared icons, buttons, typography, panels, schedules/graph summaries as available. Use **B** for tightly coupled manager/employee pages requiring auth, query providers and routing. It is legitimate to edit/direct/crop/camera-animate a real GF3 capture; it is **not** legitimate to replace it with an unrelated CSS imitation.
3. The film must visually demonstrate **at least four real-UI feature families**: manager scheduling, availability, employee schedule, shift swaps. A fifth segment must show **one** verified supplementary customer feature: notifications, schedule overview/summary, employee records, or existing export controls. The preferred fifth family is **schedule summary + notifications/export** if these exist in the locally available implementation.
4. For every feature shot, record source path(s), acquisition mode `production-component` or `actual-app-capture`, original route if relevant, and how synthetic data was supplied in `FrontEnd/src/promo/real-ui/shot-manifest.ts`. Add one contact-sheet preview per shot.
5. **Do not mark the video complete** if the main feature footage uses only promo-only recreated demo cards. An MP4 may be generated for diagnostic purposes, but report that as **provisional**, with the missing authentic shots listed in `qa-report.json`.
6. The cinematic typography, background, compositional frames, cursor illustration, masks, and scene transitions **are new promotional graphics** and need not be literal production UI. Clearly separate those layers from product content.

## 1. Locked visual/narrative direction

### 1.0 Reference-based creative brief (new approval of visual direction)

This section **overrides any older instruction implying that a polished screen recording or a simple sequence of flat screenshots is sufficient**. A realistic product walkthrough is the *raw material*, not the finished treatment. Final film must have **motion-designed, visually integrated REAL GF3 UI**, with intentional art direction, storytelling, camera choreography, sound cues, transitions and graphic composition.

**User-provided references** (creative inspiration, not assets to reproduce):

- `https://www.youtube.com/watch?v=SgmuplXU2iY` — independently identified as *Best SaaS Product Launch Ad Video | LangEase*. Reference for polished animated SaaS storytelling and branded UI motion.
- `https://www.youtube.com/watch?v=jX4dLxiso6A` — independently identified as *Video Ad for AI / SaaS Product | Doks.AI* (Zelios). Reference for persuasive problem → solution storytelling and animated explanations rather than tutorial screen recordings.
- `https://www.youtube.com/watch?v=pZv7me6dFns` — **provided by the user, visual details not independently verified in planning environment**. If CODEX can open it in a browser, include it in the 3-reference moodboard. If inaccessible, state that in `PROMO.md` and follow the concrete treatment below; never invent claims about its footage or block video export solely for unavailable external inspiration.

**Evidence limit:** The planning environment verified video identity and some third-party descriptions, **not a full frame-by-frame viewing**. Thus the directions below are *original production specifications inspired by the requested genre*, not assertions that particular cuts, camera angles, typography, colors or timing occur in the source videos. Do not scrape/reupload the original ads or use their video frames, audio, trademarks or proprietary graphics.

**Mandatory GF3-specific creative identity:**

1. The dominant visual language must come from `docs/design-system.json` **and current** GF3 React/CSS. The JSON is an older descriptive snapshot; actual UI at the local working tree wins. Preserve the real GF3 visual language (especially its calendar cells, button geometry, cards, labels, employee/workspace panels, shadows, radius, colors, type choices and spacing). Current GF3 primary blue is documented as `#2563eb` at historical commit; resolve exact current CSS tokens before rendering.
2. Render scenes on tasteful dark ink and soft paper/neutral backgrounds derived from GF3 tokens, with blue highlights, occasional accent gradients only if harmonious with GF3. No wholesale transformation into the LangEase/Doks palette or generic purple neon AI theme.
3. Each feature chapter must contain **at least one hero product UI composition** and **one close-up** of *an identifiable real GF3 component* (calendar, scheduling slot, availability selector, swap modal, status/badge/summary). The viewer should be able to name the product workflow from the animation alone.
4. The real GF3 components are the moving subjects: independent shift cards slide and align; calendar rows build a weekly schedule; availability elements respond to a meaningful selection; a real swap modal emerges from its real button; confirmation/state change remains true to existing app logic. Decorative graphics orbit or spotlight them, but never fake app states.
5. Avoid an amateur "capture browser → add music → zoom on screenshot" aesthetic. The camera must travel purposefully between component groups, with seamless match-cut geometry, foreground/background layers, authored graphic shapes and punchy but readable titles. Avoid changing every frame merely for spectacle: clarity takes priority.

**Visual hierarchy of each feature chapter:** (i) <=7-word primary hook for ~1–2 sec; (ii) 2–4 sec product close-up / interaction; (iii) 2–4 sec readable feature outcome; (iv) animated physical carry-over of an existing GF3 UI shape into the next scene. These segments may overlap the existing 75-second fixed chapters; do not change the total duration.

**Music / sound guidance:** Original legally usable music bed with modern clean electronic/percussive pulse and restrained tonal layers, beat-aligned clicks/soft swishes/confirmation cues. Narration is **not required**; the film communicates silently from its Polish on-screen copy. Do not borrow reference-track music. Synthesize a custom original bed with deliberate musical structure, not a piercing single oscillator; if user-supplied licensed music is already available, it can be used only with recorded provenance and permission (no dependence on a new external source).

### 1.0.1 Signature motion grammar (implementation must demonstrate each, not merely mention)

| Motion motif | Real GF3 material | Choreography | Minimum appearance |
|---|---|---|---|
| **Grid assembly** | Genuine schedule grid, rows, shift cells | Cells/rows arrive from separate directions and snap into their **correct real layout**, camera lands on final unchanged app | `manager` and `connected` |
| **Card-to-screen match cut** | Existing shift/availability card or genuine cropped UI element | Preserve an on-screen card's bounding box (position, size, corner radius) while changing depth/background to reveal its location inside the real complete screen; no fake morphing of its content | at least 2 transitions |
| **Layered camera travel** | Real GF3 UI layers captured/mounted independently | Screen planes tilt subtly in 2.5D perspective, parallax foreground text and background, settle to head-on for readability. Avoid aggressive perspective during data reading | at least 3 feature chapters |
| **Interaction micro-cinema** | Real button/calendar selection/confirmation | Cursor/touch indicator arrives, button reacts using true press styling, matching actual before/after product state, soft audio hit | at least 3 distinct feature workflows |
| **Kinetic type** | Promo-only Polish copy over GF3 colors | Large text revealed by mask/line split/word stagger in sync with camera/sound, not generic repeated fade-in | problem, reveal, 2 features, outro |
| **Focus extraction** | Real existing badge, date cell, summary, notification | Exact pixels from UI are isolated by non-destructive crop/alpha mask, briefly enlarged as a spotlight, then seamlessly returned to original place | at least 2 scenes |
| **Continuity move** | Same genuine UI shape/color across shots | Foreground card/blue line serves as animated bridge so adjacent clips feel like one continuous camera move | at least 4 of 8 boundaries |
| **Hero composition** | Real app canvas | Strong legible central shot with negative space, restrained shadow and a clear reason to look at a specific interaction | at least 4 scenes |

**Technical authenticity boundary:** A separate photographic/captured UI piece may be used as a 2.5D foreground layer if it is an unaltered crop of authentic GF3 output. Allowed transformations: affine/perspective movement, clip, mask, scale, opacity, shadow, blur on **background layers**, short outlines and callouts. **Not allowed:** redrawing calendar text or controls with marketing CSS and then claiming they are app components; inventing new screens or effects pretending to be native interactions; deforming real text beyond legibility.

### 1.0.2 Measurable polish rubric (manual + automated checks)

- **First 3 seconds:** an immediately comprehensible friction point. Avoid a logo-only opening or 3 seconds of empty abstract particle graphics.
- **Editorial tempo:** transition event approximately every 2–4 seconds, but each main product UI chapter contains at least one >=2.5-second continuous period during which the genuine relevant interface is readable. Do not turn the 75 seconds into 75 independent slide reveals.
- **Scene variety:** at least 3 clearly different camera compositions (macro crop, isometric 2.5D, broad product view); at least 2 scenes with very clean/minimal backgrounds; at least one deliberate pause after a strong feature reveal. Do not keep every scene at identical scale/angle.
- **Accents:** every prominent blue stroke/button/line is either a true GF3 token or a promo graphic clearly related to the selected real control; use sparingly. No random colored glowing orbs, glitch effects, excessive bounce, spinning logos or gratuitous stock mockups.
- **Legibility:** 1920x1080 output; avoid clipping titles, blurred full UI, microtext on unscaled complex tables; for detail-heavy captures, move camera to exact relevant row/card, not to a 400px-wide whole-page thumbnail. At steady product shots, date/status/shift labels must be human-readable.
- **Truthful content:** marketing title and illustrated problem metaphors are creatively authored; any alleged visible GF3 workflow/result must be sourced from actual rendered components/captures and traced in the shot manifest.
- **Professional finish:** smooth acceleration/deceleration, stable color and font rendering, no SVG/CSS aliasing or pixel jump between composited PNG layers; audio should have balanced clean intro, sync points and smooth outro.

### 1.0.3 Scene-by-scene cinematography over the FIXED 75-second storyboard

The exact nine chapter boundaries and Polish copy below remain binding. Implement **these specific directorial beats** inside the existing chapter windows. A real rendered UI source remains visible wherever a feature is claimed.

| Time | Visual choreography and authentic feature proof | Transition anchor / cue |
|---|---|---|
| `00–06` pain | Oversized Polish headline on neutral field; editorial stacks of **abstract** shift-notes/messages collide, misalign, then halt. Problem graphics must not masquerade as a broken GF3 interface. Punch into the visual gap that will become the GF3 workspace. | One hard accent at 0.3s, fast stagger 1.0–3.0, white-space pause 4.0–5.0, converging masks 5.2–6.0 |
| `06–12` reveal | One tiny authentic GF3 schedule/calendar tile appears at center; other true UI fragments assemble into a recognizable GF3 product frame. Camera smoothly expands to hero view, GF3 name/title types in. Keep first on-screen GF3 view unambiguously real. | Calendar/card geometry becomes next manager grid |
| `12–23` manager | Start macro on a real shift card and date cell, tilt/reframe to reveal the manager scheduling grid. Animate actual before/action/after layout or truthful highlights. Push slightly into genuine occupied shift cell; stabilize full readable context. | Real blue selection/shift-card edge travels into availability page |
| `23–33` availability | Real GF3 availability selectors emerge as isolated authored **crops of actual UI**, arrange in a rhythmic row, then reattach to the complete availability calendar. One genuine selection action + true visual result, hold on legible changed cell(s). | Selected calendar cell's dimensions match the opening schedule day tile |
| `33–44` schedule | Close on a real date + assigned shift detail; pull out into actual employee weekly/monthly schedule. A subtle line guides eye to shift details/summary, then hierarchy settles. Show published state; do not animate fictional publishing. | Authentic shift card scales into swap offer card |
| `44–55` swap | Make a real swap offer card the focal subject, actual real button click and real dialog open. Stage swap confirmation in a legible 2.5D foreground card **only if the app really displays that state**; otherwise show genuine offer/details and do not imply it was automatically accepted. The swap flow is the emotional highlight. | Real modal/card rectangle collapses into source-compatible notifications/summary tile |
| `55–63` more | Two quick cut-ins of confirmed, actually implemented UI only (schedule summary, notifications or export control by priority). Pan through authentic real visual details; do not show new fabricated analytics. | Tiles fan out into connected collage |
| `63–69` connected | Bring real manager, availability, employee schedule and swap frames into one clean four-part composition, using their genuine captures. Draw one restrained blue continuity line through key cards while camera floats slightly, then compress into a simple GF3 mark. | Blue line becomes GF3 brand accent |
| `69–75` outro | Quiet high-contrast hero; GF3 brand lockup, concise Polish value line, CTA. Hold final frame steadily for >=3 sec, no background screen noise or intrusive particle effects. | Soft audio resolve, full fade-out by end |

**Storytelling rule:** Each middle chapter must exhibit an event the real product makes possible, not a passive collage. There must be a clear visual "before → user gesture → visible outcome" chain for manager/availability/swap where supported; the employee schedule must show the readable result of a genuinely published schedule.

### 1.0.4 Mandatory previsualization artifacts (not permission to stop early)

Create the following **original GF3** shot-design artifacts *in addition* to the current plan's MP4/poster/contact sheet and provenance, under `FrontEnd/artifacts/promo/`:

- `styleframes/01-pain.png`, `02-reveal.png`, `03-manager.png`, `04-availability.png`, `05-schedule.png`, `06-swap.png`, `07-more.png`, `08-connected.png`, `09-outro.png` — exactly nine 1920×1080 representative creative frames from the actual film compositor, not AI-generated substitute mockups. Use original/captured GF3 pixels for all feature frames. These must be available before full export for targeted QA.
- `motion-cue-sheet.json` — timeline array `{sceneId, startSec, endSec, headline, realUiShotIds, cameraMoves, transitionAnchor, soundCueSec}` derived from the actual deterministic timeline; no invented demo operations.
- `creative-qa.md` — yes/no evaluation of the above motion grammar, scene variety, source authenticity, readability, sound design and visual polish, with links to frame names and known limitations. Never claim the film has achieved agency-grade subjective quality solely from passing ffprobe.

These are **QA artifacts**, not new web pages or a second promotional system. Preserve the old `promo.html`, previous film renderer and prior asset organization.



### 1.1 Aesthetic

- 16:9 film frame with exceptionally clear UI: oversized editorial headings, generous whitespace, deep navy/near-black contrast, white product canvases, restrained GF3 blue `#2563eb` accents; final color decisions use current application CSS.
- Motion language: rapid-but-readable type masks, elegant springless transforms, smooth reframing, tasteful UI zooms, clean match cuts, precise highlighting, subtle depth/parallax, one or two dramatic pauses, no unmotivated camera shakes, excessive blooms, generic sci-fi particles, stock imagery or fake devices.
- Show **actual readable app screen areas** for at least 3 seconds of every core feature chapter. Avoid product screenshots too tiny to recognize. Limit software-screen movement during text-heavy moments. If the screen is complex, crop into the relevant existing UI area using non-destructive scale/translate.
- Render at full native 1920×1080; use `deviceScaleFactor:1` and wait for fonts before capture. All essential text should remain safely within 80px inset from film edges. No strobing, illegible microtext, overflow or blurry CSS scale.
- Original licensed assets only. Do not reproduce OpenAI/Google/Grok brand designs, logos, slogans, proprietary fonts or voice tracks. Do not access external image/music/AI APIs to satisfy this job.

### 1.2 Exact 75-second storyline and Polish copy

Keep the following feature ordering and boundaries **exact**. Motion choreography inside a window may be refined for readability; do not add scenes or extend runtime.

| ID | Time (start inclusive, end exclusive) | Real product material and action | Primary on-screen Polish copy |
|---|---|---|---|
| `pain` | `00.0–06.0` | Many disconnected schedule cards/messages; cinematic editorial graphics (no fake GF3 UI claim). | `Grafiki. Wiadomości. Zmiany.` → `Chaos, który zabiera czas.` |
| `reveal` | `06.0–12.0` | Chaos converges into brand GF3 and one **real** product frame. | `A gdyby wszystko było w jednym miejscu?` → `Poznaj GF3.` |
| `manager` | `12.0–23.0` | Authentic manager planning/container/graph UI. Animate attention to a genuine shift row/grid and show existing workflow state before/after; do not imply magical automatic creation. | `Planowanie zmian. Pod kontrolą.` → `Przejrzysty grafik w jednym miejscu.` |
| `availability` | `23.0–33.0` | Authentic employee/manager availability interface with demonstrable availability selection and readable calendar. | `Dostępność bez zgadywania.` → `Wiesz, kto i kiedy może pracować.` |
| `schedule` | `33.0–44.0` | Authentic employee `/schedule` view: week/month shifts, summary/clear day highlight. Show the result of a **previously published** schedule, not a fake publish operation. | `Każdy widzi swój grafik.` → `Jasno. Zawsze pod ręką.` |
| `swap` | `44.0–55.0` | Authentic employee `/swap` interface: shift offer, clear confirmation and accepted state **only if represented by actual functionality**. Use two synthetic employees; no unverified auto-approval. | `Plany się zmieniają?` → `Zamiany zmian w jednym miejscu.` |
| `more` | `55.0–63.0` | Authentic existing summary, employee notifications, or export controls (choose by verified availability, priority: summary + notifications; **do not fabricate functionality**). | `Wszystko, co ważne. Czytelnie.` |
| `connected` | `63.0–69.0` | Multi-screen composition made from **genuine** captured manager/schedule/availability/swap material; editorial flow line connects them. | `Jeden system. Jeden rytm pracy.` |
| `outro` | `69.0–75.0` | GF3 logo/wordmark, quiet hold ≥3 s with no residual obscuring motion. | `GF3` → `Grafiki bez niepotrzebnego chaosu.` → `Zobacz, jak działa.` |

**Interaction choreography per chapter:**
- `manager`: 12–14 headline; 14–18 push into existing schedule grid; 18–21 reveal affected real cell / state; 21–23 hold + transition.
- `availability`: 23–25 title; 25–29 actual availability selection/highlight; 29–31 associated view/summary; 31–33 legible hold/transition.
- `schedule`: 33–35 title; 35–39 calendar day focus; 39–42 show real shift details/summary; 42–44 transition.
- `swap`: 44–46 title; 46–50 actual UI offer/details; 50–53 genuine confirmation/accepted presentation; 53–55 hold/transition.
- `more`: 55–57 title; 57–61 real supporting UI; 61–63 transition.
- `connected`: 63–66 align actual UI images; 66–69 one flowing composition with caption.
- `outro`: 69–71 logo reveal; 71–72 supporting line; 72–75 static legible ending.

## 2. Final architecture — one approach, reuse the existing work

**Keep the existing Vite/React/GSAP promo project as the film compositor, and add an authentic-product-footage ingestion + deterministic Playwright/FFmpeg MP4 pipeline.** Do **not** introduce Remotion, After Effects, a second video app, a new backend, or a custom redesign of the GF3 product.

**Data flow:** real GF3 presentational components OR controlled local app pages → synthetic demo state → reusable stills/frame sequences captured/arranged in the promo asset directory → current `promo.html` film stage with GSAP-driven titles/camera/scene transitions → frame-accurate `window.__GF3_PROMO__.seek()` → Playwright screenshots streamed to FFmpeg → H.264/AAC MP4 → FFprobe and visual/audio QA.

### 2.1 Preflight: inspect only what has changed since the prior plan

**Action: READ ONLY.** Check these local paths first, preserving local uncommitted changes:

- `FrontEnd/promo.html`
- `FrontEnd/src/promo/**` (only this subtree)
- `FrontEnd/scripts/render-promo.mjs` (if present)
- `FrontEnd/package.json`, `FrontEnd/vite.config.ts`, `FrontEnd/.gitignore`, `FrontEnd/PROMO.md`
- `docs/design-system.json` and only the five page/UI sources in section 0.1 needed for authentic captures.

Record a 10-line inventory in `FrontEnd/PROMO.md`: what already exists, whether capture controller renders deterministic frames, and which screenshots were authentic vs recreated. **Do not run a full repo scan or reset/reset hard/pull/rebase.** Keep previously functional promo scenes and renderer; extend them.

### 2.2 CREATE — minimal new source assets and utilities

All paths below are relative to the repository root.

| Action | Path | Exact responsibility |
|---|---|---|
| CREATE | `FrontEnd/src/promo/motion-cue-sheet.ts` (or exact existing storyboard equivalent) | Single source for 75-second cue timing, per-scene camera keyframes, title reveals, transition anchors, feature-shot mapping and effect markers. Only one timeline authority; do not duplicate the existing storyboard. |
| CREATE | `FrontEnd/src/promo/real-ui/shot-manifest.ts` | Typed immutable feature-shot provenance catalog. Map `{id, feature, acquisition, sourcePaths, sourceRoute?, dataSet, assets, notes}` with IDs `manager`, `availability`, `schedule`, `swap`, `more`. No invented API URLs. |
| CREATE | `FrontEnd/src/promo/real-ui/RealFeatureShot.tsx` | Pure presentational component placing **real** captured UI media or an existing production presentational component inside stable `data-promo-real-shot` wrapper. Crop/reframe via props (CSS transform), not through painting a replacement UI. |
| CREATE | `FrontEnd/src/promo/real-ui/RealFeatureShot.module.css` | Local aspect-preserving masks/shadows/device frames/sharp rendering; NEVER override global GF3 product CSS. |
| CREATE | `FrontEnd/scripts/promo/capture-product-ui.mjs` | A targeted Playwright script to capture frames of actual running app demo routes, where direct component mounts cannot satisfy the shot. Include real route and provenance metadata. Inputs and error behavior in §3.2. |
| CREATE | `FrontEnd/scripts/promo/render-film.mjs` | Mandatory reusable deterministic 75-second H.264/AAC encoder; reuse the previous `render-promo.mjs` implementation via a wrapper or refactor, but keep `npm run promo:render` working. |
| CREATE | `FrontEnd/scripts/promo/generate-audio.mjs` | Produce a subtle **original** 75s WAV bed and short cue accents using deterministic local mathematical audio synthesis (no online TTS, music generators or licensed samples). Do not overpower legible copy; avoid loudness spikes. |
| CREATE | `FrontEnd/scripts/promo/verify-film.mjs` | Probe MP4 technical parameters, input source provenance, thumbnails/black-frame diagnostics; produce `qa-report.json` and fail on mandatory criteria. |
| CREATE | `FrontEnd/src/promo/real-ui/real-ui.test.ts` | Unit checks for source provenance/catalog, five selected feature shots, no empty data, no production credential references. |
| CREATE | `FrontEnd/e2e/promo-video.spec.ts` | Preview/capture/seek order tests on existing promo entry and actual feature shots. |
| MODIFY | `FrontEnd/src/promo/` existing scene modules and existing storyboard/timeline | Retain working code; change duration to **75** and replace promo-only faux feature previews with real shots. Add camera, annotations, titles and scene transitions defined in §1. |
| MODIFY | `FrontEnd/package.json` | Add `promo:film`, `promo:verify`, and `promo:capture-ui` script commands while preserving `promo:render` as backward-compatible alias. No new animation framework. |
| MODIFY | `FrontEnd/PROMO.md` | Exact run, capture, render, verify, problem-solving commands; include real-UI provenance table, three reference links with third marked unverified if inaccessible, directorial style rubric and asset provenance. |
| MODIFY | `FrontEnd/.gitignore` | Ignore local output under `artifacts/promo/`, and any `src/promo/real-ui/private-captures/`/playwright authentication state. |

If the local implementation uses equivalent names/paths, **extend the existing module rather than create a duplicate of the same responsibility**; preserve all public routes and scripts. Fixed external filenames and commands below remain binding.

### 2.3 REUSE/DO NOT TOUCH

**REUSE:** GSAP 3 timeline/`@gsap/react`, Vite, React 19/TS, existing real shared GF3 visual components, their current CSS modules and fonts, Playwright Chromium, and FFmpeg. Use no external video generation services. Preserve existing actual app CSS implementation.

**DO NOT TOUCH:** .NET backend, SQLite/migrations, auth/permission gates, app router, application providers, data/business/swap calculations, user-facing production page behavior, `docs/design-system.json`, and normal `index.html`. No commits of auth storage-state, synthetic fake customer identities that resemble actual employees, browser videos containing real data, or giant generated MP4 files (the final MP4 is local in ignored `artifacts/`).

## 3. Authentic footage acquisition, step by step

### Step 1 — Verify and preserve existing promo pipeline

**Action: MODIFY minimally.** Inspect exactly the preflight files above. Confirm `promo.html` actually serves at local Vite port and check if a `?capture=1` deterministic `window.__GF3_PROMO__` controller exists. Keep existing good transitions/visuals. Record any broken render condition precisely. Do not start new React app or migrate the existing scenes to another framework.

If `window.__GF3_PROMO__` exists, retain contract. Otherwise add a **capture-only** controller:

```ts
type GF3FilmController = {
  ready: boolean;
  duration: 75;
  fps: 30;
  width: 1920;
  height: 1080;
  seek(seconds: number): void;
  getTime(): number;
};
```

`seek(t)` must be finite-input validated, pause playback and synchronously yield the same image at time `t` regardless of seek history. Every scene remains mounted in the DOM. No CSS auto-keyframes, timers, `Math.random()` or network-driven content may change film-critical pixels during capture.

### Step 2 — Prepare synthetic GF3 demo states **without modifying production business rules**

**Action: REUSE/CREATE in promo only.** Use one coherent fictional October 2026 workweek, 3–6 synthetic employees, plausible shift times, availability selections and a plausible shift swap. Preserve role separation: manager visual data on manager material; employee visual data on employee material. Ensure the visuals and actions reflect the app's actual supported transitions.

Preference order **for every feature**:

1. Use existing exported **production presentational component** and current CSS with typed synthetic props; capture it isolated with a promo-only wrapper and no backend.
2. If the page is API/auth-stateful, use Playwright to capture **actual production page DOM** at existing app routes in an isolated **local demonstration instance** with synthetic records. Reuse test/fixture or local seeded demo mechanisms that are *already present*; do not run it on production or real user data.
3. If a genuine screenshot cannot be obtained (no demo state, credentials, no backend), **fail that authenticity check explicitly**. Never silently substitute the `Demo*.tsx` promotional lookalike and label it a production feature capture.

For the source/route family, use the exact paths in §0.1; supplementary shot priority order `employee schedule summary` > `notifications` > `existing export actions` > `employee listing`. Do not add a new product feature.

### Step 3 — Capture authentic GF3 UI where direct React component rendering is impractical

**Action: CREATE.** `FrontEnd/scripts/promo/capture-product-ui.mjs`.

**Environment contract:**

- `PROMO_APP_URL` — address of local **demo-only** GF3 app (default `http://localhost:5173` when applicable); the script must verify local origin and reject remote production hosts by default.
- `PROMO_MANAGER_STATE` and `PROMO_EMPLOYEE_STATE` — optional paths to **locally prepared ignored** Playwright `storageState` files. Do not print, package, or copy their contents into promo artifacts. They must reference synthetic/demo users only.
- `PROMO_DEMO_DATASET` — optional path to already available local synthetic fixtures or a demo seed recipe. If production flow requires a backend that is absent, emit a descriptive blocked-source error with setup requirements. Do not create or migrate a production DB.
- `PROMO_CAPTURE_DIR` — default `FrontEnd/src/promo/real-ui/captures/` for **verified sanitized demo material**; no real employee content.

Script behavior:

1. Launch existing Playwright Chromium at fixed 1440×900 viewport, DPR 1, `locale:'pl-PL'` (only if the app supports Polish; otherwise use the existing supported UI language and keep Polish marketing headlines separate), stable `colorScheme` and time zone `Europe/Warsaw`. Wait for app bootstrap and font readiness.
2. Open exact manager/employee route(s) from §1, using correct demo session when the page requires it. Explicitly check that the expected real page loaded (not login, empty/spinner, 403, error banner, or an unconfigured mock UI).
3. For each scenario, acquire a clearly named **beginning, interaction, outcome** capture where a real UI state is available; drive clicks/hover/scroll via role/label-based selectors and **only actual controls**. For screen transitions requiring real mutation, operate only against a disposable synthetic fixture/demo instance and keep the result consistent across film takes; seed/reset between takes.
4. Save full-resolution PNGs or tightly-scoped PNG frame sequences into `real-ui/captures/{feature}/`; omit browser chrome, devtools, real usernames, passwords, notifications, tokens and scrollbars not meant for the product showcase. Preserve the **real product styling**.
5. Write/update provenance in `shot-manifest.ts` with source code path, route, frame filenames, and acquisition method; clean up any saved storage state references from output.
6. Capture a stable screenshot before and after each genuinely demonstrated product operation. A cinematic highlight or simulated cursor overlay may **focus** attention; it must **not** invent a success state absent from the captured workflow.

**Concrete target scenes:** `manager` planning grid; `availability` selections; `schedule` day/month shift detail; `swap` offer + dialog/confirmation; `more` genuine summary/notification/export. The script should fail with a per-scene list, not falsely say “success” when screenshots are missing.

### Step 4 — Replace decorative fake previews with real visual sources

**Action: MODIFY existing `FrontEnd/src/promo` scenes + CREATE `RealFeatureShot.tsx`.**

- Mount `RealFeatureShot` in `manager`, `availability`, `schedule`, `swap`, `more` and `connected` scenes. For production-component shots, render real reusable React component with synthetic props and import its **existing CSS module**. For Playwright-capture shots, display the captured full-res image(s) without recoloring/repainting product UI.
- Apply GSAP camera movement (`x`,`y`,`scale`,`opacity`, clip/mask) **to the wrapper only**; keep text and product pixels crisp. Use at most one clear cursor/click moment and one outcome highlight per feature scene. Pan/zoom should be motivated by the user action, not just decorative movement.
- Play back real **before → interaction → after** screenshots/frames as synchronized states from the same captured interface. Use real live GIF/video frames only if they were captured from the actual app; PNG state transitions can be animated by positioning two genuine screen states.
- Remove or repurpose the older `DemoWorkspace`, `DemoAvailability`, `DemoEmployeeSchedule`, `DemoSwap` from the **final film** if they are promo-only imitations. They may remain as development reference code, but do not appear in the exported core feature chapters.
- Keep the original graphic intro, brand reveal and outro as cinematic promotional visuals (they do not need to be actual app UI).
- Onscreen caption/annotation layers must not cover buttons, shift dates or app data important to the demonstration.
- No cross-scene blank frames, z-index leaks, stretched app shots, stale highlight after a cut, or two competing headline layers.

### Step 5 — Refine motion to launch-film standard

**Action: MODIFY existing GSAP timeline and stylesheet.**

- Encode exact start/end times from §1.2 in one central immutable `storyboard.ts` / current equivalent, `PROMO_DURATION_SECONDS=75`, `PROMO_FPS=30` and `2250` frames.
- Use eased cinematic camera moves, matched component geometry across adjacent scenes, and masks revealing authentic UI at high visual weight; avoid simple hard cuts between static PowerPoint-like slides.
- Problem/reveal are kinetic type-driven; each feature segment highlights a product workflow, and the ending is intentionally quiet. Build all 7 motion motifs from §1.0.1. Follow exact scene choreography from §1.0.3, not a flat slideshow.
- Assemble 2.5D compositions from real presentational components or verified actual-app cropped media (with authentic source rects). Use real UI pieces as moving subjects; static full-page screenshot pans by themselves **do not meet acceptance**.
- Generate all nine source-derived `styleframes/` and `motion-cue-sheet.json` before final export, then use them for visual QA.
- Implement time-locked effects **inside** the GSAP timeline only. All product captures/assets must be preloaded and fully decoded before `__GF3_PROMO__.ready=true`. Expose `seek` only in `?capture=1`; preserve interactive preview play/pause/scrub and `prefers-reduced-motion` handling outside export.
- At the final frame (`t=74.999…`), the GF3 CTA is visible; there must be no auto-rewind or black flash.

## 4. Mandatory actual MP4 production pipeline

### Step 6 — Audio generation and licensing safety

**Action: CREATE** `FrontEnd/scripts/promo/generate-audio.mjs`.

- Generate reproducible **original** ambient sound design locally in uncompressed WAV using Node built-ins: an extremely soft sustained musical bed with 2–3 harmonious simple intervals, plus subtle transient sounds synchronized to `6`, `12`, `23`, `33`, `44`, `55`, `63`, `69` seconds. No music download, samples, external network TTS, or alleged celebrity voice. Original WAV should be exactly `75s`, `48000Hz`, stereo, and avoid clipping.
- Keep audio restrained: transitions perceptible but not intrusive, film remains clear without sound. Add a 0.6s fade-in and ~1.5s fade-out. Do not generate loud harsh sine beeps; layer envelopes/low-pass soft edges and limiter. The existing quiet cinematic aesthetic takes priority over audible effects.
- Output to ignored `FrontEnd/artifacts/promo/audio-bed.wav`.
- Audio-generation errors must terminate export clearly rather than quietly yielding malformed streams. Validate duration and sample rate.

### Step 7 — Render every deterministic frame and encode MP4

**Action: CREATE or EXTEND.** `FrontEnd/scripts/promo/render-film.mjs`; reuse existing `FrontEnd/scripts/render-promo.mjs` where working.

**Command interface:** from `FrontEnd/`:

```bash
npm run promo:film
```

This command MUST, unattended after prerequisites are installed:

1. Resolve `ffmpeg` and `ffprobe` on `PATH`; if missing, show exact actionable Windows (`winget`) or Ubuntu (`apt`) installation command and fail clearly. Do not claim a video was rendered. Use already installed system FFmpeg; no unnecessary new npm video dependencies. Use a local Vite server if available; if not, **spawn Vite on a free fixed port internally** and wait for readiness, then stop only the spawned child when done. Do not kill unrelated processes on port 5173.
2. Start/attach Playwright Chromium and navigate to `promo.html?capture=1` on the resolved local server. Use a 1920×1080 viewport, DPR=1, `pl-PL`, stable film-stage CSS. Block unrelated external requests, wait for the film controller, `document.fonts.ready`, every image decode and scene ready signal. Do not stream unfinished images.
3. Validate `__GF3_PROMO__.duration===75`, `fps===30`, stage bounds exactly 1920×1080 and shot-manifest completeness **before encoding**.
4. For each integer frame `i=0..2249`, call `seek(i/30)` and allow deterministic layout/paint synchronization. Screenshot **only the 1920×1080 film stage**, not devtools, browser chrome, preview controls or page margins.
5. Stream frame PNG buffers immediately with backpressure to FFmpeg stdin; do not store all 2250 frames in RAM. Encode `-c:v libx264 -preset medium -crf 17 -pix_fmt yuv420p -r 30 -movflags +faststart` with exactly 2250 frames; merge the original 48 kHz WAV as AAC stereo (`-c:a aac -b:a 192k -ar 48000`); use `-t 75`/`-shortest` carefully so neither stream truncates the video below 75.0 seconds.
6. Write first to `FrontEnd/artifacts/promo/gf3-product-film-1080p.partial.mp4`, only atomically rename to `gf3-product-film-1080p.mp4` when FFmpeg exits successfully AND `verify-film` passes. Delete failed `.partial.mp4` only; never delete unrelated files.
7. Create the poster as the clean frame `t=71.5s` before the end CTA, or adjust poster time within `69–74s` to capture a completely legible hero. Create a contact sheet containing one thumbnail centered in each of the nine storyboard scenes. Save nine full-resolution `styleframes/` from compositor time points, as defined in §1.0.4. Write `motion-cue-sheet.json` and `creative-qa.md` alongside the video; all artifacts must correspond to the **final** timeline.
8. Print the **absolute final MP4 path**, output size, exact duration, resolution, codec, number of frames, and QA report location. Exit nonzero on failure.

**Implementation constraints:**
- `npm run promo:render` MUST remain an alias to the final 75-second movie pipeline; don't leave a separate silent 52-second renderer mislabeled as final.
- Frame count `75×30=2250`, frame timestamp `i/30`, expected display duration 75s (last actual frame timestamp 74.9667s). No hidden additional trailing blank or encoder-introduced 5s hold.
- Do not encode raw real-user browser captures with a screen-recorder unless provenance confirms synthetic data.
- A separate 4-second sample mode may be added as an **internal** performance check, but it is not the final deliverable and cannot count as task completion.
- If render speed is slow, keep fidelity and optimize source asset caching/effects. Do not silently lower resolution/fps or skip frames.
- If script previously uses arbitrary Vite server URL, preserve environment override `PROMO_BASE_URL` and do not break the previous preview workflow.

### Step 8 — Automated objective QA

**Action: CREATE** `FrontEnd/scripts/promo/verify-film.mjs` and `FrontEnd/e2e/promo-video.spec.ts`.

**Mandatory technical checks:**

- MP4 file exists and is substantial (at least 1 MiB; this threshold only detects empty/truncated files, not visual quality).
- Probe via `ffprobe` JSON: `codec_name=h264`, `width=1920`, `height=1080`, `pix_fmt=yuv420p`, nominal `r_frame_rate=30/1`, actual decoded/stream frame count **2250** when available, duration between `74.95` and `75.05` seconds; audio is AAC, stereo, 48000 Hz, duration approximately 75 s.
- Use FFmpeg `blackdetect` on a temporary diagnostics pass; fail if an unexpected black interval >0.20s occurs **after the intentionally dark problem scene**. Avoid treating brand-intentional dark/navy backgrounds as automatically black: inspect contrast and headlines.
- Extract representative scene stills at `3`, `9`, `17`, `28`, `38`, `49`, `59`, `66`, `72` seconds and boundary stills at `6`, `12`, `23`, `33`, `44`, `55`, `63`, `69`. Ensure visible nonempty composition, meaningful target real UI on the five feature segments and correct Polish captions without clipping.
- Check audio RMS/peak (must not be completely silent, peak must not clip at 0 dBFS); inspect several cue periods and confirm complete fade-out. Do not claim audio quality can be fully assessed by RMS alone.
- Generate contact sheet and `qa-report.json` with `status: PASS | FAIL | PROVISIONAL`, `duration`, `fps`, `width`, `height`, `videoCodec`, `audioCodec`, `frameCount`, `sourceProvenance`, `authenticScenes`, `missingAuthenticScenes`, `visualWarnings`, `renderCommand`, `outputPath`, `timestamp`.

**Vitest/Playwright tests:**

- Storyboard has nine nonoverlapping contiguous scenes covering exactly `0–75` in prescribed order.
- All five real product shot sources have a valid acquisition mode and a corresponding file or component; no empty manifest or fabricated fallback.
- Capture mode displays no preview controls and exports `window.__GF3_PROMO__` with exact expected contract.
- Determinism: screenshots at t=28 and t=49 are byte-identical after seek order `49→3→28→49→28` for repeated target times.
- All scene centers show legible title and expected feature shot, not a loading placeholder or login gate; final CTA is still visible at t=74.9.
- Browser does not attempt production domain requests, authenticated customer APIs or telemetry from the preview/capture page.
- Normal app Vite `index.html` remains usable and route handling is unchanged.

**Visual human review:** Inspect the nine styleframes and generated contact sheet and spot-watch actual MP4 at playback speed. Score all seven motion motifs from §1.0.1 and specific choreography from §1.0.3 in `creative-qa.md`; reject a film comprised of plain cuts, screenshot slide-zooms or marketing-reconstructed product screens, even when MP4 technical tests pass. All three user URLs are recorded in `PROMO.md`; the third reference may be unviewable and must be identified as such, never fabricated.

Inspect the generated contact sheet and spot-watch actual MP4 at playback speed. Identify blurry fonts, unreadable small schedules, awkward crops, repeated footage, fake interactions, too-rapid changes, clipped screen edges, sound harshness, or redundant logo moments. Fix the specific shot/cue and re-render the finished MP4 once. Do **not** endlessly redesign unrelated pages.

## 5. Error behavior and fallbacks

- **Missing preview/capture source:** re-use the working HTML and add only the absent controller/wiring. Do not rebuild all promo components.
- **Missing demo backend or auth state:** try the production-component pathway using typed synthetic props and real CSS. If still impossible, record a precise per-feature blocker in QA rather than passing off a fake preview as authentic.
- **Feature not actually implemented:** show an alternative verified customer-facing workflow from §1 (`more` priority) and adjust only its supporting caption, not the entire structure. Never simulate a nonexistent success.
- **Screenshots contain private data:** fail validation, delete/replace affected capture with synthetic demo material and re-export. Never put secret storage-state files in the media directory.
- **Encoder unavailable or crashes:** return nonzero, keep diagnostics, never announce completion or leave an empty final MP4. A `.partial` may be deleted after an error.
- **Seeking produces different pixels:** fix non-deterministic React state/CSS timers/network/images; do not hide the failure by tolerating mismatches.
- **Audio generator fails:** do not silently report a valid audio-supported export; fix source generator or flag explicit blocker and do not pass final acceptance.
- **No access to local changed promo code from the planning AI:** CODEX itself sees its local working tree; only do targeted inspection in §2.1, not a full audit or architectural restart.

## 6. Exact executable sequence for CODEX

Execute sequentially, reporting commands and results; **do not stop after build or after viewing promo.html**:

1. Preserve worktree; targeted inspect existing promo modules/renderer.
2. Implement five authentic-product shot sources + manifest; validate real UI capture or genuine reused components.
3. Replace promo-only faux feature previews in 75-second storyboard and implement all seven signature motion motifs using the real GF3 components/media, per detailed §1.0.1–§1.0.3 scene direction. Produce authentic nine-scene styleframes and a motion cue sheet.
4. Implement/repair deterministic capture seek and 1920×1080 stage readiness.
5. Implement 75s original audio-bed generator and renderer; add package.json command aliases.
6. Run targeted tests, then perform FULL 2250-frame encoding into actual H.264/AAC MP4.
7. Run technical verification, extract all thumbnails/styleframes, perform §1.0 creative-qa review (the product UI must move as the subject), fix major flaws and re-render.
8. Verify normal frontend builds and no app regressions; document exact video path and final test results.

Expected developer commands:

```bash
# From repository root
cd FrontEnd
npm install
npx playwright install chromium
# Ensure ffmpeg and ffprobe are installed and on PATH; see PROMO.md
npm run lint
npm run build
npm run test
npx playwright test e2e/promo-video.spec.ts --project=chromium
npm run promo:film
npm run promo:verify

# The actual deliverable must exist here:
# FrontEnd/artifacts/promo/gf3-product-film-1080p.mp4
```

For development on Ubuntu, only if FFmpeg is absent and package installation is permitted: `sudo apt-get update && sudo apt-get install -y ffmpeg`. For Windows, only if absent and permitted: `winget install --id Gyan.FFmpeg -e`, restart shell and verify `ffmpeg -version` + `ffprobe -version`. Never claim an installation succeeded without checking it. If permissions or system prerequisites block encoding, state **the exact blocker** and leave a valid, runnable command for the user; do not report the user goal as completed.

## 7. Acceptance criteria — all required for `PASS`

- [ ] Existing working `promo.html` is retained and upgraded; no wholesale rewrite or loss of previous work.
- [ ] Film story tells a coherent product problem → GF3 → authentic walkthrough → value → brand pitch.
- [ ] Manager scheduling, availability, employee schedule, swaps, and one additional verified workflow use real GF3 visual UI sources, not independently recreated marketing imitation cards.
- [ ] The actual screen interactions/states shown are plausible and supported by the app; no false AI or performance claims.
- [ ] Every authentic feature shot is traceable to real components/route and synthetic demo data.
- [ ] Clear Polish captions, consistent brand styling from current CSS, readable interface crops, smooth scene rhythm and no visual glitches.
- [ ] Motion-designed real GF3 interface: real card/grid assembly, actual interaction before/after, matched transitions, 2.5D camera layers, kinetic type, source-authentic focus extractions (requirements §1.0.1). **A basic screenshot recording, a zoom/pan slideshow or recreated marketing UI FAILS.**
- [ ] Exactly nine film-derived 1920×1080 styleframes, `motion-cue-sheet.json`, `creative-qa.md` exist and accurately reflect the final MP4.
- [ ] All three reference URLs preserved as creative inspiration; no copied copyrighted frames/audio, and any inaccessible reference acknowledged honestly.
- [ ] One 75s / 1920×1080 / 30fps H.264 `yuv420p` MP4 with AAC audio is physically present at fixed required output path and plays to the GF3 closing CTA.
- [ ] Poster PNG, nine-scene contact sheet and `qa-report.json` exist at fixed output paths.
- [ ] `ffprobe` and video frame/audio checks pass; `qa-report.json` says `PASS`, not `PROVISIONAL`.
- [ ] Targeted Vitest/Playwright and frontend build/lint pass or failures are accurately reported.
- [ ] No backend, auth, SQLite, business logic, app router or non-promo production UI behavior was modified.
- [ ] No real user/customer data or private auth state was included in exported/committed media.
- [ ] Final CODEX reply provides actual MP4 absolute path, file size, ffprobe summary, provenance count, QA report path and preview commands. **Do not say “done” solely because an HTML page renders.**

## 8. Codex final response contract

CODEX must finish with:

1. **Video:** absolute path to existing `gf3-product-film-1080p.mp4`, human-readable size, verified codec, dimensions, duration and fps.
2. **Actual GF3 UI used:** table of `manager`, `availability`, `schedule`, `swap`, `more`, with source code location or demo route, and acquisition mode; explicitly list any blockers or unverified visuals.
3. **Supporting deliverables:** exact poster, contact-sheet, nine styleframes, motion cue sheet, creative QA report and technical QA-report paths.
4. **Verification:** lint, build, unit/E2E, frame+audio probe results; mark failures and environmental blockers truthfully.
5. **Worktree:** concise list of files changed/created; no unrelated changes.

**STOP CONDITION:** only stop with `PASS` after the MP4 and all acceptance artifacts are actually created and verified. If a required external prerequisite is unavailable, stop with `BLOCKED`, explicit reason and precise reproducible commands; never misrepresent an unrendered or non-authentic promo as completed video.

---

**CODEX MODEL EXECUTION GUIDANCE (not part of architecture):** This is **High implementation complexity**, primarily authentic multi-role UI acquisition, cinema-grade GSAP timeline integration, and deterministic video encoding/QA. Prefer a **Strong CODEX model with Medium reasoning** (use High if debugging real UI fixture/auth dependencies becomes nontrivial). Use an exact currently available model name only after checking CODEX's model selector. Avoid highest reasoning by default: core architecture, narrative, file ownership and quality gates are already decided in this plan.
