import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useAvailabilityGroupsListQuery } from "@entities/availability-groups";
import {
  ContainerGraphProfileWorkspace,
  useContainerByIdQuery,
  useDeleteGraphMutation,
  useGraphByIdQuery,
  useGraphCellStylesQuery,
  useGraphEmployeesQuery,
  useGraphSlotsQuery,
} from "@entities/containers";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import { useExportGraphExcelMutation, useExportGraphSqlMutation } from "@entities/exports/api/queries";
import { useShopsListQuery } from "@entities/shops/api/queries";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { IosButton } from "@shared/ui/components/IosButton";
import { CodeIcon, ExcelIcon } from "@shared/ui/icons";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./ContainerGraphProfilePage.module.css";

function runMutation<TData, TVariables>(
  mutate: (
    variables: TVariables,
    callbacks?: { onSuccess?: (data: TData) => void; onError?: (error: unknown) => void },
  ) => void,
  variables: TVariables,
) {
  return new Promise<TData>((resolve, reject) => {
    mutate(variables, {
      onSuccess: resolve,
      onError: reject,
    });
  });
}

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  document.body.append(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

function buildExportFilename(kind: "excel" | "sql", graphName: string, year: number, month: number) {
  const safeName = graphName
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  const baseName = safeName || "schedule";
  const suffix = `${year}-${String(month).padStart(2, "0")}`;

  return `${baseName}-${suffix}.${kind === "excel" ? "xlsx" : "sql"}`;
}

export function ContainerGraphProfilePage() {
  const navigate = useNavigate();
  const { containerId: containerIdParam, graphId: graphIdParam } = useParams<{ containerId: string; graphId: string }>();
  const parsedContainerId = containerIdParam ? Number(containerIdParam) : null;
  const parsedGraphId = graphIdParam ? Number(graphIdParam) : null;
  const containerId = Number.isFinite(parsedContainerId) ? parsedContainerId : null;
  const graphId = Number.isFinite(parsedGraphId) ? parsedGraphId : null;
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [isHeaderCollapsed, setIsHeaderCollapsed] = useState(false);

  const containerQuery = useContainerByIdQuery(containerId);
  const graphQuery = useGraphByIdQuery(containerId, graphId);
  const graphEmployeesQuery = useGraphEmployeesQuery(containerId, graphId);
  const slotsQuery = useGraphSlotsQuery(containerId, graphId);
  const cellStylesQuery = useGraphCellStylesQuery(containerId, graphId);
  const employeesQuery = useEmployeesListQuery();
  const shopsQuery = useShopsListQuery();
  const availabilityGroupsQuery = useAvailabilityGroupsListQuery();
  const deleteGraphMutation = useDeleteGraphMutation();
  const exportExcelMutation = useExportGraphExcelMutation();
  const exportSqlMutation = useExportGraphSqlMutation();

  const employeesById = useMemo(
    () => new Map((employeesQuery.data ?? []).map(employee => [employee.id, employee])),
    [employeesQuery.data],
  );

  const graph = graphQuery.data ?? null;
  const shop = useMemo(
    () => (shopsQuery.data ?? []).find(item => item.id === graph?.shopId) ?? null,
    [graph?.shopId, shopsQuery.data],
  );
  const availabilityGroup = useMemo(
    () => (availabilityGroupsQuery.data ?? []).find(item => item.id === graph?.availabilityGroupId) ?? null,
    [availabilityGroupsQuery.data, graph?.availabilityGroupId],
  );

  const hasValidId = containerId !== null && graphId !== null;
  const isLoading =
    hasValidId &&
    (!containerQuery.data ||
      !graphQuery.data ||
      !graphEmployeesQuery.data ||
      !slotsQuery.data ||
      !cellStylesQuery.data) &&
    (
      containerQuery.isLoading ||
      graphQuery.isLoading ||
      graphEmployeesQuery.isLoading ||
      slotsQuery.isLoading ||
      cellStylesQuery.isLoading
    );

  const hasLoadError =
    !hasValidId ||
    (!isLoading &&
      (
        containerQuery.isError ||
        graphQuery.isError ||
        graphEmployeesQuery.isError ||
        slotsQuery.isError ||
        cellStylesQuery.isError ||
        !graphQuery.data
      ));

  const handleDeleteConfirm = () => {
    if (!containerId || !graphId) {
      return;
    }

    deleteGraphMutation.mutate(
      { containerId, graphId },
      {
        onSuccess: () => {
          navigate(`/container?openContainerId=${containerId}`);
        },
      },
    );
  };

  const handleExportExcel = async () => {
    if (!containerId || !graph) {
      return;
    }

    const file = await runMutation(exportExcelMutation.mutate, { containerId, graphId: graph.id });
    downloadBlob(file, buildExportFilename("excel", graph.name, graph.year, graph.month));
  };

  const handleExportSql = async () => {
    if (!containerId || !graph) {
      return;
    }

    const file = await runMutation(exportSqlMutation.mutate, { containerId, graphId: graph.id });
    downloadBlob(file, buildExportFilename("sql", graph.name, graph.year, graph.month));
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title="Schedule Profile"
        subtitle="View schedule details, matrix layout, conflicts and employee summary"
        onBack={() => navigate(`/container?openContainerId=${containerId ?? ""}`)}
        onCollapseChange={setIsHeaderCollapsed}
        rightSlot={
          graph ? (
            <div className={styles.headerActions}>
              <IosButton
                label={exportExcelMutation.isPending ? "Exporting..." : "Export to Excel"}
                icon={<ExcelIcon size={18} />}
                variant="secondary"
                disabled={exportExcelMutation.isPending || exportSqlMutation.isPending}
                onClick={() => void handleExportExcel()}
              />
              <IosButton
                label={exportSqlMutation.isPending ? "Exporting..." : "Export to Code"}
                icon={<CodeIcon size={18} />}
                variant="secondary"
                disabled={exportExcelMutation.isPending || exportSqlMutation.isPending}
                onClick={() => void handleExportSql()}
              />
            </div>
          ) : null
        }
      />

      <ContainerGraphProfileWorkspace
        container={containerQuery.data}
        graph={graph}
        shop={shop}
        availabilityGroup={availabilityGroup}
        graphEmployees={graphEmployeesQuery.data ?? []}
        slots={slotsQuery.data ?? []}
        cellStyles={cellStylesQuery.data ?? []}
        employeesById={employeesById}
        isLoading={isLoading}
        hasLoadError={hasLoadError}
        isDeleting={deleteGraphMutation.isPending}
        isHeaderCollapsed={isHeaderCollapsed}
        onEdit={() => {
          if (containerId && graphId) {
            navigate(`/container/${containerId}/graphs/${graphId}/edit`);
          }
        }}
        onDelete={() => setIsDeleteOpen(true)}
      />

      <ConfirmDialog
        open={isDeleteOpen}
        title="Delete schedule"
        message="Are you sure you want to delete this schedule? This action cannot be undone."
        onCancel={() => setIsDeleteOpen(false)}
        onConfirm={handleDeleteConfirm}
        confirmText={deleteGraphMutation.isPending ? "Deleting..." : "Delete"}
        confirmDisabled={deleteGraphMutation.isPending}
        cancelDisabled={deleteGraphMutation.isPending}
      />
    </div>
  );
}
