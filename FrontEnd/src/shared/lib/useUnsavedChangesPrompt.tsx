import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigationBlocker } from "./react-router-dom";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";

type UseUnsavedChangesPromptOptions = {
  when: boolean;
  title?: string;
  message?: string;
  confirmText?: string;
  cancelText?: string;
};

type PendingActionKind = "route" | "local" | null;

export function useUnsavedChangesPrompt({
  when,
  title = "Leave without saving?",
  message = "You have unsaved changes on this page. If you leave now, those changes will be lost.",
  confirmText = "Leave page",
  cancelText = "Stay here",
}: UseUnsavedChangesPromptOptions) {
  const [pendingActionKind, setPendingActionKind] = useState<PendingActionKind>(null);
  const localActionRef = useRef<(() => void) | null>(null);
  const bypassPromptRef = useRef(false);

  const handleRouteBlocked = useCallback(() => {
    localActionRef.current = null;
    setPendingActionKind("route");
  }, []);

  const shouldBlockRoute = useCallback(() => when && !bypassPromptRef.current, [when]);

  const {
    proceedBlockedNavigation,
    cancelBlockedNavigation,
  } = useNavigationBlocker(when, shouldBlockRoute, handleRouteBlocked);

  useEffect(() => {
    if (when) {
      return;
    }

    localActionRef.current = null;
    setPendingActionKind(null);
    cancelBlockedNavigation();
  }, [cancelBlockedNavigation, when]);

  useEffect(() => {
    if (!when) {
      return;
    }

    const handleBeforeUnload = (event: BeforeUnloadEvent) => {
      if (bypassPromptRef.current) {
        return;
      }

      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [when]);

  const runWithoutPrompt = useCallback((action: () => void) => {
    bypassPromptRef.current = true;

    try {
      action();
    } finally {
      window.setTimeout(() => {
        bypassPromptRef.current = false;
      }, 0);
    }
  }, []);

  const confirmIfNeeded = useCallback((action: () => void) => {
    if (!when || bypassPromptRef.current) {
      action();
      return true;
    }

    localActionRef.current = action;
    setPendingActionKind("local");
    return false;
  }, [when]);

  const handleCancel = useCallback(() => {
    localActionRef.current = null;
    setPendingActionKind(null);
    cancelBlockedNavigation();
  }, [cancelBlockedNavigation]);

  const handleConfirm = useCallback(() => {
    const nextActionKind = pendingActionKind;
    const localAction = localActionRef.current;

    localActionRef.current = null;
    setPendingActionKind(null);

    if (nextActionKind === "local" && localAction) {
      runWithoutPrompt(localAction);
      return;
    }

    runWithoutPrompt(() => {
      proceedBlockedNavigation();
    });
  }, [pendingActionKind, proceedBlockedNavigation, runWithoutPrompt]);

  const dialog = useMemo(() => (
    <ConfirmDialog
      open={pendingActionKind !== null}
      title={title}
      message={message}
      onCancel={handleCancel}
      onConfirm={handleConfirm}
      confirmText={confirmText}
      cancelText={cancelText}
    />
  ), [cancelText, confirmText, handleCancel, handleConfirm, message, pendingActionKind, title]);

  return {
    confirmIfNeeded,
    runWithoutPrompt,
    dialog,
  };
}
