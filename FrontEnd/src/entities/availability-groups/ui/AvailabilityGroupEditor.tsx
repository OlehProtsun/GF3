import { useState } from "react";
import type { Employee } from "@entities/employees/model/types";
import type { AvailabilityMatrixCellMap, AvailabilityMatrixColumn } from "@entities/availability-groups/model/editor";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { SaveIcon } from "@shared/ui/icons";
import { AvailabilityBindCard } from "./AvailabilityBindCard";
import { AvailabilityEmployeeCard } from "./AvailabilityEmployeeCard";
import { AvailabilityInformationCard, type AvailabilityInformationErrors } from "./AvailabilityInformationCard";
import { AvailabilityScheduleMatrix } from "./AvailabilityScheduleMatrix";
import { AvailabilitySidebarCollapseButton, AvailabilitySidebarSection } from "./AvailabilitySidebarSection";
import { AvailabilityWorkspaceLayout } from "./AvailabilityWorkspaceLayout";
import styles from "./AvailabilityGroupEditor.module.css";

type AvailabilityGroupEditorBindRow = {
  clientId: string;
  id: number | null;
  key: string;
  value: string;
  isActive: boolean;
};

type AvailabilityGroupEditorProps = {
  name: string;
  month: number;
  year: number;
  informationErrors?: AvailabilityInformationErrors;
  employeeError?: string;
  employees: Employee[];
  employeeSearchText: string;
  selectedEmployeeId: number | null;
  assignedEmployees: { id: number; label: string }[];
  columns: AvailabilityMatrixColumn[];
  cellMap: AvailabilityMatrixCellMap;
  cellErrors?: Record<string, string>;
  binds: AvailabilityGroupEditorBindRow[];
  selectedBindClientId: string | null;
  bindValueByKey: ReadonlyMap<string, string>;
  isLoading: boolean;
  hasLoadError: boolean;
  isSaving: boolean;
  isBindsLoading: boolean;
  isBindBusy: boolean;
  errorMessage?: string;
  bindErrorMessage?: string;
  onNameChange: (value: string) => void;
  onMonthChange: (value: number) => void;
  onYearChange: (value: number) => void;
  onEmployeeSearchTextChange: (value: string) => void;
  onSelectedEmployeeIdChange: (value: number | null) => void;
  onSelectedBindChange: (clientId: string | null) => void;
  onBindFieldChange: (clientId: string, patch: Partial<Pick<AvailabilityGroupEditorBindRow, "key" | "value" | "isActive">>) => void;
  onBindCommit: (clientId: string) => void;
  onAddEmployee: () => void;
  onRemoveEmployee: () => void;
  onAddBind: () => void;
  onDeleteBind: () => void;
  onColumnMove: (employeeId: number, targetEmployeeId: number) => void;
  onCellChange: (employeeId: number, dayOfMonth: number, value: string) => void;
  onSave: () => void;
};

type SidebarSectionKey = "information" | "employee" | "bind";

function joinClassNames(...values: Array<string | undefined>) {
  return values.filter(Boolean).join(" ");
}

