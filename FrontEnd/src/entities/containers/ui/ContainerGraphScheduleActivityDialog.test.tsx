import { useState } from "react";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { ContainerGraphScheduleActivityDialog, type ScheduleActivitySummary } from "./ContainerGraphScheduleActivityDialog";

const summary: ScheduleActivitySummary = {
  swaps: 1, manual: 2, corrections: 3, contractApprovals: 1, total: 7,
  pending: 3, approvedOrAccepted: 2, rejected: 1, offered: 1,
};

beforeEach(() => setLanguage("en"));

function Harness({ counts = summary }: { counts?: ScheduleActivitySummary }) {
  const [open, setOpen] = useState(false);
  return <>
    <button onClick={() => setOpen(true)}>Open details</button>
    <ContainerGraphScheduleActivityDialog open={open} onClose={() => setOpen(false)} summary={counts}
      swapPanel={<input aria-label="Swap search" />}
      manualPanel={<select aria-label="Manual selection" defaultValue="first"><option>first</option><option>second</option></select>}
      correctionsPanel={<details><summary>Correction details</summary><p>Request summary</p></details>} />
  </>;
}

test("renders counts and keeps each panel mounted through tab and keyboard changes", async () => {
  const user = userEvent.setup();
  const view = render(<Harness />);
  await user.click(screen.getByRole("button", { name: "Open details" }));
  const dialog = screen.getByRole("dialog", { name: "Schedule Activity" });
  expect(dialog.parentElement?.parentElement).toBe(document.body);
  const tabs = within(dialog).getAllByRole("tab");
  expect(tabs.map(tab => tab.textContent)).toEqual(["All7", "Shift Corrections4", "Manual shifts2", "Swap log1"]);
  expect(within(dialog).getAllByRole("region")).toHaveLength(3);
  const search = within(dialog).getByRole("textbox", { name: "Swap search" });
  const manual = within(dialog).getByRole("combobox", { name: "Manual selection" });
  await user.type(search, "Ada");
  await user.selectOptions(manual, "second");
  await user.click(within(dialog).getByText("Correction details"));
  await user.click(tabs[0]);
  await user.keyboard("{End}");
  expect(tabs[3]).toHaveFocus();
  expect(tabs[3]).toHaveAttribute("aria-selected", "true");
  expect(within(dialog).getByRole("tabpanel")).toContainElement(search);
  expect(manual).not.toBeVisible();
  expect(dialog.querySelectorAll('[aria-label="Swap search"]')).toHaveLength(1);
  await user.keyboard("{ArrowRight}");
  expect(tabs[0]).toHaveFocus();
  expect(search).toHaveValue("Ada");
  expect(manual).toHaveValue("second");
  expect(within(dialog).getByText("Request summary")).toBeVisible();
  view.rerender(<Harness counts={{ ...summary, manual: 1, total: 6, pending: 2, offered: 0 }} />);
  expect(tabs[2]).toHaveTextContent("Manual shifts1");
  expect(within(dialog).getByText("Offered").nextElementSibling).toHaveTextContent("0");
});

test("focus wraps within the dialog, returns on close, and reopens on All", async () => {
  const user = userEvent.setup();
  render(<Harness />);
  const trigger = screen.getByRole("button", { name: "Open details" });
  await user.click(trigger);
  const dialog = screen.getByRole("dialog", { name: "Schedule Activity" });
  const close = within(dialog).getByRole("button", { name: "Close" });
  expect(close).toHaveFocus();
  expect(document.body.style.overflow).toBe("hidden");
  await user.click(within(dialog).getByRole("tab", { name: /Swap log/ }));
  close.focus();
  await user.tab({ shift: true });
  expect(within(dialog).getByRole("textbox")).toHaveFocus();
  await user.tab();
  expect(close).toHaveFocus();
  trigger.focus();
  expect(close).toHaveFocus();
  fireEvent.click(dialog);
  expect(dialog).toBeVisible();
  await user.keyboard("{Escape}");
  expect(dialog).not.toBeInTheDocument();
  expect(trigger).toHaveFocus();
  expect(document.body.style.overflow).toBe("");
  await user.click(trigger);
  expect(screen.getByRole("tab", { name: /All/ })).toHaveAttribute("aria-selected", "true");
  fireEvent.click(screen.getByRole("dialog").parentElement!);
  expect(trigger).toHaveFocus();
  await user.click(trigger);
  await user.click(screen.getByRole("button", { name: "Close" }));
  expect(trigger).toHaveFocus();
});

test("Escape and focus containment defer to another open modal", () => {
  const onClose = vi.fn();
  const { rerender } = render(<>
    <ContainerGraphScheduleActivityDialog open onClose={onClose} summary={summary} swapPanel={null} manualPanel={null} correctionsPanel={null} />
    <div role="dialog" aria-modal="true" aria-label="Child"><button>Child action</button></div>
  </>);
  const child = screen.getByRole("button", { name: "Child action" });
  child.focus();
  fireEvent.keyDown(child, { key: "Tab" });
  fireEvent.keyDown(child, { key: "Escape" });
  expect(child).toHaveFocus();
  expect(onClose).not.toHaveBeenCalled();
  rerender(<ContainerGraphScheduleActivityDialog open onClose={onClose} summary={summary} swapPanel={null} manualPanel={null} correctionsPanel={null} />);
  fireEvent.keyDown(window, { key: "Escape" });
  expect(onClose).toHaveBeenCalledOnce();
});
