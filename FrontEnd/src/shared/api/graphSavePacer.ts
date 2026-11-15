import { ApiError, RequestCanceledError } from "./httpClient";
import { waitForReadRetry } from "./retry429Read";

export type RunGraphSaveRequest = <T>(send: () => Promise<T>, signal?: AbortSignal) => Promise<T>;

/** A single Save owns its queue; other application traffic is unaffected. */
export function createGraphSavePacer(): {
  run: RunGraphSaveRequest;
  noteRateLimit: (retryAfterMs: number) => void;
} {
  type Job = { signal?: AbortSignal; canceled: boolean; issue: () => void; reject: (error: unknown) => void };
  const queue: Job[] = [];
  let active = 0;
  let starts = 0;
  let lastStart = 0;
  let notBefore = 0;
  let draining = false;

  const noteRateLimit = (retryAfterMs: number) => {
    notBefore = Math.max(notBefore, performance.now() + Math.max(0, retryAfterMs));
  };

  const drain = async () => {
    if (draining) return;
    draining = true;
    try {
      while (queue.length && active < 2) {
        const job = queue[0];
        if (job.canceled || job.signal?.aborted) {
          queue.shift();
          job.reject(new RequestCanceledError());
          continue;
        }
        const delay = Math.max(notBefore, starts >= 12 ? lastStart + 350 : 0) - performance.now();
        if (delay > 0) {
          try { await waitForReadRetry(delay, job.signal); }
          catch (error) { job.canceled = true; job.reject(error); }
          continue;
        }
        queue.shift();
        starts++;
        active++;
        lastStart = performance.now();
        job.issue();
      }
    } finally {
      draining = false;
    }
  };

  const run: RunGraphSaveRequest = (send, signal) => new Promise((resolve, reject) => {
    const abort = () => { job.canceled = true; reject(new RequestCanceledError()); };
    const job: Job = {
      signal, canceled: false, reject,
      issue: () => {
        signal?.removeEventListener("abort", abort);
        // A dispatched write is never canceled or automatically retried here.
        void Promise.resolve().then(send).then(resolve, error => {
          if (error instanceof ApiError && error.status === 429) noteRateLimit(error.retryAfterMs ?? 3000);
          reject(error);
        }).finally(() => { active--; void drain(); });
      },
    };
    if (signal?.aborted) { reject(new RequestCanceledError()); return; }
    signal?.addEventListener("abort", abort, { once: true });
    queue.push(job);
    void drain();
  });

  return { run, noteRateLimit };
}
