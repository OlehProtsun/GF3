export const queryKeys = {
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
  containers: {
    all: ["containers"] as const,
    list: () => ["containers", "list"] as const,
    byId: (id: number) => ["containers", "byId", id] as const,
    graphs: (containerId: number) => ["containers", containerId, "graphs"] as const,
    graphById: (containerId: number, graphId: number) => ["containers", containerId, "graphs", graphId] as const,
    graphSlots: (containerId: number, graphId: number) => ["containers", containerId, "graphs", graphId, "slots"] as const,
    graphEmployees: (containerId: number, graphId: number) => ["containers", containerId, "graphs", graphId, "employees"] as const,
    graphCellStyles: (containerId: number, graphId: number) => ["containers", containerId, "graphs", graphId, "cellStyles"] as const,
  },
  availabilityGroups: {
    all: ["availabilityGroups"] as const,
    list: () => ["availabilityGroups", "list"] as const,
    byId: (id: number) => ["availabilityGroups", "byId", id] as const,
    items: (id: number) => ["availabilityGroups", "items", id] as const,
    members: (groupId: number) => ["availabilityGroups", groupId, "members"] as const,
    slots: (groupId: number) => ["availabilityGroups", groupId, "slots"] as const,
  },
  adminDb: {
    metadata: () => ["adminDb", "metadata"] as const,
    hash: () => ["adminDb", "hash"] as const,
  },
  health: {
    status: () => ["health", "status"] as const,
  },
};
