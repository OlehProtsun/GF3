import { afterEach, expect, test, vi } from "vitest";
import { ApiError, RequestCanceledError, setAuthAccessToken } from "./httpClient";
import { retry429Read } from "./retry429Read";

const limited = new ApiError({ status: 429, message: "Limited", retryAfterMs: 7000 });
afterEach(() => { vi.useRealTimers(); setAuthAccessToken(null); });

test("waits for Retry-After and retries only the failed read", async () => {
  vi.useFakeTimers();
  const read = vi.fn().mockRejectedValueOnce(limited).mockResolvedValue("loaded");
  const result = retry429Read(read);
  await vi.advanceTimersByTimeAsync(6999);
  expect(read).toHaveBeenCalledOnce();
  await vi.advanceTimersByTimeAsync(1);
  await expect(result).resolves.toBe("loaded");
  expect(read).toHaveBeenCalledTimes(2);
});

test("caps a persistently limited read at two retries", async () => {
  vi.useFakeTimers();
  const read = vi.fn().mockRejectedValue(limited);
  const rejected = expect(retry429Read(read, { maxRetries: 50 })).rejects.toBe(limited);
  await vi.advanceTimersByTimeAsync(14_000);
  await rejected;
  expect(read).toHaveBeenCalledTimes(3);
});

test("Save can opt into six retries without changing the default budget", async () => {
  vi.useFakeTimers();
  const read = vi.fn().mockRejectedValue(limited);
  const rejected = expect(retry429Read(read, { maxRetries: 6, maxElapsedMs: 180000 })).rejects.toBe(limited);
  await vi.advanceTimersByTimeAsync(42000);
  await rejected;
  expect(read).toHaveBeenCalledTimes(7);
});

test("does not sleep beyond the Save elapsed budget or shorten Retry-After", async () => {
  vi.useFakeTimers();
  const read = vi.fn().mockRejectedValue(new ApiError({ status: 429, message: "Long cooldown", retryAfterMs: 90000 }));
  const rejected = expect(retry429Read(read, { maxRetries: 6, maxElapsedMs: 180000 })).rejects.toThrow("Long cooldown");
  await vi.advanceTimersByTimeAsync(90000);
  await rejected;
  expect(read).toHaveBeenCalledTimes(2);
  expect(vi.getTimerCount()).toBe(0);
});

test.each([400, 409, 500])("does not retry HTTP %s", async status => {
  const error = new ApiError({ status, message: "Failed" });
  const read = vi.fn().mockRejectedValue(error);
  await expect(retry429Read(read)).rejects.toBe(error);
  expect(read).toHaveBeenCalledOnce();
});

test.each(["abort", "logout", "reauthenticate"])("cancels on %s before reissuing a GET", async action => {
  vi.useFakeTimers();
  setAuthAccessToken("original");
  const abort = new AbortController();
  const read = vi.fn().mockRejectedValue(limited);
  const rejected = expect(retry429Read(read, { signal: abort.signal })).rejects.toBeInstanceOf(RequestCanceledError);
  await vi.advanceTimersByTimeAsync(1);
  if (action === "abort") abort.abort();
  else {
    setAuthAccessToken(null);
    if (action === "reauthenticate") setAuthAccessToken("original");
  }
  await vi.advanceTimersByTimeAsync(7000);
  await rejected;
  expect(read).toHaveBeenCalledOnce();
  expect(vi.getTimerCount()).toBe(0);
});
