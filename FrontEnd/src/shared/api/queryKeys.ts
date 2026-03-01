export const queryKeys = {
  employees: {
    all: ["employees"] as const,
    list: (search = "") => ["employees", "list", search] as const,
    byId: (id: number) => ["employees", "byId", id] as const,
  },
  shops: {
    all: ["shops"] as const,
    list: () => ["shops", "list"] as const,
    byId: (id: number) => ["shops", "byId", id] as const,
  },
};
