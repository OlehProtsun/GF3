import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { adminDbApi } from "./adminDbApi";
import type { AdminDbSelectDatabaseRequest, AdminDbSqlRequest } from "@entities/admin-db/model/types";

export const useAdminDbMetadataQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.adminDb.metadata(),
    enabled,
    queryFn: ({ signal }) => adminDbApi.metadata({ signal }),
  });

export const useAdminDbHashQuery = (enabled = true) =>
  useQuery({
    queryKey: queryKeys.adminDb.hash(),
    enabled,
    queryFn: ({ signal }) => adminDbApi.hash({ signal }),
  });

export const useAdminDbQueryMutation = () =>
  useMutation({
    mutationFn: (payload: AdminDbSqlRequest) => adminDbApi.query(payload),
  });

export const useAdminDbExecuteMutation = () =>
  useMutation({
    mutationFn: (payload: AdminDbSqlRequest) => adminDbApi.execute(payload),
  });

export const useAdminDbImportMutation = () =>
  useMutation({
    mutationFn: (file: File) => adminDbApi.importSql(file),
  });

export const useAdminDbManualCopyMutation = () =>
  useMutation({
    mutationFn: () => adminDbApi.createManualCopy(),
  });

export const useAdminDbSelectDatabaseMutation = () =>
  useMutation({
    mutationFn: (payload: AdminDbSelectDatabaseRequest) => adminDbApi.selectDatabase(payload),
  });
