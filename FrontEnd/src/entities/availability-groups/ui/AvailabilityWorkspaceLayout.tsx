import type { ReactNode } from "react";
import styles from "./AvailabilityWorkspaceLayout.module.css";

type AvailabilityWorkspaceLayoutProps = {
  sidebar?: ReactNode;
  main: ReactNode;
  className?: string;
  sidebarColumnClassName?: string;
  sidebarContentClassName?: string;
  mainColumnClassName?: string;
  mainBlockClassName?: string;
};

function joinClassNames(...values: Array<string | undefined>) {
  return values.filter(Boolean).join(" ");
}

export function AvailabilityWorkspaceLayout({
  sidebar,
  main,
  className,
  sidebarColumnClassName,
  sidebarContentClassName,
  mainColumnClassName,
  mainBlockClassName,
}: AvailabilityWorkspaceLayoutProps) {
  return (
    <div className={joinClassNames(styles.layout, className)}>
      {sidebar ? (
        <div className={joinClassNames(styles.sidebarColumn, sidebarColumnClassName)}>
          <div className={joinClassNames(styles.sidebarContent, sidebarContentClassName)}>{sidebar}</div>
        </div>
      ) : null}

      <div className={joinClassNames(styles.mainColumn, mainColumnClassName)}>
        <div className={joinClassNames(styles.mainBlock, mainBlockClassName)}>{main}</div>
      </div>
    </div>
  );
}
