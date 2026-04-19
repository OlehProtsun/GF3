import { useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  buildActiveAvailabilityBindMap,
  normalizeBindKey,
  type AvailabilityBind,
  type SaveAvailabilityBindInput,
  useAvailabilityBindsListQuery,
  useCreateAvailabilityBindMutation,
  useDeleteAvailabilityBindMutation,
  useUpdateAvailabilityBindMutation,
} from "@entities/availability-binds";
import {
  buildAvailabilityCellMap,
  clampAvailabilityMonth,
  clampAvailabilityYear,
  getAvailabilityCellKey,
  parseAvailabilityCode,
  sanitizeAvailabilityCellMap,
  type AvailabilityMatrixCellMap,
  useAvailabilityGroupByIdQuery,
  useAvailabilityGroupMembersQuery,
  useAvailabilityGroupSlotsQuery,
  useSaveAvailabilityGroupGraphMutation,
} from "@entities/availability-groups";
import { AvailabilityGroupEditor } from "@entities/availability-groups/ui";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { stableSerialize } from "@shared/lib/stableSerialize";
import { useSyncedDraft } from "@shared/lib/useSyncedDraft";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import { SavingOverlay } from "@shared/ui/SavingOverlay";
import styles from "./AvailabilityEditPage.module.css";

type EditableAvailabilityBind = {
  clientId: string;
  id: number | null;
  key: string;
  value: string;
  isActive: boolean;
  persistedKey: string;
  persistedValue: string;
  persistedIsActive: boolean;
};

type AvailabilityEditorInformationErrors = {
  name?: string;
  month?: string;
  year?: string;
};

type AvailabilityEditorState = {
  name: string;
  month: number;
  year: number;
  selectedEmployeeId: number | null;
  selectedEmployeeIds: number[];
  cellMap: AvailabilityMatrixCellMap;
  cellErrors: Record<string, string>;
  informationErrors: AvailabilityEditorInformationErrors;
  employeeError?: string;
  editorError?: string;
};

type AvailabilityGroupSource = {
  name: string;
  month: number;
  year: number;
} | null | undefined;

type AvailabilityMembersSource = Parameters<typeof buildAvailabilityCellMap>[0] | null | undefined;
type AvailabilitySlotsSource = Parameters<typeof buildAvailabilityCellMap>[1] | null | undefined;

type CompactSizeHeaderToggleProps = {
  checked: boolean;
  onToggle: () => void;
};

function joinClassNames(...values: Array<string | false | undefined>) {
  return values.filter(Boolean).join(" ");
}

function toErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message;
  }

  return "Something went wrong while saving this availability group.";
}

function getDefaultDateParts() {
  const today = new Date();
  return {
    month: clampAvailabilityMonth(today.getMonth() + 1),
    year: clampAvailabilityYear(today.getFullYear()),
  };
}

function toEditableAvailabilityBind(bind: AvailabilityBind): EditableAvailabilityBind {
  return {
    clientId: `bind-${bind.id}`,
    id: bind.id,
    key: bind.key,
    value: bind.value,
    isActive: bind.isActive,
    persistedKey: bind.key,
    persistedValue: bind.value,
    persistedIsActive: bind.isActive,
  };
}

function createDraftAvailabilityBind(): EditableAvailabilityBind {
  return {
    clientId: `draft-${crypto.randomUUID()}`,
    id: null,
    key: "",
    value: "",
    isActive: true,
    persistedKey: "",
    persistedValue: "",
    persistedIsActive: true,
  };
}

function runMutation<TData, TVariables>(
  mutate: (variables: TVariables, callbacks?: { onSuccess?: (data: TData) => void; onError?: (error: unknown) => void }) => void,
  variables: TVariables,
) {
  return new Promise<TData>((resolve, reject) => {
    mutate(variables, {
      onSuccess: resolve,
      onError: reject,
    });
  });
}

