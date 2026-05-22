import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { getAvailabilityCellKey } from "../model/editor";
import type { AvailabilityGroupMember, AvailabilitySlot } from "../model/types";
import { useSaveAvailabilityGroupGraphMutation } from "./queries";

const apiMocks = vi.hoisted(() => ({
  create: vi.fn(),
  update: vi.fn(),
  remove: vi.fn(),
  list: vi.fn(),
  byId: vi.fn(),
  items: vi.fn(),
  members: vi.fn(),
  slots: vi.fn(),
  createMember: vi.fn(),
  updateMember: vi.fn(),
  removeMember: vi.fn(),
  createSlot: vi.fn(),
  updateSlot: vi.fn(),
  removeSlot: vi.fn(),
}));

vi.mock("./availabilityGroupsApi", () => ({
  availabilityGroupsApi: apiMocks,
}));

function SaveGraphHarness({ mode }: { mode: "create" | "update" }) {
  const saveGraph = useSaveAvailabilityGroupGraphMutation();

  return (
    <button
      type="button"
      onClick={() => saveGraph.mutate(mode === "create" ? createInput() : updateInput())}
    >
      save graph
    </button>
  );
}

function renderHarness(client: QueryClient, mode: "create" | "update") {
  return render(
    <QueryClientProvider client={client}>
      <SaveGraphHarness mode={mode} />
    </QueryClientProvider>,
  );
}

function createInput() {
  return {
    id: null,
    payload: {
      name: "Summer availability",
      year: 2026,
      month: 2,
      publicationStatus: "private" as const,
    },
    employeeIds: [7, 5, 7],
    cellMap: {
      [getAvailabilityCellKey(7, 1)]: "+",
      [getAvailabilityCellKey(5, 2)]: "9:00-13:30",
    },
  };
}

function updateInput() {
  const existingMembers: AvailabilityGroupMember[] = [
    { id: 10, availabilityGroupId: 44, employeeId: 7, displayOrder: 0 },
    { id: 11, availabilityGroupId: 44, employeeId: 5, displayOrder: 1 },
    { id: 12, availabilityGroupId: 44, employeeId: 9, displayOrder: 2 },
  ];
  const existingSlots: AvailabilitySlot[] = [
    ...buildMonthSlots(existingMembers[0], 2026, 2, 1, null),
    ...buildMonthSlots(existingMembers[1], 2026, 2, 1, null),
    { id: 900, availabilityGroupMemberId: 12, dayOfMonth: 1, kind: 0, intervalStr: null },
  ];

  return {
    id: 44,
    payload: {
      name: "  Updated availability  ",
      year: 2026,
      month: 2,
      publicationStatus: "public" as const,
    },
    employeeIds: [5, 7],
    cellMap: {
      [getAvailabilityCellKey(5, 1)]: "+",
      [getAvailabilityCellKey(7, 2)]: "08:15 - 12:45",
    },
    existingMembers,
    existingSlots,
  };
}

function buildMonthSlots(
  member: AvailabilityGroupMember,
  year: number,
  month: number,
  kind: AvailabilitySlot["kind"],
  intervalStr: string | null,
): AvailabilitySlot[] {
  const daysInMonth = new Date(year, month, 0).getDate();

  return Array.from({ length: daysInMonth }, (_, index) => ({
    id: member.id * 100 + index + 1,
    availabilityGroupMemberId: member.id,
    dayOfMonth: index + 1,
    kind,
    intervalStr,
  }));
}

beforeEach(() => {
  Object.values(apiMocks).forEach(mock => mock.mockReset());
  apiMocks.create.mockResolvedValue({ id: 44 });
  apiMocks.update.mockResolvedValue(undefined);
  apiMocks.remove.mockResolvedValue(undefined);
  apiMocks.createMember.mockImplementation(async (_groupId: number, payload: { employeeId: number; displayOrder: number }) => ({
    id: payload.employeeId === 7 ? 70 : 50,
    availabilityGroupId: 44,
    employeeId: payload.employeeId,
    displayOrder: payload.displayOrder,
  }));
  apiMocks.updateMember.mockResolvedValue(undefined);
  apiMocks.removeMember.mockResolvedValue(undefined);
  apiMocks.createSlot.mockResolvedValue({});
  apiMocks.updateSlot.mockResolvedValue(undefined);
  apiMocks.removeSlot.mockResolvedValue(undefined);
});

