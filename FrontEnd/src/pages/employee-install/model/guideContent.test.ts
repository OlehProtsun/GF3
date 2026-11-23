import { expect, test } from "vitest";
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