function reorderEmployeeIds(employeeIds: number[], sourceEmployeeId: number, targetEmployeeId: number) {
  const sourceIndex = employeeIds.indexOf(sourceEmployeeId);
  const targetIndex = employeeIds.indexOf(targetEmployeeId);

  if (sourceIndex < 0 || targetIndex < 0 || sourceIndex === targetIndex) {
    return employeeIds;
  }

  const nextEmployeeIds = [...employeeIds];
  const [movedEmployeeId] = nextEmployeeIds.splice(sourceIndex, 1);
  nextEmployeeIds.splice(targetIndex, 0, movedEmployeeId);
  return nextEmployeeIds;
}

function sanitizeAvailabilityEditorMatrices(
  cellMap: AvailabilityMatrixCellMap,
  cellErrors: Record<string, string>,
  selectedEmployeeIds: number[],
  year: number,
  month: number,
) {
  return {
    cellMap: sanitizeAvailabilityCellMap(cellMap, selectedEmployeeIds, year, month),
    cellErrors: sanitizeAvailabilityCellMap(cellErrors, selectedEmployeeIds, year, month),
  };
}

function createAvailabilityEditorState({
  defaults,
  isCreate,
  group,
  members,
  slots,
}: {
  defaults: ReturnType<typeof getDefaultDateParts>;
  isCreate: boolean;
  group: AvailabilityGroupSource;
  members: AvailabilityMembersSource;
  slots: AvailabilitySlotsSource;
}): AvailabilityEditorState {
  if (isCreate || !group || !members || !slots) {
    return {
      name: "",
      month: defaults.month,
      year: defaults.year,
      selectedEmployeeId: null,
      selectedEmployeeIds: [],
      cellMap: {},
      cellErrors: {},
      informationErrors: {},
      employeeError: undefined,
      editorError: undefined,
    };
  }

  const selectedEmployeeIds = members.map(member => member.employeeId);
  const hydratedCellMap = buildAvailabilityCellMap(members, slots);

  return {
    name: group.name,
    month: group.month,
    year: group.year,
    selectedEmployeeId: selectedEmployeeIds[0] ?? null,
    selectedEmployeeIds,
    cellMap: sanitizeAvailabilityCellMap(hydratedCellMap, selectedEmployeeIds, group.year, group.month),
    cellErrors: {},
    informationErrors: {},
    employeeError: undefined,
    editorError: undefined,
  };
}

function buildAvailabilityEditorSourceKey({
  defaults,
  isCreate,
  groupId,
  group,
  members,
  slots,
}: {
  defaults: ReturnType<typeof getDefaultDateParts>;
  isCreate: boolean;
  groupId: number | null;
  group: AvailabilityGroupSource;
  members: AvailabilityMembersSource;
  slots: AvailabilitySlotsSource;
}) {
  if (isCreate) {
    return `create:${defaults.month}:${defaults.year}`;
  }

  if (!groupId || !group || !members || !slots) {
    return `loading:${groupId ?? "new"}`;
  }

  const memberKey = members.map(member => `${member.id}:${member.employeeId}:${member.displayOrder}`).join("|");
  const slotKey = slots
    .map(slot => `${slot.id}:${slot.availabilityGroupMemberId}:${slot.dayOfMonth}:${slot.kind}:${slot.intervalStr ?? ""}`)
    .join("|");

  return `group:${groupId}:${group.name}:${group.month}:${group.year}:${memberKey}:${slotKey}`;
}

function buildAvailabilityEditorSnapshot({
  name,
  month,
  year,
  selectedEmployeeIds,
  cellMap,
}: Pick<AvailabilityEditorState, "name" | "month" | "year" | "selectedEmployeeIds" | "cellMap">) {
  const sanitizedCellMap = sanitizeAvailabilityCellMap(cellMap, selectedEmployeeIds, year, month);

  return stableSerialize({
    name,
    month,
    year,
    selectedEmployeeIds,
    cellMap: Object.entries(sanitizedCellMap).sort(([leftKey], [rightKey]) => leftKey.localeCompare(rightKey)),
  });
}

