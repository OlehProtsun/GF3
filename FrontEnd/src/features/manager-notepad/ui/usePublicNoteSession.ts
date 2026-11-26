import { useEffect, useEffectEvent, useState } from "react";
import { managerNotepadApi, type ManagerNote } from "@entities/manager-notepad";
import { ApiError } from "@shared/api/httpClient";

type Status = "connecting" | "active" | "busy" | "lost" | "unavailable";

export function usePublicNoteSession(
  noteId: number | null,
  onAcquired: (note: ManagerNote) => void,
  onChange: () => void,
) {
  const [attempt, retry] = useState(0);
  const [invalidatedNoteId, setInvalidated] = useState<number | null>(null);
  const invalidated = noteId !== null && noteId === invalidatedNoteId;
  const [session, setSession] = useState<{ noteId: number; attempt: number; status: Status; token?: string }>();
  const acquired = useEffectEvent(onAcquired);
  const changed = useEffectEvent(onChange);

  useEffect(() => {
    if (noteId === null) {
      if (invalidatedNoteId !== null) setInvalidated(null);
      return;
    }
    if (invalidated) return;
    // A token is specific to this acquisition, even across tabs for the same manager.
    const token = Array.from(crypto.getRandomValues(new Uint8Array(16)), byte =>
      byte.toString(16).padStart(2, "0")).join("");
    let closed = false;
    let suspended = false;
    let heartbeat: number | undefined;
    let deadline: number | undefined;
    let operation: Promise<ManagerNote> | undefined;
    let leaseStarted = false;
    let hasLease = false;
    const update = (status: Status) => setSession({ noteId, attempt, status, token: status === "active" ? token : undefined });
    const release = (keepalive = false) => {
      if (!leaseStarted) return;
      void managerNotepadApi.releaseEditSession(noteId, token, keepalive)
        .then(() => { if (!closed) changed(); }, () => undefined);
    };
    const lose = (status: Status) => {
      hasLease = false;
      window.clearTimeout(heartbeat);
      window.clearTimeout(deadline);
      update(status);
      changed();
    };

    const request = (first: boolean) => {
      if (closed || suspended || operation || (!first && !hasLease)) return;
      const started = performance.now();
      leaseStarted = true;
      const pending = managerNotepadApi.editSession(noteId, token, first);
      operation = pending;
      void pending.then(note => {
        if (closed || suspended) return;
        if (!first && !hasLease) {
          release();
          return;
        }
        // Use a local monotonic deadline so client wall-clock skew cannot extend the lease.
        const remaining = 55_000 - (performance.now() - started);
        if (remaining <= 0) {
          lose("lost");
          release();
          return;
        }
        hasLease = true;
        if (first) acquired(note);
        update("active");
        if (first) changed();
        window.clearTimeout(deadline);
        deadline = window.setTimeout(() => lose("lost"), remaining);
        heartbeat = window.setTimeout(() => request(false), 20_000);
      }, error => {
        if (closed || suspended) return;
        lose(error instanceof ApiError && error.status === 404 ? "unavailable"
          : first && error instanceof ApiError && error.status === 409 ? "busy" : "lost");
        if (!first) release();
      }).finally(() => { operation = undefined; });
    };

    const pagehide = () => {
      if (closed) return;
      suspended = true;
      lose("lost");
      release(true);
      void operation?.then(() => release(true), () => undefined);
    };
    const reconnect = () => {
      if (hasLease) {
        window.clearTimeout(heartbeat);
        request(false);
      } else if (!closed) changed();
    };
    update("connecting");
    request(true);
    window.addEventListener("pagehide", pagehide);
    window.addEventListener("online", reconnect);
    return () => {
      closed = true;
      window.clearTimeout(heartbeat);
      window.clearTimeout(deadline);
      window.removeEventListener("pagehide", pagehide);
      window.removeEventListener("online", reconnect);
      if (operation) void operation.then(() => release(true), () => release(true));
      else release(true);
    };
  }, [noteId, attempt, invalidated, invalidatedNoteId]);

  const current = noteId !== null && session?.noteId === noteId && session.attempt === attempt ? session : null;
  return {
    token: invalidated ? undefined : current?.token,
    status: noteId === null ? null : invalidated ? "lost" : current?.status ?? "connecting",
    invalidate: () => { setInvalidated(noteId); onChange(); },
    retry: () => { setInvalidated(null); retry(value => value + 1); },
  };
}
