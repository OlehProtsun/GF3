import { useEffect, useMemo, useState, type ChangeEvent, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAvailabilityGroupsListQuery } from "@entities/availability-groups";
import type { ContainerGraphRecords, Graph } from "@entities/containers";
import {
  ContainerDetailsForm,
  ContainerGraphDetailsForm,
  ContainerListCard,
  ContainerProfileWorkspace,
  buildContainerGraphSummaries,
  buildContainerStatistics,
  filterGraphSummaries,
  containersApi,
  matchesContainerSearch,
  type ContainerGraphFormErrors,
  type ContainerGraphFormFieldElement,
  type ContainerGraphFormState,
  useContainerByIdQuery,
  useContainerForm,
  useContainerGraphsQuery,
  useContainersListQuery,
  useCreateContainerMutation,
  useCreateGraphMutation,
  useDeleteContainerMutation,
  useUpdateContainerMutation,
} from "@entities/containers";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import { useShopsListQuery } from "@entities/shops/api/queries";
import { ApiError } from "@shared/api/httpClient";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { IosButton } from "@shared/ui/components/IosButton";
import { PlusIcon } from "@shared/ui/icons";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./ContainerPage.module.css";

type ContainerPageMode = "list" | "profile" | "edit" | "graphCreate";

type GraphRecordsState = {
  recordsByGraphId: Record<number, ContainerGraphRecords>;
  isLoading: boolean;
  error: unknown;
};

const SHIFT_TIME_PATTERN = /^(\d{1,2}):(\d{2})\s*-\s*(\d{1,2}):(\d{2})$/;

function createInitialGraphForm(defaultShopId?: number | null): ContainerGraphFormState {
  const today = new Date();

  return {
    name: "",
    shopId: defaultShopId ? String(defaultShopId) : "",
    year: String(today.getFullYear()),
    month: String(today.getMonth() + 1),
    peoplePerShift: "1",
    shift1Time: "06:00 - 14:00",
    shift2Time: "14:00 - 22:00",
    maxHoursPerEmpMonth: "160",
    maxConsecutiveDays: "5",
    maxConsecutiveFull: "3",
    maxFullPerMonth: "10",
    availabilityGroupId: "",
    note: "",
  };
}

function parseIntegerField(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }

  const parsed = Number(trimmed);
  return Number.isInteger(parsed) ? parsed : null;
}

function isValidShiftTime(value: string) {
  const match = value.trim().match(SHIFT_TIME_PATTERN);
  if (!match) {
    return false;
  }

  const fromHour = Number(match[1]);
  const fromMinute = Number(match[2]);
  const toHour = Number(match[3]);
  const toMinute = Number(match[4]);

  if (
    Number.isNaN(fromHour) ||
    Number.isNaN(fromMinute) ||
    Number.isNaN(toHour) ||
    Number.isNaN(toMinute) ||
    fromHour > 23 ||
    toHour > 23 ||
    fromMinute > 59 ||
    toMinute > 59
  ) {
    return false;
  }

  const fromTotalMinutes = fromHour * 60 + fromMinute;
  const toTotalMinutes = toHour * 60 + toMinute;
  return toTotalMinutes > fromTotalMinutes;
}

