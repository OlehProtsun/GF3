import type { CSSProperties, ReactNode } from "react";
import styles from "./ContainerGraphSessionTabs.module.css";

type ContainerGraphSessionTab = {
  graphId: number;
  label: string;
  active: boolean;
};

type ContainerGraphSessionTabsProps = {
  items: ContainerGraphSessionTab[];
  onSelect: (graphId: number) => void;
  actionSlot?: ReactNode;
  ariaLabel?: string;
  className?: string;
};

function joinClassNames(...values: Array<string | false | null | undefined>) {
  return values.filter(Boolean).join(" ");
}

export function ContainerGraphSessionTabs({
  items,
  onSelect,
  actionSlot,
  ariaLabel = "Open schedules",
  className,
}: ContainerGraphSessionTabsProps) {
  const visibleTabCount = Math.min(Math.max(items.length, 1), 3);
  const rootStyle = {
    "--session-visible-tab-count": String(visibleTabCount),
  } as CSSProperties;

  return (
    <div className={joinClassNames(styles.root, className)} style={rootStyle}>
      <div className={styles.tabsRail}>
        <div className={styles.tabs} role="tablist" aria-label={ariaLabel}>
          {items.map(item => (
            <button
              key={item.graphId}
              type="button"
              role="tab"
              aria-selected={item.active}
              className={joinClassNames(styles.tab, item.active && styles.tabActive)}
              onClick={() => onSelect(item.graphId)}
            >
              <span className={styles.tabLabel}>{item.label}</span>
            </button>
          ))}
        </div>
      </div>

      {actionSlot ? <div className={styles.actions}>{actionSlot}</div> : null}
    </div>
  );
}
