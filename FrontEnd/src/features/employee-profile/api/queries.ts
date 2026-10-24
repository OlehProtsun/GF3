import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { CompletePasswordResetDto, UpdateEmployeeProfileDto } from "./dto";
import { employeeProfileApi } from "./employeeProfileApi";

export function useEmployeeProfileQuery() {
  return useQuery({
    queryKey: queryKeys.employeeProfile.me(),
    queryFn: ({ signal }) => employeeProfileApi.current(signal),
  });
}

export function useUpdateEmployeeProfileMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UpdateEmployeeProfileDto) => employeeProfileApi.update(payload),
    onSuccess: (profile) => {
      queryClient.setQueryData(queryKeys.employeeProfile.me(), profile);
    },
  });
}

export function useSendEmployeePasswordResetCodeMutation() {
  return useMutation({
    mutationFn: () => employeeProfileApi.sendPasswordResetCode(),
  });
}

export function useConfirmEmployeePasswordResetMutation() {
  return useMutation({
    mutationFn: (payload: CompletePasswordResetDto) => employeeProfileApi.confirmPasswordReset(payload),
  });
}
