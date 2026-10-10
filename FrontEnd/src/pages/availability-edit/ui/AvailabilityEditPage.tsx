import { t } from "@shared/i18n";
import { useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  buildManagerEditLockMessage,
  managerEditResourceTypes,
  useManagerEditLocks,
} from "@app/providers/PresenceProvider";
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
  buildAvailabilityTransferSourceHintData,
  clampAvailabilityMonth,
  clampAvailabilityYear,
  getAvailabilityCellKey,
  parseAvailabilityCode,
  removeStagedAvailabilityTransferDay,
  sanitizeAvailabilityCellMap,
  stageAvailabilityTransfer,
  type AvailabilityMatrixCellMap,
  type StagedAvailabilityTransfer,
  useAvailabilityGroupByIdQuery,
  useAvailabilityGroupMembersQuery,
  useAvailabilityGroupSlotsQuery,
  useSaveAvailabilityGroupGraphMutation,
  useAvailabilityTransferPreviewQuery,
} from "@entities/availability-groups";
import {
  AvailabilityGroupEditor,
  AvailabilityRelatedHintDialog,
  AvailabilityTransferDialog,
} from "@entities/availability-groups/ui";
import type { AvailabilityPublicationStatus } from "@entities/availability-groups/model/types";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { stableSerialize } from "@shared/lib/stableSerialize";
import { useSyncedDraft } from "@shared/lib/useSyncedDraft";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { ManagerEditLockDialog } from "@shared/ui/ManagerEditLockDialog";
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

type AvailabilityEditorPublicationErrors = {
  visibleFrom?: string;
  visibleTo?: string;
};

type AvailabilityEditorState = {
  name: string;
  month: number;
  year: number;
  publicationStatus: AvailabilityPublicationStatus;
  visibleFrom: string;
  visibleTo: string;
  selectedEmployeeId: number | null;
  selectedEmployeeIds: number[];
  cellMap: AvailabilityMatrixCellMap;
  stagedTransfers: StagedAvailabilityTransfer[];
  cellErrors: Record<string, string>;
  informationErrors: AvailabilityEditorInformationErrors;
  publicationErrors: AvailabilityEditorPublicationErrors;
  employeeError?: string;
  editorError?: string;
};

type AvailabilityGroupSource = {
  name: string;
  month: number;
  year: number;
  publicationStatus?: AvailabilityPublicationStatus | string | null;
  visibleFromUtc?: string | null;
  visibleToUtc?: string | null;
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

  return t("Something went wrong while saving this availability group.");
}

function getDefaultDateParts() {
  const today = new Date();
  return {
    month: clampAvailabilityMonth(today.getMonth() + 1),
    year: clampAvailabilityYear(today.getFullYear()),
  };
}

function toDateTimeLocalValue(value?: string | null) {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const offsetDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return offsetDate.toISOString().slice(0, 16);
}

