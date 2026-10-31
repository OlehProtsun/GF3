import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { employmentContractSettingsApi } from "./employmentContractSettingsApi";
import type { EmploymentContractSettings } from "../model/types";
export function useEmploymentContractSettingsQuery(enabled = true) {
  const client = useQueryClient();
  const query = useQuery({ queryKey: queryKeys.employmentContracts.settings(), enabled, staleTime: 0,
    queryFn: ({ signal }) => employmentContractSettingsApi.get(signal) });
  return { ...query, refetch: () => client.invalidateQueries({ queryKey: queryKeys.employmentContracts.settings() }) };
}
export function useUpdateEmploymentContractSettingsMutation() {
  const client = useQueryClient();
  return useMutation({ mutationFn: (settings: EmploymentContractSettings) => employmentContractSettingsApi.update(settings),
    onSuccess: data => {
      client.setQueryData(queryKeys.employmentContracts.settings(), data);
      client.invalidateQueries({ queryKey: queryKeys.employmentContracts.settings() });
      client.invalidateQueries({ queryKey: queryKeys.employeeAvailability.all });
      client.invalidateQueries({ queryKey: queryKeys.shiftSwaps.all });
    },
  });
}