export function AvailabilityGroupEditor({
  name,
  month,
  year,
  informationErrors,
  employeeError,
  employees,
  employeeSearchText,
  selectedEmployeeId,
  assignedEmployees,
  columns,
  cellMap,
  cellErrors = {},
  binds,
  selectedBindClientId,
  bindValueByKey,
  isLoading,
  hasLoadError,
  isSaving,
  isBindsLoading,
  isBindBusy,
  errorMessage,
  bindErrorMessage,
  onNameChange,
  onMonthChange,
  onYearChange,
  onEmployeeSearchTextChange,
  onSelectedEmployeeIdChange,
  onSelectedBindChange,
  onBindFieldChange,
  onBindCommit,
  onAddEmployee,
  onRemoveEmployee,
  onAddBind,
  onDeleteBind,
  onColumnMove,
  onCellChange,
  onSave,
}: AvailabilityGroupEditorProps) {
  const [collapsedSections, setCollapsedSections] = useState<Record<SidebarSectionKey, boolean>>({
    information: false,
    employee: false,
    bind: false,
  });

  const allSectionsCollapsed = Object.values(collapsedSections).every(Boolean);

  const setSectionCollapsed = (section: SidebarSectionKey, collapsed: boolean) => {
    setCollapsedSections(current =>
      current[section] === collapsed ? current : { ...current, [section]: collapsed },
    );
  };

  const renderCollapseButton = (label: string, section: SidebarSectionKey) => (
    <AvailabilitySidebarCollapseButton
      label={label}
      onCollapse={() => setSectionCollapsed(section, true)}
    />
  );

  if (isLoading) {
    return <div className={styles.state}>Loading availability editor...</div>;
  }

  if (hasLoadError) {
    return <ErrorBanner className={styles.banner}>Could not load this availability group.</ErrorBanner>;
  }

  return (
    <AvailabilityWorkspaceLayout
      className={allSectionsCollapsed ? styles.layoutAllCollapsed : undefined}
      sidebarColumnClassName={joinClassNames(
        styles.sidebarColumn,
        allSectionsCollapsed ? styles.sidebarColumnAllCollapsed : undefined,
      )}
      sidebarContentClassName={joinClassNames(
        styles.sidebar,
        allSectionsCollapsed ? styles.sidebarAllCollapsed : undefined,
      )}
      mainColumnClassName={joinClassNames(
        styles.mainColumn,
        allSectionsCollapsed ? styles.mainColumnAllCollapsed : undefined,
      )}
      mainBlockClassName={styles.mainBlock}
      sidebar={
        <>
          <AvailabilitySidebarSection
            label="Information"
            collapsed={collapsedSections.information}
            collapsedOffset={allSectionsCollapsed ? "flush" : "default"}
            onExpand={() => setSectionCollapsed("information", false)}
          >
            <AvailabilityInformationCard
              name={name}
              month={month}
              year={year}
              errors={informationErrors}
              headerRightSlot={renderCollapseButton("Information", "information")}
              onNameChange={onNameChange}
              onMonthChange={onMonthChange}
              onYearChange={onYearChange}
            />
          </AvailabilitySidebarSection>

          <AvailabilitySidebarSection
            label="Employee"
            collapsed={collapsedSections.employee}
            collapsedOffset={allSectionsCollapsed ? "flush" : "default"}
            onExpand={() => setSectionCollapsed("employee", false)}
          >
            <AvailabilityEmployeeCard
              employees={employees}
              selectedEmployeeId={selectedEmployeeId}
              employeeSearchText={employeeSearchText}
              assignedEmployees={assignedEmployees}
              groupError={employeeError}
              headerRightSlot={renderCollapseButton("Employee", "employee")}
              onEmployeeSearchTextChange={onEmployeeSearchTextChange}
              onSelectedEmployeeIdChange={onSelectedEmployeeIdChange}
              onAddEmployee={onAddEmployee}
              onRemoveEmployee={onRemoveEmployee}
            />
          </AvailabilitySidebarSection>

          <AvailabilitySidebarSection
            label="Bind Information"
            collapsed={collapsedSections.bind}
            collapsedOffset={allSectionsCollapsed ? "flush" : "default"}
            onExpand={() => setSectionCollapsed("bind", false)}
          >
            <AvailabilityBindCard
              binds={binds}
              selectedBindClientId={selectedBindClientId}
              isLoading={isBindsLoading}
              isBusy={isBindBusy}
              errorMessage={bindErrorMessage}
              headerRightSlot={renderCollapseButton("Bind Information", "bind")}
              onSelectedBindChange={onSelectedBindChange}
              onBindFieldChange={onBindFieldChange}
              onBindCommit={onBindCommit}
              onAddBind={onAddBind}
              onDeleteBind={onDeleteBind}
            />
          </AvailabilitySidebarSection>
        </>
      }
      main={
        <AvailabilityScheduleMatrix
          className={styles.scheduleCard}
          year={year}
          month={month}
          columns={columns}
          cellMap={cellMap}
          cellErrors={cellErrors}
          headerCenterSlot={
            errorMessage ? (
              <div className={styles.scheduleHeaderMessage} role="alert" aria-live="polite">
                {errorMessage}
              </div>
            ) : null
          }
          headerRightSlot={
            <IosButton
              className={styles.scheduleSaveButton}
              label={isSaving ? "Saving..." : "Save Changes"}
              icon={<SaveIcon size={18} />}
              onClick={onSave}
              disabled={isSaving}
            />
          }
          bindValueByKey={bindValueByKey}
          onColumnMove={onColumnMove}
          onCellChange={onCellChange}
        />
      }
    />
  );
}

