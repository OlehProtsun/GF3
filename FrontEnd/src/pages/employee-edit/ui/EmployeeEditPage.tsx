import { useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  useCreateEmployeeMutation,
  useEmployeeByIdQuery,
  useUpdateEmployeeMutation,
} from "@entities/employees/api/queries";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./EmployeeEditPage.module.css";

type FormState = { firstName: string; lastName: string; email: string; phone: string };

export function EmployeeEditPage() {
  const navigate = useNavigate();
  const { employeeId } = useParams<{ employeeId: string }>();
  const isCreate = !employeeId;
  const id = employeeId ? Number(employeeId) : null;

  const employeeQuery = useEmployeeByIdQuery(!isCreate && Number.isFinite(id) ? id : null);
  const createMutation = useCreateEmployeeMutation();
  const updateMutation = useUpdateEmployeeMutation();

  const [form, setForm] = useState<FormState>({ firstName: "", lastName: "", email: "", phone: "" });
  const [errors, setErrors] = useState<Partial<FormState>>({});

  useEffect(() => {
    if (employeeQuery.data && !isCreate) {
      setForm({
        firstName: employeeQuery.data.firstName,
        lastName: employeeQuery.data.lastName,
        email: employeeQuery.data.email ?? "",
        phone: employeeQuery.data.phone ?? "",
      });
    }
  }, [employeeQuery.data, isCreate]);

  const backTo = useMemo(() => {
    if (isCreate) return "/employee";
    return `/employee/${id}`;
  }, [id, isCreate]);

  const validate = () => {
    const nextErrors: Partial<FormState> = {};
    if (!form.firstName.trim()) nextErrors.firstName = "First name is required";
    if (!form.lastName.trim()) nextErrors.lastName = "Last name is required";
    if (form.email && !/^\S+@\S+\.\S+$/.test(form.email)) nextErrors.email = "Invalid email format";
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (!validate()) return;

    if (isCreate) {
      createMutation.mutate(form, {
        onSuccess: (created) => navigate(`/employee/${created.id}`),
      });
      return;
    }

    if (!id) return;
    updateMutation.mutate(
      { id, payload: form },
      { onSuccess: () => navigate(`/employee/${id}`) },
    );
  };

  return (
    <div>
      <PageHeader
        title={isCreate ? "Add Employee" : "Edit Employee"}
        subtitle={isCreate ? "Create new employee record" : "Update employee information"}
        backTo={backTo}
      />
      <section className={styles.card}>
        {!isCreate && employeeQuery.isLoading ? <p>Loading...</p> : null}
        <form className={styles.form} onSubmit={onSubmit}>
          <label>
            First Name
            <input value={form.firstName} onChange={(event) => setForm((prev) => ({ ...prev, firstName: event.target.value }))} />
            {errors.firstName ? <div className={styles.error}>{errors.firstName}</div> : null}
          </label>
          <label>
            Last Name
            <input value={form.lastName} onChange={(event) => setForm((prev) => ({ ...prev, lastName: event.target.value }))} />
            {errors.lastName ? <div className={styles.error}>{errors.lastName}</div> : null}
          </label>
          <label>
            Email
            <input value={form.email} onChange={(event) => setForm((prev) => ({ ...prev, email: event.target.value }))} />
            {errors.email ? <div className={styles.error}>{errors.email}</div> : null}
          </label>
          <label>
            Phone
            <input value={form.phone} onChange={(event) => setForm((prev) => ({ ...prev, phone: event.target.value }))} />
          </label>
          <div className={styles.actions}>
            <button type="submit" disabled={createMutation.isPending || updateMutation.isPending}>
              {createMutation.isPending || updateMutation.isPending ? "Saving..." : "Save"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
