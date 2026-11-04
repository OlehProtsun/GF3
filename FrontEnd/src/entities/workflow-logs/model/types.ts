export type WorkflowLog = {
  id: number;
  occurredAtUtc: string;
  actorRole: "manager" | "employee" | string;
  actorEmployeeId?: number | null;
  actorName: string;
  action: string;
};

export type WorkflowLogAudience = "all" | "managers" | "employees";

export type WorkflowLogSettings = {
  isEnabled: boolean;
  audience: WorkflowLogAudience;
};

export type WorkflowLogBulkDeleteRequest = {
  deleteAll: boolean;
  fromUtc?: string;
  toUtc?: string;
};

export type WorkflowLogDeleteResult = {
  deletedCount: number;
};
