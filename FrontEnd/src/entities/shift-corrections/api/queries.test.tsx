import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { useGraphShiftCorrectionsQuery } from "./queries";

const mocks = vi.hoisted(() => ({ fetch: vi.fn() }));
vi.mock("./shiftCorrectionsApi", () => ({ shiftCorrectionsApi: { listGraph: mocks.fetch } }));
beforeEach(() => mocks.fetch.mockReset().mockResolvedValue([]));
const wrapper = ({ children }: { children: React.ReactNode }) =>
  <QueryClientProvider client={new QueryClient()}>{children}</QueryClientProvider>;

describe("useGraphShiftCorrectionsQuery", () => {
  test.each([null, 0, -1, 1.5])("does not fetch unsaved/invalid graph %s", graphId => {
    renderHook(() => useGraphShiftCorrectionsQuery(2, graphId), { wrapper });
    expect(mocks.fetch).not.toHaveBeenCalled();
  });
  test.each([null, 0, -1, 1.5])("does not fetch invalid container %s", containerId => {
    renderHook(() => useGraphShiftCorrectionsQuery(containerId, 9), { wrapper });
    expect(mocks.fetch).not.toHaveBeenCalled();
  });
  test("fetches persisted graphs", async () => {
    const { result } = renderHook(() => useGraphShiftCorrectionsQuery(2, 9), { wrapper });
    await waitFor(() => expect(result.current.data).toEqual([]));
    expect(mocks.fetch).toHaveBeenCalledExactlyOnceWith(2, 9, expect.any(AbortSignal));
  });
});
