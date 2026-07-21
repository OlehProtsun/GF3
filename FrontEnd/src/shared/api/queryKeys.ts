export const queryKeys = {
  home: {
    dashboard: () => ["home", "dashboard"] as const,
  },
  employeeUiState: {
    current: () => ["employeeUiState", "current"] as const,
  },
  employeeProfile: {
    me: () => ["employeeProfile", "me"] as const,
  },
  managerProfile: {
    me: () => ["managerProfile", "me"] as const,
    list: () => ["managerProfile", "list"] as const,
  },
  managerNotepad: {
    current: () => ["managerNotepad", "current"] as const,
  },
  systemNews: {
    all: ["systemNews"] as const,
    visible: () => ["systemNews", "visible"] as const,
    admin: () => ["systemNews", "admin"] as const,
  },
  employeeAvailability: {
    all: ["employeeAvailability"] as const,
    list: () => ["employeeAvailability", "list"] as const,
    byId: (id: number) => ["employeeAvailability", "byId", id] as const,
  },
  employeeSchedules: {
    all: ["employeeSchedules"] as const,
    list: () => ["employeeSchedules", "list"] as const,
  },
  shiftSwaps: {
    all: ["shiftSwaps"] as const,
    employee: () => ["shiftSwaps", "employee"] as const,
    employees: () => ["shiftSwaps", "employees"] as const,
    graphLog: (containerId: number, graphId: number) => ["shiftSwaps", "graphLog", containerId, graphId] as const,
    container: (containerId: number) => ["shiftSwaps", "container", containerId] as const,
  },
  workflowLogs: {
    all: ["workflowLogs"] as const,
    list: () => ["workflowLogs", "list"] as const,
    settings: () => ["workflowLogs", "settings"] as const,
  },
  communications: {
    all: ["communications"] as const,
    managerList: () => ["communications", "managerList"] as const,
    employeePendingAll: ["communications", "employeePending"] as const,
    employeePending: (employeeId: number) => ["communications", "employeePending", employeeId] as const,
  },
  employees: {
    all: ["employees"] as const,
    list: () => ["employees", "list"] as const,
    byId: (id: number) => ["employees", "byId", id] as const,
  },
  shops: {
    all: ["shops"] as const,
    list: () => ["shops", "list"] as const,
    byId: (id: number) => ["shops", "byId", id] as const,
  },
  health: {
    status: () => ["health", "status"] as const,
  },
  adminDb: {
    metadata: () => ["adminDb", "metadata"] as const,
    hash: () => ["adminDb", "hash"] as const,
  },
  containers: {
    all: ["containers"] as const,
    list: () => ["containers", "list"] as const,
    byId: (id: number) => ["containers", "byId", id] as const,
    graphs: (containerId: number) => ["containers", containerId, "graphs"] as const,
    schedulePresets: (containerId: number) => ["containers", containerId, "schedulePresets"] as const,
    graphById: (containerId: number, graphId: number) => ["containers", containerId, "graphs", graphId] as const,
    graphSlots: (containerId: number, graphId: number) => ["containers", containerId, "graphs", graphId, "slots"] as const,
    graphSlotsBatches: (containerId: number) => ["containers", containerId, "graphs", "slotsBatch"] as const,
    graphSlotsBatch: (containerId: number, graphIds: readonly number[]) =>
      ["containers", containerId, "graphs", "slotsBatch", ...graphIds] as const,
    graphEmployees: (containerId: number, graphId: number) => ["containers", containerId, "graphs", graphId, "employees"] as const,
    graphCellStyles: (containerId: number, graphId: number) => ["containers", containerId, "graphs", graphId, "cellStyles"] as const,
    graphRecordsPrefix: (containerId: number) => ["containers", containerId, "graphRecords"] as const,
    graphRecords: (containerId: number, graphIdsKey: string) => ["containers", containerId, "graphRecords", graphIdsKey] as const,
  },
  availabilityBinds: {
    all: ["availabilityBinds"] as const,
    list: () => ["availabilityBinds", "list"] as const,
    active: () => ["availabilityBinds", "active"] as const,
    byId: (id: number) => ["availabilityBinds", "byId", id] as const,
  },
  availabilityGroups: {
    all: ["availabilityGroups"] as const,
    list: () => ["availabilityGroups", "list"] as const,
    byId: (id: number) => ["availabilityGroups", "byId", id] as const,
    items: (id: number) => ["availabilityGroups", "items", id] as const,
    members: (id: number) => ["availabilityGroups", id, "members"] as const,
    slots: (id: number) => ["availabilityGroups", id, "slots"] as const,
    transferSources: (id: number, memberId: number) => ["availabilityGroups", id, "members", memberId, "transferSources"] as const,
    transferPreview: (employeeIdsKey: string, year: number, month: number, targetGroupId: number | null) =>
      ["availabilityGroups", "transferPreview", employeeIdsKey, year, month, targetGroupId ?? "new"] as const,
    transferHints: (id: number) => ["availabilityGroups", id, "transferHints"] as const,
  },
};
