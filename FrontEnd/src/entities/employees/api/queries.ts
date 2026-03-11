import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { EmployeesListParams, SaveEmployeeInput } from "@entities/employees/model/types";
import { employeesApi } from "./employeesApi";

export function useEmployeesListQuery(_params?: EmployeesListParams) {
  return useQuery({
    queryKey: queryKeys.employees.list(),
    // Keep the employees list request alive through StrictMode remounts in dev.
    queryFn: () => employeesApi.list(),
  });
}

export function useEmployeeByIdQuery(id: number | null) {
  return useQuery({
    queryKey: id ? queryKeys.employees.byId(id) : queryKeys.employees.byId(0),
    enabled: id !== null,
    queryFn: ({ signal }) => employeesApi.byId(id as number, signal),
  });
}

export function useCreateEmployeeMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: SaveEmployeeInput) => employeesApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.employees.all });
    },
  });
}

export function useUpdateEmployeeMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: SaveEmployeeInput }) =>
      employeesApi.update(id, payload),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.employees.all });
      queryClient.invalidateQueries({ queryKey: queryKeys.employees.byId(variables.id) });
    },
  });
}

export function useDeleteEmployeeMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => employeesApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.employees.all });
    },
  });
}
