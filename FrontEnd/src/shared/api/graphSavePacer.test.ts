import { afterEach, expect, test, vi } from "vitest";
import { ApiError, RequestCanceledError } from "./httpClient";
import { createGraphSavePacer } from "./graphSavePacer";

afterEach(() => vi.useRealTimers());

test("starts twelve in FIFO order with peak two, then spaces starts by 350ms", async () => {
  vi.useFakeTimers();
  const pacer = createGraphSavePacer();
  const starts: number[] = [];
  const order: number[] = [];
  let active = 0;
  let peak = 0;
  const results = Array.from({ length: 16 }, (_, index) => pacer.run(async () => {
    order.push(index); starts.push(performance.now());
    peak = Math.max(peak, ++active);
    await new Promise(resolve => setTimeout(resolve, 10));
    active--; return index;
  }));
  await vi.advanceTimersByTimeAsync(70);
  expect(starts).toHaveLength(12);
  expect(starts[11]).toBeLessThan(350);
  await vi.runAllTimersAsync();
  expect(await Promise.all(results)).toEqual(Array.from({ length: 16 }, (_, i) => i));
  expect(order).toEqual(Array.from({ length: 16 }, (_, i) => i));
  expect(peak).toBe(2);
  for (let i = 12; i < starts.length; i++) expect(starts[i] - starts[i - 1]).toBeGreaterThanOrEqual(350);
});

test("429 imposes a shared cooldown without replaying the failed send", async () => {
  vi.useFakeTimers();
  const pacer = createGraphSavePacer();
  const error = new ApiError({ status: 429, message: "Limited", retryAfterMs: 4000 });
  const send = vi.fn().mockRejectedValue(error);
  await expect(pacer.run(send)).rejects.toBe(error);
  const next = vi.fn().mockResolvedValue(1);
  const pending = pacer.run(next);
  await vi.advanceTimersByTimeAsync(3999);
  expect(next).not.toHaveBeenCalled();
  pacer.noteRateLimit(1000);
  await vi.advanceTimersByTimeAsync(999);
  expect(next).not.toHaveBeenCalled();
  await vi.advanceTimersByTimeAsync(1);
  await expect(pending).resolves.toBe(1);
  expect(send).toHaveBeenCalledOnce();
});

test("cancels queued tasks before issue and errors release permits", async () => {
  const pacer = createGraphSavePacer();
  let release!: () => void;
  const blocked = () => new Promise<void>(resolve => { release = resolve; });
  const first = pacer.run(blocked);
  const second = pacer.run(async () => { throw new Error("failure"); });
  const controller = new AbortController();
  const send = vi.fn();
  const canceled = expect(pacer.run(send, controller.signal)).rejects.toBeInstanceOf(RequestCanceledError);
  controller.abort();
  await canceled;
  await expect(second).rejects.toThrow("failure");
  await expect(pacer.run(async () => "next")).resolves.toBe("next");
  release(); await first;
  expect(send).not.toHaveBeenCalled();
});

test("separate saves have independent burst and cooldown", async () => {
  vi.useFakeTimers();
  const first = createGraphSavePacer();
  first.noteRateLimit(30000);
  const second = createGraphSavePacer();
  const send = vi.fn().mockResolvedValue(1);
  await Promise.all(Array.from({ length: 12 }, () => second.run(send)));
  expect(send).toHaveBeenCalledTimes(12);
  expect(vi.getTimerCount()).toBe(0);
});
