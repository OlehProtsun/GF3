import { describe, expect, test } from "vitest";
import {
  buildGraphSessionSearch,
  getGraphSessionIds,
  parseGraphSessionIds,
  resolveGraphSession,
} from "./graphSession";
import type { Graph } from "./types";

const createGraph = (id: number, name = `Graph ${id}`): Graph => ({
  id,
  containerId: 1,
  shopId: 2,
  name,
  year: 2026,
  month: 5,
  publicationStatus: "private",
  peoplePerShift: 1,
  shift1Time: "08:00 - 16:00",
  shift2Time: "16:00 - 20:00",
  maxHoursPerEmpMonth: 160,
  maxConsecutiveDays: 5,
  maxConsecutiveFull: 3,
  maxFullPerMonth: 10,
  note: null,
  availabilityGroupId: null,
});

describe("container graph session model", () => {
  test("parses session ids while dropping duplicates and invalid values", () => {
    expect(parseGraphSessionIds(" 3, 2, 3, 0, -1, abc, 4.5, 7 ")).toEqual([3, 2, 7]);
    expect(parseGraphSessionIds(null)).toEqual([]);
  });

  test("keeps current graph in the session when the URL does not already contain it", () => {
    expect(getGraphSessionIds("?openGraphIds=2%2C4", 5)).toEqual([2, 4, 5]);
    expect(getGraphSessionIds("?openGraphIds=2%2C4", 4)).toEqual([2, 4]);
    expect(getGraphSessionIds("", 6)).toEqual([6]);
    expect(getGraphSessionIds("?openGraphIds=2%2C4", -1)).toEqual([2, 4]);
  });

  test("builds sanitized search strings for open graph ids", () => {
    expect(buildGraphSessionSearch([3, 3, 0, 8, -1])).toBe("?openGraphIds=3%2C8");
    expect(buildGraphSessionSearch([0, Number.NaN, -2])).toBe("");
  });

  test("resolves open graph ids in session order with a current graph fallback", () => {
    const firstGraph = createGraph(1, "First");
    const secondGraph = createGraph(2, "Second");
    const currentGraph = createGraph(3, "Current");

    expect(resolveGraphSession([2, 3, 99, 1], [firstGraph, secondGraph], currentGraph).map(graph => graph.name))
      .toEqual(["Second", "Current", "First"]);
    expect(resolveGraphSession([99], [firstGraph], currentGraph)).toEqual([currentGraph]);
    expect(resolveGraphSession([99], [firstGraph], null)).toEqual([]);
  });
});
