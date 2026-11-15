import { describe, expect, it } from "vitest";
import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import { SHOTS, SHOT_DETAILS } from "./shot-manifest";

describe("authentic product provenance", () => {
  it("contains all five real source families with sanitized actual captures", () => {
    expect(SHOTS.map(shot => shot.id)).toEqual(["manager", "availability", "schedule", "swap", "more"]);
    const provenance = JSON.parse(readFileSync(path.resolve("src/promo/real-ui/captures/capture-provenance.json"), "utf8"));
    expect(provenance.blocked).toEqual([]);
    for (const shot of SHOTS) {
      expect(["production-component", "actual-app-capture"]).toContain(shot.acquisition);
      expect(shot.dataSet).toBe("gf3-film-october-2026");
      expect(shot.assets).toHaveLength(3);
      expect(shot.sourcePaths.length).toBeGreaterThan(0);
      for (const source of shot.sourcePaths) expect(existsSync(path.resolve(source))).toBe(true);
      for (const asset of shot.assets) expect(existsSync(path.resolve("src/promo/real-ui/captures", asset))).toBe(true);
      expect(provenance.shots.find((item: { id: string }) => item.id === shot.id)?.synthetic).toBe(true);
      expect(JSON.stringify(shot)).not.toMatch(/Bearer |access-token|password|https:\/\//i);
      expect(Object.isFrozen(shot)).toBe(true);
    }
  });
  it("extracts only valid rectangles from genuine native product captures", () => {
    for (const details of Object.values(SHOT_DETAILS)) {
      const png = readFileSync(path.resolve("src/promo/real-ui/captures", details.asset));
      expect([png.readUInt32BE(16), png.readUInt32BE(20)]).toEqual(details.size);
      for (const [x, y, width, height] of details.rects) {
        expect(x).toBeGreaterThanOrEqual(0); expect(y).toBeGreaterThanOrEqual(0);
        expect(width).toBeGreaterThan(0); expect(height).toBeGreaterThan(0);
        expect(x + width).toBeLessThanOrEqual(details.size[0]);
        expect(y + height).toBeLessThanOrEqual(details.size[1]);
      }
    }
  });
});
