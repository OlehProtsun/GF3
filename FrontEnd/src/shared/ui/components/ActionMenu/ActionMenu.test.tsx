import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { expect, test, vi } from "vitest";
import { ActionMenu } from "./ActionMenu";

function mount() {
  const first = vi.fn(), disabled = vi.fn(), last = vi.fn();
  function Fixture() {
    const [anchor, setAnchor] = useState<HTMLButtonElement | null>(null);
    return <>
      <button type="button" onClick={event => setAnchor(event.currentTarget)}>Open</button>
      <button type="button">Outside</button>
      <ActionMenu open={Boolean(anchor)} anchor={anchor} label="Test actions" onClose={() => setAnchor(null)} items={[
        { id: "first", label: "First", onSelect: first },
        { id: "disabled", label: "Disabled", disabled: true, onSelect: disabled },
        { id: "last", label: "Last", destructive: true, onSelect: last },
      ]} />
    </>;
  }
  return { ...render(<Fixture />), first, disabled, last, user: userEvent.setup() };
}

test("portals, focuses enabled actions and restores focus after keyboard selection", async () => {
  const { user, first, disabled, last } = mount();
  const trigger = screen.getByRole("button", { name: "Open" });
  await user.click(trigger);
  const menu = screen.getByRole("menu", { name: "Test actions" });
  expect(menu.parentElement).toBe(document.body);
  expect(screen.getByRole("menuitem", { name: "First" })).toHaveFocus();
  expect(screen.getByRole("menuitem", { name: "Disabled" })).toBeDisabled();
  await user.keyboard("{ArrowDown}");
  expect(screen.getByRole("menuitem", { name: "Last" })).toHaveFocus();
  await user.keyboard("{Home}{ArrowUp}{End}{Enter}");
  expect(last).toHaveBeenCalledOnce();
  expect(first).not.toHaveBeenCalled();
  expect(disabled).not.toHaveBeenCalled();
  expect(menu).not.toBeInTheDocument();
  expect(trigger).toHaveFocus();
});

test("dismisses with Escape, Tab, outside pointer and unmount", async () => {
  const { user, unmount, first } = mount();
  const trigger = screen.getByRole("button", { name: "Open" });
  const outside = screen.getByRole("button", { name: "Outside" });
  await user.click(trigger);
  await user.keyboard("{Escape}");
  expect(trigger).toHaveFocus();
  await user.click(trigger);
  await user.tab();
  expect(screen.queryByRole("menu")).toBeNull();
  await user.click(trigger);
  fireEvent.pointerDown(outside);
  expect(screen.queryByRole("menu")).toBeNull();
  expect(first).not.toHaveBeenCalled();
  await user.click(trigger);
  unmount();
  expect(screen.queryByRole("menu")).toBeNull();
});
