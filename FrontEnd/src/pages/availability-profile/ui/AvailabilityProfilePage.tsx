import { t } from "@shared/i18n";
import { useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  buildAvailabilityCellMap,
  buildAvailabilityCellMapFromItems,
  buildAvailabilityProfileHintMap,
  buildAvailabilityTransferHintMap,
  buildAvailabilityTransferSourceHintData,
  buildAvailabilityColumns,
  buildAvailabilityColumnsFromItems,
  getAvailabilityCellKey,
  parseAvailabilityCode,
  type AvailabilityTransferSource,
  useAvailabilityGroupByIdQuery,
  useAvailabilityGroupItemsQuery,
  useAvailabilityGroupMembersQuery,
  useAvailabilityGroupSlotsQuery,
  useAvailabilityTransferHintsQuery,
  useAvailabilityTransferPreviewQuery,
  useDeleteAvailabilityGroupMutation,
} from "@entities/availability-groups";
import { AvailabilityGroupProfileCard, AvailabilityRelatedHintDialog } from "@entities/availability-groups/ui";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./AvailabilityProfilePage.module.css";

export function AvailabilityProfilePage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { availabilityId } = useParams<{ availabilityId: string }>();
  const parsedId = availabilityId ? Number(availabilityId) : null;
  const groupId = Number.isFinite(parsedId) ? parsedId : null;
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [activeRelatedHintCellKey, setActiveRelatedHintCellKey] = useState<string | null>(null);

  const groupQuery = useAvailabilityGroupByIdQuery(groupId);
  const itemsQuery = useAvailabilityGroupItemsQuery(groupId);
  const membersQuery = useAvailabilityGroupMembersQuery(groupId);
  const slotsQuery = useAvailabilityGroupSlotsQuery(groupId);
  const transferHintsQuery = useAvailabilityTransferHintsQuery(groupId);
  const employeesQuery = useEmployeesListQuery({ refreshKey: location.key });
  const deleteMutation = useDeleteAvailabilityGroupMutation();

  const employeeNameById = useMemo(() => {
    return new Map((employeesQuery.data ?? []).map(employee => [employee.id, getEmployeeFullName(employee)]));
  }, [employeesQuery.data]);

  const hasItemsData = Array.isArray(itemsQuery.data);
  const hasMembersData = Array.isArray(membersQuery.data);
  const hasSlotsData = Array.isArray(slotsQuery.data);
  const hasItemsError = itemsQuery.isError || Boolean(itemsQuery.error);
  const hasMembersError = membersQuery.isError || Boolean(membersQuery.error);
  const hasSlotsError = slotsQuery.isError || Boolean(slotsQuery.error);
  const hasGroupError = groupQuery.isError || Boolean(groupQuery.error);
  const canUseItemsData = hasItemsData;
  const canUseNestedData = hasMembersData && hasSlotsData;

  const columns = useMemo(() => {
    if (hasMembersData && (membersQuery.data ?? []).length > 0) {
      return buildAvailabilityColumns(membersQuery.data ?? [], employeeNameById);
    }

    if (canUseItemsData) {
      return buildAvailabilityColumnsFromItems(itemsQuery.data ?? [], employeeNameById);
    }

    if (canUseNestedData) {
      return buildAvailabilityColumns(membersQuery.data ?? [], employeeNameById);
    }

    return [];
  }, [canUseItemsData, canUseNestedData, employeeNameById, hasMembersData, itemsQuery.data, membersQuery.data]);
  const profileEmployeeIds = useMemo(() => columns.map(column => column.employeeId), [columns]);
  const transferPreviewQuery = useAvailabilityTransferPreviewQuery(
    profileEmployeeIds,
    groupQuery.data?.year ?? 0,
    groupQuery.data?.month ?? 0,
    groupId,
  );
  const transferSourceHintData = useMemo(
    () => buildAvailabilityTransferSourceHintData(transferPreviewQuery.data ?? []),
    [transferPreviewQuery.data],
  );

  const cellMap = useMemo(() => {
    if (canUseItemsData) {
      return buildAvailabilityCellMapFromItems(itemsQuery.data ?? []);
    }

    if (canUseNestedData) {
      return buildAvailabilityCellMap(membersQuery.data ?? [], slotsQuery.data ?? []);
    }

    return {};
  }, [canUseItemsData, canUseNestedData, itemsQuery.data, membersQuery.data, slotsQuery.data]);

  const transferredAwayVisualHintMap = useMemo(
    () => buildAvailabilityTransferHintMap(transferHintsQuery.data ?? []),
    [transferHintsQuery.data],
  );
  const visualHintMap = useMemo(
    () => buildAvailabilityProfileHintMap(
      transferPreviewQuery.data ?? [],
      transferHintsQuery.data ?? [],
    ),
    [transferHintsQuery.data, transferPreviewQuery.data],
  );
  const activeTransferredAwayHint = useMemo(() => {
    if (!activeRelatedHintCellKey) {
      return null;
    }

    return (transferHintsQuery.data ?? []).find(hint => (
      getAvailabilityCellKey(hint.employeeId, hint.dayOfMonth) === activeRelatedHintCellKey &&
      Boolean(transferredAwayVisualHintMap[activeRelatedHintCellKey])
    )) ?? null;
  }, [activeRelatedHintCellKey, transferHintsQuery.data, transferredAwayVisualHintMap]);
  const activeSourceHint = !activeTransferredAwayHint && activeRelatedHintCellKey
    ? transferSourceHintData.detailMap[activeRelatedHintCellKey] ?? null
    : null;
  const relatedGroupId = activeTransferredAwayHint?.targetGroupId ?? null;
  const relatedGroupQuery = useAvailabilityGroupByIdQuery(relatedGroupId);
  const relatedItemsQuery = useAvailabilityGroupItemsQuery(relatedGroupId);
  const activeTransferredAwaySource = useMemo<AvailabilityTransferSource | null>(() => {
    if (!activeTransferredAwayHint || !relatedGroupQuery.data) {
      return null;
    }

    const employeeItems = (relatedItemsQuery.data ?? [])
      .filter(item => item.employeeId === activeTransferredAwayHint.employeeId)
      .sort((left, right) => left.dayOfMonth - right.dayOfMonth);
    const memberId = employeeItems[0]?.memberId;
    if (!memberId) {
      return null;
    }

    return {
      groupId: relatedGroupQuery.data.id,
      groupName: relatedGroupQuery.data.name,
      memberId,
      employeeId: activeTransferredAwayHint.employeeId,
      days: employeeItems.map(item => ({
        dayOfMonth: item.dayOfMonth,
        kind: item.kind,
        intervalStr: item.intervalStr,
        canTransfer: false,
      })),
    };
  }, [activeTransferredAwayHint, relatedGroupQuery.data, relatedItemsQuery.data]);
  const activePreviewSource = activeSourceHint
    ? (transferPreviewQuery.data ?? []).find(source => (
      source.employeeId === activeSourceHint.employeeId &&
      source.groupId === activeSourceHint.sourceGroupId
    )) ?? null
    : null;
  const activeRelatedSource = activePreviewSource ?? activeTransferredAwaySource;
  const activeRelatedHighlightedDays = useMemo(() => {
    if (activeSourceHint && activePreviewSource) {
      return activePreviewSource.days.flatMap(day => {
        const cellKey = getAvailabilityCellKey(activeSourceHint.employeeId, day.dayOfMonth);
        const detail = transferSourceHintData.detailMap[cellKey];
        const parsedTarget = parseAvailabilityCode(cellMap[cellKey] ?? "-");
        const isRenderedAsHint = parsedTarget.ok && parsedTarget.value.normalizedCode === "-";

        return detail?.sourceGroupId === activeSourceHint.sourceGroupId && isRenderedAsHint
          ? [day.dayOfMonth]
          : [];
      });
    }

    if (!activeTransferredAwayHint) {
      return [];
    }

    return (transferHintsQuery.data ?? []).flatMap(hint => {
      const cellKey = getAvailabilityCellKey(hint.employeeId, hint.dayOfMonth);
      return hint.employeeId === activeTransferredAwayHint.employeeId &&
        hint.targetGroupId === activeTransferredAwayHint.targetGroupId &&
        transferredAwayVisualHintMap[cellKey]
        ? [hint.dayOfMonth]
        : [];
    });
  }, [
    activePreviewSource,
    activeSourceHint,
    activeTransferredAwayHint,
    cellMap,
    transferHintsQuery.data,
    transferredAwayVisualHintMap,
    transferSourceHintData.detailMap,
  ]);
  const activeRelatedEmployeeId = activeTransferredAwayHint?.employeeId ?? activeSourceHint?.employeeId ?? 0;
  const activeRelatedEmployeeName = activeRelatedEmployeeId > 0
    ? employeeNameById.get(activeRelatedEmployeeId) ?? t("Employee #{0}", activeRelatedEmployeeId)
    : "";
  const hasActiveRelatedHint = Boolean(activeTransferredAwayHint || activeSourceHint);

  const hasValidId = Number.isFinite(parsedId);
  const hasResolvedMatrix = canUseItemsData || canUseNestedData;
  const isGroupPending = hasValidId && !groupQuery.data && !hasGroupError && (groupQuery.isLoading || groupQuery.isFetching);
  const isItemsPending = !canUseItemsData && !hasItemsError && (itemsQuery.isLoading || itemsQuery.isFetching);
  const isNestedPending =
    !canUseNestedData &&
    !(hasMembersError || hasSlotsError) &&
    (membersQuery.isLoading || membersQuery.isFetching || slotsQuery.isLoading || slotsQuery.isFetching);

  const isLoading =
    isGroupPending ||
    (hasValidId && Boolean(groupQuery.data) && !hasResolvedMatrix && (isItemsPending || isNestedPending));

  const hasLoadError =
    !hasValidId ||
    (!isLoading && !groupQuery.data) ||
    (!isLoading && Boolean(groupQuery.data) && !hasResolvedMatrix && hasItemsError && (hasMembersError || hasSlotsError));

  const handleDeleteConfirm = () => {
    if (!groupId) {
      return;
    }

    deleteMutation.mutate(groupId, {
      onSuccess: () => {
        navigate("/availability");
      },
    });
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title={t("Availability Profile")}
        subtitle={t("View availability details, assigned employees and saved daily schedule")}
        backTo="/availability"
      />

      <AvailabilityGroupProfileCard
        group={groupQuery.data}
        columns={columns}
        cellMap={cellMap}
        visualHintMap={visualHintMap}
        isLoading={isLoading}
        hasLoadError={hasLoadError}
        isDeleting={deleteMutation.isPending}
        onEdit={() => {
          if (groupId) {
            navigate(`/availability/${groupId}/edit`);
          }
        }}
        onDelete={() => setIsDeleteOpen(true)}
        onVisualHintClick={(employeeId, dayOfMonth) => {
          const cellKey = getAvailabilityCellKey(employeeId, dayOfMonth);
          setActiveRelatedHintCellKey(visualHintMap[cellKey] ? cellKey : null);
        }}
      />

      <AvailabilityRelatedHintDialog
        open={hasActiveRelatedHint}
        employeeId={activeRelatedEmployeeId}
        employeeName={activeRelatedEmployeeName}
        year={relatedGroupQuery.data?.year ?? groupQuery.data?.year ?? new Date().getFullYear()}
        month={relatedGroupQuery.data?.month ?? groupQuery.data?.month ?? 1}
        source={activeRelatedSource}
        highlightedDayOfMonths={activeRelatedHighlightedDays}
        isLoading={Boolean(activeTransferredAwayHint) && (relatedGroupQuery.isLoading || relatedItemsQuery.isLoading)}
        errorMessage={activeTransferredAwayHint && (relatedGroupQuery.isError || relatedItemsQuery.isError)
          ? t("Could not load the related availability.")
          : undefined}
        onCancel={() => setActiveRelatedHintCellKey(null)}
      />

      <ConfirmDialog
        open={isDeleteOpen}
        title={t("Delete availability group")}
        message={t("Are you sure you want to delete this availability group? This action cannot be undone.")}
        onCancel={() => setIsDeleteOpen(false)}
        onConfirm={handleDeleteConfirm}
        confirmText={deleteMutation.isPending ? t("Deleting...") : t("Delete")}
      />
    </div>
  );
}
