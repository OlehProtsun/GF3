import { useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useAvailabilityGroupsListQuery } from "@entities/availability-groups";
import {
  ContainerGraphSessionTabs,
  ContainerGraphProfileWorkspace,
  buildGraphSessionSearch,
  getGraphSessionIds,
  resolveGraphSession,
  useContainerByIdQuery,
  useContainerGraphsQuery,
  useDeleteGraphMutation,
  useGraphByIdQuery,
  useGraphCellStylesQuery,
  useGraphEmployeesQuery,
  useGraphSlotsBatchQuery,
  useGraphSlotsQuery,
} from "@entities/containers";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import {
  ProfileExportActions,
  buildGraphExportFallbackFilename,
  downloadExportFile,
  getExportErrorMessage,
  runMutation,
  useExportGraphExcelMutation,
  useExportGraphSqlMutation,
} from "@entities/exports";
import { useShopsListQuery } from "@entities/shops/api/queries";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { IosButton } from "@shared/ui/components/IosButton";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./ContainerGraphProfilePage.module.css";

type CompactSizeHeaderToggleProps = {
  checked: boolean;
  onToggle: () => void;
};

function joinClassNames(...values: Array<string | false | undefined>) {
  return values.filter(Boolean).join(" ");
}

function CompactSizeHeaderToggle({ checked, onToggle }: CompactSizeHeaderToggleProps) {
  return (
    <button
      type="button"
      className={joinClassNames(styles.compactToggle, checked && styles.compactToggleActive)}
      aria-pressed={checked}
      onClick={onToggle}
    >
      <span className={styles.compactToggleTitle}>Compact Size</span>

      <span className={styles.compactToggleTrack} aria-hidden="true">
        <span className={styles.compactToggleThumb} />
      </span>
    </button>
  );
}