function buildGraphFormErrors(
  form: ContainerGraphFormState,
  availableShopIds: Set<number>,
  availableAvailabilityGroupIds: Set<number>,
): ContainerGraphFormErrors {
  const errors: ContainerGraphFormErrors = {};
  const shopId = parseIntegerField(form.shopId);
  const month = parseIntegerField(form.month);
  const year = parseIntegerField(form.year);
  const peoplePerShift = parseIntegerField(form.peoplePerShift);
  const maxHoursPerEmpMonth = parseIntegerField(form.maxHoursPerEmpMonth);
  const maxConsecutiveDays = parseIntegerField(form.maxConsecutiveDays);
  const maxConsecutiveFull = parseIntegerField(form.maxConsecutiveFull);
  const maxFullPerMonth = parseIntegerField(form.maxFullPerMonth);
  const availabilityGroupId = form.availabilityGroupId ? parseIntegerField(form.availabilityGroupId) : null;

  if (!form.name.trim()) {
    errors.name = "Name is required.";
  }

  if (availableShopIds.size === 0) {
    errors.shopId = "Create at least one shop before adding a schedule.";
  } else if (shopId === null || !availableShopIds.has(shopId)) {
    errors.shopId = "Select a valid shop.";
  }

  if (month === null || month < 1 || month > 12) {
    errors.month = "Month must be between 1 and 12.";
  }

  if (year === null || year < 2000 || year > 2100) {
    errors.year = "Enter a valid year.";
  }

  if (peoplePerShift === null || peoplePerShift < 1) {
    errors.peoplePerShift = "People per shift must be at least 1.";
  }

  if (!isValidShiftTime(form.shift1Time)) {
    errors.shift1Time = "Use HH:mm - HH:mm format.";
  }

  if (!isValidShiftTime(form.shift2Time)) {
    errors.shift2Time = "Use HH:mm - HH:mm format.";
  }

  if (maxHoursPerEmpMonth === null || maxHoursPerEmpMonth < 1) {
    errors.maxHoursPerEmpMonth = "Max hours must be at least 1.";
  }

  if (maxConsecutiveDays === null || maxConsecutiveDays < 0) {
    errors.maxConsecutiveDays = "Use 0 or more.";
  }

  if (maxConsecutiveFull === null || maxConsecutiveFull < 0) {
    errors.maxConsecutiveFull = "Use 0 or more.";
  }

  if (maxFullPerMonth === null || maxFullPerMonth < 0) {
    errors.maxFullPerMonth = "Use 0 or more.";
  }

  if (
    availabilityGroupId !== null &&
    (availabilityGroupId <= 0 || !availableAvailabilityGroupIds.has(availabilityGroupId))
  ) {
    errors.availabilityGroupId = "Select a valid availability group.";
  }

  return errors;
}

