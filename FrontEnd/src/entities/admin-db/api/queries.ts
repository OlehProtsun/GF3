import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { adminDbApi } from "./adminDbApi";
import type { AdminDbSqlRequest } from "@entities/admin-db/model/types";

export const useAdminDbMetadataQuery = () => useQuery({ queryKey: queryKeys.adminDb.metadata(), queryFn: ({ signal }) => adminDbApi.metadata(signal) });
export const useAdminDbHashQuery = () => useQuery({ queryKey: queryKeys.adminDb.hash(), queryFn: ({ signal }) => adminDbApi.hash(signal) });
export const useAdminDbQueryMutation = () => useMutation({ mutationFn: (payload: AdminDbSqlRequest) => adminDbApi.query(payload) });
export const useAdminDbExecuteMutation = () => useMutation({ mutationFn: (payload: AdminDbSqlRequest) => adminDbApi.execute(payload) });
export const useAdminDbImportMutation = () => useMutation({ mutationFn: (file: File) => adminDbApi.importSql(file) });
