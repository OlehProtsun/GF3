import type { ReactNode } from "react";
import { ArrowIcon } from "@shared/ui/icons";
import styles from "./AvailabilitySidebarSection.module.css";

type AvailabilitySidebarSectionProps = {
  label: string;
  collapsed: boolean;
  collapsedIcon?: ReactNode;
  collapsedOffset?: "default" | "compact" | "flush";
  attention?: boolean;
  preserveCollapsedOnMobile?: boolean;
  onExpand: () => void;
  children: ReactNode;
};

type AvailabilitySidebarCollapseButtonProps = {
  label: string;
  onCollapse: () => void;
};

function joinClassNames(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
}

export function AvailabilitySidebarSection({
  label,
  collapsed,
  collapsedIcon,
  collapsedOffset = "default",
  attention = false,
  preserveCollapsedOnMobile = false,
  onExpand,
  children,
}: AvailabilitySidebarSectionProps) {
  const collapsedClassName =
    collapsedOffset === "compact"
      ? styles.sectionShellCollapsedCompact
      : collapsedOffset === "flush"
        ? styles.sectionShellCollapsedFlush
        : styles.sectionShellCollapsedDefault;

  return (
    <div
      className={joinClassNames(
        styles.sectionShell,
        collapsed ? collapsedClassName : styles.sectionShellExpanded,
        collapsed && attention ? styles.sectionShellAttention : undefined,
        preserveCollapsedOnMobile ? styles.sectionShellPreserveCollapsed : undefined,
      )}
    >
      <button
        type="button"
        className={styles.sectionExpandButton}
        onClick={onExpand}
        aria-label={`Expand ${label}`}
        title={`Expand ${label}`}
      >
        {collapsedIcon ?? <ArrowIcon size={16} className={styles.sectionExpandArrow} />}
      </button>

      <div className={styles.sectionCardWrap}>{children}</div>
    </div>
  );
}

export function AvailabilitySidebarCollapseButton({
  label,
  onCollapse,
}: AvailabilitySidebarCollapseButtonProps) {
  return (
    <button
      type="button"
      className={styles.sectionCollapseButton}
      onClick={onCollapse}
      aria-label={`Collapse ${label}`}
      title={`Collapse ${label}`}
    >
      <ArrowIcon size={16} className={styles.sectionCollapseArrow} />
    </button>
  );
}

