import { startTransition, useDeferredValue, useEffect, useMemo, useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import type { ContainerGraphRecords, Graph } from "@entities/containers";
import {
  ContainerDetailsForm,
  ContainerListCard,
  ContainerProfileWorkspace,
  buildGraphSessionSearch,
  buildContainerGraphSummaries,
  buildContainerStatistics,
  createContainerFormState,
  filterGraphSummaries,
  containersApi,
  matchesContainerSearch,
  useContainerByIdQuery,
  useContainerForm,
  useContainerGraphsQuery,
  useContainersListQuery,
  useCreateContainerMutation,
  useDeleteContainerMutation,
  useUpdateContainerMutation,
} from "@entities/containers";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import {
  ProfileExportActions,
  buildContainerExportFallbackFilename,
  downloadExportFile,
  getExportErrorMessage,
  runMutation,
  useExportContainerExcelMutation,
  useExportContainerSqlMutation,
} from "@entities/exports";
import { useShopsListQuery } from "@entities/shops/api/queries";
import { ApiError } from "@shared/api/httpClient";
import { queryKeys } from "@shared/api/queryKeys";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { stableSerialize } from "@shared/lib/stableSerialize";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { SavingOverlay } from "@shared/ui/SavingOverlay";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { PlusIcon } from "@shared/ui/icons";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./ContainerPage.module.css";

type ContainerPageMode = "list" | "profile" | "edit";

type GraphRecordsState = {
  recordsByGraphId: Record<number, ContainerGraphRecords>;
  isLoading: boolean;
  error: unknown;
};

function sanitizeSelectedGraphIds(selectedGraphIds: number[], graphs: Graph[]) {
  const validGraphIdSet = new Set(graphs.map(graph => graph.id));
  return selectedGraphIds.filter(graphId => validGraphIdSet.has(graphId));
}

function useContainerGraphRecords(containerId: number | null, graphs: Graph[], enabled: boolean): GraphRecordsState {
  const graphIdsKey = useMemo(
    () => graphs.map((graph) => graph.id).sort((left, right) => left - right).join(","),
    [graphs],
  );

  const recordsQuery = useQuery({
    queryKey: queryKeys.containers.graphRecords(containerId ?? 0, graphIdsKey),
    enabled: enabled && containerId !== null && graphs.length > 0,
    cancelOnUnmount: true,
    staleTime: 30_000,
    queryFn: async ({ signal }) => {
      const results = await Promise.all(
        graphs.map(async (graph) => {
          const [employees, slots] = await Promise.all([
            containersApi.listGraphEmployees(containerId as number, graph.id, signal),
            containersApi.listGraphSlots(containerId as number, graph.id, signal),
          ]);

          return {
            graphId: graph.id,
            employees,
            slots,
          };
        }),
      );

      return results.reduce<Record<number, ContainerGraphRecords>>((accumulator, result) => {
        accumulator[result.graphId] = {
          employees: result.employees,
          slots: result.slots,
        };

        return accumulator;
      }, {});
    },
  });

  if (!enabled || containerId === null || graphs.length === 0) {
    return {
      recordsByGraphId: {},
      isLoading: false,
      error: null,
    };
  }

  return {
    recordsByGraphId: recordsQuery.data ?? {},
    isLoading: recordsQuery.isLoading || recordsQuery.isFetching,
    error: recordsQuery.error,
  };
}

export function ContainerPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [mode, setMode] = useState<ContainerPageMode>("list");
  const [listSearchQuery, setListSearchQuery] = useState("");
  const [graphSearchQuery, setGraphSearchQuery] = useState("");
  const [selectedContainerId, setSelectedContainerId] = useState<number | null>(null);
  const [editingContainerId, setEditingContainerId] = useState<number | null>(null);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [isMultiOpenEnabled, setIsMultiOpenEnabled] = useState(false);
  const [selectedGraphIds, setSelectedGraphIds] = useState<number[]>([]);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [profileExportError, setProfileExportError] = useState<string | null>(null);

  usePageScrollbarHidden(mode === "list" || mode === "profile");

  const deferredListSearchQuery = useDeferredValue(listSearchQuery);
  const deferredGraphSearchQuery = useDeferredValue(graphSearchQuery);
  const containersQuery = useContainersListQuery(location.key);
  const employeesQuery = useEmployeesListQuery({ refreshKey: location.key });
  const shopsQuery = useShopsListQuery({ refreshKey: location.key });
  const createMutation = useCreateContainerMutation();
  const updateMutation = useUpdateContainerMutation();
  const deleteMutation = useDeleteContainerMutation();
  const exportContainerExcelMutation = useExportContainerExcelMutation();
  const exportContainerSqlMutation = useExportContainerSqlMutation();

  const profileContainerId = mode === "profile" ? selectedContainerId : null;
  const editContainerId = mode === "edit" && editingContainerId !== null ? editingContainerId : null;
  const profileQueriesEnabled = mode === "profile" && !deleteMutation.isPending;

  const profileContainerQuery = useContainerByIdQuery(profileContainerId, profileQueriesEnabled);
  const editContainerQuery = useContainerByIdQuery(editContainerId);
  const graphsQuery = useContainerGraphsQuery(profileContainerId, profileQueriesEnabled);

  const allContainers = useMemo(() => containersQuery.data ?? [], [containersQuery.data]);
  const filteredContainers = useMemo(
    () => allContainers.filter((container) => matchesContainerSearch(container, deferredListSearchQuery)),
    [allContainers, deferredListSearchQuery],
  );

  const selectedContainerFromList = useMemo(
    () => allContainers.find((container) => container.id === selectedContainerId) ?? null,
    [allContainers, selectedContainerId],
  );

  const profileContainer = profileContainerQuery.data ?? selectedContainerFromList;
  const editingContainer = editContainerId !== null ? editContainerQuery.data ?? selectedContainerFromList : null;
  const isCreateMode = mode === "edit" && editingContainerId === null;

  const { form, errors, handleFieldChange, validate, applyApiErrors } = useContainerForm(
    editingContainer,
    isCreateMode,
  );

  const employeesById = useMemo(
    () => new Map((employeesQuery.data ?? []).map((employee) => [employee.id, employee])),
    [employeesQuery.data],
  );
  const shopsById = useMemo(
    () => new Map((shopsQuery.data ?? []).map((shop) => [shop.id, shop])),
    [shopsQuery.data],
  );

  const graphs = useMemo(() => graphsQuery.data ?? [], [graphsQuery.data]);
  const sanitizedSelectedGraphIds = useMemo(
    () => sanitizeSelectedGraphIds(selectedGraphIds, graphs),
    [graphs, selectedGraphIds],
  );
  const graphRecordsState = useContainerGraphRecords(profileContainerId, graphs, mode === "profile");

  const graphSummaries = useMemo(
    () => buildContainerGraphSummaries(graphs, graphRecordsState.recordsByGraphId, shopsById),
    [graphRecordsState.recordsByGraphId, graphs, shopsById],
  );
  const filteredGraphSummaries = useMemo(
    () => filterGraphSummaries(graphSummaries, deferredGraphSearchQuery),
    [deferredGraphSearchQuery, graphSummaries],
  );
  const statistics = useMemo(
    () =>
      buildContainerStatistics({
        graphs,
        graphRecordsById: graphRecordsState.recordsByGraphId,
        employeesById,
        shopsById,
      }),
    [employeesById, graphRecordsState.recordsByGraphId, graphs, shopsById],
  );

  const isSaving = createMutation.isPending || updateMutation.isPending;
  const isProfileLoading =
    mode === "profile" &&
    selectedContainerId !== null &&
    !profileContainer &&
    (profileContainerQuery.isLoading || profileContainerQuery.isFetching || containersQuery.isLoading);
  const hasProfileLoadError =
    mode === "profile" &&
    (!selectedContainerId || (!isProfileLoading && !profileContainer && (profileContainerQuery.isError || Boolean(profileContainerQuery.error))));
  const isEditLoading =
    mode === "edit" &&
    editContainerId !== null &&
    !editingContainer &&
    (editContainerQuery.isLoading || editContainerQuery.isFetching);
  const hasEditLoadError =
    mode === "edit" &&
    editContainerId !== null &&
    !editingContainer &&
    !isEditLoading &&
    (editContainerQuery.isError || Boolean(editContainerQuery.error));
  const initialFormSnapshot = useMemo(
    () => stableSerialize(createContainerFormState(editingContainer)),
    [editingContainer],
  );
  const currentFormSnapshot = useMemo(() => stableSerialize(form), [form]);
  const hasUnsavedChanges = useMemo(() => {
    if (mode !== "edit" || isEditLoading || hasEditLoadError) {
      return false;
    }

    if (isCreateMode) {
      return currentFormSnapshot !== initialFormSnapshot;
    }

    return Boolean(editingContainer) && currentFormSnapshot !== initialFormSnapshot;
  }, [
    currentFormSnapshot,
    editingContainer,
    hasEditLoadError,
    initialFormSnapshot,
    isCreateMode,
    isEditLoading,
    mode,
  ]);
  const { confirmIfNeeded, dialog: unsavedChangesDialog } = useUnsavedChangesPrompt({
    when: hasUnsavedChanges && !isSaving,
  });

  useEffect(() => {
    if (location.pathname !== "/container") {
      return;
    }

    const nextContainerId = Number(new URLSearchParams(location.search).get("openContainerId"));
    if (!Number.isInteger(nextContainerId) || nextContainerId <= 0) {
      return;
    }

    startTransition(() => {
      setSelectedContainerId(nextContainerId);
      setEditingContainerId(null);
      setGraphSearchQuery("");
      setIsMultiOpenEnabled(false);
      setSelectedGraphIds([]);
      setSubmitError(null);
      setProfileExportError(null);
      setMode("profile");
    });
  }, [location.pathname, location.search]);

  useEffect(() => {
    if (mode !== "profile" || selectedContainerId === null || containersQuery.isLoading) {
      return;
    }

    if (containersQuery.isError) {
      return;
    }

    const stillExists = allContainers.some((container) => container.id === selectedContainerId);
    if (!stillExists && !profileContainerQuery.isLoading && !profileContainerQuery.data) {
      startTransition(() => {
        setMode("list");
        setSelectedContainerId(null);
        setEditingContainerId(null);
        setGraphSearchQuery("");
        setIsMultiOpenEnabled(false);
        setSelectedGraphIds([]);
        setProfileExportError(null);
      });
    }
  }, [
    allContainers,
    containersQuery.isError,
    containersQuery.isLoading,
    mode,
    profileContainerQuery.data,
    profileContainerQuery.isLoading,
    selectedContainerId,
  ]);

  const handleOpenProfile = (containerId: number) => {
    setSelectedContainerId(containerId);
    setEditingContainerId(null);
    setGraphSearchQuery("");
    setIsMultiOpenEnabled(false);
    setSelectedGraphIds([]);
    setSubmitError(null);
    setProfileExportError(null);
    setMode("profile");
  };

  const handleStartCreate = () => {
    setSelectedContainerId(null);
    setEditingContainerId(null);
    setIsMultiOpenEnabled(false);
    setSelectedGraphIds([]);
    setSubmitError(null);
    setProfileExportError(null);
    setMode("edit");
  };

  const handleStartEdit = (containerId: number) => {
    setSelectedContainerId(containerId);
    setEditingContainerId(containerId);
    setIsMultiOpenEnabled(false);
    setSelectedGraphIds([]);
    setSubmitError(null);
    setProfileExportError(null);
    setMode("edit");
  };

  const handleStartGraphCreate = () => {
    if (selectedContainerId === null) {
      return;
    }

    navigate(`/container/${selectedContainerId}/graphs/new`);
  };

  const handleBackFromProfile = () => {
    setMode("list");
    setGraphSearchQuery("");
    setIsMultiOpenEnabled(false);
    setSelectedGraphIds([]);
    setProfileExportError(null);
  };

  const handleCancelEdit = () => {
    confirmIfNeeded(() => {
      setSubmitError(null);

      if (editingContainerId !== null) {
        setProfileExportError(null);
        setMode("profile");
        return;
      }

      setProfileExportError(null);
      setMode("list");
    });
  };

  const handleMutationError = (error: unknown) => {
    if (error instanceof ApiError) {
      applyApiErrors(error.validationErrors);
      setSubmitError(error.message);
      return;
    }

    setSubmitError("Could not save container.");
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);

    if (!validate()) {
      return;
    }

    const payload = {
      name: form.name.trim(),
      note: form.note.trim() || undefined,
    };

    if (isCreateMode) {
      createMutation.mutate(payload, {
        onSuccess: (created) => {
          setSelectedContainerId(created.id);
          setEditingContainerId(null);
          setProfileExportError(null);
          setMode("profile");
        },
        onError: handleMutationError,
      });
      return;
    }

    if (editingContainerId === null) {
      return;
    }

    updateMutation.mutate(
      { id: editingContainerId, payload },
      {
        onSuccess: () => {
          setSelectedContainerId(editingContainerId);
          setProfileExportError(null);
          setMode("profile");
        },
        onError: handleMutationError,
      },
    );
  };

  const handleDeleteConfirm = () => {
    if (!selectedContainerId) {
      return;
    }

    deleteMutation.mutate(selectedContainerId, {
      onSuccess: () => {
        setIsDeleteOpen(false);
        setMode("list");
        setSelectedContainerId(null);
        setEditingContainerId(null);
        setGraphSearchQuery("");
        setIsMultiOpenEnabled(false);
        setSelectedGraphIds([]);
        setProfileExportError(null);
      },
    });
  };

  const handleExportContainerExcel = async () => {
    if (!profileContainer) {
      return;
    }

    setProfileExportError(null);

    try {
      const file = await runMutation(exportContainerExcelMutation.mutate, { containerId: profileContainer.id });
      downloadExportFile(file, buildContainerExportFallbackFilename("excel", profileContainer.name));
    } catch (error) {
      setProfileExportError(getExportErrorMessage(error, "Could not export this container to Excel."));
    }
  };

  const handleExportContainerCode = async () => {
    if (!profileContainer) {
      return;
    }

    setProfileExportError(null);

    try {
      const file = await runMutation(exportContainerSqlMutation.mutate, { containerId: profileContainer.id });
      downloadExportFile(file, buildContainerExportFallbackFilename("sql", profileContainer.name));
    } catch (error) {
      setProfileExportError(getExportErrorMessage(error, "Could not export this container to code."));
    }
  };

  const header = (() => {
    if (mode === "edit") {
      return (
        <PageHeader
          title={isCreateMode ? "Add Container" : "Edit Container"}
          subtitle={isCreateMode ? "Create a new container workspace" : "Update container information"}
          onBack={handleCancelEdit}
        />
      );
    }

    if (mode === "profile") {
      return (
        <PageHeader
          title="Container Profile"
          subtitle="View container details, schedules and aggregate workload statistics"
          onBack={handleBackFromProfile}
          rightSlot={
            profileContainer ? (
              <ProfileExportActions
                isExcelPending={exportContainerExcelMutation.isPending}
                isCodePending={exportContainerSqlMutation.isPending}
                disabled={graphsQuery.isLoading || graphRecordsState.isLoading || graphs.length === 0}
                onExportExcel={() => void handleExportContainerExcel()}
                onExportCode={() => void handleExportContainerCode()}
              />
            ) : null
          }
        />
      );
    }

    return (
      <PageHeader
        title="Container List"
        subtitle="Browse containers and open their schedule workspaces"
        backTo="/"
        rightSlot={<IosButton label="Add New" icon={<PlusIcon size={18} />} onClick={handleStartCreate} />}
        searchMeta={`Total: ${filteredContainers.length}`}
        search={{
          value: listSearchQuery,
          onChange: setListSearchQuery,
          placeholder: "Search container",
          ariaLabel: "Search container",
        }}
      />
    );
  })();

  const contentClassName = [
    styles.content,
    mode === "profile" ? styles.contentWide : styles.contentNarrow,
  ].join(" ");

  return (
    <div className={styles.page}>
      {header}

      <div className={contentClassName}>
        {mode === "list" ? (
          <ContainerListCard
            containers={filteredContainers}
            error={containersQuery.error}
            isLoading={containersQuery.isLoading}
            searchQuery={listSearchQuery}
            onClearSearch={() => setListSearchQuery("")}
            onAddContainer={handleStartCreate}
            onContainerOpen={handleOpenProfile}
          />
        ) : null}

        {mode === "edit" ? (
          <div className={styles.editShell}>
            <ContainerDetailsForm
              form={form}
              errors={errors}
              isLoading={isEditLoading}
              hasLoadError={Boolean(hasEditLoadError)}
              isSaving={isSaving}
              submitError={submitError}
              onFieldChange={handleFieldChange}
              onCancel={handleCancelEdit}
              onSubmit={handleSubmit}
            />
          </div>
        ) : null}

        {mode === "profile" ? (
          <>
            {profileExportError ? <ErrorBanner className={styles.profileBanner}>{profileExportError}</ErrorBanner> : null}

            <ContainerProfileWorkspace
              container={profileContainer}
              graphs={filteredGraphSummaries}
              totalGraphsCount={graphs.length}
              statistics={statistics}
              isLoading={Boolean(isProfileLoading)}
              hasLoadError={Boolean(hasProfileLoadError)}
              isDeleting={deleteMutation.isPending}
              isGraphsLoading={graphsQuery.isLoading || graphRecordsState.isLoading}
              hasGraphsError={graphsQuery.isError || Boolean(graphsQuery.error) || Boolean(graphRecordsState.error)}
              graphSearchQuery={graphSearchQuery}
              isMultiOpenEnabled={isMultiOpenEnabled}
              selectedGraphIds={sanitizedSelectedGraphIds}
              onGraphSearchChange={setGraphSearchQuery}
              onClearGraphSearch={() => setGraphSearchQuery("")}
              onToggleMultiOpen={() => {
                setIsMultiOpenEnabled(current => !current);
                setSelectedGraphIds([]);
              }}
              onToggleGraphSelection={(graphId) => {
                setSelectedGraphIds(currentSelection => {
                  const nextSelection = sanitizeSelectedGraphIds(currentSelection, graphs);

                  return nextSelection.includes(graphId)
                    ? nextSelection.filter(item => item !== graphId)
                    : [...nextSelection, graphId];
                });
              }}
              onOpenSelectedGraphs={() => {
                if (selectedContainerId === null || sanitizedSelectedGraphIds.length === 0) {
                  return;
                }

                navigate(
                  `/container/${selectedContainerId}/graphs/${sanitizedSelectedGraphIds[0]}${buildGraphSessionSearch(sanitizedSelectedGraphIds)}`,
                );
              }}
              onAddGraph={handleStartGraphCreate}
              onOpenGraph={(graphId) => {
                if (selectedContainerId !== null) {
                  navigate(`/container/${selectedContainerId}/graphs/${graphId}`);
                }
              }}
              onEditContainer={handleStartEdit}
              onDeleteContainer={() => setIsDeleteOpen(true)}
            />
          </>
        ) : null}
      </div>

      <ConfirmDialog
        open={isDeleteOpen}
        title="Delete container"
        message="Are you sure you want to delete this container? This action cannot be undone."
        onCancel={() => setIsDeleteOpen(false)}
        onConfirm={handleDeleteConfirm}
        confirmText={deleteMutation.isPending ? "Deleting..." : "Delete"}
      />

      <SavingOverlay active={mode === "edit" && isSaving} />
      {unsavedChangesDialog}
    </div>
  );
}
