import { ApiError, getAuthAccessToken, getAuthGeneration, RequestCanceledError } from "./httpClient";

export function waitForReadRetry(delay: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(new RequestCanceledError());
      return;
    }
    const abort = () => {
      clearTimeout(timer);
      reject(new RequestCanceledError());
    };
    const timer = setTimeout(() => {
      signal?.removeEventListener("abort", abort);
      resolve();
    }, delay);
    signal?.addEventListener("abort", abort, { once: true });
  });
}

/** The callback must issue only a GET. Writes need authoritative readback before any explicit retry. */
export async function retry429Read<T>(read: () => Promise<T>, options: { signal?: AbortSignal; maxRetries?: number } = {}): Promise<T> {
  const { signal, maxRetries = 2 } = options;
  const retries = Number.isNaN(maxRetries) ? 0 : Math.min(2, Math.max(0, Math.floor(maxRetries)));
  const token = getAuthAccessToken();
  const generation = getAuthGeneration();
  const ensureCurrent = () => {
    if (signal?.aborted || token !== getAuthAccessToken() || generation !== getAuthGeneration()) {
      throw new RequestCanceledError();
    }
  };
  for (let attempt = 0; ; attempt++) {
    ensureCurrent();
    try {
      const result = await read();
      ensureCurrent();
      return result;
    } catch (error) {
      ensureCurrent();
      if (!(error instanceof ApiError) || error.status !== 429 || attempt >= retries) throw error;
      await waitForReadRetry(error.retryAfterMs ?? 3000, signal);
    }
  }
}