function hasPendingBindDraftChanges(bindRows: EditableAvailabilityBind[]) {
  return bindRows.some(bind => {
    if (bind.id === null) {
      return bind.key.trim().length > 0 || bind.value.trim().length > 0 || bind.isActive !== true;
    }

    return (
      bind.key !== bind.persistedKey ||
      bind.value !== bind.persistedValue ||
      bind.isActive !== bind.persistedIsActive
    );
  });
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

export function AvailabilityEditPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { availabilityId } = useParams<{ availabilityId: string }>();
  const parsedId = availabilityId ? Number(availabilityId) : null;
  const isCreate = !availabilityId;
  const groupId = !isCreate && Number.isFinite(parsedId) ? parsedId : null;
  const defaults = useMemo(() => getDefaultDateParts(), []);

  const groupQuery = useAvailabilityGroupByIdQuery(groupId);
  const membersQuery = useAvailabilityGroupMembersQuery(groupId);
  const slotsQuery = useAvailabilityGroupSlotsQuery(groupId);
  const employeesQuery = useEmployeesListQuery({ refreshKey: location.key });
  const bindsQuery = useAvailabilityBindsListQuery();
  const saveMutation = useSaveAvailabilityGroupGraphMutation();
  const createBindMutation = useCreateAvailabilityBindMutation();
  const updateBindMutation = useUpdateAvailabilityBindMutation();
  const deleteBindMutation = useDeleteAvailabilityBindMutation();

  const editorSourceKey = useMemo(
    () => buildAvailabilityEditorSourceKey({
      defaults,
      isCreate,
      groupId,
      group: groupQuery.data,
      members: membersQuery.data,
      slots: slotsQuery.data,
    }),
    [defaults, groupId, groupQuery.data, isCreate, membersQuery.data, slotsQuery.data],
  );
  const initialEditorState = useMemo(
    () => createAvailabilityEditorState({
      defaults,
      isCreate,
      group: groupQuery.data,
      members: membersQuery.data,
      slots: slotsQuery.data,
    }),
    [defaults, groupQuery.data, isCreate, membersQuery.data, slotsQuery.data],
  );
  const {
    value: editorState,
    setValue: setEditorState,
  } = useSyncedDraft(editorSourceKey, initialEditorState);
  const {
    name,
    month,
    year,
    selectedEmployeeId,
    selectedEmployeeIds,
    cellMap,
    cellErrors,
    informationErrors,
    employeeError,
    editorError,
  } = editorState;
  const [bindError, setBindError] = useState<string | undefined>();
  const remoteBindRows = useMemo(
    () => (bindsQuery.data ?? []).map(toEditableAvailabilityBind),
    [bindsQuery.data],
  );
  const [localBindRows, setLocalBindRows] = useState<EditableAvailabilityBind[] | null>(null);
  const bindRows = localBindRows ?? remoteBindRows;
  const [selectedBindClientId, setSelectedBindClientId] = useState<string | null>(null);
  const [bindDeleteTarget, setBindDeleteTarget] = useState<EditableAvailabilityBind | null>(null);
  const [isHeaderCollapsed, setIsHeaderCollapsed] = useState(false);
  const [isCompactMatrix, setIsCompactMatrix] = useState(false);

  const employees = useMemo(() => employeesQuery.data ?? [], [employeesQuery.data]);
  const employeeNameById = useMemo(() => {
    return new Map(employees.map(employee => [employee.id, getEmployeeFullName(employee)]));
  }, [employees]);
  const resolvedSelectedBindClientId =
    selectedBindClientId && bindRows.some(bind => bind.clientId === selectedBindClientId)
      ? selectedBindClientId
      : bindRows[0]?.clientId ?? null;

  const existingMemberByEmployeeId = useMemo(() => {
    return new Map((membersQuery.data ?? []).map(member => [member.employeeId, member]));
  }, [membersQuery.data]);

  const columns = useMemo(() => {
    return selectedEmployeeIds.map((employeeId, index) => {
      const member = existingMemberByEmployeeId.get(employeeId);

      return {
        employeeId,
        memberId: member?.id ?? null,
        displayOrder: member?.displayOrder ?? index,
        label: employeeNameById.get(employeeId) ?? `Employee #${employeeId}`,
      };
    });
  }, [employeeNameById, existingMemberByEmployeeId, selectedEmployeeIds]);

  const assignedEmployees = useMemo(() => {
    return columns.map(column => ({ id: column.employeeId, label: column.label }));
  }, [columns]);

  const selectedBindRow = useMemo(
    () => bindRows.find(bind => bind.clientId === resolvedSelectedBindClientId) ?? null,
    [bindRows, resolvedSelectedBindClientId],
  );

  const activeBindValueByKey = useMemo(() => buildActiveAvailabilityBindMap(bindRows), [bindRows]);
  const bindDeleteLabel = bindDeleteTarget?.key.trim() || "this bind";

  const backTo = isCreate ? "/availability" : `/availability/${groupId}`;
  const hasGroupLoadError = (groupQuery.isError || Boolean(groupQuery.error)) && !groupQuery.data;
  const hasMembersLoadError = (membersQuery.isError || Boolean(membersQuery.error)) && !membersQuery.data;
  const hasSlotsLoadError = (slotsQuery.isError || Boolean(slotsQuery.error)) && !slotsQuery.data;
  const isLoading = !isCreate && (!groupQuery.data || !membersQuery.data || !slotsQuery.data) && (
    groupQuery.isLoading || membersQuery.isLoading || slotsQuery.isLoading || groupQuery.isFetching || membersQuery.isFetching || slotsQuery.isFetching
  );
  const hasLoadError =
    !isCreate &&
    (!Number.isFinite(parsedId) || (!isLoading && (hasGroupLoadError || hasMembersLoadError || hasSlotsLoadError)));
  const initialEditorSnapshot = useMemo(
    () => buildAvailabilityEditorSnapshot(initialEditorState),
    [initialEditorState],
  );
  const currentEditorSnapshot = useMemo(
    () =>
      buildAvailabilityEditorSnapshot({
        name,
        month,
        year,
        selectedEmployeeIds,
        cellMap,
      }),
    [cellMap, month, name, selectedEmployeeIds, year],
  );
  const hasUnsavedChanges = useMemo(() => {
    if (isLoading || hasLoadError) {
      return false;
    }

    const hasEditorChanges = currentEditorSnapshot !== initialEditorSnapshot;
    const hasPendingBindChanges = hasPendingBindDraftChanges(bindRows);

    if (isCreate) {
      return hasEditorChanges || hasPendingBindChanges;
    }

    return Boolean(groupQuery.data && membersQuery.data && slotsQuery.data) && (hasEditorChanges || hasPendingBindChanges);
  }, [
    bindRows,
    currentEditorSnapshot,
    groupQuery.data,
    hasLoadError,
    initialEditorSnapshot,
    isCreate,
    isLoading,
    membersQuery.data,
    slotsQuery.data,
  ]);
  const {
    dialog: unsavedChangesDialog,
    runWithoutPrompt,
  } = useUnsavedChangesPrompt({
    when: hasUnsavedChanges && !saveMutation.isPending,
  });

  const updateBindRows = (nextRows: EditableAvailabilityBind[]) => {
    setLocalBindRows(nextRows);
  };

  const replaceBindRow = (clientId: string, updater: (row: EditableAvailabilityBind) => EditableAvailabilityBind) => {
    setLocalBindRows(currentRows => {
      const baseRows = currentRows ?? bindRows;
      return baseRows.map(row => (row.clientId === clientId ? updater(row) : row));
    });
  };

  const removeBindRowLocally = (clientId: string) => {
    const currentIndex = bindRows.findIndex(bind => bind.clientId === clientId);
    const nextRows = bindRows.filter(bind => bind.clientId !== clientId);

    updateBindRows(nextRows);

    if (resolvedSelectedBindClientId !== clientId) {
      return;
    }

    const nextSelectedBind = nextRows[currentIndex] ?? nextRows[currentIndex - 1] ?? nextRows[0] ?? null;
    setSelectedBindClientId(nextSelectedBind?.clientId ?? null);
  };

  const handleAddEmployee = () => {
    setEditorState((current) => {
      if (!current.selectedEmployeeId) {
        return {
          ...current,
          employeeError: undefined,
          editorError: "Select employee first.",
        };
      }

      if (current.selectedEmployeeIds.includes(current.selectedEmployeeId)) {
        return {
          ...current,
          employeeError: undefined,
          editorError: "This employee is already added.",
        };
      }

      const nextSelectedEmployeeIds = [...current.selectedEmployeeIds, current.selectedEmployeeId];

      return {
        ...current,
        selectedEmployeeIds: nextSelectedEmployeeIds,
        employeeError: undefined,
        editorError: undefined,
        ...sanitizeAvailabilityEditorMatrices(
          current.cellMap,
          current.cellErrors,
          nextSelectedEmployeeIds,
          current.year,
          current.month,
        ),
      };
    });
  };

  const handleRemoveEmployee = () => {
    setEditorState((current) => {
      if (!current.selectedEmployeeId) {
        return {
          ...current,
          employeeError: undefined,
          editorError: "Select employee first.",
        };
      }

      if (!current.selectedEmployeeIds.includes(current.selectedEmployeeId)) {
        return {
          ...current,
          employeeError: undefined,
          editorError: "This employee is not in the group.",
        };
      }

      const nextSelectedEmployeeIds = current.selectedEmployeeIds.filter(
        employeeId => employeeId !== current.selectedEmployeeId,
      );

      return {
        ...current,
        selectedEmployeeId: null,
        selectedEmployeeIds: nextSelectedEmployeeIds,
        employeeError: undefined,
        editorError: undefined,
        ...sanitizeAvailabilityEditorMatrices(
          current.cellMap,
          current.cellErrors,
          nextSelectedEmployeeIds,
          current.year,
          current.month,
        ),
      };
    });
  };

  const handleCellChange = (employeeId: number, dayOfMonth: number, value: string) => {
    const cellKey = getAvailabilityCellKey(employeeId, dayOfMonth);

    setEditorState((current) => {
      const nextErrors = current.cellErrors[cellKey]
        ? Object.fromEntries(Object.entries(current.cellErrors).filter(([key]) => key !== cellKey))
        : current.cellErrors;

      return {
        ...current,
        cellMap: {
          ...current.cellMap,
          [cellKey]: value,
        },
        cellErrors: nextErrors,
      };
    });
  };

  const handleColumnMove = (sourceEmployeeId: number, targetEmployeeId: number) => {
    setEditorState((current) => ({
      ...current,
      selectedEmployeeIds: reorderEmployeeIds(current.selectedEmployeeIds, sourceEmployeeId, targetEmployeeId),
      editorError: undefined,
    }));
  };

  const handleBindFieldChange = (clientId: string, patch: Partial<Pick<EditableAvailabilityBind, "key" | "value" | "isActive">>) => {
    setBindError(undefined);
    replaceBindRow(clientId, row => ({ ...row, ...patch }));
  };

  const handleBindCommit = (clientId: string) => {
    const bind = bindRows.find(item => item.clientId === clientId);
    if (!bind) {
      return;
    }

    const trimmedKey = bind.key.trim();
    const trimmedValue = bind.value.trim();

    if (!trimmedKey && !trimmedValue) {
      return;
    }

    if (!trimmedKey || !trimmedValue) {
      setBindError("Bind key and value are required before the bind can be saved.");
      return;
    }

    const normalizedKey = normalizeBindKey(trimmedKey);
    if (!normalizedKey) {
      setBindError("Invalid bind key format.");
      return;
    }

    const duplicateBindExists = bindRows.some(item => item.clientId !== clientId && normalizeBindKey(item.key) === normalizedKey);
    if (duplicateBindExists) {
      setBindError(`Bind '${normalizedKey}' already exists.`);
      return;
    }

    const payload: SaveAvailabilityBindInput = {
      key: normalizedKey,
      value: trimmedValue,
      isActive: bind.isActive,
    };

    const isUnchanged =
      bind.id !== null &&
      bind.persistedKey === payload.key &&
      bind.persistedValue === payload.value &&
      bind.persistedIsActive === payload.isActive;

    if (isUnchanged) {
      replaceBindRow(clientId, row => ({
        ...row,
        key: payload.key,
        value: payload.value,
      }));
      return;
    }

    setBindError(undefined);

    if (bind.id === null) {
      void runMutation(createBindMutation.mutate, payload)
        .then(createdBind => {
          const nextBind = toEditableAvailabilityBind(createdBind);
          setLocalBindRows(currentRows => {
            const baseRows = currentRows ?? bindRows;
            return baseRows.map(row => (row.clientId === clientId ? nextBind : row));
          });
          setSelectedBindClientId(currentSelection => (currentSelection === clientId ? nextBind.clientId : currentSelection));
        })
        .catch(error => {
          setBindError(toErrorMessage(error));
        });
      return;
    }

    void runMutation(updateBindMutation.mutate, { id: bind.id, payload })
      .then(() => {
        replaceBindRow(clientId, row => ({
          ...row,
          key: payload.key,
          value: payload.value,
          isActive: payload.isActive,
          persistedKey: payload.key,
          persistedValue: payload.value,
          persistedIsActive: payload.isActive,
        }));
      })
      .catch(error => {
        setBindError(toErrorMessage(error));
      });
  };

  const handleAddBind = () => {
    const nextBind = createDraftAvailabilityBind();
    setBindError(undefined);
    updateBindRows([...bindRows, nextBind]);
    setSelectedBindClientId(nextBind.clientId);
  };

  const handleDeleteBind = () => {
    setBindError(undefined);

    if (!selectedBindRow) {
      setBindError("Select bind first.");
      return;
    }

    setBindDeleteTarget(selectedBindRow);
  };

  const handleDeleteBindConfirm = () => {
    if (!bindDeleteTarget) {
      return;
    }

    setBindError(undefined);

    if (bindDeleteTarget.id === null) {
      removeBindRowLocally(bindDeleteTarget.clientId);
      setBindDeleteTarget(null);
      return;
    }

    void runMutation(deleteBindMutation.mutate, bindDeleteTarget.id)
      .then(() => {
        removeBindRowLocally(bindDeleteTarget.clientId);
        setBindDeleteTarget(null);
      })
      .catch(error => {
        setBindError(toErrorMessage(error));
      });
  };

  const handleSave = () => {
    const trimmedName = name.trim();
    const nextInformationErrors: AvailabilityEditorInformationErrors = {};
    const nextEmployeeError = selectedEmployeeIds.length === 0 ? "Add at least one employee to the group." : undefined;

    if (!trimmedName) {
      nextInformationErrors.name = "Availability name is required.";
    }

    const nextCellErrors: Record<string, string> = {};
    columns.forEach(column => {
      const daysInMonth = new Date(year, month, 0).getDate();

      for (let dayOfMonth = 1; dayOfMonth <= daysInMonth; dayOfMonth += 1) {
        const cellKey = getAvailabilityCellKey(column.employeeId, dayOfMonth);
        const rawCode = cellMap[cellKey] ?? "-";
        const parsedCode = parseAvailabilityCode(rawCode);

        if (!parsedCode.ok) {
          nextCellErrors[cellKey] = parsedCode.error;
        }
      }
    });

    setEditorState((current) => ({
      ...current,
      informationErrors: nextInformationErrors,
      employeeError: nextEmployeeError,
      cellErrors: nextCellErrors,
    }));

    if (Object.keys(nextInformationErrors).length > 0 || nextEmployeeError || Object.keys(nextCellErrors).length > 0) {
      setEditorState((current) => ({
        ...current,
        editorError: "Check highlighted fields before saving.",
      }));
      return;
    }

    setEditorState((current) => ({
      ...current,
      editorError: undefined,
    }));

    saveMutation.mutate(
      {
        id: groupId,
        payload: {
          name: trimmedName,
          month,
          year,
        },
        employeeIds: selectedEmployeeIds,
        cellMap,
        existingMembers: membersQuery.data ?? [],
        existingSlots: slotsQuery.data ?? [],
      },
      {
        onSuccess: result => {
          runWithoutPrompt(() => navigate(`/availability/${result.id}`));
        },
        onError: error => {
          setEditorState((current) => ({
            ...current,
            editorError: toErrorMessage(error),
          }));
        },
      }
    );
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title={isCreate ? "Add Availability" : "Availability Edit"}
        subtitle={isCreate ? "Create a new monthly availability schedule" : "Update availability information, employees and day codes"}
        backTo={backTo}
        onCollapseChange={setIsHeaderCollapsed}
        rightSlot={(
          <CompactSizeHeaderToggle
            checked={isCompactMatrix}
            onToggle={() => setIsCompactMatrix(current => !current)}
          />
        )}
      />

      <AvailabilityGroupEditor
        name={name}
        month={month}
        year={year}
        isHeaderCollapsed={isHeaderCollapsed}
        compactSize={isCompactMatrix}
        informationErrors={informationErrors}
        employeeError={employeeError}
        employees={employees}
        selectedEmployeeId={selectedEmployeeId}
        assignedEmployees={assignedEmployees}
        columns={columns}
        cellMap={cellMap}
        cellErrors={cellErrors}
        binds={bindRows}
        selectedBindClientId={resolvedSelectedBindClientId}
        bindValueByKey={activeBindValueByKey}
        isLoading={isLoading}
        hasLoadError={hasLoadError}
        isSaving={saveMutation.isPending}
        isBindsLoading={bindsQuery.isLoading && bindRows.length === 0}
        isBindBusy={createBindMutation.isPending || updateBindMutation.isPending || deleteBindMutation.isPending}
        errorMessage={editorError}
        bindErrorMessage={bindError ?? (bindsQuery.isError && bindRows.length === 0 ? "Could not load bind information." : undefined)}
        onNameChange={value => {
          setEditorState((current) => {
            const nextInformationErrors = { ...current.informationErrors };
            delete nextInformationErrors.name;

            return {
              ...current,
              name: value,
              informationErrors: nextInformationErrors,
              editorError: undefined,
            };
          });
        }}
        onMonthChange={value => {
          const nextMonth = clampAvailabilityMonth(value);

          setEditorState((current) => {
            const nextInformationErrors = { ...current.informationErrors };
            delete nextInformationErrors.month;

            return {
              ...current,
              month: nextMonth,
              informationErrors: nextInformationErrors,
              editorError: undefined,
              ...sanitizeAvailabilityEditorMatrices(
                current.cellMap,
                current.cellErrors,
                current.selectedEmployeeIds,
                current.year,
                nextMonth,
              ),
            };
          });
        }}
        onYearChange={value => {
          const nextYear = clampAvailabilityYear(value);

          setEditorState((current) => {
            const nextInformationErrors = { ...current.informationErrors };
            delete nextInformationErrors.year;

            return {
              ...current,
              year: nextYear,
              informationErrors: nextInformationErrors,
              editorError: undefined,
              ...sanitizeAvailabilityEditorMatrices(
                current.cellMap,
                current.cellErrors,
                current.selectedEmployeeIds,
                nextYear,
                current.month,
              ),
            };
          });
        }}
        onSelectedEmployeeIdChange={value => {
          setEditorState((current) => ({
            ...current,
            selectedEmployeeId: value,
            editorError: undefined,
          }));
        }}
        onSelectedBindChange={clientId => {
          setSelectedBindClientId(clientId);
          setBindError(undefined);
        }}
        onBindFieldChange={handleBindFieldChange}
        onBindCommit={handleBindCommit}
        onAddEmployee={handleAddEmployee}
        onRemoveEmployee={handleRemoveEmployee}
        onAddBind={handleAddBind}
        onDeleteBind={handleDeleteBind}
        onColumnMove={handleColumnMove}
        onCellChange={handleCellChange}
        onSave={handleSave}
      />

      <ConfirmDialog
        open={bindDeleteTarget !== null}
        title="Delete bind"
        message={`Are you sure you want to delete '${bindDeleteLabel}' from the bind list?`}
        onCancel={() => setBindDeleteTarget(null)}
        onConfirm={handleDeleteBindConfirm}
        confirmText={deleteBindMutation.isPending ? "Deleting..." : "Delete"}
        confirmDisabled={deleteBindMutation.isPending}
        cancelDisabled={deleteBindMutation.isPending}
      />

      <SavingOverlay active={saveMutation.isPending} />
      {unsavedChangesDialog}
    </div>
  );
}



