import { useId, useLayoutEffect, useMemo, useRef, useState, type RefObject } from "react";
import { createPortal } from "react-dom";
import { useQueryClient } from "@tanstack/react-query";
import { buildManagerEditLockMessage, useManagerEditLocks, type ManagerEditLockTarget } from "@app/providers/PresenceProvider";
import { ContainerGraphContractApprovalsCard } from "@entities/containers/ui/ContainerGraphContractApprovalsCard";
import { ContainerGraphShiftCorrectionsCard } from "@entities/containers/ui/ContainerGraphShiftCorrectionsCard";
import { queryKeys } from "@shared/api/queryKeys";
import { t } from "@shared/i18n";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { CloseIcon, ExternalLinkIcon, RequestReviewIcon, SwapOffersIcon } from "@shared/ui/icons";
import type { HomeApprovalTarget } from "./homeActivity";
import styles from "./HomeApprovalDialog.module.css";

function hasOtherModal(root: HTMLElement) {
  return [...document.querySelectorAll('[aria-modal="true"]')].some(element =>
    element !== root && !element.closest("[hidden], [inert], [aria-hidden='true']"));
}

export function HomeApprovalDialog({ target, trigger, fallbackFocusRef, onGoToSchedule, onClose }: {
  target: HomeApprovalTarget;
  trigger: HTMLElement;
  fallbackFocusRef: RefObject<HTMLElement | null>;
  onGoToSchedule?: () => void;
  onClose: () => void;
}) {
  const { kind, containerId, graphId, requestId } = target;
  const id = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);
  const [busy, setBusy] = useState(false);
  const [reviewType, setReviewType] = useState<string | null>(null);
  const tryCloseRef = useRef(() => {});
  const client = useQueryClient();
  const targets = useMemo<ManagerEditLockTarget[]>(() => [
    { resourceType: "schedule", resourceId: `${containerId}:${graphId}`, containerId, graphId },
  ], [containerId, graphId]);
  const { isCheckingLocks, isLockedByOther, lockedByOtherState } = useManagerEditLocks(targets);

  useLayoutEffect(() => {
    tryCloseRef.current = () => {
      if (!busy && dialogRef.current && !hasOtherModal(dialogRef.current)) onClose();
    };
  }, [busy, onClose]);

  useLayoutEffect(() => {
    const root = dialogRef.current;
    if (!root) return;
    const fallbackFocus = fallbackFocusRef.current;
    const previousOverflow = document.body.style.overflow;
    const previousRootOverflow = document.documentElement.style.overflow;
    document.body.style.overflow = "hidden";
    document.documentElement.style.overflow = "hidden";
    closeRef.current?.focus();

    const handleFocus = (event: FocusEvent) => {
      if (!hasOtherModal(root) && event.target instanceof Node && !root.contains(event.target)) {
        (closeRef.current?.disabled ? root : closeRef.current)?.focus();
      }
    };
    const handleKey = (event: KeyboardEvent) => {
      if ((event.key !== "Escape" && event.key !== "Tab") || hasOtherModal(root)) return;
      if (event.key === "Escape") {
        event.preventDefault();
        event.stopPropagation();
        tryCloseRef.current();
        return;
      }
      const stops = [...root.querySelectorAll<HTMLElement>("button, a[href], input, select, textarea, [tabindex]")]
        .filter(element => element.tabIndex >= 0 && !element.matches(":disabled, input[type='hidden']") &&
          !element.closest("[hidden], [inert], [aria-hidden='true']"));
      const index = stops.indexOf(document.activeElement as HTMLElement);
      const next = stops[index + (event.shiftKey ? -1 : 1)];
      if (index === -1 || !next) {
        event.preventDefault();
        ((event.shiftKey ? stops.at(-1) : stops[0]) ?? root).focus();
      }
    };
    window.addEventListener("keydown", handleKey, true);
    document.addEventListener("focusin", handleFocus);
    return () => {
      window.removeEventListener("keydown", handleKey, true);
      document.removeEventListener("focusin", handleFocus);
      document.body.style.overflow = previousOverflow;
      document.documentElement.style.overflow = previousRootOverflow;
      (trigger.isConnected ? trigger : fallbackFocus)?.focus({ preventScroll: true });
    };
  }, [fallbackFocusRef, trigger]);

  const Card = kind === "correction" ? ContainerGraphShiftCorrectionsCard : ContainerGraphContractApprovalsCard;
  const TypeIcon = kind === "correction" ? RequestReviewIcon : SwapOffersIcon;
  const scheduleLink = onGoToSchedule && <button type="button" className={styles.scheduleLink} disabled={busy} onClick={onGoToSchedule}>
    <ExternalLinkIcon size={17} />{t("Go to schedule")}
  </button>;
  return createPortal(<div className={styles.overlay} onClick={event => {
    if (event.target === event.currentTarget) tryCloseRef.current();
  }}>
    <div ref={dialogRef} tabIndex={-1} className={styles.dialog} role="dialog" aria-modal="true" aria-labelledby={id}>
      <header className={styles.header}>
        <span className={styles.headerIcon} aria-hidden="true"><TypeIcon size={22} /></span>
        <div className={styles.headerCopy}>
          <h2 id={id}>{t("Review request")}</h2>
          <p>{reviewType ?? t(kind === "correction" ? "Schedule correction" : "Contract approvals")}</p>
        </div>
        <button ref={closeRef} type="button" className={styles.close} aria-label={t("Close")} disabled={busy}
          onClick={() => tryCloseRef.current()}><CloseIcon size={18} /></button>
      </header>
      <div className={styles.content}>
        {isCheckingLocks && <p role="status">{t("Checking schedule edit locks...")}</p>}
        {isLockedByOther && <ErrorBanner dismissible={false}>{buildManagerEditLockMessage(lockedByOtherState, t("This schedule"))}</ErrorBanner>}
        <Card containerId={containerId} graphId={graphId} reviewRequestId={requestId} hasUnsavedChanges={false}
          disabled={isCheckingLocks || isLockedByOther} onReviewBusyChange={setBusy}
          onReviewTypeChange={setReviewType} reviewScheduleLink={scheduleLink}
          onApproved={() => client.invalidateQueries({ queryKey: queryKeys.containers.graphVersions(containerId, graphId) })} />
      </div>
    </div>
  </div>, document.body);
}
