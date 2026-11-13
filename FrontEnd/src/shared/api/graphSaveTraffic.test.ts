import { QueryClient } from "@tanstack/react-query";
import { expect, test, vi } from "vitest";
import { beginGraphSaveTraffic, deferGraphRealtimeRefresh, finishGraphSaveTraffic, isGraphSaveTrafficActive } from "./graphSaveTraffic";
import { queryKeys } from "./queryKeys";

test("isolates save gates by client and container and marks verified data stale once without refetching", async () => {
  const client = new QueryClient();
  const other = new QueryClient();
  const invalidate = vi.spyOn(client, "invalidateQueries");
  const observe = vi.fn();
  client.subscribe(observe);
  const key = queryKeys.containers.graphById(7, 34);
  const unrelated = queryKeys.containers.graphSlotsBatch(7, [42]);
  client.setQueryData(key, { verified: true });
  client.setQueryData(unrelated, []);
  observe.mockClear();
  beginGraphSaveTraffic(client, 7);
  expect(() => beginGraphSaveTraffic(client, 7)).toThrow();
  expect(deferGraphRealtimeRefresh(other, 7, 34)).toBe(false);
  expect(deferGraphRealtimeRefresh(client, 8, 34)).toBe(false);
  expect(deferGraphRealtimeRefresh(client, 7, 34)).toBe(true);
  expect(deferGraphRealtimeRefresh(client, 7, 34)).toBe(true);
  await finishGraphSaveTraffic(client, 7, [34, 36]);
  expect(isGraphSaveTrafficActive(client, 7)).toBe(false);
  expect(invalidate.mock.calls.filter(([f]) => f.queryKey?.join("/") === key.join("/"))).toEqual([
    [{ queryKey: key, exact: true, refetchType: "none" }],
  ]);
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.containers.graphs(7), exact: true, refetchType: "none" });
  expect(invalidate.mock.calls.every(([f]) => f.refetchType === "none")).toBe(true);
  expect(observe).not.toHaveBeenCalled();
  expect(client.getQueryState(key)).toEqual({ data: { verified: true }, updatedAt: 0 });
  expect(client.getQueryState(unrelated).updatedAt).toBeGreaterThan(0);
});

test("cleanup can be called after a failed save and cannot replace a confirmed graph with an older in-flight read", async () => {
  const client = new QueryClient();
  const key = queryKeys.containers.graphById(7, 34);
  let resolve!: (value: string) => void;
  const old = client.fetchQuery(key, () => new Promise<string>(done => { resolve = done; }));
  beginGraphSaveTraffic(client, 7);
  client.setQueryData(key, "confirmed");
  await finishGraphSaveTraffic(client, 7, [34]);
  resolve("before save");
  await old;
  expect(client.getQueryState(key)).toEqual({ data: "confirmed", updatedAt: 0 });
  beginGraphSaveTraffic(client, 7);
  await finishGraphSaveTraffic(client, 7, []);
  expect(isGraphSaveTrafficActive(client, 7)).toBe(false);
});
