import { useEffect, useRef, useState } from "react";
import type { CSSProperties } from "react";
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

const DESKTOP_MEDIA_QUERY = "(min-width: 1181px)";

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
  isHeaderCollapsed?: boolean;
  compactSize?: boolean;
  informationErrors?: AvailabilityInformationErrors;
  employeeError?: string;
  employees: Employee[];
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
  isHeaderCollapsed = false,
  compactSize = false,
  informationErrors,
  employeeError,
  employees,
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
  const [isDesktopLayout, setIsDesktopLayout] = useState(false);
  const [viewportHeight, setViewportHeight] = useState(0);
  const [compactMatrixMeasurement, setCompactMatrixMeasurement] = useState<{ key: string; height: number } | null>(null);
  const matrixCardShellRef = useRef<HTMLDivElement | null>(null);

  const allSectionsCollapsed = Object.values(collapsedSections).every(Boolean);
  const compactMatrixMeasurementKey = [
    viewportHeight,
    isHeaderCollapsed ? "collapsed" : "expanded",
    year,
    month,
    columns.length,
    allSectionsCollapsed ? "sidebar-collapsed" : "sidebar-open",
    errorMessage ?? "",
  ].join(":");

  useEffect(() => {
    if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
      return;
    }

    const mediaQuery = window.matchMedia(DESKTOP_MEDIA_QUERY);
    const handleChange = () => {
      setIsDesktopLayout(mediaQuery.matches);
      setViewportHeight(window.innerHeight);
    };

    handleChange();

    window.addEventListener("resize", handleChange);

    if (typeof mediaQuery.addEventListener === "function") {
      mediaQuery.addEventListener("change", handleChange);
      return () => {
        window.removeEventListener("resize", handleChange);
        mediaQuery.removeEventListener("change", handleChange);
      };
    }

    mediaQuery.addListener(handleChange);
    return () => {
      window.removeEventListener("resize", handleChange);
      mediaQuery.removeListener(handleChange);
    };
  }, []);

  useEffect(() => {
    if (!compactSize || !isDesktopLayout) {
      return;
    }

    const frameId = window.requestAnimationFrame(() => {
      const nextHeight = Math.ceil(matrixCardShellRef.current?.scrollHeight ?? 0);
      if (nextHeight > 0) {
        setCompactMatrixMeasurement(current =>
          current?.key === compactMatrixMeasurementKey && current.height === nextHeight
            ? current
            : { key: compactMatrixMeasurementKey, height: nextHeight },
        );
      }
    });

    return () => {
      window.cancelAnimationFrame(frameId);
    };
  }, [
    compactSize,
    isDesktopLayout,
    compactMatrixMeasurementKey,
  ]);

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

  const scheduleMatrixBaseMinHeight =
    isDesktopLayout
      ? Math.max(isHeaderCollapsed ? 680 : 660, viewportHeight - (isHeaderCollapsed ? 188 : 228))
      : null;
  const scheduleMatrixCardHeight =
    scheduleMatrixBaseMinHeight !== null
      ? Math.min(isHeaderCollapsed ? 1000 : 980, Math.round(scheduleMatrixBaseMinHeight * 1.15))
      : null;
  const compactMatrixHeight =
    compactSize && compactMatrixMeasurement?.key === compactMatrixMeasurementKey
      ? compactMatrixMeasurement.height
      : null;
  const resolvedCompactMatrixHeight =
    compactSize && scheduleMatrixCardHeight !== null && compactMatrixHeight !== null
      ? Math.min(scheduleMatrixCardHeight, compactMatrixHeight)
      : null;
  const scheduleMatrixCardShellStyle =
    isDesktopLayout && scheduleMatrixCardHeight !== null
      ? {
        maxHeight: `${scheduleMatrixCardHeight}px`,
        ...(compactSize
          ? resolvedCompactMatrixHeight !== null
            ? {
              minHeight: `${resolvedCompactMatrixHeight}px`,
              height: `${resolvedCompactMatrixHeight}px`,
            }
            : {}
          : {
            minHeight: `${scheduleMatrixCardHeight}px`,
            height: `${scheduleMatrixCardHeight}px`,
          }),
      }
      : undefined;
  const matrixCardCssVariables = {
    "--matrix-card-padding-bottom": "18px",
    "--matrix-layout-padding-bottom": "0px",
  } as CSSProperties;
  const matrixCardStyle =
    isDesktopLayout
      ? compactSize
        ? resolvedCompactMatrixHeight !== null
          ? {
            ...matrixCardCssVariables,
            height: "100%",
            maxHeight: "100%",
          }
          : matrixCardCssVariables
        : {
          ...matrixCardCssVariables,
          height: "100%",
          maxHeight: "100%",
        }
      : matrixCardCssVariables;

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
              assignedEmployees={assignedEmployees}
              groupError={employeeError}
              headerRightSlot={renderCollapseButton("Employee", "employee")}
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
        <div
          ref={matrixCardShellRef}
          className={joinClassNames(
            styles.matrixCardShell,
            styles.scheduleMatrixCardShell,
            compactSize ? styles.matrixCardShellCompact : undefined,
          )}
          style={scheduleMatrixCardShellStyle}
        >
          <AvailabilityScheduleMatrix
            className={joinClassNames(
              styles.matrixCard,
              styles.trimmedMatrixCard,
            )}
            style={matrixCardStyle}
            compactSize={compactSize}
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
        </div>
      }
    />
  );
}

