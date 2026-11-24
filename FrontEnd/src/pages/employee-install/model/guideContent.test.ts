import { expect, test } from "vitest";
import { readFileSync } from "node:fs";
import path from "node:path";
import { translate } from "@shared/i18n";
import polish from "@shared/i18n/pl.json";
import ukrainian from "@shared/i18n/uk.json";
import { guideTopics, platformSteps } from "./guideContent";

test("exposes eight ordered guide topics with PL, UK and EN menu copy", () => {
  expect(guideTopics.map(topic => topic.id)).toEqual([
    "getting-started", "notifications", "schedules", "work-hours", "shift-corrections", "shift-swaps", "availability-urlop", "profile",
  ]);
  for (const topic of guideTopics) {
    expect(topic.groups.length).toBeGreaterThan(0);
    for (const label of [topic.title, topic.subtitle]) {
      expect(translate("en", label)).toBe(label);
      expect(polish).toHaveProperty([label]);
      expect(ukrainian).toHaveProperty([label]);
      expect(translate("pl", label).trim()).not.toBe("");
      expect(translate("uk", label).trim()).not.toBe("");
    }
  }
  expect(platformSteps.ios.length).toBeGreaterThan(0);
  expect(platformSteps.android.length).toBeGreaterThan(0);
});

test("uses original vector illustrations for the ten browser installation steps", () => {
  const captures = JSON.parse(readFileSync(path.resolve("src/pages/employee-install/model/guideCaptures.json"), "utf8")) as Record<string, { src: string; width: number; height: number }>;
  const ids = ["ios-open", "ios-share", "ios-share-sheet", "ios-add", "ios-confirm", "android-open", "android-menu", "android-add", "android-install", "android-confirm"];
  expect(ids).toHaveLength(10);
  for (const id of ids) {
    const capture = captures[id];
    expect(capture.src).toMatch(/\.svg$/);
    const svg = readFileSync(path.resolve("public/guide", capture.src), "utf8");
    expect(svg).toContain(`<svg xmlns="http://www.w3.org/2000/svg" width="${capture.width}" height="${capture.height}"`);
    expect(svg).toContain("<title");
    expect(svg).toContain("<desc");
  }
});
