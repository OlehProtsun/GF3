import type { GraphSaveComparisonInput, PersistedGraphState } from "./graphSaveIntegrity";

export const GRAPH_RECOVERY_VERSION = 1;
const recoveryLifetimeMs = 24 * 60 * 60 * 1000;
const keyPrefix = "gf3:graph-drafts:";

export type RecoveryDraft = {
  version: typeof GRAPH_RECOVERY_VERSION;
  graphId: number;
  draft: unknown;
  persistedBaseline: PersistedGraphState;
  comparisonBaseline: GraphSaveComparisonInput;
  editedAt: number;
  blockedBy?: "workspace-write" | "version-commit";
  needsStyleVersion?: true;
  manualPublicationAttempted?: true;
};

const isId = (id: unknown): id is number => typeof id === "number" && Number.isSafeInteger(id) && id > 0;
const isRecord = (value: unknown): value is Record<string, unknown> =>
  !!value && typeof value === "object" && !Array.isArray(value);
const managerPrefix = (managerId: number) => `${keyPrefix}${managerId}:`;
const key = (managerId: number, containerId: number) => `${managerPrefix(managerId)}${containerId}`;

function validStyles(styles: unknown) {
  return Array.isArray(styles) && styles.every(style => isRecord(style) &&
    Number.isSafeInteger(style.id) && Number.isSafeInteger(style.scheduleId) &&
    Number.isSafeInteger(style.employeeId) && isId(style.dayOfMonth) &&
    (style.backgroundColorArgb == null || Number.isSafeInteger(style.backgroundColorArgb)) &&
    (style.textColorArgb == null || Number.isSafeInteger(style.textColorArgb)));
}

function validBaseline(value: unknown, graphId: number, containerId: number) {
  if (!isRecord(value) || !isRecord(value.graph)) return false;
  const graph = value.graph;
  return graph.id === graphId && graph.containerId === containerId &&
    ["name", "shift1Time", "shift2Time"].every(field => typeof graph[field] === "string") &&
    ["shopId", "year", "month", "peoplePerShift", "maxHoursPerEmpMonth", "maxConsecutiveDays",
      "maxConsecutiveFull", "maxFullPerMonth"].every(field => Number.isFinite(graph[field])) &&
    (graph.note == null || typeof graph.note === "string") &&
    (graph.publicationStatus === "private" || graph.publicationStatus === "public") &&
    Array.isArray(value.employees) && value.employees.every(row => isRecord(row) &&
      isId(row.employeeId) && isId(row.id) && Number.isSafeInteger(row.displayOrder)) &&
    Array.isArray(value.slots) && value.slots.every(slot => isRecord(slot) &&
      isId(slot.dayOfMonth) && isId(slot.slotNo) && typeof slot.fromTime === "string" && typeof slot.toTime === "string" &&
      (typeof slot.status === "number" || typeof slot.status === "string")) &&
    validStyles(value.styles);
}

function validDraft(entry: unknown, containerId: number): entry is RecoveryDraft {
  if (!isRecord(entry) || entry.version !== GRAPH_RECOVERY_VERSION || !isId(entry.graphId) ||
    typeof entry.editedAt !== "number" || !Number.isFinite(entry.editedAt) || entry.editedAt <= 0 ||
    entry.editedAt > Date.now() || Date.now() - entry.editedAt >= recoveryLifetimeMs ||
    !isRecord(entry.draft) || entry.draft.graphId !== entry.graphId ||
    (entry.blockedBy !== undefined && entry.blockedBy !== "workspace-write" && entry.blockedBy !== "version-commit") ||
    (entry.needsStyleVersion !== undefined && entry.needsStyleVersion !== true) ||
    (entry.manualPublicationAttempted !== undefined && entry.manualPublicationAttempted !== true)) {
    return false;
  }
  const comparison = entry.comparisonBaseline;
  return validBaseline(entry.persistedBaseline, entry.graphId, containerId) &&
    isRecord(comparison) && typeof comparison.nonStyleFingerprint === "string" && validStyles(comparison.styles);
}

function parseDrafts(raw: string | null, containerId: number, onInvalid?: () => void) {
  if (raw === null) return [];
  try {
    const parsed: unknown = JSON.parse(raw);
    if (Array.isArray(parsed)) {
      const drafts = parsed.filter(entry => validDraft(entry, containerId));
      if (drafts.length !== parsed.length) onInvalid?.();
      return drafts;
    }
  } catch {
    // Corrupt data should not prevent opening the editor.
  }
  onInvalid?.();
  return [];
}

export function writeGraphRecovery(managerId: number, containerId: number, drafts: RecoveryDraft[]): { ok: boolean; reason?: string } {
  if (!isId(managerId) || !isId(containerId) || !drafts.every(draft => validDraft(draft, containerId))) {
    return { ok: false, reason: "invalid-draft" };
  }
  try {
    const storage = window.sessionStorage;
    const storageKey = key(managerId, containerId);
    const previous = parseDrafts(storage.getItem(storageKey), containerId);
    const merged = new Map([...previous, ...drafts].map(draft => [draft.graphId, draft]));
    if (merged.size) {
      storage.setItem(storageKey, JSON.stringify([...merged.values()], (field, value) =>
        /^(password|token|accessToken|refreshToken|authorization|cookie)$/i.test(field) ? undefined : value));
    }
    return { ok: true };
  } catch {
    return { ok: false, reason: "storage-unavailable" };
  }
}

export function readGraphRecovery(managerId: number, containerId: number, onInvalid?: () => void): RecoveryDraft[] | null {
  if (!isId(managerId) || !isId(containerId)) return null;
  try {
    const drafts = parseDrafts(window.sessionStorage.getItem(key(managerId, containerId)), containerId, onInvalid);
    return drafts.length ? drafts : null;
  } catch {
    return null;
  }
}

export function clearGraphRecovery(managerId: number, containerId: number, graphIds?: number[]): void {
  if (!isId(managerId) || !isId(containerId)) return;
  try {
    const storage = window.sessionStorage;
    const storageKey = key(managerId, containerId);
    const remaining = graphIds
      ? parseDrafts(storage.getItem(storageKey), containerId).filter(draft => !graphIds.includes(draft.graphId))
      : [];
    if (remaining.length) storage.setItem(storageKey, JSON.stringify(remaining));
    else storage.removeItem(storageKey);
  } catch {
    // Recovery remains best effort when browser storage is unavailable.
  }
}

export function clearAllGraphRecoveryForManager(managerId: number): void {
  if (!isId(managerId)) return;
  try {
    const storage = window.sessionStorage;
    for (let index = storage.length - 1; index >= 0; index--) {
      const storageKey = storage.key(index);
      if (storageKey?.startsWith(managerPrefix(managerId))) storage.removeItem(storageKey);
    }
  } catch {
    // Local logout must still complete.
  }
}
