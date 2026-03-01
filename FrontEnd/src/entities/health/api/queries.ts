import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { healthApi } from "./healthApi";

export const useHealthQuery = () => useQuery({ queryKey: queryKeys.health.status(), queryFn: ({ signal }) => healthApi.get(signal) });