export function ContainerGraphProfilePage() {
  usePageScrollbarHidden();

  const location = useLocation();
  const navigate = useNavigate();
  const { containerId: containerIdParam, graphId: graphIdParam } = useParams<{ containerId: string; graphId: string }>();
  const parsedContainerId = containerIdParam ? Number(containerIdParam) : null;
  const parsedGraphId = graphIdParam ? Number(graphIdParam) : null;
  const containerId = Number.isFinite(parsedContainerId) ? parsedContainerId : null;
  const graphId = Number.isFinite(parsedGraphId) ? parsedGraphId : null;
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [isEditConfirmOpen, setIsEditConfirmOpen] = useState(false);
  const [isHeaderCollapsed, setIsHeaderCollapsed] = useState(false);
  const [isCompactMatrix, setIsCompactMatrix] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);
  const hasValidId = containerId !== null && graphId !== null;
  const deleteGraphMutation = useDeleteGraphMutation();
  const graphQueriesEnabled = hasValidId && !deleteGraphMutation.isPending;

  const containerQuery = useContainerByIdQuery(containerId);
  const containerGraphsQuery = useContainerGraphsQuery(containerId, graphQueriesEnabled);
  const graphQuery = useGraphByIdQuery(containerId, graphId, graphQueriesEnabled);
  const graphEmployeesQuery = useGraphEmployeesQuery(containerId, graphId, graphQueriesEnabled);
  const slotsQuery = useGraphSlotsQuery(containerId, graphId, graphQueriesEnabled);
  const cellStylesQuery = useGraphCellStylesQuery(containerId, graphId, graphQueriesEnabled);
  const employeesQuery = useEmployeesListQuery({ refreshKey: location.key });
  const shopsQuery = useShopsListQuery({ refreshKey: location.key });
  const availabilityGroupsQuery = useAvailabilityGroupsListQuery(location.key);
  const exportExcelMutation = useExportGraphExcelMutation();
  const exportSqlMutation = useExportGraphSqlMutation();

  const employeesById = useMemo(
    () => new Map((employeesQuery.data ?? []).map(employee => [employee.id, employee])),
    [employeesQuery.data],
  );

  const graph = graphQuery.data ?? null;
  const hasExplicitSession = useMemo(
    () => new URLSearchParams(location.search).has("openGraphIds"),
    [location.search],
  );
  const openGraphIds = useMemo(() => {
    const requestedGraphIds = getGraphSessionIds(location.search, graphId);
    const availableGraphIdSet = new Set((containerGraphsQuery.data ?? []).map(item => item.id));

    if (availableGraphIdSet.size === 0) {
      return requestedGraphIds;
    }

    const filteredGraphIds = requestedGraphIds.filter(item => availableGraphIdSet.has(item));
    if (graphId !== null && availableGraphIdSet.has(graphId) && !filteredGraphIds.includes(graphId)) {
      filteredGraphIds.unshift(graphId);
    }

    return filteredGraphIds.length > 0 ? filteredGraphIds : (graphId !== null ? [graphId] : []);
  }, [containerGraphsQuery.data, graphId, location.search]);
  const sessionGraphs = useMemo(
    () => resolveGraphSession(openGraphIds, containerGraphsQuery.data ?? [], graph),
    [containerGraphsQuery.data, graph, openGraphIds],
  );
  const sessionSearch = useMemo(
    () => (openGraphIds.length > 1 || hasExplicitSession ? buildGraphSessionSearch(openGraphIds) : ""),
    [hasExplicitSession, openGraphIds],
  );
  const sessionGraphNames = useMemo(() => {
    const fallbackName = graph?.name?.trim();
    const names = sessionGraphs.map(item => item.name.trim()).filter(Boolean);

    if (names.length > 0) {
      return names;
    }

    return fallbackName ? [fallbackName] : [];
  }, [graph?.name, sessionGraphs]);
  const showSessionTabs = openGraphIds.length > 1 || hasExplicitSession;
  const visibleSessionTabCount = Math.min(Math.max(sessionGraphs.length, 1), 3);
  const pageHeaderMaxWidth = showSessionTabs
    ? `${1000 + (visibleSessionTabCount * 118)}px`
    : "1080px";
  const shop = useMemo(
    () => (shopsQuery.data ?? []).find(item => item.id === graph?.shopId) ?? null,
    [graph?.shopId, shopsQuery.data],
  );
  const availabilityGroup = useMemo(
    () => (availabilityGroupsQuery.data ?? []).find(item => item.id === graph?.availabilityGroupId) ?? null,
    [availabilityGroupsQuery.data, graph?.availabilityGroupId],
  );
  const relatedGraphs = useMemo(
    () => (containerGraphsQuery.data ?? []).filter(item =>
      item.id !== graphId &&
      item.year === graph?.year &&
      item.month === graph?.month,
    ),
    [containerGraphsQuery.data, graph?.month, graph?.year, graphId],
  );
  const relatedGraphSlotsQuery = useGraphSlotsBatchQuery(
    containerId,
    relatedGraphs.map(item => item.id),
    graphQueriesEnabled,
  );

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

  const handleOpenEdit = () => {
    if (!containerId || !graphId) {
      return;
    }

    navigate(`/container/${containerId}/graphs/${graphId}/edit${sessionSearch}`);
  };

  const handleExportExcel = async () => {
    if (!containerId || !graph) {
      return;
    }

    setExportError(null);

    try {
      const file = await runMutation(exportExcelMutation.mutate, { containerId, graphId: graph.id });
      downloadExportFile(file, buildGraphExportFallbackFilename("excel", graph.name, graph.year, graph.month));
    } catch (error) {
      setExportError(getExportErrorMessage(error, "Could not export this schedule to Excel."));
    }
  };

  const handleExportSql = async () => {
    if (!containerId || !graph) {
      return;
    }

    setExportError(null);

    try {
      const file = await runMutation(exportSqlMutation.mutate, { containerId, graphId: graph.id });
      downloadExportFile(file, buildGraphExportFallbackFilename("sql", graph.name, graph.year, graph.month));
    } catch (error) {
      setExportError(getExportErrorMessage(error, "Could not export this schedule to code."));
    }
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title="Schedule Profile"
        subtitle="View schedule details, matrix layout, conflicts and employee summary"
        onBack={() => navigate(`/container?openContainerId=${containerId ?? ""}`)}
        onCollapseChange={setIsHeaderCollapsed}
        maxWidth={pageHeaderMaxWidth}
        rightSlot={
          graph ? (
            showSessionTabs ? (
              <div className={styles.headerActionCluster}>
                <ProfileExportActions
                  isExcelPending={exportExcelMutation.isPending}
                  isCodePending={exportSqlMutation.isPending}
                  onExportExcel={() => void handleExportExcel()}
                  onExportCode={() => void handleExportSql()}
                />

                <CompactSizeHeaderToggle
                  checked={isCompactMatrix}
                  onToggle={() => setIsCompactMatrix(current => !current)}
                />

                <ContainerGraphSessionTabs
                  className={styles.sessionSection}
                  items={sessionGraphs.map(item => ({
                    graphId: item.id,
                    label: item.name,
                    active: item.id === graphId,
                  }))}
                  onSelect={(nextGraphId) => {
                    if (!containerId || nextGraphId === graphId) {
                      return;
                    }

                    navigate(`/container/${containerId}/graphs/${nextGraphId}${sessionSearch}`);
                  }}
                  actionSlot={<IosButton label="Edit all" onClick={() => setIsEditConfirmOpen(true)} />}
                />
              </div>
            ) : (
              <div className={styles.headerActions}>
                <ProfileExportActions
                  isExcelPending={exportExcelMutation.isPending}
                  isCodePending={exportSqlMutation.isPending}
                  onExportExcel={() => void handleExportExcel()}
                  onExportCode={() => void handleExportSql()}
                />

                <CompactSizeHeaderToggle
                  checked={isCompactMatrix}
                  onToggle={() => setIsCompactMatrix(current => !current)}
                />
              </div>
            )
          ) : null
        }
      />

      <div className={`${styles.content} ${styles.contentWide}`}>
        {exportError ? <ErrorBanner className={styles.banner}>{exportError}</ErrorBanner> : null}

        <ContainerGraphProfileWorkspace
          container={containerQuery.data}
          graph={graph}
          shop={shop}
          availabilityGroup={availabilityGroup}
          graphEmployees={graphEmployeesQuery.data ?? []}
          slots={slotsQuery.data ?? []}
          cellStyles={cellStylesQuery.data ?? []}
          relatedGraphs={relatedGraphs}
          relatedGraphSlotsById={relatedGraphSlotsQuery.data ?? {}}
          employeesById={employeesById}
          isLoading={isLoading}
          hasLoadError={hasLoadError}
          isDeleting={deleteGraphMutation.isPending}
          isHeaderCollapsed={isHeaderCollapsed}
          compactSize={isCompactMatrix}
          showEditAction={!showSessionTabs}
          onEdit={() => setIsEditConfirmOpen(true)}
          onDelete={() => setIsDeleteOpen(true)}
        />
      </div>

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

      <ConfirmDialog
        open={isEditConfirmOpen}
        title={sessionGraphNames.length > 1 ? "Edit selected schedules" : "Edit schedule"}
        message={
          sessionGraphNames.length > 1
            ? "Open the editor for these schedules?"
            : `Open the editor for '${sessionGraphNames[0] ?? graph?.name ?? "this schedule"}'?`
        }
        footerSlot={
          sessionGraphNames.length > 0 ? (
            <div className={styles.dialogList}>
              <span className={styles.dialogListTitle}>
                {sessionGraphNames.length > 1 ? "Schedules in this session" : "Selected schedule"}
              </span>
              <span className={styles.dialogListValue}>{sessionGraphNames.join(", ")}</span>
            </div>
          ) : null
        }
        onCancel={() => setIsEditConfirmOpen(false)}
        onConfirm={handleOpenEdit}
        confirmText="Edit"
        variant="confirm"
      />
    </div>
  );
}
