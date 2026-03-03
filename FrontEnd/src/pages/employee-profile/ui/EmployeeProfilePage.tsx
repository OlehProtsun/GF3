import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useDeleteEmployeeMutation, useEmployeeByIdQuery } from "@entities/employees/api/queries";
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

  return (
    <div>
      <PageHeader title="Employee Profile" subtitle="View employee details and actions" backTo="/employee" />
      <section className={styles.card}>
        {employeeQuery.isLoading ? <p>Loading...</p> : null}
        {employeeQuery.error ? <p className={styles.error}>Failed to load employee.</p> : null}
        {employee ? (
          <>
            <div className={styles.grid}>
              <strong>Employee ID</strong><span>{employee.id}</span>
              <strong>Full Name</strong><span>{employee.firstName} {employee.lastName}</span>
              <strong>Email</strong><span>{employee.email ?? "-"}</span>
              <strong>Phone</strong><span>{employee.phone ?? "-"}</span>
            </div>
            <div className={styles.actions}>
              <button type="button" onClick={() => navigate(`/employee/${employee.id}/edit`)}>Edit</button>
              <button type="button" onClick={() => setIsDeleteOpen(true)} disabled={deleteMutation.isPending}>Delete</button>
            </div>
          </>
        ) : null}
      </section>
      <ConfirmDialog
        open={isDeleteOpen}
        title="Delete employee"
        message="Are you sure you want to delete this employee?"
        onCancel={() => setIsDeleteOpen(false)}
        onConfirm={() => {
          if (!id) return;
          deleteMutation.mutate(id, { onSuccess: () => navigate("/employee") });
        }}
        confirmText={deleteMutation.isPending ? "Deleting..." : "Delete"}
      />
    </div>
  );
}
