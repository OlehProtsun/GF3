export type FeatureId = "manager" | "availability" | "schedule" | "swap" | "more";
export type FeatureShot = Readonly<{
  id: FeatureId; feature: string; acquisition: "production-component" | "actual-app-capture";
  sourcePaths: readonly string[]; sourceRoute?: string; dataSet: string;
  assets: readonly string[]; notes: string;
}>;
export const SHOTS: readonly FeatureShot[] = Object.freeze([
  { id: "manager", feature: "Planowanie grafiku", acquisition: "production-component", sourcePaths: ["src/entities/containers/ui/ContainerGraphMatrix.tsx", "src/entities/containers/ui/ContainerGraphMatrix.module.css"], sourceRoute: "/container", dataSet: "gf3-film-october-2026", assets: ["manager/beginning.png", "manager/interaction.png", "manager/outcome.png"], notes: "Actual exported matrix; edit and commit a synthetic shift through its own editor." },
  { id: "availability", feature: "Dostępność", acquisition: "actual-app-capture", sourcePaths: ["src/pages/employee-availability/ui/EmployeeAvailabilityPage.tsx"], sourceRoute: "/availability", dataSet: "gf3-film-october-2026", assets: ["availability/beginning.png", "availability/interaction.png", "availability/outcome.png"], notes: "Existing E2E fixture interception, actual day/preset dialog and local draft. No backend mutation claim." },
  { id: "schedule", feature: "Opublikowany grafik", acquisition: "actual-app-capture", sourcePaths: ["src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx"], sourceRoute: "/schedule", dataSet: "gf3-film-october-2026", assets: ["schedule/beginning.png", "schedule/interaction.png", "schedule/outcome.png"], notes: "Published synthetic October schedule; actual day and view controls." },
  { id: "swap", feature: "Zamiany zmian", acquisition: "actual-app-capture", sourcePaths: ["src/pages/employee-swap/ui/EmployeeSwapPage.tsx", "src/entities/shift-swaps/api/shiftSwapsApi.ts"], sourceRoute: "/swap", dataSet: "gf3-film-october-2026", assets: ["swap/beginning.png", "swap/interaction.png", "swap/outcome.png"], notes: "Actual offer expansion and acceptance confirmation. Final image shows confirmation, not fabricated server approval." },
  { id: "more", feature: "Podsumowanie godzin", acquisition: "actual-app-capture", sourcePaths: ["src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx"], sourceRoute: "/schedule", dataSet: "gf3-film-october-2026", assets: ["more/beginning.png", "more/interaction.png", "more/outcome.png"], notes: "Actual hours summary derived by production code from the same synthetic published slots." },
].map(shot => Object.freeze({ ...shot, sourcePaths: Object.freeze(shot.sourcePaths), assets: Object.freeze(shot.assets) })) as FeatureShot[]);

const captured = import.meta.glob<string>("./captures/**/*.png", { query: "?url", import: "default", eager: true });

// Native PNG source rectangles; these layers retain the captured product pixels.
export const SHOT_DETAILS = {
  manager: { asset: "manager/beginning.png", size: [1240, 360], rects: [[106, 171, 366, 31], [106, 203, 1103, 30], [106, 234, 1103, 30]], cursor: [289, 186], actionAsset: "manager/interaction.png", actionSize: [1240, 360], actionCursor: [289, 186] },
  availability: { asset: "availability/beginning.png", size: [740, 660], rects: [[420, 237, 90, 78], [30, 237, 90, 78], [128, 237, 90, 78]], cursor: [465, 275], actionAsset: "availability/interaction.png", actionSize: [500, 510], actionCursor: [392, 435] },
  schedule: { asset: "schedule/beginning.png", size: [1032, 753], rects: [[88, 491, 146, 28], [11, 491, 75, 28], [11, 519, 513, 28]], cursor: [515, 196], actionAsset: "schedule/interaction.png", actionSize: [1032, 528], actionCursor: [515, 196] },
  swap: { asset: "swap/interaction.png", size: [1400, 256], rects: [[0, 0, 119, 230], [11, 191, 91, 36]], cursor: [56, 209], actionAsset: "swap/interaction.png", actionSize: [1400, 256], actionCursor: [56, 209] },
  more: { asset: "more/outcome.png", size: [1032, 497], rects: [[15, 443, 1002, 38], [968, 19, 49, 32]], cursor: [330, 462], actionAsset: "more/outcome.png", actionSize: [1032, 497], actionCursor: [330, 462] },
} as const;

export function fittedPoint(size: readonly [number, number], point: readonly [number, number]) {
  const scale = Math.min(1, 1520 / size[0], 620 / size[1]);
  return [(1520 - size[0] * scale) / 2 + point[0] * scale, (620 - size[1] * scale) / 2 + point[1] * scale] as const;
}
export function shotAsset(path: string): string {
  return captured[`./captures/${path}`] ?? "";
}
