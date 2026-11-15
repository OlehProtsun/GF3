import { request } from "@shared/api/httpClient";
import type { ContractShiftApproval } from "../model/types";
import type { CreateShiftSwapInput, AcceptShiftSwapInput } from "@entities/shift-swaps/model/types";
const employee = "employee-shift-swap-approvals";
const graph = (containerId: number, graphId: number) => `containers/${containerId}/graphs/${graphId}/shift-swap-approvals`;
export const shiftSwapApprovalsApi = {
  mine: (signal?: AbortSignal) => request<ContractShiftApproval[]>(employee, { signal }),
  graph: (containerId: number, graphId: number, signal?: AbortSignal) => request<ContractShiftApproval[]>(graph(containerId, graphId), { signal }),
  claim: ({ id, fromTime, toTime }: AcceptShiftSwapInput) => request<ContractShiftApproval>(`${employee}/claims`, { method: "POST", body: { shiftSwapRequestId: id, fromTime, toTime } }),
  giveawayException: (body: CreateShiftSwapInput) => request<ContractShiftApproval>(`${employee}/giveaway-exceptions`, { method: "POST", body }),
  cancel: (id: number) => request<ContractShiftApproval>(`${employee}/${id}/cancel`, { method: "POST" }),
  review: ({ containerId, graphId, id, action }: { containerId: number; graphId: number; id: number; action: "approve" | "reject" }) =>
    request<ContractShiftApproval>(`${graph(containerId, graphId)}/${id}/${action}`, { method: "POST" }),
};
