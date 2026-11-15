import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import type { AvailabilityGroup } from "../model/types";
import { AvailabilityGroupListCard } from "./AvailabilityGroupListCard";
import styles from "./AvailabilityGroupListCard.module.css";

const groups: AvailabilityGroup[] = [
  { id: 1, name: "First", year: 2026, month: 11, publicationStatus: "private" },
  { id: 2, name: "Second", year: 2027, month: 11, publicationStatus: "public" },
  { id: 3, name: "Third", year: 2026, month: 12, publicationStatus: "private" },
  { id: 4, name: "Fourth", year: 2028, month: 12, publicationStatus: "private" },
  { id: 5, name: "Invalid", year: 2026, month: 13, publicationStatus: "private" },
];

beforeEach(() => {
  setLanguage("en");
  localStorage.clear();
});

const tile = (name: string) => screen.getByRole("button", { name: `Open availability group ${name}` });

test("month controls the card tint across years and publication states", () => {
  render(<AvailabilityGroupListCard groups={groups} isLoading={false} searchQuery="" onClearSearch={vi.fn()} onOpenGroup={vi.fn()} />);
  for (const name of ["First", "Second"]) expect(tile(name)).toHaveClass(styles.monthTint, styles.november);
  for (const name of ["Third", "Fourth"]) expect(tile(name)).toHaveClass(styles.monthTint, styles.december);
  expect(styles.november).not.toBe(styles.december);
  expect(tile("Invalid")).not.toHaveClass(styles.monthTint);
});

test("tinted cards still open by mouse and keyboard, pin and render search results", async () => {
  const user = userEvent.setup();
  const onOpenGroup = vi.fn();
  const onClearSearch = vi.fn();
  const props = { isLoading: false, onClearSearch, onOpenGroup };
  const view = render(<AvailabilityGroupListCard {...props} groups={groups} searchQuery="" />);

  await user.click(within(tile("Fourth")).getByRole("button", { name: "Pin Fourth" }));
  expect(onOpenGroup).not.toHaveBeenCalled();
  expect(screen.getAllByRole("button", { name: /^Open availability group/ })[0]).toBe(tile("Fourth"));
  expect(tile("Fourth")).toHaveClass(styles.december);
  await user.click(tile("First"));
  tile("Fourth").focus();
  await user.keyboard("{Enter}");
  expect(onOpenGroup.mock.calls).toEqual([[1], [4]]);

  view.rerender(<AvailabilityGroupListCard {...props} groups={[groups[3]]} searchQuery="Fourth" />);
  expect(tile("Fourth")).toHaveClass(styles.december);
  expect(within(tile("Fourth")).getByRole("button", { name: "Unpin Fourth", pressed: true })).toBeInTheDocument();

  view.rerender(<AvailabilityGroupListCard {...props} groups={[]} searchQuery="Missing" />);
  await user.click(screen.getByRole("button", { name: "Clear Search" }));
  expect(onClearSearch).toHaveBeenCalledTimes(1);
});
