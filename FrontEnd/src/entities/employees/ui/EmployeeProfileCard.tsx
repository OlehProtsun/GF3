import { t } from "@shared/i18n";
import type { Employee } from "@entities/employees/model/types";
import {
  getEmployeeContactDetails,
  getEmployeeContactState,
  getEmployeeFullName,
  getEmployeeInitials,
  getEmployeePresenceTone,
} from "@entities/employees/model/presentation";
import { IosButton } from "@shared/ui/components/IosButton";
import { PresenceBadge } from "@shared/ui/components/PresenceBadge";
import {
  RecordProfileCard,
  renderRecordDetailValue,
} from "@shared/ui/components/RecordProfileCard";
import { type ProfileSummaryDetail } from "@shared/ui/components/ProfileSummaryCard";
import { EmployeeIcon } from "@shared/ui/icons";

type EmployeeProfileCardProps = {
  employee?: Employee;
  isLoading: boolean;
  hasLoadError: boolean;
  isDeleting: boolean;
  isKicking: boolean;
  onEditEmployee: (employeeId: Employee["id"]) => void;
  onDeleteEmployee: () => void;
  onKickEmployee: () => void;
};

export function EmployeeProfileCard({
  employee,
  isLoading,
  hasLoadError,
  isDeleting,
  isKicking,
  onEditEmployee,
  onDeleteEmployee,
  onKickEmployee,
}: EmployeeProfileCardProps) {
  const fullName = getEmployeeFullName(employee, t("Employee Profile"));
  const initials = getEmployeeInitials(employee);
  const presenceLabel = getEmployeeContactState(employee);
  const presenceTone = getEmployeePresenceTone(employee);
  const details: ProfileSummaryDetail[] = employee
    ? getEmployeeContactDetails(employee).map(item => ({
        key: item.key,
        label: item.label,
        value: renderRecordDetailValue(item.value, item.href),
      }))
    : [];

  return (
    <RecordProfileCard
      sectionTitle="Employee Profile"
      icon={<EmployeeIcon size={18} />}
      headerMeta={employee ? `ID ${employee.id}` : undefined}
      isLoading={isLoading}
      hasLoadError={hasLoadError}
      loadingMessage="Loading employee details..."
      errorMessage="Could not load employee."
      avatar={employee ? initials : undefined}
      name={employee ? fullName : undefined}
      subtitle={employee ? <PresenceBadge label={presenceLabel} tone={presenceTone} /> : undefined}
      details={details}
      actions={
        employee ? (
          <>
            <IosButton label={t("Edit Employee")} onClick={() => onEditEmployee(employee.id)} />
            {employee.hasLoginAccount ? (
              <IosButton
                label={isKicking ? t("Kicking...") : t("Kick Employee")}
                variant="secondary"
                customColor="#2563eb"
                customBorderColor="#2563eb"
                onClick={onKickEmployee}
                disabled={isKicking}
              />
            ) : null}
            <IosButton
              label={isDeleting ? t("Deleting...") : t("Delete Employee")}
              variant="secondary"
              customColor="#ef4444"
              customBorderColor="#ef4444"
              onClick={onDeleteEmployee}
              disabled={isDeleting}
            />
          </>
        ) : undefined
      }
    />
  );
}