function useContainerGraphRecords(containerId: number | null, graphs: Graph[], enabled: boolean): GraphRecordsState {
  const [state, setState] = useState<GraphRecordsState>({
    recordsByGraphId: {},
    isLoading: false,
    error: null,
  });

  const graphIdsKey = useMemo(() => graphs.map(graph => graph.id).sort((left, right) => left - right).join(","), [graphs]);

  useEffect(() => {
    if (!enabled || !containerId) {
      setState({ recordsByGraphId: {}, isLoading: false, error: null });
      return;
    }

    if (graphs.length === 0) {
      setState({ recordsByGraphId: {}, isLoading: false, error: null });
      return;
    }

    let isActive = true;
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    Promise.all(
      graphs.map(async graph => {
        const [employees, slots] = await Promise.all([
          containersApi.listGraphEmployees(containerId, graph.id),
          containersApi.listGraphSlots(containerId, graph.id),
        ]);

        return {
          graphId: graph.id,
          employees,
          slots,
        };
      }),
    )
      .then(results => {
        if (!isActive) {
          return;
        }

        const recordsByGraphId = results.reduce<Record<number, ContainerGraphRecords>>((accumulator, result) => {
          accumulator[result.graphId] = {
            employees: result.employees,
            slots: result.slots,
          };
          return accumulator;
        }, {});

        setState({ recordsByGraphId, isLoading: false, error: null });
      })
      .catch(error => {
        if (!isActive) {
          return;
        }

        setState({ recordsByGraphId: {}, isLoading: false, error });
      });

    return () => {
      isActive = false;
    };
  }, [containerId, enabled, graphIdsKey]);

  return state;
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
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [graphForm, setGraphForm] = useState<ContainerGraphFormState>(() => createInitialGraphForm());
  const [graphErrors, setGraphErrors] = useState<ContainerGraphFormErrors>({});
  const [graphSubmitError, setGraphSubmitError] = useState<string | null>(null);

  usePageScrollbarHidden(mode === "list" || mode === "profile");

  const containersQuery = useContainersListQuery();
  const availabilityGroupsQuery = useAvailabilityGroupsListQuery();
  const employeesQuery = useEmployeesListQuery();
  const shopsQuery = useShopsListQuery();
  const createMutation = useCreateContainerMutation();
  const createGraphMutation = useCreateGraphMutation();
  const updateMutation = useUpdateContainerMutation();
  const deleteMutation = useDeleteContainerMutation();

  const profileContainerId = mode === "profile" || mode === "graphCreate" ? selectedContainerId : null;
  const editContainerId = mode === "edit" && editingContainerId !== null ? editingContainerId : null;

  const profileContainerQuery = useContainerByIdQuery(profileContainerId);
  const editContainerQuery = useContainerByIdQuery(editContainerId);
  const graphsQuery = useContainerGraphsQuery(mode === "profile" ? profileContainerId : null);

  const allContainers = containersQuery.data ?? [];
  const filteredContainers = useMemo(
    () => allContainers.filter(container => matchesContainerSearch(container, listSearchQuery)),
    [allContainers, listSearchQuery],
  );

  const selectedContainerFromList = useMemo(
    () => allContainers.find(container => container.id === selectedContainerId) ?? null,
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
    () => new Map((employeesQuery.data ?? []).map(employee => [employee.id, employee])),
    [employeesQuery.data],
  );
  const shopsById = useMemo(
    () => new Map((shopsQuery.data ?? []).map(shop => [shop.id, shop])),
    [shopsQuery.data],
  );
  const shopIdSet = useMemo(() => new Set((shopsQuery.data ?? []).map(shop => shop.id)), [shopsQuery.data]);

  const graphs = graphsQuery.data ?? [];
  const graphRecordsState = useContainerGraphRecords(profileContainerId, graphs, mode === "profile");

  const graphSummaries = useMemo(
    () => buildContainerGraphSummaries(graphs, graphRecordsState.recordsByGraphId, shopsById),
    [graphRecordsState.recordsByGraphId, graphs, shopsById],
  );
  const filteredGraphSummaries = useMemo(
    () => filterGraphSummaries(graphSummaries, graphSearchQuery),
    [graphSearchQuery, graphSummaries],
  );
  const matchingAvailabilityGroups = useMemo(() => {
    const year = parseIntegerField(graphForm.year);
    const month = parseIntegerField(graphForm.month);

    if (year === null || month === null) {
      return [];
    }

    return (availabilityGroupsQuery.data ?? []).filter(group => group.year === year && group.month === month);
  }, [availabilityGroupsQuery.data, graphForm.month, graphForm.year]);
  const availabilityGroupIdSet = useMemo(
    () => new Set(matchingAvailabilityGroups.map(group => group.id)),
    [matchingAvailabilityGroups],
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
  const isGraphSaving = createGraphMutation.isPending;
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

  useEffect(() => {
    if (location.pathname !== "/container") {
      return;
    }

    const nextContainerId = Number(new URLSearchParams(location.search).get("openContainerId"));
    if (!Number.isInteger(nextContainerId) || nextContainerId <= 0) {
      return;
    }

    setSelectedContainerId(nextContainerId);
    setEditingContainerId(null);
    setGraphSearchQuery("");
    setSubmitError(null);
    setMode("profile");
  }, [location.pathname, location.search]);

  useEffect(() => {
    if (mode !== "profile" || selectedContainerId === null || containersQuery.isLoading) {
      return;
    }

    if (containersQuery.isError) {
      return;
    }

    const stillExists = allContainers.some(container => container.id === selectedContainerId);
    if (!stillExists && !profileContainerQuery.isLoading && !profileContainerQuery.data) {
      setMode("list");
      setSelectedContainerId(null);
      setEditingContainerId(null);
      setGraphSearchQuery("");
    }
  }, [allContainers, containersQuery.isError, containersQuery.isLoading, mode, profileContainerQuery.data, profileContainerQuery.isLoading, selectedContainerId]);

  useEffect(() => {
    if (mode !== "graphCreate" || graphForm.shopId || shopsQuery.isLoading) {
      return;
    }

    const defaultShopId = shopsQuery.data?.[0]?.id;
    if (!defaultShopId) {
      return;
    }

    setGraphForm(previousForm => ({ ...previousForm, shopId: String(defaultShopId) }));
  }, [graphForm.shopId, mode, shopsQuery.data, shopsQuery.isLoading]);

  useEffect(() => {
    if (mode !== "graphCreate" || !graphForm.availabilityGroupId) {
      return;
    }

    const selectedAvailabilityGroupId = Number(graphForm.availabilityGroupId);
    if (availabilityGroupIdSet.has(selectedAvailabilityGroupId)) {
      return;
    }

    setGraphForm(previousForm => ({ ...previousForm, availabilityGroupId: "" }));
  }, [availabilityGroupIdSet, graphForm.availabilityGroupId, mode]);

  const handleOpenProfile = (containerId: number) => {
    setSelectedContainerId(containerId);
    setEditingContainerId(null);
    setGraphSearchQuery("");
    setSubmitError(null);
    setMode("profile");
  };

  const handleStartCreate = () => {
    setSelectedContainerId(null);
    setEditingContainerId(null);
    setSubmitError(null);
    setMode("edit");
  };

  const handleStartEdit = (containerId: number) => {
    setSelectedContainerId(containerId);
    setEditingContainerId(containerId);
    setSubmitError(null);
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
  };

  const handleCancelEdit = () => {
    setSubmitError(null);

    if (editingContainerId !== null) {
      setMode("profile");
      return;
    }

    setMode("list");
  };

  const handleCancelGraphCreate = () => {
    setGraphErrors({});
    setGraphSubmitError(null);
    setMode("profile");
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
        onSuccess: created => {
          setSelectedContainerId(created.id);
          setEditingContainerId(null);
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
          setMode("profile");
        },
        onError: handleMutationError,
      },
    );
  };

  const handleGraphFieldChange =
    (field: keyof ContainerGraphFormState) => (event: ChangeEvent<ContainerGraphFormFieldElement>) => {
      const { value } = event.target;

      setGraphForm(previousForm => ({ ...previousForm, [field]: value }));
      setGraphSubmitError(null);
      setGraphErrors(previousErrors => {
        if (!previousErrors[field]) {
          return previousErrors;
        }

        const nextErrors = { ...previousErrors };
        delete nextErrors[field];
        return nextErrors;
      });
    };

  const handleGraphSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setGraphSubmitError(null);

    const nextErrors = buildGraphFormErrors(graphForm, shopIdSet, availabilityGroupIdSet);
    setGraphErrors(nextErrors);

    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    if (selectedContainerId === null) {
      setGraphSubmitError("Could not determine which container should receive the schedule.");
      return;
    }

    createGraphMutation.mutate(
      {
        containerId: selectedContainerId,
        payload: {
          shopId: Number(graphForm.shopId),
          name: graphForm.name.trim(),
          year: Number(graphForm.year),
          month: Number(graphForm.month),
          peoplePerShift: Number(graphForm.peoplePerShift),
          shift1Time: graphForm.shift1Time.trim(),
          shift2Time: graphForm.shift2Time.trim(),
          maxHoursPerEmpMonth: Number(graphForm.maxHoursPerEmpMonth),
          maxConsecutiveDays: Number(graphForm.maxConsecutiveDays),
          maxConsecutiveFull: Number(graphForm.maxConsecutiveFull),
          maxFullPerMonth: Number(graphForm.maxFullPerMonth),
          note: graphForm.note.trim() || undefined,
          availabilityGroupId: graphForm.availabilityGroupId ? Number(graphForm.availabilityGroupId) : null,
        },
      },
      {
        onSuccess: () => {
          setGraphErrors({});
          setGraphSubmitError(null);
          setGraphSearchQuery("");
          setMode("profile");
        },
        onError: error => {
          if (error instanceof ApiError) {
            setGraphSubmitError(error.message);
            return;
          }

          setGraphSubmitError("Could not save schedule.");
        },
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
      },
    });
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

    if (mode === "graphCreate") {
      return (
        <PageHeader
          title="Add Schedule"
          subtitle="Create a new saved schedule for this container"
          onBack={handleCancelGraphCreate}
        />
      );
    }

    if (mode === "profile") {
      return (
        <PageHeader
          title="Container Profile"
          subtitle="View container details, schedules and aggregate workload statistics"
          onBack={handleBackFromProfile}
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

        {mode === "graphCreate" ? (
          <div className={styles.editShell}>
            <ContainerGraphDetailsForm
              form={graphForm}
              errors={graphErrors}
              shops={shopsQuery.data ?? []}
              availabilityGroups={matchingAvailabilityGroups}
              isOptionsLoading={shopsQuery.isLoading || availabilityGroupsQuery.isLoading}
              optionsError={
                shopsQuery.isError || availabilityGroupsQuery.isError
                  ? "Some supporting data for schedules could not be loaded yet."
                  : null
              }
              isSaving={isGraphSaving}
              submitError={graphSubmitError}
              onFieldChange={handleGraphFieldChange}
              onCancel={handleCancelGraphCreate}
              onSubmit={handleGraphSubmit}
            />
          </div>
        ) : null}

        {mode === "profile" ? (
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
            onGraphSearchChange={setGraphSearchQuery}
            onClearGraphSearch={() => setGraphSearchQuery("")}
            onAddGraph={handleStartGraphCreate}
            onOpenGraph={graphId => {
              if (selectedContainerId !== null) {
                navigate(`/container/${selectedContainerId}/graphs/${graphId}`);
              }
            }}
            onEditContainer={handleStartEdit}
            onDeleteContainer={() => setIsDeleteOpen(true)}
          />
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
    </div>
  );
}



