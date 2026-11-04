import { t } from "@shared/i18n";
import { useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  useDeleteEmployeeMutation,
  useEmployeeByIdQuery,
  useKickEmployeeMutation,
} from "@entities/employees/api/queries";
import { EmployeeProfileCard } from "@entities/employees/ui/EmployeeProfileCard";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./EmployeeProfilePage.module.css";
import { useEmployeeRegulationHistoryQuery } from "@entities/regulations";
import { RegulationHistoryCard } from "@entities/regulations/ui/RegulationHistoryCard";

export function EmployeeProfilePage() {
  const navigate = useNavigate();
  const { employeeId } = useParams<{ employeeId: string }>();
  const id = employeeId ? Number(employeeId) : null;
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const deleteInFlight = useRef(false);
  const [isKickOpen, setIsKickOpen] = useState(false);

  const employeeQuery = useEmployeeByIdQuery(Number.isFinite(id) ? id : null);
  const regulationHistoryQuery = useEmployeeRegulationHistoryQuery(Number.isFinite(id) ? id : null);
  const deleteMutation = useDeleteEmployeeMutation();
  const kickMutation = useKickEmployeeMutation();

  const employee = employeeQuery.data;
  const hasValidId = Number.isFinite(id);
  const isEmployeeLoading = hasValidId && !employee && (employeeQuery.isLoading || employeeQuery.isFetching);
  const hasLoadError = !employee && (!hasValidId || (!isEmployeeLoading && (employeeQuery.isError || Boolean(employeeQuery.error))));

  const handleDeleteConfirm = () => {
    if (!id || !isDeleteOpen || deleteInFlight.current || deleteMutation.isPending) return;

    deleteInFlight.current = true;
    deleteMutation.mutate(id, {
      onSuccess: () => {
        deleteInFlight.current = false;
        navigate("/employee");
      },
      onError: () => { deleteInFlight.current = false; },
    });
  };

  const handleKickConfirm = () => {
    if (!id) return;

    kickMutation.mutate(id, {
      onSuccess: () => setIsKickOpen(false),
    });
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title={t("Employee Profile")}
        subtitle={t("View employee details, contact information and record status")}
        backTo="/employee"
      />

      <EmployeeProfileCard
        employee={employee}
        isLoading={isEmployeeLoading}
        hasLoadError={hasLoadError}
        isDeleting={deleteMutation.isPending}
        isKicking={kickMutation.isPending}
        onEditEmployee={employeeIdValue => navigate(`/employee/${employeeIdValue}/edit`)}
        onDeleteEmployee={() => setIsDeleteOpen(true)}
        onKickEmployee={() => setIsKickOpen(true)}
      />

      <RegulationHistoryCard
        acceptances={regulationHistoryQuery.data ?? []}
        isLoading={regulationHistoryQuery.isLoading}
        className={styles.regulationHistory}
      />

      <ConfirmDialog
        open={isDeleteOpen}
        title={t("Delete employee?")}
        message={t("Are you sure you want to delete this employee? This action cannot be undone.")}
        onCancel={() => setIsDeleteOpen(false)}
        onConfirm={handleDeleteConfirm}
        confirmText={t("Delete")}
        cancelText={t("Cancel")}
        confirmDisabled={deleteMutation.isPending}
        cancelDisabled={deleteMutation.isPending}
      />

      <ConfirmDialog
        open={isKickOpen}
        title={t("Kick employee")}
        message={t("This will immediately sign the employee out of every active session. They can sign in again with the same credentials.")}
        onCancel={() => setIsKickOpen(false)}
        onConfirm={handleKickConfirm}
        confirmText={kickMutation.isPending ? t("Kicking...") : t("Kick")}
      />
    </div>
  );
}
