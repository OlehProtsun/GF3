import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiError } from "@shared/api/httpClient";
import { queryKeys } from "@shared/api/queryKeys";
import { urlopApi } from "./urlopApi";
import { isoDate, monthDays } from "../model/calendar";

export function useEmployeeUrlopCalendarQuery() {
  return useQuery({ queryKey: queryKeys.urlop.employeeCalendar(), queryFn: ({ signal }) => urlopApi.employeeCalendar(signal) });
}
export function useManagerUrlopMonthQuery(shopId: number, year: number, month: number) {
  return useQuery({ queryKey: queryKeys.urlop.managerMonth(shopId, year, month), enabled: shopId > 0,
    queryFn: ({ signal }) => urlopApi.managerMonth(shopId, year, month, signal) });
}
export function useApprovedUrlopQuery(from: string, to: string, shopId?: number, enabled = true) {
  return useQuery({ queryKey: queryKeys.urlop.approved(from, to, shopId), enabled: enabled && !!from && !!to,
    queryFn: ({ signal }) => urlopApi.approved(from, to, shopId, signal) });
}
export function useEmployeeApprovedUrlopQuery(shopId: number, year: number, month: number) {
  return useQuery({ queryKey: queryKeys.urlop.employeeApproved(shopId, year, month), enabled: shopId > 0 && year > 0 && month > 0,
    queryFn: ({ signal }) => urlopApi.employeeApproved(shopId, year, month, signal) });
}
export function useGraphApprovedUrlopQuery(graph?: { shopId: number; year: number; month: number } | null) {
  const { shopId = 0, year = 2000, month = 1 } = graph ?? {};
  return useApprovedUrlopQuery(isoDate(year, month, 1), isoDate(year, month, monthDays(year, month).length), shopId, shopId > 0);
}
export function useUrlopMutation<TData, TVariables>(mutationFn: (variables: TVariables) => Promise<TData>) {
  const client = useQueryClient();
  const refresh = () => {
    for (const queryKey of [queryKeys.urlop.all, queryKeys.employeeAvailability.all, queryKeys.employeeSchedules.all,
      queryKeys.containers.all, queryKeys.availabilityGroups.all]) client.invalidateQueries({ queryKey });
  };
  return useMutation({ mutationFn, onSuccess: refresh, onError: error => { if (error instanceof ApiError && error.status === 409) refresh(); } });
}
