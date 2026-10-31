import { request } from "@shared/api/httpClient";
import type { ApprovedUrlopDay, SubmitUrlop, UrlopEmployeeCalendar, UrlopManagerMonth, UrlopMonth, UrlopRequest } from "../model/types";

export const urlopApi = {
  employeeCalendar: (signal?: AbortSignal) => request<UrlopEmployeeCalendar>("employee-urlop", { signal }),
  employeeApproved: (shopId: number, year: number, month: number, signal?: AbortSignal) => request<ApprovedUrlopDay[]>("employee-urlop/approved", { query: { shopId, year, month }, signal }),
  submit: (body: SubmitUrlop) => request<UrlopRequest>("employee-urlop/requests", { method: "POST", body }),
  managerMonth: (shopId: number, year: number, month: number, signal?: AbortSignal) => request<UrlopManagerMonth>("manager-urlop", { query: { shopId, year, month }, signal }),
  approved: (from: string, to: string, shopId?: number, signal?: AbortSignal) => request<ApprovedUrlopDay[]>("manager-urlop/approved", { query: { from, to, shopId }, signal }),
  setMonth: (body: UrlopMonth) => request<UrlopMonth>("manager-urlop/month", { method: "PUT", body }),
  create: (body: SubmitUrlop & { employeeId: number }) => request<UrlopRequest>("manager-urlop/requests", { method: "POST", body }),
  replace: (requestId: string, days: number[]) => request<UrlopRequest>(`manager-urlop/requests/${requestId}`, { method: "PUT", body: { days } }),
  approve: (requestId: string) => request<UrlopRequest>(`manager-urlop/requests/${requestId}/approve`, { method: "POST" }),
  reject: (requestId: string) => request<UrlopRequest>(`manager-urlop/requests/${requestId}/reject`, { method: "POST" }),
  remove: (requestId: string) => request<void>(`manager-urlop/requests/${requestId}`, { method: "DELETE" }),
};
