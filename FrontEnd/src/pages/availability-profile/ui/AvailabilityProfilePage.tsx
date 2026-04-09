import { useMemo, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  buildAvailabilityCellMap,
  buildAvailabilityCellMapFromItems,
  buildAvailabilityColumns,
  buildAvailabilityColumnsFromItems,
  useAvailabilityGroupByIdQuery,
  useAvailabilityGroupItemsQuery,
  useAvailabilityGroupMembersQuery,
  useAvailabilityGroupSlotsQuery,
  useDeleteAvailabilityGroupMutation,
} from "@entities/availability-groups";
import { AvailabilityGroupProfileCard } from "@entities/availability-groups/ui";
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

  const groupQuery = useAvailabilityGroupByIdQuery(groupId);
  const itemsQuery = useAvailabilityGroupItemsQuery(groupId);
  const membersQuery = useAvailabilityGroupMembersQuery(groupId);
  const slotsQuery = useAvailabilityGroupSlotsQuery(groupId);
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

  const cellMap = useMemo(() => {
    if (canUseItemsData) {
      return buildAvailabilityCellMapFromItems(itemsQuery.data ?? []);
    }

    if (canUseNestedData) {
      return buildAvailabilityCellMap(membersQuery.data ?? [], slotsQuery.data ?? []);
    }

    return {};
  }, [canUseItemsData, canUseNestedData, itemsQuery.data, membersQuery.data, slotsQuery.data]);

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
        title="Availability Profile"
        subtitle="View availability details, assigned employees and saved daily schedule"
        backTo="/availability"
      />

      <AvailabilityGroupProfileCard
        group={groupQuery.data}
        columns={columns}
        cellMap={cellMap}
        isLoading={isLoading}
        hasLoadError={hasLoadError}
        isDeleting={deleteMutation.isPending}
        onEdit={() => {
          if (groupId) {
            navigate(`/availability/${groupId}/edit`);
          }
        }}
        onDelete={() => setIsDeleteOpen(true)}
      />

      <ConfirmDialog
        open={isDeleteOpen}
        title="Delete availability group"
        message="Are you sure you want to delete this availability group? This action cannot be undone."
        onCancel={() => setIsDeleteOpen(false)}
        onConfirm={handleDeleteConfirm}
        confirmText={deleteMutation.isPending ? "Deleting..." : "Delete"}
      />
    </div>
  );
}
