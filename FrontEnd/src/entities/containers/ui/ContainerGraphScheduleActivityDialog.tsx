import { useId, useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { t } from "@shared/i18n";
import { CheckIcon, ClockIcon, CloseIcon, ScheduleDetailsIcon, StatisticsIcon, SwapOffersIcon } from "@shared/ui/icons";
import selectStyles from "@shared/ui/components/SearchableSelect/SearchableSelect.module.css";
import activity from "./ContainerGraphActivity.module.css";
import styles from "./ContainerGraphScheduleActivityDialog.module.css";

export type ScheduleActivitySummary = {
  swaps: number;
  manual: number;
  corrections: number;
  contractApprovals: number;
  total: number;
  pending: number;
  approvedOrAccepted: number;
  rejected: number;
  offered: number;
};

type ContainerGraphScheduleActivityDialogProps = {
  open: boolean;
  onClose: () => void;
  summary: ScheduleActivitySummary;
  swapPanel: ReactNode;
  manualPanel: ReactNode;
  correctionsPanel: ReactNode;
};

const tabKeys = ["all", "corrections", "manual", "swaps"] as const;
type TabKey = typeof tabKeys[number];
const tabbableSelector = "button, a[href], input, select, textarea, [tabindex], [contenteditable='true']";
const openSelectSelector = '[aria-haspopup="listbox"][aria-expanded="true"]';

function hasOtherModal(root: HTMLElement) {
  return Array.from(document.querySelectorAll('[aria-modal="true"]')).some(element =>
    element !== root && !element.closest("[hidden], [inert], [aria-hidden='true']"),
  );
}

function getTabStops(root: HTMLElement) {
  return Array.from(root.querySelectorAll<HTMLElement>(tabbableSelector)).filter(element =>
    element.tabIndex >= 0 && !element.matches(":disabled, input[type='hidden']") &&
    !element.closest("[hidden], [inert], [aria-hidden='true']"),
  );
}

function OpenScheduleActivityDialog({
  onClose, summary, swapPanel, manualPanel, correctionsPanel,
}: Omit<ContainerGraphScheduleActivityDialogProps, "open">) {
  const id = useId();
  const [active, setActive] = useState<TabKey>("all");
  const dialogRef = useRef<HTMLDivElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);
  const onCloseRef = useRef(onClose);
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([]);
  useLayoutEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  useLayoutEffect(() => {
    const root = dialogRef.current!;
    const previousFocus = document.activeElement;
    const previousOverflow = document.body.style.overflow;
    const previousRootOverflow = document.documentElement.style.overflow;
    document.body.style.overflow = "hidden";
    document.documentElement.style.overflow = "hidden";
    closeRef.current?.focus();

    // The manual recipient selector renders its dropdown through a body portal.
    const getOpenSelect = () => root.querySelector<HTMLElement>(openSelectSelector);
    const getSelectPortal = (trigger: HTMLElement | null) => trigger && Array.from(
      document.querySelectorAll<HTMLElement>(`.${selectStyles.dropdownPortal}`),
    ).find(element =>
      element.querySelector('[role="listbox"]')?.getAttribute("aria-label") === trigger.getAttribute("aria-label"),
    );
    const handleFocus = (event: FocusEvent) => {
      if (hasOtherModal(root) || !(event.target instanceof Node)) return;
      if (!root.contains(event.target) && !getSelectPortal(getOpenSelect())?.contains(event.target)) {
        closeRef.current?.focus();
      }
    };
    const handleKey = (event: KeyboardEvent) => {
      if ((event.key !== "Escape" && event.key !== "Tab") || hasOtherModal(root)) return;
      const select = getOpenSelect();
      if (event.key === "Escape") {
        if (select) {
          select.focus();
          return;
        }
        event.preventDefault();
        event.stopPropagation();
        onCloseRef.current();
        return;
      }
      const activeElement = document.activeElement;
      const portal = getSelectPortal(select);
      const stops = getTabStops(root);
      if (select && portal) {
        stops.splice(stops.indexOf(select) + 1, 0, ...getTabStops(portal));
      }
      const index = stops.findIndex(element => element === activeElement);
      const step = event.shiftKey ? -1 : 1;
      if (portal || index === -1 || !stops[index + step]) {
        event.preventDefault();
        const next = index === -1 ? (event.shiftKey ? stops.at(-1) : stops[0]) : stops.at((index + step) % stops.length);
        (next ?? closeRef.current)?.focus();
      }
    };
    window.addEventListener("keydown", handleKey, true);
    document.addEventListener("focusin", handleFocus);
    return () => {
      window.removeEventListener("keydown", handleKey, true);
      document.removeEventListener("focusin", handleFocus);
      document.body.style.overflow = previousOverflow;
      document.documentElement.style.overflow = previousRootOverflow;
      if (previousFocus instanceof HTMLElement && previousFocus.isConnected) previousFocus.focus();
    };
  }, []);

  function selectTab(key: TabKey) {
    if (key === active) return;
    dialogRef.current?.querySelector<HTMLButtonElement>(openSelectSelector)?.click();
    setActive(key);
  }

  const tabs: Record<TabKey, { label: string; count: number }> = {
    all: { label: t("All"), count: summary.total },
    corrections: { label: t("Shift Corrections"), count: summary.corrections + summary.contractApprovals },
    manual: { label: t("Manual shifts"), count: summary.manual },
    swaps: { label: t("Swap log"), count: summary.swaps },
  };
  const panels = { swaps: swapPanel, manual: manualPanel, corrections: correctionsPanel };
  const metrics = [
    [t("Total activities"), summary.total, StatisticsIcon, activity.info],
    [t("Pending"), summary.pending, ClockIcon, activity.pending],
    [t("Approved / Accepted"), summary.approvedOrAccepted, CheckIcon, activity.success],
    [t("Rejected"), summary.rejected, CloseIcon, activity.danger],
    [t("Offered"), summary.offered, SwapOffersIcon, activity.pending],
  ] as const;

  return createPortal(
    <div className={styles.overlay} onClick={event => {
      if (event.target === event.currentTarget && dialogRef.current && !hasOtherModal(dialogRef.current)) onClose();
    }}>
      <div ref={dialogRef} className={styles.dialog} role="dialog" aria-modal="true"
        aria-labelledby={`${id}-title`} aria-describedby={`${id}-description`}>
        <header className={styles.header}>
          <span className={styles.icon} aria-hidden="true"><ScheduleDetailsIcon size={23} /></span>
          <div className={styles.heading}>
            <h2 id={`${id}-title`}>{t("Schedule Activity")}</h2>
            <p id={`${id}-description`}>{t("Review schedule activity. Counts reflect loaded records.")}</p>
          </div>
          <button ref={closeRef} type="button" className={styles.close} aria-label={t("Close")} onClick={onClose}><CloseIcon size={18} /></button>
        </header>
        <div role="tablist" aria-labelledby={`${id}-title`} className={styles.tabs}>
          {tabKeys.map((key, index) => (
            <button key={key} ref={element => { tabRefs.current[index] = element; }} type="button" role="tab"
              id={`${id}-tab-${key}`} aria-controls={`${id}-panel-${key}`} aria-selected={active === key} tabIndex={active === key ? 0 : -1}
              onClick={() => selectTab(key)} onKeyDown={event => {
                const count = tabKeys.length;
                let next: number;
                switch (event.key) {
                  case "ArrowLeft": next = (index + count - 1) % count; break;
                  case "ArrowRight": next = (index + 1) % count; break;
                  case "Home": next = 0; break;
                  case "End": next = count - 1; break;
                  default: return;
                }
                event.preventDefault();
                selectTab(tabKeys[next]);
                tabRefs.current[next]?.focus();
              }}>
              {tabs[key].label}<span>{tabs[key].count}</span>
            </button>
          ))}
        </div>
        <dl className={styles.summary}>
          {metrics.map(([label, count, Icon, tone]) => (
            <div key={label} className={`${styles.metric} ${tone}`}>
              <dt><span className={styles.metricIcon} aria-hidden="true"><Icon size={16} /></span>{label}</dt>
              <dd>{count}</dd>
            </div>
          ))}
        </dl>
        <div className={styles.panels} data-all={active === "all" || undefined} id={`${id}-panel-all`}
          role={active === "all" ? "tabpanel" : undefined} aria-labelledby={active === "all" ? `${id}-tab-all` : undefined}>
          {(["swaps", "manual", "corrections"] as const).map(key => (
            <div key={key} className={styles.panel} id={`${id}-panel-${key}`}
              role={active === "all" ? "region" : "tabpanel"} aria-labelledby={`${id}-tab-${key}`}
              hidden={active !== "all" && active !== key} tabIndex={0}>
              {panels[key]}
            </div>
          ))}
        </div>
      </div>
    </div>,
    document.body,
  );
}

export function ContainerGraphScheduleActivityDialog({ open, ...props }: ContainerGraphScheduleActivityDialogProps) {
  return open ? <OpenScheduleActivityDialog {...props} /> : null;
}
