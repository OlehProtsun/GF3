import { fireEvent, render, screen, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { employeeScheduleApi } from "@entities/employee-schedule/api/employeeScheduleApi";
import { ApiError } from "@shared/api/httpClient";
import { setLanguage } from "@shared/i18n";
import { EmployeeScheduleSelectDialog } from "./EmployeeScheduleSelectDialog";

beforeEach(() => { setLanguage("en"); vi.spyOn(window, "scrollTo").mockImplementation(() => {}); });
afterEach(() => vi.restoreAllMocks());

const schedule = { id: 42, name: "Unrelated published schedule", year: 2026, month: 11,
  publicationStatus: "public" as const, lastUpdatedAtUtc: null };

function mount() {
  const selected = vi.fn();
  const client = new QueryClient();
  function Launcher() {
    const [open, setOpen] = useState(false);
    return <>
      <button onClick={() => setOpen(true)}>Browse</button>
      <EmployeeScheduleSelectDialog open={open} schedules={[]} selectedScheduleId={null}
        onSelect={selected} onClose={() => setOpen(false)} />
    </>;
  }
  render(<QueryClientProvider client={client}><Launcher /></QueryClientProvider>);
  const launcher = screen.getByRole("button", { name: "Browse" });
  launcher.focus();
  fireEvent.click(launcher);
  return { launcher, selected };
}

it("opens the public catalog with no own schedules and retries a failed catalog read", async () => {
  const get = vi.spyOn(employeeScheduleApi, "listPublic")
    .mockRejectedValueOnce(new ApiError({ status: 503, message: "Offline" })).mockResolvedValue([]);
  mount();
  const dialog = screen.getByRole("dialog", { name: "Public schedules" });
  expect(await screen.findByRole("alert")).toHaveTextContent("Could not load public schedules.");
  fireEvent.click(within(dialog).getByRole("button", { name: "Retry" }));
  expect(await within(dialog).findByText("No published schedules")).toBeVisible();
  expect(get).toHaveBeenCalledTimes(2);
});

it("shows an unavailable public preview, restores list scroll and leaves own selection untouched", async () => {
  vi.spyOn(employeeScheduleApi, "listPublic").mockResolvedValue([schedule]);
  const preview = vi.spyOn(employeeScheduleApi, "getPublic").mockRejectedValue(new ApiError({ status: 404, message: "Missing" }));
  const { launcher, selected } = mount();
  const row = await screen.findByRole("button", { name: /^Unrelated published schedule/ });
  row.parentElement!.scrollTop = 112;
  fireEvent.click(row);
  const detail = screen.getByRole("dialog", { name: "Schedule Preview" });
  expect(await within(detail).findByRole("alert")).toHaveTextContent("This schedule is no longer available.");
  expect(preview).toHaveBeenCalledExactlyOnceWith(schedule.id, expect.any(AbortSignal));
  expect(within(detail).queryByRole("button", { name: /Remove shift|Request correction|Offer shift/ })).toBeNull();
  fireEvent.click(within(detail).getByRole("button", { name: "Back" }));
  expect((await screen.findByRole("button", { name: /^Unrelated published schedule/ })).parentElement!.scrollTop).toBe(112);
  fireEvent.keyDown(window, { key: "Escape" });
  expect(launcher).toHaveFocus();
  expect(document.body).not.toHaveAttribute("data-schedule-scroll-lock");
  expect(selected).not.toHaveBeenCalled();
});

it("retries a transient preview error using the same published schedule ID", async () => {
  vi.spyOn(employeeScheduleApi, "listPublic").mockResolvedValue([schedule]);
  const preview = vi.spyOn(employeeScheduleApi, "getPublic")
    .mockRejectedValueOnce(new ApiError({ status: 503, message: "Offline" }))
    .mockResolvedValue({ ...schedule, containerId: 2, containerName: "Container", shopId: 3, shopName: "Shop", slots: [] });
  const { selected } = mount();
  fireEvent.click(await screen.findByRole("button", { name: /^Unrelated published schedule/ }));
  const detail = screen.getByRole("dialog", { name: "Schedule Preview" });
  expect(await within(detail).findByRole("alert")).toHaveTextContent("Could not load the schedule preview.");
  fireEvent.click(within(detail).getByRole("button", { name: "Retry" }));
  expect(await within(detail).findByText(schedule.name)).toBeVisible();
  expect(preview.mock.calls.map(([id]) => id)).toEqual([schedule.id, schedule.id]);
  expect(selected).not.toHaveBeenCalled();
});
