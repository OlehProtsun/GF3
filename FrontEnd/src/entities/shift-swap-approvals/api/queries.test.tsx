import { act, renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { beforeEach, expect, test, vi } from "vitest";
import { queryKeys } from "@shared/api/queryKeys";
import { useGraphContractApprovalsQuery, useReviewContractApprovalMutation } from "./queries";

const mocks = vi.hoisted(() => ({ graph: vi.fn(), getGraphRequest: vi.fn(), review: vi.fn() }));
vi.mock("./shiftSwapApprovalsApi", () => ({ shiftSwapApprovalsApi: mocks }));
let client: QueryClient;
const wrapper = ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>;
beforeEach(() => {
  client = new QueryClient();
  for (const mock of Object.values(mocks)) mock.mockReset();
});

test("focused reads refresh cached results and leave the regular list key intact", async () => {
  const exactKey = queryKeys.shiftSwaps.graphApprovalById(1, 2, 42);
  const listKey = queryKeys.shiftSwaps.graphApprovals(1, 2);
  const terminal = { id: 42, status: "expired" };
  client.setQueryData(exactKey, [{ id: 42, status: "pending" }]);
  mocks.graph.mockResolvedValue([{ id: 1 }]);
  mocks.getGraphRequest.mockResolvedValue(terminal);
  const { result } = renderHook(() => ({ exact: useGraphContractApprovalsQuery(1, 2, 42), list: useGraphContractApprovalsQuery(1, 2) }), { wrapper });
  await waitFor(() => expect(result.current.exact.data).toEqual([terminal]));
  await waitFor(() => expect(result.current.list.data).toEqual([{ id: 1 }]));
  expect(mocks.getGraphRequest).toHaveBeenCalledExactlyOnceWith(1, 2, 42, expect.any(AbortSignal));
  expect(mocks.graph).toHaveBeenCalledExactlyOnceWith(1, 2, expect.any(AbortSignal));
  expect(client.getQueryState(exactKey).data).toEqual([terminal]);
  expect(client.getQueryState(listKey).data).toEqual([{ id: 1 }]);
});

test.each(["approve", "reject"] as const)("%s seeds the exact terminal state and refreshes Home", async action => {
  const resultDto = { id: 42, status: action === "approve" ? "approved" : "rejected" };
  mocks.review.mockResolvedValue(resultDto);
  const invalidate = vi.spyOn(client, "invalidateQueries");
  const { result } = renderHook(useReviewContractApprovalMutation, { wrapper });
  act(() => result.current.mutate({ containerId: 1, graphId: 2, id: 42, action }));
  await waitFor(() => expect(client.getQueryState(queryKeys.shiftSwaps.graphApprovalById(1, 2, 42)).data).toEqual([resultDto]));
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.home.activity() });
  expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.home.navBadges() });
  if (action === "approve") expect(invalidate).toHaveBeenCalledWith({ queryKey: queryKeys.containers.all });
});
