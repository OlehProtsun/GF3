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
import { IosButton } from "@shared/ui/components/IosButton";
import { CheckIcon, CloseIcon, InformationIcon } from "@shared/ui/icons";

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
  <div className={styles.page}>
    <PageHeader
      title={isCreate ? "Add Employee" : "Edit Employee"}
      subtitle={isCreate ? "Create new employee record" : "Update employee information"}
      backTo={backTo}
    />

    <section className={styles.card}>
      <div className={styles.sectionHeader}>
        <div className={styles.sectionTitle}>
          <InformationIcon size={18} className={styles.infoIcon}/>
          <span>Details</span>
        </div>
      </div>
      {!isCreate && employeeQuery.isLoading ? <div className={styles.loading}>Loading...</div> : null}

      {/* iOS-like error banner як у ListCardSection */}
      {!isCreate && employeeQuery.error && !employeeQuery.isLoading && !employeeQuery.data ? (
        <div className={styles.errorWrap}>
          <div className={styles.errorBanner} role="alert" aria-live="polite">
            <svg className={styles.errorIcon} viewBox="0 0 24 24" aria-hidden="true" focusable="false">
              <path d="M12 9v4M12 17h.01" />
              <path d="M10.29 3.86 2.17 17.92A2 2 0 0 0 3.9 21h16.2a2 2 0 0 0 1.73-3.08L13.71 3.86a2 2 0 0 0-3.42 0Z" />
            </svg>
            <span className={styles.errorText}>Could not load employee.</span>
          </div>
        </div>
      ) : null}

      <form className={styles.form} onSubmit={onSubmit}>
        <div className={styles.row2}>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="firstName">
              First Name
            </label>
              <input
                id="firstName"
                className={styles.input}
                value={form.firstName}
                placeholder="Example: John"
                onChange={(event) => setForm((prev) => ({ ...prev, firstName: event.target.value }))}
                aria-invalid={Boolean(errors.firstName)}
                aria-describedby={errors.firstName ? "firstName-error" : undefined}
              />
              {errors.firstName ? (
                <div id="firstName-error" className={styles.fieldError}>
                  {errors.firstName}
                </div>
              ) : null}
          </div>

          <div className={styles.field}>
            <label className={styles.label} htmlFor="lastName">
              Last Name
            </label>
              <input
                id="lastName"
                className={styles.input}
                value={form.lastName}
                placeholder="Example: Doe"
                onChange={(event) => setForm((prev) => ({ ...prev, lastName: event.target.value }))}
                aria-invalid={Boolean(errors.lastName)}
                aria-describedby={errors.lastName ? "lastName-error" : undefined}
              />
              {errors.lastName ? (
                <div id="lastName-error" className={styles.fieldError}>
                  {errors.lastName}
                </div>
              ) : null}          
              </div>
        </div>

        <div className={styles.field}>
          <label className={styles.label} htmlFor="email">
            Email
          </label>
            <input
              id="email"
              className={styles.input}
              type="email"
              placeholder="Example: john.doe@example.com"
              value={form.email}
              onChange={(event) => setForm((prev) => ({ ...prev, email: event.target.value }))}
              aria-invalid={Boolean(errors.email)}
              aria-describedby={errors.email ? "email-error" : undefined}
            />
            {errors.email ? (
              <div id="email-error" className={styles.fieldError}>
                {errors.email}
              </div>
            ) : null}        
        </div>

        <div className={styles.field}>
          <label className={styles.label} htmlFor="phone">
            Phone
          </label>
          <input
            id="phone"
            className={styles.input}
            type="tel"
            placeholder="Example: +1 (555) 123-4567"
            value={form.phone}
            onChange={(event) => setForm((prev) => ({ ...prev, phone: event.target.value }))}
          />
        </div>

        <div className={styles.actions}>
          <IosButton
            label="Cancel"
            variant="secondary"
            icon={<CloseIcon size={18} />}
            onClick={() => navigate(backTo)}
            disabled={createMutation.isPending || updateMutation.isPending}
            className={styles.cancelBtn}
          />

          <IosButton
            label={createMutation.isPending || updateMutation.isPending ? "Saving..." : "Save"}
            variant="primary"
            icon={<CheckIcon size={18} />}
            type="submit"
            disabled={createMutation.isPending || updateMutation.isPending}
          />
        </div>
      </form>
    </section>
  </div>
);}
