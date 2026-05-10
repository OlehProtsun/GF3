export const queryKeys = {
  home: {
    dashboard: () => ["home", "dashboard"] as const,
  },
  employeeProfile: {
    me: () => ["employeeProfile", "me"] as const,
  },
  managerProfile: {
    me: () => ["managerProfile", "me"] as const,
    list: () => ["managerProfile", "list"] as const,
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
  },
  workflowLogs: {
    all: ["workflowLogs"] as const,
    list: () => ["workflowLogs", "list"] as const,
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
  },
};
