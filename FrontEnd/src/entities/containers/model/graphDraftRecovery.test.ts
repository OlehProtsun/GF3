import { afterEach, beforeEach, expect, test, vi } from "vitest";
import {
  clearAllGraphRecoveryForManager, clearGraphRecovery, GRAPH_RECOVERY_VERSION, readGraphRecovery,
  writeGraphRecovery, type RecoveryDraft,
} from "./graphDraftRecovery";

const entry = (graphId: number, containerId = 7): RecoveryDraft => ({
  version: GRAPH_RECOVERY_VERSION,
  graphId,
  draft: { graphId, form: { name: "F35" } },
  persistedBaseline: {
    graph: {
      id: graphId, containerId, shopId: 1, name: "F35", year: 2026, month: 11, publicationStatus: "private",
      peoplePerShift: 1, shift1Time: "", shift2Time: "", maxHoursPerEmpMonth: 1,
      maxConsecutiveDays: 1, maxConsecutiveFull: 1, maxFullPerMonth: 1,
    },
    employees: [], slots: [], styles: [],
  },
  comparisonBaseline: { nonStyleFingerprint: "original", styles: [] },
  editedAt: Date.now(),
});

beforeEach(() => window.sessionStorage.clear());
afterEach(() => vi.restoreAllMocks());

test("roundtrips multiple graphs and clears only the requested graphs", () => {
  const first = entry(34);
  const second = { ...entry(36), blockedBy: "version-commit" as const };
  expect(writeGraphRecovery(1, 7, [first])).toEqual({ ok: true });
  expect(writeGraphRecovery(1, 7, [second])).toEqual({ ok: true });
  expect(readGraphRecovery(1, 7)).toEqual([first, second]);
  clearGraphRecovery(1, 7, [34]);
  expect(readGraphRecovery(1, 7)).toEqual([second]);
});

test("isolates manager and container, including prefix overlap on manager cleanup", () => {
  expect(writeGraphRecovery(1, 7, [entry(34)]).ok).toBe(true);
  expect(readGraphRecovery(11, 7)).toBeNull();
  expect(readGraphRecovery(1, 8)).toBeNull();
  expect(writeGraphRecovery(11, 7, [entry(34)]).ok).toBe(true);
  expect(writeGraphRecovery(1, 8, [entry(34, 8)]).ok).toBe(true);
  expect(writeGraphRecovery(0, 7, [entry(34)]).ok).toBe(false);
  clearAllGraphRecoveryForManager(1);
  expect(readGraphRecovery(1, 7)).toBeNull();
  expect(readGraphRecovery(1, 8)).toBeNull();
  expect(readGraphRecovery(11, 7)).not.toBeNull();
});

test("ignores expired, corrupt and incompatible entries while keeping valid graphs", () => {
  const good = entry(34);
  writeGraphRecovery(1, 7, [good]);
  const key = window.sessionStorage.key(0)!;
  window.sessionStorage.setItem(key, JSON.stringify([good, {
    ...entry(36), editedAt: Date.now() - 24 * 60 * 60 * 1000,
  }, { ...entry(37), version: 0 }, { ...entry(38), draft: null },
  { ...entry(42), persistedBaseline: { ...entry(42).persistedBaseline, slots: [null] } }]));
  const onInvalid = vi.fn();
  expect(readGraphRecovery(1, 7, onInvalid)).toEqual([good]);
  expect(onInvalid).toHaveBeenCalledOnce();
  window.sessionStorage.setItem(key, "{bad json");
  expect(readGraphRecovery(1, 7)).toBeNull();
});

test("handles storage quota and security failures and omits credential fields", () => {
  const draft = { ...entry(34), draft: { graphId: 34, password: "secret-password", form: { accessToken: "secret-token" } } };
  expect(writeGraphRecovery(1, 7, [draft]).ok).toBe(true);
  expect(window.sessionStorage.getItem(window.sessionStorage.key(0)!)).not.toMatch(/secret-password|secret-token|accessToken|password/);
  vi.spyOn(Storage.prototype, "setItem").mockImplementationOnce(() => { throw new DOMException("Quota", "QuotaExceededError"); });
  expect(writeGraphRecovery(1, 7, [entry(36)]).ok).toBe(false);
  vi.spyOn(window, "sessionStorage", "get").mockImplementation(() => { throw new DOMException("Denied", "SecurityError"); });
  expect(readGraphRecovery(1, 7)).toBeNull();
  expect(writeGraphRecovery(1, 7, [entry(36)]).ok).toBe(false);
  expect(() => clearAllGraphRecoveryForManager(1)).not.toThrow();
});
