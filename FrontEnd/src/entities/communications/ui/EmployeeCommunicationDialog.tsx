import { t } from "@shared/i18n";
import { useEffect, useMemo, useRef, useState } from "react";
import type { PointerEvent } from "react";
import {
  useDismissEmployeeCommunicationMutation,
  useEmployeePendingCommunicationsQuery,
  type CommunicationMessageDto,
} from "@entities/communications";
import { getErrorMessage } from "@shared/api/httpClient";
import { pushErrorAlertFromError } from "@shared/ui/feedback/error-alerts/errorAlerts";
import { ChevronLeftIcon, ChevronRightIcon } from "@shared/ui/icons";
import { CommunicationMessageContent, CommunicationMessageFooter } from "./CommunicationMessageContent";
import { getCommunicationBlocks } from "../model/contentBlocks";
import styles from "./EmployeeCommunicationDialog.module.css";

const emptyMessages: CommunicationMessageDto[] = [];
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
      const message = getErrorMessage(error, t("Could not save your communication preference."));
      setInlineError(message);
      pushErrorAlertFromError(error, t("Could not save your communication preference."));
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
          <CommunicationMessageContent message={activeMessage} index={activeIndex} count={visibleMessages.length}
            onClose={() => void handleClose()} busy={dismissMutation.isPending} blocks={getCommunicationBlocks(activeMessage)} />
        </div>

        {visibleMessages.length > 1 ? (
          <div className={styles.carouselNavigation} aria-label={t("Communication navigation")}>
            <button
              type="button"
              className={styles.carouselButton}
              onClick={showPrevious}
              aria-label={t("Previous communication")}
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
                  aria-label={t("Show communication {0} of {1}", index + 1, visibleMessages.length)}
                  aria-current={index === activeIndex ? "true" : undefined}
                />
              ))}
            </div>

            <button
              type="button"
              className={styles.carouselButton}
              onClick={showNext}
              aria-label={t("Next communication")}
            >
              <ChevronRightIcon size={17} />
            </button>
          </div>
        ) : null}

        {inlineError ? <div className={styles.errorText}>{inlineError}</div> : null}

        <CommunicationMessageFooter doNotShowAgain={doNotShowAgain} onChange={setDoNotShowAgain}
          onClose={() => void handleClose()} busy={dismissMutation.isPending} />
      </section>
    </div>
  );
}
