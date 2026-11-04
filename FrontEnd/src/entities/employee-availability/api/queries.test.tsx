import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { useSaveEmployeeAvailabilityMutation } from "./queries";
import { employeeAvailabilityApi } from "./employeeAvailabilityApi";
import type { EmployeeAvailabilityGroup } from "../model/types";
import { estimateAvailabilityHours } from "../model/estimatedHours";

vi.mock("./employeeAvailabilityApi", () => ({ employeeAvailabilityApi: { saveSlots: vi.fn() } }));
beforeEach(() => { vi.mocked(employeeAvailabilityApi.saveSlots).mockReset(); });
const original: EmployeeAvailabilityGroup = { id: 5, name: "May", year: 2026, month: 5, canSubmit: true, isEditLocked: false, slots: [] };
function setup() {
  const client = new QueryClient();
  client.setQueryData(queryKeys.employeeAvailability.list(), [original, { ...original, id: 6 }]);
  const view = renderHook(() => useSaveEmployeeAvailabilityMutation(), {
    wrapper: ({ children }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>,
  });
  return { client, ...view };
}

test("successful submission immediately replaces only its saved group in the list and detail caches", async () => {
  const saved = { ...original, employeeLastModifiedAtUtc: "2026-05-01T10:00:00Z", slots: Array.from({ length: 31 }, (_, i) => ({ id: i + 1, availabilityGroupMemberId: 50, dayOfMonth: i + 1, kind: 0 })) };
  vi.mocked(employeeAvailabilityApi.saveSlots).mockResolvedValue(saved);
  const { client, result } = setup();
  act(() => result.current.mutate({ id: 5, payload: { slots: saved.slots } }));
  await waitFor(() => expect(client.getQueryState<EmployeeAvailabilityGroup[]>(queryKeys.employeeAvailability.list()).data?.[0]).toEqual(saved));
  const groups = client.getQueryState<EmployeeAvailabilityGroup[]>(queryKeys.employeeAvailability.list()).data!;
  expect(estimateAvailabilityHours(groups[0])).toMatchObject({ status: "ready", potentialHours: 248 });
  expect(groups[1]).toEqual({ ...original, id: 6 });
  expect(client.getQueryState(queryKeys.employeeAvailability.byId(5)).data).toEqual(saved);
});

test("failed submission leaves the last saved representation untouched", async () => {
  vi.mocked(employeeAvailabilityApi.saveSlots).mockRejectedValue(new Error("Offline"));
  const { client, result } = setup();
  const onError = vi.fn();
  await act(async () => { result.current.mutate({ id: 5, payload: { slots: [] } }, { onError }); });
  expect(onError).toHaveBeenCalledWith(expect.objectContaining({ message: "Offline" }));
  expect(client.getQueryState<EmployeeAvailabilityGroup[]>(queryKeys.employeeAvailability.list()).data?.[0]).toEqual(original);
  expect(estimateAvailabilityHours(original)).toEqual({ status: "unsubmitted" });
});