function toIsoDateTime(value: string) {
  const trimmedValue = value.trim();
  if (!trimmedValue) {
    return null;
  }

  const date = new Date(trimmedValue);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

function toDraftPublicationStatus(status?: string | null): AvailabilityPublicationStatus {
  return status?.toLowerCase() === "public" ? "public" : "private";
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
      publicationStatus: "private",
      visibleFrom: "",
      visibleTo: "",
      selectedEmployeeId: null,
      selectedEmployeeIds: [],
      cellMap: {},
      stagedTransfers: [],
      cellErrors: {},
      informationErrors: {},
      publicationErrors: {},
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
    publicationStatus: toDraftPublicationStatus(group.publicationStatus),
    visibleFrom: toDateTimeLocalValue(group.visibleFromUtc),
    visibleTo: toDateTimeLocalValue(group.visibleToUtc),
    selectedEmployeeId: selectedEmployeeIds[0] ?? null,
    selectedEmployeeIds,
    cellMap: sanitizeAvailabilityCellMap(hydratedCellMap, selectedEmployeeIds, group.year, group.month),
    stagedTransfers: [],
    cellErrors: {},
    informationErrors: {},
    publicationErrors: {},
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

  return [
    "group",
    groupId,
    group.name,
    group.month,
    group.year,
    toDraftPublicationStatus(group.publicationStatus),
    group.visibleFromUtc ?? "",
    group.visibleToUtc ?? "",
    memberKey,
    slotKey,
  ].join(":");
}

function buildAvailabilityEditorSnapshot({
  name,
  month,
  year,
  selectedEmployeeIds,
  cellMap,
  stagedTransfers,
  publicationStatus,
  visibleFrom,
  visibleTo,
}: Pick<
  AvailabilityEditorState,
  "name" | "month" | "year" | "selectedEmployeeIds" | "cellMap" | "stagedTransfers" | "publicationStatus" | "visibleFrom" | "visibleTo"
>) {
  const sanitizedCellMap = sanitizeAvailabilityCellMap(cellMap, selectedEmployeeIds, year, month);

  return stableSerialize({
    name,
    month,
    year,
    publicationStatus,
    visibleFrom,
    visibleTo,
    selectedEmployeeIds,
    cellMap: Object.entries(sanitizedCellMap).sort(([leftKey], [rightKey]) => leftKey.localeCompare(rightKey)),
    stagedTransfers: stagedTransfers
      .map(transfer => ({
        ...transfer,
        dayOfMonths: [...transfer.dayOfMonths].sort((left, right) => left - right),
      }))
      .sort((left, right) =>
        left.employeeId - right.employeeId || left.sourceGroupId - right.sourceGroupId),
  });
}

function validatePublicationFields({
  publicationStatus,
  visibleFrom,
  visibleTo,
}: Pick<AvailabilityEditorState, "publicationStatus" | "visibleFrom" | "visibleTo">) {
  const errors: AvailabilityEditorPublicationErrors = {};
  const visibleFromDate = visibleFrom ? new Date(visibleFrom) : null;
  const visibleToDate = visibleTo ? new Date(visibleTo) : null;

  if (publicationStatus === "public") {
    if (!visibleFrom) {
      errors.visibleFrom = t("Publication start is required.");
    }

    if (!visibleTo) {
      errors.visibleTo = t("Publication end is required.");
    }
  }

  if (visibleFrom && (!visibleFromDate || Number.isNaN(visibleFromDate.getTime()))) {
    errors.visibleFrom = t("Use a valid publication start.");
  }

  if (visibleTo && (!visibleToDate || Number.isNaN(visibleToDate.getTime()))) {
    errors.visibleTo = t("Use a valid publication end.");
  }

  if (visibleFromDate && visibleToDate && visibleFromDate > visibleToDate) {
    errors.visibleTo = t("Publication end must be after publication start.");
  }

  return errors;
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
      <span className={styles.compactToggleTitle}>{t("Compact Size")}</span>

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
  const editLockTargets = useMemo(
    () => groupId
      ? [{
        resourceType: managerEditResourceTypes.availabilityGroup,
        resourceId: String(groupId),
      }]
      : [],
    [groupId],
  );
  const { lockedByOtherState, isCheckingLocks } = useManagerEditLocks(editLockTargets);
  const editLockMessage = lockedByOtherState
    ? buildManagerEditLockMessage(lockedByOtherState, t("This availability group"))
    : null;
  const canEdit = !editLockMessage && !isCheckingLocks;

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
    publicationStatus,
    visibleFrom,
    visibleTo,
    selectedEmployeeId,
    selectedEmployeeIds,
    cellMap,
    stagedTransfers,
    cellErrors,
    informationErrors,
    publicationErrors,
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
  const [employeeRemoveTargetId, setEmployeeRemoveTargetId] = useState<number | null>(null);
  const [isHeaderCollapsed, setIsHeaderCollapsed] = useState(false);
  const [isCompactMatrix, setIsCompactMatrix] = useState(false);
  const [transferEmployeeId, setTransferEmployeeId] = useState<number | null>(null);
  const [activeRelatedHintCellKey, setActiveRelatedHintCellKey] = useState<string | null>(null);

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
  const transferEmployeeName = transferEmployeeId !== null
    ? employeeNameById.get(transferEmployeeId) ?? t("Employee #") + transferEmployeeId
    : "";
  const transferPreviewQuery = useAvailabilityTransferPreviewQuery(
    selectedEmployeeIds,
    year,
    month,
    groupId,
  );
  const stagedTransferCellKeys = useMemo(
    () => new Set(stagedTransfers.flatMap(transfer =>
      transfer.dayOfMonths.map(dayOfMonth => getAvailabilityCellKey(transfer.employeeId, dayOfMonth)))),
    [stagedTransfers],
  );
  const transferSources = useMemo(() => {
    if (transferEmployeeId === null) {
      return [];
    }

    return (transferPreviewQuery.data ?? [])
      .filter(source => source.employeeId === transferEmployeeId)
      .map(source => ({
        ...source,
        days: source.days.map(day => {
          const cellKey = getAvailabilityCellKey(transferEmployeeId, day.dayOfMonth);
          const parsedTarget = parseAvailabilityCode(cellMap[cellKey] ?? "-");
          const targetIsEmpty = parsedTarget.ok && parsedTarget.value.normalizedCode === "-";

          return {
            ...day,
            canTransfer: day.canTransfer && targetIsEmpty && !stagedTransferCellKeys.has(cellKey),
          };
        }),
      }));
  }, [cellMap, stagedTransferCellKeys, transferEmployeeId, transferPreviewQuery.data]);
  const transferVisualHintData = useMemo(
    () => buildAvailabilityTransferSourceHintData(transferPreviewQuery.data ?? []),
    [transferPreviewQuery.data],
  );
  const transferVisualHintMap = transferVisualHintData.visualHintMap;
  const activeRelatedHint = activeRelatedHintCellKey
    ? transferVisualHintData.detailMap[activeRelatedHintCellKey] ?? null
    : null;
  const activeRelatedSource = activeRelatedHint
    ? (transferPreviewQuery.data ?? []).find(source => (
      source.employeeId === activeRelatedHint.employeeId &&
      source.groupId === activeRelatedHint.sourceGroupId
    )) ?? null
    : null;
  const activeRelatedHighlightedDays = useMemo(() => {
    if (!activeRelatedHint || !activeRelatedSource) {
      return [];
    }

    return activeRelatedSource.days.flatMap(day => {
      const cellKey = getAvailabilityCellKey(activeRelatedHint.employeeId, day.dayOfMonth);
      const detail = transferVisualHintData.detailMap[cellKey];
      const parsedTarget = parseAvailabilityCode(cellMap[cellKey] ?? "-");
      const isRenderedAsHint = parsedTarget.ok && parsedTarget.value.normalizedCode === "-";

      return detail?.sourceGroupId === activeRelatedHint.sourceGroupId && isRenderedAsHint
        ? [day.dayOfMonth]
        : [];
    });
  }, [activeRelatedHint, activeRelatedSource, cellMap, transferVisualHintData.detailMap]);
  const activeRelatedEmployeeName = activeRelatedHint
    ? employeeNameById.get(activeRelatedHint.employeeId) ?? t("Employee #{0}", activeRelatedHint.employeeId)
    : "";


  const columns = useMemo(() => {
    return selectedEmployeeIds.map((employeeId, index) => {
      const member = existingMemberByEmployeeId.get(employeeId);

      return {
        employeeId,
        memberId: member?.id ?? null,
        displayOrder: member?.displayOrder ?? index,
        employeeLastModifiedAtUtc: member?.employeeLastModifiedAtUtc ?? null,
        label: employeeNameById.get(employeeId) ?? t("Employee #{0}", employeeId),
      };
    });
  }, [employeeNameById, existingMemberByEmployeeId, selectedEmployeeIds]);

  const assignedEmployees = useMemo(() => {
    return columns.map(column => ({
      id: column.employeeId,
      label: column.label,
      canChooseFromAnother: (transferPreviewQuery.data ?? []).some(source =>
        source.employeeId === column.employeeId && source.days.some(day => {
          const cellKey = getAvailabilityCellKey(column.employeeId, day.dayOfMonth);
          const parsedTarget = parseAvailabilityCode(cellMap[cellKey] ?? "-");
          return day.canTransfer &&
            parsedTarget.ok &&
            parsedTarget.value.normalizedCode === "-" &&
            !stagedTransferCellKeys.has(cellKey);
        })),
    }));
  }, [cellMap, columns, stagedTransferCellKeys, transferPreviewQuery.data]);

  const selectedBindRow = useMemo(
    () => bindRows.find(bind => bind.clientId === resolvedSelectedBindClientId) ?? null,
    [bindRows, resolvedSelectedBindClientId],
  );

  const activeBindValueByKey = useMemo(() => buildActiveAvailabilityBindMap(bindRows), [bindRows]);
  const bindDeleteLabel = bindDeleteTarget?.key.trim() || t("this bind");
  const employeeRemoveTargetLabel = employeeRemoveTargetId !== null
    ? employeeNameById.get(employeeRemoveTargetId) ?? t("Employee #{0}", employeeRemoveTargetId)
    : t("this employee");

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
        publicationStatus,
        visibleFrom,
        visibleTo,
        selectedEmployeeIds,
        cellMap,
        stagedTransfers,
      }),
    [cellMap, month, name, publicationStatus, selectedEmployeeIds, stagedTransfers, visibleFrom, visibleTo, year],
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

  const handleEditLockDialogClose = () => {
    runWithoutPrompt(() => navigate(backTo));
  };

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
          editorError: t("Select employee first."),
        };
      }

      if (current.selectedEmployeeIds.includes(current.selectedEmployeeId)) {
        return {
          ...current,
          employeeError: undefined,
          editorError: t("This employee is already added."),
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

  const handleRemoveEmployeeConfirm = () => {
    if (employeeRemoveTargetId === null) {
      return;
    }

    const employeeId = employeeRemoveTargetId;
    setEditorState((current) => {
      if (!current.selectedEmployeeIds.includes(employeeId)) {
        return {
          ...current,
          employeeError: undefined,
          editorError: t("This employee is not in the group."),
        };
      }

      const nextSelectedEmployeeIds = current.selectedEmployeeIds.filter(
        currentEmployeeId => currentEmployeeId !== employeeId,
      );

      return {
        ...current,
        selectedEmployeeId: current.selectedEmployeeId === employeeId ? null : current.selectedEmployeeId,
        selectedEmployeeIds: nextSelectedEmployeeIds,
        stagedTransfers: current.stagedTransfers.filter(transfer => transfer.employeeId !== employeeId),
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
    setEmployeeRemoveTargetId(null);
  };

  const handleChooseFromAnother = (employeeId: number) => {
    setEditorState(current => ({
      ...current,
      editorError: undefined,
    }));
    setTransferEmployeeId(employeeId);
  };

  const handleTransferConfirm = (sourceGroupId: number, dayOfMonths: number[]) => {
    if (transferEmployeeId === null) {
      return;
    }

    setEditorState(current => {
      const staged = stageAvailabilityTransfer({
        cellMap: current.cellMap,
        transfers: current.stagedTransfers,
        sources: transferSources,
        employeeId: transferEmployeeId,
        sourceGroupId,
        dayOfMonths,
      });

      return {
        ...current,
        cellMap: staged.cellMap,
        stagedTransfers: staged.transfers,
        editorError: undefined,
      };
    });
    setTransferEmployeeId(null);
  };

  const handleVisualHintClick = (employeeId: number, dayOfMonth: number) => {
    const cellKey = getAvailabilityCellKey(employeeId, dayOfMonth);
    setActiveRelatedHintCellKey(transferVisualHintData.detailMap[cellKey] ? cellKey : null);
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
        stagedTransfers: removeStagedAvailabilityTransferDay(
          current.stagedTransfers,
          employeeId,
          dayOfMonth,
        ),
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
      setBindError(t("Bind key and value are required before the bind can be saved."));
      return;
    }

    const normalizedKey = normalizeBindKey(trimmedKey);
    if (!normalizedKey) {
      setBindError(t("Invalid bind key format."));
      return;
    }

    const duplicateBindExists = bindRows.some(item => item.clientId !== clientId && normalizeBindKey(item.key) === normalizedKey);
    if (duplicateBindExists) {
      setBindError(t("Bind '{0}' already exists.", normalizedKey));
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
      setBindError(t("Select bind first."));
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
    if (editLockMessage) {
      setEditorState((current) => ({
        ...current,
        editorError: editLockMessage,
      }));
      return;
    }

    if (isCheckingLocks) {
      setEditorState((current) => ({
        ...current,
        editorError: t("Checking edit access. Please wait a moment."),
      }));
      return;
    }

    const trimmedName = name.trim();
    const nextInformationErrors: AvailabilityEditorInformationErrors = {};
    const nextPublicationErrors = validatePublicationFields({
      publicationStatus,
      visibleFrom,
      visibleTo,
    });
    const nextEmployeeError = selectedEmployeeIds.length === 0 ? t("Add at least one employee to the group.") : undefined;

    if (!trimmedName) {
      nextInformationErrors.name = t("Availability name is required.");
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
      publicationErrors: nextPublicationErrors,
      employeeError: nextEmployeeError,
      cellErrors: nextCellErrors,
    }));

    if (
      Object.keys(nextInformationErrors).length > 0 ||
      Object.keys(nextPublicationErrors).length > 0 ||
      nextEmployeeError ||
      Object.keys(nextCellErrors).length > 0
    ) {
      setEditorState((current) => ({
        ...current,
        editorError: t("Check highlighted fields before saving."),
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
          publicationStatus,
          visibleFromUtc: toIsoDateTime(visibleFrom),
          visibleToUtc: toIsoDateTime(visibleTo),
        },
        employeeIds: selectedEmployeeIds,
        cellMap,
        transfers: stagedTransfers,
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
        title={isCreate ? t("Add Availability") : t("Availability Edit")}
        subtitle={isCreate ? t("Create a new monthly availability schedule") : t("Update availability information, employees and day codes")}
        backTo={backTo}
        onCollapseChange={setIsHeaderCollapsed}
        rightSlot={(
          <CompactSizeHeaderToggle
            checked={isCompactMatrix}
            onToggle={() => setIsCompactMatrix(current => !current)}
          />
        )}
      />

      {isCheckingLocks ? (
        <ManagerEditLockDialog
          open
          title={t("Checking edit access")}
          message={t("Please wait while we check whether this availability can be edited.")}
        />
      ) : null}

      {editLockMessage ? (
        <ManagerEditLockDialog
          open
          message={editLockMessage}
          actionText={t("Back to availability")}
          onClose={handleEditLockDialogClose}
        />
      ) : null}

      {canEdit ? (
        <AvailabilityGroupEditor
        name={name}
        month={month}
        year={year}
        isHeaderCollapsed={isHeaderCollapsed}
        compactSize={isCompactMatrix}
        informationErrors={informationErrors}
        publicationStatus={publicationStatus}
        visibleFrom={visibleFrom}
        visibleTo={visibleTo}
        publicationErrors={publicationErrors}
        employeeError={employeeError}
        employees={employees}
        selectedEmployeeId={selectedEmployeeId}
        assignedEmployees={assignedEmployees}
        columns={columns}
        cellMap={cellMap}
        visualHintMap={transferVisualHintMap}
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
        bindErrorMessage={bindError ?? (bindsQuery.isError && bindRows.length === 0 ? t("Could not load bind information.") : undefined)}
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
              stagedTransfers: [],
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
              stagedTransfers: [],
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
        onPublicationStatusChange={value => {
          setEditorState((current) => ({
            ...current,
            publicationStatus: value,
            publicationErrors: {},
            editorError: undefined,
          }));
        }}
        onVisibleFromChange={value => {
          setEditorState((current) => {
            const nextPublicationErrors = { ...current.publicationErrors };
            delete nextPublicationErrors.visibleFrom;

            return {
              ...current,
              visibleFrom: value,
              publicationErrors: nextPublicationErrors,
              editorError: undefined,
            };
          });
        }}
        onVisibleToChange={value => {
          setEditorState((current) => {
            const nextPublicationErrors = { ...current.publicationErrors };
            delete nextPublicationErrors.visibleTo;

            return {
              ...current,
              visibleTo: value,
              publicationErrors: nextPublicationErrors,
              editorError: undefined,
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
        onRemoveEmployee={setEmployeeRemoveTargetId}
        onChooseFromAnother={handleChooseFromAnother}
        onAddBind={handleAddBind}
        onDeleteBind={handleDeleteBind}
        onColumnMove={handleColumnMove}
        onCellChange={handleCellChange}
        onVisualHintClick={handleVisualHintClick}
        onSave={handleSave}
        />
      ) : null}

      <AvailabilityTransferDialog
        open={transferEmployeeId !== null}
        employeeId={transferEmployeeId ?? 0}
        employeeName={transferEmployeeName}
        year={year}
        month={month}
        sources={transferSources}
        isLoading={transferPreviewQuery.isLoading || transferPreviewQuery.isFetching}
        isPending={false}
        errorMessage={transferPreviewQuery.isError ? t("Could not load availability transfer sources.") : undefined}
        onCancel={() => setTransferEmployeeId(null)}
        onConfirm={handleTransferConfirm}
      />

      <AvailabilityRelatedHintDialog
        open={activeRelatedHint !== null}
        employeeId={activeRelatedHint?.employeeId ?? 0}
        employeeName={activeRelatedEmployeeName}
        year={year}
        month={month}
        source={activeRelatedSource}
        highlightedDayOfMonths={activeRelatedHighlightedDays}
        onCancel={() => setActiveRelatedHintCellKey(null)}
      />

      <ConfirmDialog
        open={employeeRemoveTargetId !== null}
        title={t("Remove employee")}
        message={t("Are you sure you want to remove '{0}' from this availability? Their availability data in this editor will be removed.", employeeRemoveTargetLabel)}
        onCancel={() => setEmployeeRemoveTargetId(null)}
        onConfirm={handleRemoveEmployeeConfirm}
        confirmText={t("Remove")}
      />
      <ConfirmDialog
        open={bindDeleteTarget !== null}
        title={t("Delete bind")}
        message={t("Are you sure you want to delete '{0}' from the bind list?", bindDeleteLabel)}
        onCancel={() => setBindDeleteTarget(null)}
        onConfirm={handleDeleteBindConfirm}
        confirmText={deleteBindMutation.isPending ? t("Deleting...") : t("Delete")}
        confirmDisabled={deleteBindMutation.isPending}
        cancelDisabled={deleteBindMutation.isPending}
      />

      <SavingOverlay active={saveMutation.isPending} />
      {unsavedChangesDialog}
    </div>
  );
}



