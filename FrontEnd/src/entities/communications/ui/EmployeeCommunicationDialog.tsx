import { useEffect, useMemo, useRef, useState } from "react";
import type { PointerEvent } from "react";
import {
  useDismissEmployeeCommunicationMutation,
  useEmployeePendingCommunicationsQuery,
  type CommunicationMessageDto,
} from "@entities/communications";
import { getErrorMessage } from "@shared/api/httpClient";
import { pushErrorAlertFromError } from "@shared/ui/feedback/error-alerts/errorAlerts";
import { ChevronLeftIcon, ChevronRightIcon, CloseIcon, NoteIcon } from "@shared/ui/icons";
import styles from "./EmployeeCommunicationDialog.module.css";

const emptyMessages: CommunicationMessageDto[] = [];
const deadlineFormatter = new Intl.DateTimeFormat(undefined, {
  month: "short",
  day: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

function runMutation<TData, TVariables>(
  mutate: (variables: TVariables, callbacks?: { onSuccess?: (data: TData) => void; onError?: (error: unknown) => void }) => void,
  variables: TVariables,
) {
  return new Promise<TData>((resolve, reject) => {
    mutate(variables, {
      onSuccess: resolve,
      onError: reject,
    });
  });
}

function formatDeadline(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "until the deadline";
  }

  return `until ${deadlineFormatter.format(date)}`;
}

type EmployeeCommunicationDialogProps = {
  employeeId: number | null;
};

export function EmployeeCommunicationDialog({ employeeId }: EmployeeCommunicationDialogProps) {
  const pendingQuery = useEmployeePendingCommunicationsQuery(employeeId);
  const dismissMutation = useDismissEmployeeCommunicationMutation();
  const [hiddenIds, setHiddenIds] = useState<Set<number>>(() => new Set());
  const [activeIndex, setActiveIndex] = useState(0);
  const [slideDirection, setSlideDirection] = useState<"next" | "previous">("next");
  const [isClosedForSession, setIsClosedForSession] = useState(false);
  const [doNotShowAgain, setDoNotShowAgain] = useState(false);
  const [inlineError, setInlineError] = useState<string | null>(null);
  const pointerStartXRef = useRef<number | null>(null);
  const messages = pendingQuery.data ?? emptyMessages;
  const visibleMessages = useMemo(
    () => messages.filter(message => !hiddenIds.has(message.id)),
    [hiddenIds, messages],
  );
  const activeMessage = visibleMessages[activeIndex] ?? visibleMessages[0] ?? null;

  useEffect(() => {
    setHiddenIds(new Set());
    setActiveIndex(0);
    setIsClosedForSession(false);
  }, [employeeId]);

  useEffect(() => {
    if (visibleMessages.length === 0) {
      setActiveIndex(0);
      return;
    }

    setActiveIndex(current => Math.min(current, visibleMessages.length - 1));
  }, [visibleMessages.length]);

  useEffect(() => {
    setDoNotShowAgain(false);
    setInlineError(null);
  }, [activeMessage?.id]);

  if (isClosedForSession || !activeMessage || pendingQuery.isLoading || pendingQuery.error) {
    return null;
  }

  const hideActiveMessage = (communicationId: number) => {
    setHiddenIds(current => {
      const next = new Set(current);
      next.add(communicationId);
      return next;
    });
  };

  const showMessage = (nextIndex: number, direction: "next" | "previous") => {
    const messageCount = visibleMessages.length;
    if (messageCount <= 1) {
      return;
    }

    setSlideDirection(direction);
    setActiveIndex((nextIndex + messageCount) % messageCount);
  };

  const showPrevious = () => showMessage(activeIndex - 1, "previous");
  const showNext = () => showMessage(activeIndex + 1, "next");

  const handlePointerDown = (event: PointerEvent<HTMLElement>) => {
    pointerStartXRef.current = event.clientX;
  };

  const handlePointerUp = (event: PointerEvent<HTMLElement>) => {
    const startX = pointerStartXRef.current;
    const endX = event.clientX;
    pointerStartXRef.current = null;

    if (startX === null || Math.abs(endX - startX) < 44) {
      return;
    }

    if (endX > startX) {
      showPrevious();
      return;
    }

    showNext();
  };

  const handleClose = async () => {
    if (!doNotShowAgain) {
      setIsClosedForSession(true);
      return;
    }

    const dismissedId = activeMessage.id;
    try {
      await runMutation(dismissMutation.mutate, dismissedId);
      hideActiveMessage(dismissedId);
      if (visibleMessages.length <= 1) {
        setIsClosedForSession(true);
      }
    } catch (error) {
      const message = getErrorMessage(error, "Could not save your communication preference.");
      setInlineError(message);
      pushErrorAlertFromError(error, "Could not save your communication preference.");
    }
  };

  return (
    <div className={styles.backdrop} role="presentation">
      <section
        className={styles.dialog}
        role="dialog"
        aria-modal="true"
        aria-labelledby="employee-communication-title"
        onPointerDown={handlePointerDown}
        onPointerUp={handlePointerUp}
      >
        <div
          key={activeMessage.id}
          className={[
            styles.slide,
            slideDirection === "next" ? styles.slideNext : styles.slidePrevious,
          ].join(" ")}
          aria-live="polite"
        >
          <div className={styles.header}>
            <span className={styles.icon} aria-hidden="true">
              <NoteIcon size={18} />
            </span>
            <div>
              <span className={styles.eyebrow}>Communication</span>
              <h2 id="employee-communication-title">{activeMessage.title}</h2>
            </div>
            <button
              type="button"
              className={styles.iconButton}
              onClick={() => void handleClose()}
              aria-label="Close communication"
              disabled={dismissMutation.isPending}
            >
              <CloseIcon size={16} />
            </button>
          </div>

          <p className={styles.body}>{activeMessage.body}</p>

          <div className={styles.metaLine}>
            <span>{formatDeadline(activeMessage.deadlineAtUtc)}</span>
            <span>From {activeMessage.createdByManagerName}</span>
            {visibleMessages.length > 1 ? (
              <span>{activeIndex + 1} of {visibleMessages.length}</span>
            ) : null}
          </div>
        </div>

        {visibleMessages.length > 1 ? (
          <div className={styles.carouselNavigation} aria-label="Communication navigation">
            <button
              type="button"
              className={styles.carouselButton}
              onClick={showPrevious}
              aria-label="Previous communication"
            >
              <ChevronLeftIcon size={17} />
            </button>

            <div className={styles.paginationDots}>
              {visibleMessages.map((message, index) => (
                <button
                  key={message.id}
                  type="button"
                  className={[
                    styles.paginationDot,
                    index === activeIndex ? styles.paginationDotActive : "",
                  ].filter(Boolean).join(" ")}
                  onClick={() => showMessage(index, index >= activeIndex ? "next" : "previous")}
                  aria-label={`Show communication ${index + 1} of ${visibleMessages.length}`}
                  aria-current={index === activeIndex ? "true" : undefined}
                />
              ))}
            </div>

            <button
              type="button"
              className={styles.carouselButton}
              onClick={showNext}
              aria-label="Next communication"
            >
              <ChevronRightIcon size={17} />
            </button>
          </div>
        ) : null}

        {inlineError ? <div className={styles.errorText}>{inlineError}</div> : null}

        <div className={styles.footer}>
          <label className={styles.checkboxRow}>
            <input
              type="checkbox"
              checked={doNotShowAgain}
              onChange={event => setDoNotShowAgain(event.target.checked)}
              disabled={dismissMutation.isPending}
            />
            <span>Don't show this again</span>
          </label>

          <button
            type="button"
            className={styles.primaryButton}
            onClick={() => void handleClose()}
            disabled={dismissMutation.isPending}
          >
            {dismissMutation.isPending ? "Saving..." : "Close"}
          </button>
        </div>
      </section>
    </div>
  );
}
