import { t } from "@shared/i18n";
﻿import { useMemo, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  buildManagerEditLockMessage,
  managerEditResourceTypes,
  useManagerEditLocks,
} from "@app/providers/PresenceProvider";
import {
  useCreateEmployeeMutation,
  useEmployeeByIdQuery,
  useUpdateEmployeeMutation,
} from "@entities/employees/api/queries";
import { createEmployeeFormState, useEmployeeForm } from "@entities/employees/model/form";
import { EmployeeDetailsForm } from "@entities/employees/ui/EmployeeDetailsForm";
import { stableSerialize } from "@shared/lib/stableSerialize";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import { ManagerEditLockDialog } from "@shared/ui/ManagerEditLockDialog";
import { PageHeader } from "@shared/ui/PageHeader";
import { SavingOverlay } from "@shared/ui/SavingOverlay";
import styles from "./EmployeeEditPage.module.css";

export function EmployeeEditPage() {
  const navigate = useNavigate();
  const { employeeId } = useParams<{ employeeId: string }>();
  const isCreate = !employeeId;
  const id = employeeId ? Number(employeeId) : null;
  const editLockTargets = useMemo(
    () => !isCreate && Number.isFinite(id)
      ? [{
        resourceType: managerEditResourceTypes.employee,
        resourceId: String(id),
      }]
      : [],
    [id, isCreate],
  );
  const { lockedByOtherState, isCheckingLocks } = useManagerEditLocks(editLockTargets);
  const editLockMessage = lockedByOtherState
    ? buildManagerEditLockMessage(lockedByOtherState, t("This employee"))
    : null;
  const canEdit = !editLockMessage && !isCheckingLocks;

  const employeeQuery = useEmployeeByIdQuery(!isCreate && Number.isFinite(id) ? id : null);
  const createMutation = useCreateEmployeeMutation();
  const updateMutation = useUpdateEmployeeMutation();
  const { form, errors, handleFieldChange, validate } = useEmployeeForm(employeeQuery.data, isCreate);

  const backTo = useMemo(() => {
    if (isCreate) {
      return "/employee";
    }

    return `/employee/${id}`;
  }, [id, isCreate]);

  const isSaving = createMutation.isPending || updateMutation.isPending;
  const hasLoadError = !isCreate && Boolean(employeeQuery.error) && !employeeQuery.isLoading && !employeeQuery.data;
  const initialFormSnapshot = useMemo(
    () => stableSerialize(createEmployeeFormState(employeeQuery.data)),
    [employeeQuery.data],
  );
  const currentFormSnapshot = useMemo(() => stableSerialize(form), [form]);
  const hasUnsavedChanges = useMemo(() => {
    if (isCreate) {
      return currentFormSnapshot !== initialFormSnapshot;
    }

    return Boolean(employeeQuery.data) && currentFormSnapshot !== initialFormSnapshot;
  }, [currentFormSnapshot, employeeQuery.data, initialFormSnapshot, isCreate]);
  const { dialog: unsavedChangesDialog, runWithoutPrompt } = useUnsavedChangesPrompt({
    when: hasUnsavedChanges && !isSaving,
  });

  const handleEditLockDialogClose = () => {
    runWithoutPrompt(() => navigate(backTo));
  };

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (editLockMessage) return;
    if (!validate()) return;

    if (isCreate) {
      createMutation.mutate(form, {
        onSuccess: (created) => runWithoutPrompt(() => navigate(`/employee/${created.id}`)),
      });
      return;
    }

    if (!id) return;
    updateMutation.mutate({ id, payload: form }, { onSuccess: () => runWithoutPrompt(() => navigate(`/employee/${id}`)) });
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title={isCreate ? t("Add Employee") : t("Edit Employee")}
        subtitle={isCreate ? t("Create new employee record") : t("Update employee information")}
        backTo={backTo}
      />

      {isCheckingLocks ? (
        <ManagerEditLockDialog
          open
          title={t("Checking edit access")}
          message={t("Please wait while we check whether this employee can be edited.")}
        />
      ) : null}

      {editLockMessage ? (
        <ManagerEditLockDialog
          open
          message={editLockMessage}
          actionText="Back to employee"
          onClose={handleEditLockDialogClose}
        />
      ) : null}

      {canEdit ? (
        <EmployeeDetailsForm
          form={form}
          errors={errors}
          isLoading={!isCreate && employeeQuery.isLoading}
          hasLoadError={hasLoadError}
          isSaving={isSaving}
          onFieldChange={handleFieldChange}
          onCancel={() => navigate(backTo)}
          onSubmit={onSubmit}
        />
      ) : null}

      <SavingOverlay active={isSaving} />
      {unsavedChangesDialog}
    </div>
  );
}
