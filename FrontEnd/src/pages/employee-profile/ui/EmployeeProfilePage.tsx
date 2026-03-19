import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useDeleteEmployeeMutation, useEmployeeByIdQuery } from "@entities/employees/api/queries";
import { EmployeeProfileCard } from "@entities/employees/ui/EmployeeProfileCard";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./EmployeeProfilePage.module.css";

export function EmployeeProfilePage() {
  const navigate = useNavigate();
  const { employeeId } = useParams<{ employeeId: string }>();
  const id = employeeId ? Number(employeeId) : null;
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);

  const employeeQuery = useEmployeeByIdQuery(Number.isFinite(id) ? id : null);
  const deleteMutation = useDeleteEmployeeMutation();

  const employee = employeeQuery.data;
  const hasValidId = Number.isFinite(id);
  const isEmployeeLoading = hasValidId && !employee && (employeeQuery.isLoading || employeeQuery.isFetching);
  const hasLoadError = !employee && (!hasValidId || (!isEmployeeLoading && (employeeQuery.isError || Boolean(employeeQuery.error))));

  const handleDeleteConfirm = () => {
    if (!id) return;

    deleteMutation.mutate(id, {
      onSuccess: () => navigate("/employee"),
    });
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title="Employee Profile"
        subtitle="View employee details, contact information and record status"
        backTo="/employee"
      />

      <EmployeeProfileCard
        employee={employee}
        isLoading={isEmployeeLoading}
        hasLoadError={hasLoadError}
        isDeleting={deleteMutation.isPending}
        onEditEmployee={employeeIdValue => navigate(`/employee/${employeeIdValue}/edit`)}
        onDeleteEmployee={() => setIsDeleteOpen(true)}
      />

      <ConfirmDialog
        open={isDeleteOpen}
        title="Delete employee"
        message="Are you sure you want to delete this employee? This action cannot be undone."
        onCancel={() => setIsDeleteOpen(false)}
        onConfirm={handleDeleteConfirm}
        confirmText={deleteMutation.isPending ? "Deleting..." : "Delete"}
      />
    </div>
  );
}
