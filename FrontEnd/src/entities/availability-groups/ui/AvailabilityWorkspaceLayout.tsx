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
  preserveSideLayoutOnMobile?: boolean;
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
  preserveSideLayoutOnMobile = false,
}: AvailabilityWorkspaceLayoutProps) {
  return (
    <div className={joinClassNames(
      styles.layout,
      preserveSideLayoutOnMobile ? styles.layoutPreserveSideLayout : undefined,
      className,
    )}>
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
