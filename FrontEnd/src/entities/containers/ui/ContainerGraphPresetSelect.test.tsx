import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { expect, test, vi } from "vitest";
import type { SchedulePreset } from "../model/types";
import { ContainerGraphPresetSelect } from "./ContainerGraphPresetSelect";

beforeEnglishTest(() => setTestLanguage("en"));

const preset: SchedulePreset = {
  id: 8, containerId: null, name: "Same name", scheduleName: "Schedule", shopId: 3,
  year: 2026, month: 4, peoplePerShift: 1, shift1Time: "08:00 - 16:00", shift2Time: "16:00 - 20:00",
  maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 2, maxFullPerMonth: 4, employees: [],
};

test("keeps orphan and duplicate-name presets independently selectable by ID", async () => {
  const user = userEvent.setup();
  const onSelect = vi.fn();
  const props = { presets: [preset, { ...preset, id: 9, containerId: 2 }], shops: [], isLoading: false,
    onSelect, onAddPreset: vi.fn() };
  const view = render(<ContainerGraphPresetSelect {...props} selectedPresetId={8} />);
  expect(screen.getByText("Same name #8")).toBeVisible();
  await user.click(screen.getByRole("button", { name: "Open schedule preset list" }));
  await user.click(screen.getByRole("option", { name: /Same name #9/ }));
  expect(onSelect).toHaveBeenLastCalledWith(9);
  view.rerender(<ContainerGraphPresetSelect {...props} selectedPresetId={9} />);
  await user.click(screen.getByRole("button", { name: "Open schedule preset list" }));
  await user.click(screen.getByRole("option", { name: /Same name #8/ }));
  expect(onSelect).toHaveBeenLastCalledWith(8);
});