describe("availability group graph query mutation", () => {
  test("creates a group, de-duplicates employees, materializes every day slot, and invalidates the saved graph", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    renderHarness(client, "create");

    await user.click(screen.getByRole("button", { name: "save graph" }));

    await waitFor(() => {
      expect(apiMocks.create).toHaveBeenCalledWith({
        name: "Summer availability : 02.2026",
        year: 2026,
        month: 2,
        publicationStatus: "private",
      });
      expect(apiMocks.createSlot).toHaveBeenCalledTimes(56);
    });

    expect(apiMocks.createMember).toHaveBeenNthCalledWith(1, 44, { employeeId: 7, displayOrder: 0 });
    expect(apiMocks.createMember).toHaveBeenNthCalledWith(2, 44, { employeeId: 5, displayOrder: 1 });
    expect(apiMocks.createSlot).toHaveBeenCalledWith(44, {
      availabilityGroupMemberId: 70,
      dayOfMonth: 1,
      kind: 0,
      intervalStr: null,
    });
    expect(apiMocks.createSlot).toHaveBeenCalledWith(44, {
      availabilityGroupMemberId: 50,
      dayOfMonth: 2,
      kind: 2,
      intervalStr: "09:00 - 13:30",
    });
    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.availabilityGroups.all },
      { queryKey: queryKeys.availabilityGroups.byId(44) },
      { queryKey: queryKeys.availabilityGroups.items(44) },
      { queryKey: queryKeys.availabilityGroups.members(44) },
      { queryKey: queryKeys.availabilityGroups.slots(44) },
    ]);
  });

  test("updates an existing graph by reordering members, deleting removed records, and only updating changed slots", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    const invalidateSpy = vi.spyOn(client, "invalidateQueries");
    renderHarness(client, "update");

    await user.click(screen.getByRole("button", { name: "save graph" }));

    await waitFor(() => {
      expect(apiMocks.update).toHaveBeenCalledWith(44, {
        name: "Updated availability",
        year: 2026,
        month: 2,
        publicationStatus: "public",
      });
      expect(apiMocks.removeMember).toHaveBeenCalledWith(44, 12);
    });

    expect(apiMocks.create).not.toHaveBeenCalled();
    expect(apiMocks.createMember).not.toHaveBeenCalled();
    expect(apiMocks.createSlot).not.toHaveBeenCalled();
    expect(apiMocks.removeSlot).toHaveBeenCalledWith(44, 900);
    expect(apiMocks.updateMember).toHaveBeenCalledWith(44, 11, { employeeId: 5, displayOrder: 0 });
    expect(apiMocks.updateMember).toHaveBeenCalledWith(44, 10, { employeeId: 7, displayOrder: 1 });
    expect(apiMocks.updateSlot).toHaveBeenCalledTimes(2);
    expect(apiMocks.updateSlot).toHaveBeenCalledWith(44, 1101, {
      availabilityGroupMemberId: 11,
      dayOfMonth: 1,
      kind: 0,
      intervalStr: null,
    });
    expect(apiMocks.updateSlot).toHaveBeenCalledWith(44, 1002, {
      availabilityGroupMemberId: 10,
      dayOfMonth: 2,
      kind: 2,
      intervalStr: "08:15 - 12:45",
    });
    expect(invalidateSpy.mock.calls.map(([filters]) => filters)).toEqual([
      { queryKey: queryKeys.availabilityGroups.all },
      { queryKey: queryKeys.availabilityGroups.byId(44) },
      { queryKey: queryKeys.availabilityGroups.items(44) },
      { queryKey: queryKeys.availabilityGroups.members(44) },
      { queryKey: queryKeys.availabilityGroups.slots(44) },
    ]);
  });
});
