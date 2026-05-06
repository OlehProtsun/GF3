export type WorkflowLog = {
  id: number;
  occurredAtUtc: string;
  actorRole: "manager" | "employee" | string;
  actorEmployeeId?: number | null;
  actorName: string;
  action: string;
};
