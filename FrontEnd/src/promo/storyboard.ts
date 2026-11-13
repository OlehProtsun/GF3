export type SceneId = "pain" | "reveal" | "manager" | "availability" | "schedule" | "swap" | "more" | "connected" | "outro";
export type SceneSpec = Readonly<{ id: SceneId; start: number; end: number; headline: string; supporting?: string }>;

export const PROMO_DURATION_SECONDS = 75;
export const PROMO_FPS = 30;
export const PROMO_FRAME_COUNT = PROMO_DURATION_SECONDS * PROMO_FPS;
export const PROMO_WIDTH = 1920;
export const PROMO_HEIGHT = 1080;

export const SCENES: readonly SceneSpec[] = Object.freeze([
  Object.freeze({ id: "pain", start: 0, end: 6, headline: "Grafiki. Wiadomości. Zmiany.", supporting: "Chaos, który zabiera czas." }),
  Object.freeze({ id: "reveal", start: 6, end: 12, headline: "A gdyby wszystko było w jednym miejscu?", supporting: "Poznaj GF3." }),
  Object.freeze({ id: "manager", start: 12, end: 23, headline: "Planowanie zmian. Pod kontrolą.", supporting: "Przejrzysty grafik w jednym miejscu." }),
  Object.freeze({ id: "availability", start: 23, end: 33, headline: "Dostępność bez zgadywania.", supporting: "Wiesz, kto i kiedy może pracować." }),
  Object.freeze({ id: "schedule", start: 33, end: 44, headline: "Każdy widzi swój grafik.", supporting: "Jasno. Zawsze pod ręką." }),
  Object.freeze({ id: "swap", start: 44, end: 55, headline: "Plany się zmieniają?", supporting: "Zamiany zmian w jednym miejscu." }),
  Object.freeze({ id: "more", start: 55, end: 63, headline: "Wszystko, co ważne. Czytelnie." }),
  Object.freeze({ id: "connected", start: 63, end: 69, headline: "Jeden system. Jeden rytm pracy." }),
  Object.freeze({ id: "outro", start: 69, end: 75, headline: "GF3", supporting: "Grafiki bez niepotrzebnego chaosu." }),
]);

export const FEATURE_CUES = Object.freeze(SCENES.slice(2, 7).map(scene => Object.freeze({
  id: scene.id,
  start: scene.start, end: scene.end,
  assemble: scene.start + 2,
  interaction: scene.start + (scene.id === "manager" ? 6 : 3),
  outcome: scene.start + (scene.id === "manager" ? 8 : scene.id === "more" ? 5 : 6),
})));

export const MOTION_CUE_SHEET = Object.freeze(SCENES.map(scene => {
  const feature = FEATURE_CUES.find(cue => cue.id === scene.id);
  const realUiShotIds = scene.id === "pain" || scene.id === "outro" ? [] : scene.id === "reveal" ? ["manager"] : scene.id === "connected" ? ["manager", "availability", "schedule", "swap"] : [scene.id];
  return Object.freeze({ sceneId: scene.id, startSec: scene.start, endSec: scene.end, headline: scene.headline, realUiShotIds,
    cameraMoves: feature ? [
      { at: scene.start + .5, duration: 1.5, kind: "source-crop-macro" },
      { at: feature.assemble, duration: 1.4, kind: "grid-assembly-match-cut" },
      { at: feature.interaction, duration: .45, kind: "actual-interaction-state" },
      { at: feature.outcome, duration: .45, kind: "actual-outcome-state" },
      { at: feature.end - .7, duration: .7, kind: "source-card-continuity" },
    ] : [{ at: scene.start, duration: scene.id === "outro" ? 3 : 2, kind: scene.id === "pain" ? "kinetic-type-chaos" : scene.id === "reveal" ? "tile-to-screen" : scene.id === "connected" ? "four-plane-grid-assembly" : "quiet-brand-hold" }],
    transitionAnchor: scene.id === "outro" ? "quiet CTA" : "authentic calendar/card rectangle and GF3 blue edge",
    soundCueSec: scene.start === 0 ? [] : [scene.start],
  });
}));

if (SCENES[0].start !== 0 || SCENES.at(-1)?.end !== PROMO_DURATION_SECONDS ||
  SCENES.some((scene, index) => scene.end <= scene.start || (index > 0 && scene.start !== SCENES[index - 1].end))) {
  throw new Error("Promo storyboard must contain contiguous scene windows from 0 to 75 seconds.");
}

export type PromoCapture = Readonly<{
  duration: number; width: number; height: number; fps: number; ready: true;
  seek: (seconds: number) => void; getTime: () => number;
}>;

declare global {
  interface Window { __GF3_PROMO__?: PromoCapture }
}
