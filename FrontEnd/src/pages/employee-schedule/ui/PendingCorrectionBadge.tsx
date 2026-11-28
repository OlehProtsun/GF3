import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { t } from "@shared/i18n";
import { useDropdownPosition } from "@shared/lib/useDropdownPosition";
import { CheckIcon, ClockIcon, DoubleCheckIcon, SwapOffersIcon } from "@shared/ui/icons";
import type { ScheduleShiftStatus } from "./scheduleShiftStatuses";
import styles from "./PendingCorrectionBadge.module.css";

function StatusBadge({ title, tone, icon, children }: {
  title: string;
  tone: "correction" | "open" | "corrected" | "accepted";
  icon: ReactNode;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const id = useId();
  const anchorRef = useRef<HTMLButtonElement>(null);
  const bubbleRef = useRef<HTMLDivElement>(null);
  const position = useDropdownPosition(open, anchorRef, bubbleRef, "down", 250);

  useEffect(() => {
    if (!open) return;
    const closeOutside = (event: PointerEvent) => {
      if (event.target instanceof Node &&
        !anchorRef.current?.contains(event.target) && !bubbleRef.current?.contains(event.target)) setOpen(false);
    };
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        event.stopPropagation();
        setOpen(false);
      }
    };
    document.addEventListener("pointerdown", closeOutside);
    document.addEventListener("keydown", closeOnEscape, true);
    return () => {
      document.removeEventListener("pointerdown", closeOutside);
      document.removeEventListener("keydown", closeOnEscape, true);
    };
  }, [open]);

  return <>
    <button ref={anchorRef} type="button" className={styles.badge} data-tone={tone} aria-label={title}
      aria-expanded={open} aria-controls={open ? id : undefined} onClick={() => setOpen(value => !value)}>
      {icon}
    </button>
    {open && createPortal(
      <div ref={bubbleRef} id={id} style={position} className={styles.bubble} data-tone={tone} role="status">
        <div><strong>{title}</strong>{children}</div>
      </div>, document.body,
    )}
  </>;
}

export function ScheduleShiftBadges({ status }: { status: ScheduleShiftStatus }) {
  return <>
    {status.correctionPending && (
      <StatusBadge title={t("Correction requested")} tone="correction" icon={<ClockIcon size={13} />}>
        <p>{t("This shift has a pending correction request and is waiting for manager approval.")}</p>
      </StatusBadge>
    )}
    {status.open.length > 0 && (
      <StatusBadge key="open" title={t("Swap pending")} tone="open" icon={<SwapOffersIcon size={13} />}>
        <p>{t("This shift has been offered for a swap and is waiting for acceptance.")}</p>
      </StatusBadge>
    )}
    {status.correctionApproved && (
      <StatusBadge title={t("Correction approved")} tone="corrected" icon={<DoubleCheckIcon size={13} />}>
        <p>{t("Your manager approved a correction for this day and updated the schedule.")}</p>
      </StatusBadge>
    )}
    {status.accepted.length > 0 && (
      <StatusBadge key="accepted" title={t("Swap completed")} tone="accepted" icon={<CheckIcon size={13} />}>
        <p>{t("This shift was successfully transferred or exchanged.")}</p>
      </StatusBadge>
    )}
  </>;
}
