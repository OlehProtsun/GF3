import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
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
  filterAvailabilityEmployees,
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
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { PageHeader } from "@shared/ui/PageHeader";
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

export function AvailabilityEditPage() {
  const navigate = useNavigate();
  const { availabilityId } = useParams<{ availabilityId: string }>();
  const parsedId = availabilityId ? Number(availabilityId) : null;
  const isCreate = !availabilityId;
  const groupId = !isCreate && Number.isFinite(parsedId) ? parsedId : null;
  const defaults = useMemo(() => getDefaultDateParts(), []);

  const groupQuery = useAvailabilityGroupByIdQuery(groupId);
  const membersQuery = useAvailabilityGroupMembersQuery(groupId);
  const slotsQuery = useAvailabilityGroupSlotsQuery(groupId);
  const employeesQuery = useEmployeesListQuery();
  const bindsQuery = useAvailabilityBindsListQuery();
  const saveMutation = useSaveAvailabilityGroupGraphMutation();
  const createBindMutation = useCreateAvailabilityBindMutation();
  const updateBindMutation = useUpdateAvailabilityBindMutation();
  const deleteBindMutation = useDeleteAvailabilityBindMutation();

  const [name, setName] = useState("");
  const [month, setMonth] = useState(defaults.month);
  const [year, setYear] = useState(defaults.year);
  const [employeeSearchText, setEmployeeSearchText] = useState("");
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | null>(null);
  const [selectedEmployeeIds, setSelectedEmployeeIds] = useState<number[]>([]);
  const [cellMap, setCellMap] = useState<AvailabilityMatrixCellMap>({});
  const [cellErrors, setCellErrors] = useState<Record<string, string>>({});
  const [informationErrors, setInformationErrors] = useState<AvailabilityEditorInformationErrors>({});
  const [employeeError, setEmployeeError] = useState<string | undefined>();
  const [editorError, setEditorError] = useState<string | undefined>();
  const [bindError, setBindError] = useState<string | undefined>();
  const [hydratedKey, setHydratedKey] = useState<string | null>(null);
  const [bindRows, setBindRows] = useState<EditableAvailabilityBind[]>([]);
  const [selectedBindClientId, setSelectedBindClientId] = useState<string | null>(null);
  const [hasLocalBindChanges, setHasLocalBindChanges] = useState(false);
  const [bindDeleteTarget, setBindDeleteTarget] = useState<EditableAvailabilityBind | null>(null);

  const employees = employeesQuery.data ?? [];
  const employeeNameById = useMemo(() => {
    return new Map(employees.map(employee => [employee.id, getEmployeeFullName(employee)]));
  }, [employees]);

  useEffect(() => {
    if (isCreate) {
      if (hydratedKey === "create") {
        return;
      }

      setName("");
      setMonth(defaults.month);
      setYear(defaults.year);
      setEmployeeSearchText("");
      setSelectedEmployeeId(null);
      setSelectedEmployeeIds([]);
      setCellMap({});
      setCellErrors({});
      setInformationErrors({});
      setEmployeeError(undefined);
      setEditorError(undefined);
      setHydratedKey("create");
      return;
    }

    if (!groupId || !groupQuery.data || !membersQuery.data || !slotsQuery.data) {
      return;
    }

    const nextHydratedKey = `${groupId}`;
    if (hydratedKey === nextHydratedKey) {
      return;
    }

    const nextSelectedEmployeeIds = membersQuery.data.map(member => member.employeeId);

    setName(groupQuery.data.name);
    setMonth(groupQuery.data.month);
    setYear(groupQuery.data.year);
    setEmployeeSearchText("");
    setSelectedEmployeeId(nextSelectedEmployeeIds[0] ?? null);
    setSelectedEmployeeIds(nextSelectedEmployeeIds);
    setCellMap(buildAvailabilityCellMap(membersQuery.data, slotsQuery.data));
    setCellErrors({});
    setInformationErrors({});
    setEmployeeError(undefined);
    setEditorError(undefined);
    setHydratedKey(nextHydratedKey);
  }, [
    defaults.month,
    defaults.year,
    groupId,
    groupQuery.data,
    hydratedKey,
    isCreate,
    membersQuery.data,
    slotsQuery.data,
  ]);

  useEffect(() => {
    if (!bindsQuery.data || hasLocalBindChanges) {
      return;
    }

    const nextBindRows = bindsQuery.data.map(toEditableAvailabilityBind);
    setBindRows(nextBindRows);
    setSelectedBindClientId(currentSelection => {
      if (currentSelection && nextBindRows.some(bind => bind.clientId === currentSelection)) {
        return currentSelection;
      }

      return nextBindRows[0]?.clientId ?? null;
    });
  }, [bindsQuery.data, hasLocalBindChanges]);

  useEffect(() => {
    setCellMap(currentCellMap => sanitizeAvailabilityCellMap(currentCellMap, selectedEmployeeIds, year, month));
    setCellErrors(currentErrors => sanitizeAvailabilityCellMap(currentErrors, selectedEmployeeIds, year, month));
  }, [month, selectedEmployeeIds, year]);

  const filteredEmployees = useMemo(() => {
    const baseEmployees = filterAvailabilityEmployees(employees, employeeSearchText);
    if (!selectedEmployeeId || baseEmployees.some(employee => employee.id === selectedEmployeeId)) {
      return baseEmployees;
    }

    const selectedEmployee = employees.find(employee => employee.id === selectedEmployeeId);
    if (!selectedEmployee) {
      return baseEmployees;
    }

    return [selectedEmployee, ...baseEmployees];
  }, [employeeSearchText, employees, selectedEmployeeId]);

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
    () => bindRows.find(bind => bind.clientId === selectedBindClientId) ?? null,
    [bindRows, selectedBindClientId],
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

  const updateBindRows = (nextRows: EditableAvailabilityBind[]) => {
    setHasLocalBindChanges(true);
    setBindRows(nextRows);
  };

  const replaceBindRow = (clientId: string, updater: (row: EditableAvailabilityBind) => EditableAvailabilityBind) => {
    setHasLocalBindChanges(true);
    setBindRows(currentRows => currentRows.map(row => (row.clientId === clientId ? updater(row) : row)));
  };

  const removeBindRowLocally = (clientId: string) => {
    const currentIndex = bindRows.findIndex(bind => bind.clientId === clientId);
    const nextRows = bindRows.filter(bind => bind.clientId !== clientId);

    updateBindRows(nextRows);

    if (selectedBindClientId !== clientId) {
      return;
    }

    const nextSelectedBind = nextRows[currentIndex] ?? nextRows[currentIndex - 1] ?? nextRows[0] ?? null;
    setSelectedBindClientId(nextSelectedBind?.clientId ?? null);
  };

  const clearInformationError = (field: keyof AvailabilityEditorInformationErrors) => {
    setInformationErrors(currentErrors => {
      if (!currentErrors[field]) {
        return currentErrors;
      }

      const nextErrors = { ...currentErrors };
      delete nextErrors[field];
      return nextErrors;
    });
  };

  const handleAddEmployee = () => {
    setEditorError(undefined);
    setEmployeeError(undefined);

    if (!selectedEmployeeId) {
      setEditorError("Select employee first.");
      return;
    }

    if (selectedEmployeeIds.includes(selectedEmployeeId)) {
      setEditorError("This employee is already added.");
      return;
    }

    setSelectedEmployeeIds(currentIds => [...currentIds, selectedEmployeeId]);
  };

  const handleRemoveEmployee = () => {
    setEditorError(undefined);
    setEmployeeError(undefined);

    if (!selectedEmployeeId) {
      setEditorError("Select employee first.");
      return;
    }

    if (!selectedEmployeeIds.includes(selectedEmployeeId)) {
      setEditorError("This employee is not in the group.");
      return;
    }

    setSelectedEmployeeIds(currentIds => currentIds.filter(employeeId => employeeId !== selectedEmployeeId));
    setSelectedEmployeeId(null);
  };

  const handleCellChange = (employeeId: number, dayOfMonth: number, value: string) => {
    const cellKey = getAvailabilityCellKey(employeeId, dayOfMonth);

    setCellMap(currentCellMap => ({
      ...currentCellMap,
      [cellKey]: value,
    }));

    setCellErrors(currentErrors => {
      if (!currentErrors[cellKey]) {
        return currentErrors;
      }

      const nextErrors = { ...currentErrors };
      delete nextErrors[cellKey];
      return nextErrors;
    });
  };

  const handleColumnMove = (sourceEmployeeId: number, targetEmployeeId: number) => {
    setSelectedEmployeeIds(currentEmployeeIds => reorderEmployeeIds(currentEmployeeIds, sourceEmployeeId, targetEmployeeId));
    setEditorError(undefined);
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
          setHasLocalBindChanges(true);
          const nextBind = toEditableAvailabilityBind(createdBind);
          setBindRows(currentRows => currentRows.map(row => (row.clientId === clientId ? nextBind : row)));
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

    setInformationErrors(nextInformationErrors);
    setEmployeeError(nextEmployeeError);
    setCellErrors(nextCellErrors);

    if (Object.keys(nextInformationErrors).length > 0 || nextEmployeeError || Object.keys(nextCellErrors).length > 0) {
      setEditorError("Check highlighted fields before saving.");
      return;
    }

    setEditorError(undefined);

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
          navigate(`/availability/${result.id}`);
        },
        onError: error => {
          setEditorError(toErrorMessage(error));
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
      />

      <AvailabilityGroupEditor
        name={name}
        month={month}
        year={year}
        informationErrors={informationErrors}
        employeeError={employeeError}
        employees={filteredEmployees}
        employeeSearchText={employeeSearchText}
        selectedEmployeeId={selectedEmployeeId}
        assignedEmployees={assignedEmployees}
        columns={columns}
        cellMap={cellMap}
        cellErrors={cellErrors}
        binds={bindRows}
        selectedBindClientId={selectedBindClientId}
        bindValueByKey={activeBindValueByKey}
        isLoading={isLoading}
        hasLoadError={hasLoadError}
        isSaving={saveMutation.isPending}
        isBindsLoading={bindsQuery.isLoading && bindRows.length === 0}
        isBindBusy={createBindMutation.isPending || updateBindMutation.isPending || deleteBindMutation.isPending}
        errorMessage={editorError}
        bindErrorMessage={bindError ?? (bindsQuery.isError && bindRows.length === 0 ? "Could not load bind information." : undefined)}
        onNameChange={value => {
          setName(value);
          clearInformationError("name");
          setEditorError(undefined);
        }}
        onMonthChange={value => {
          setMonth(clampAvailabilityMonth(value));
          clearInformationError("month");
          setEditorError(undefined);
        }}
        onYearChange={value => {
          setYear(clampAvailabilityYear(value));
          clearInformationError("year");
          setEditorError(undefined);
        }}
        onEmployeeSearchTextChange={setEmployeeSearchText}
        onSelectedEmployeeIdChange={value => {
          setSelectedEmployeeId(value);
          setEditorError(undefined);
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
    </div>
  );
}



