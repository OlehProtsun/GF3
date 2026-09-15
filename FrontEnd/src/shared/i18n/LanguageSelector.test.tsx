import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { LanguageSelector } from "./LanguageSelector";

const context = vi.hoisted(() => ({ language: "en", ready: true, save: vi.fn() }));
vi.mock("@app/providers/LanguageProvider", () => ({ useLanguage: () => context }));

beforeEach(() => { context.save.mockReset(); vi.useFakeTimers(); });
afterEach(() => { cleanup(); vi.useRealTimers(); });

function choosePolish() {
  fireEvent.click(screen.getByRole("button", { name: "Application language" }));
  fireEvent.click(screen.getByRole("option", { name: "Polska" }));
}

it("shows confirmation only after saving succeeds and removes it after 2.4 seconds", async () => {
  let finish!: () => void;
  context.save.mockImplementation(() => new Promise<void>(resolve => { finish = resolve; }));
  render(<LanguageSelector appearance="rounded" />);
  choosePolish();
  expect(screen.getByRole("status")).toBeEmptyDOMElement();
  expect(screen.getByRole("button", { name: "Application language" })).toBeDisabled();
  await act(async () => finish());
  expect(screen.getByRole("status")).toHaveTextContent("Saved");
  expect(context.save).toHaveBeenCalledWith("pl");
  act(() => vi.advanceTimersByTime(2400));
  expect(screen.getByRole("status")).toBeEmptyDOMElement();
});

it("keeps the current language and shows an error without confirmation when saving fails", async () => {
  context.save.mockRejectedValue(new Error("offline"));
  render(<LanguageSelector appearance="rounded" />);
  choosePolish();
  await act(async () => {});
  expect(screen.getByRole("alert")).toHaveTextContent("Could not save language");
  expect(screen.getByRole("status")).toBeEmptyDOMElement();
  expect(screen.getByRole("button", { name: "Application language" })).toHaveTextContent("English");
  expect(screen.getByRole("button", { name: "Application language" })).toBeEnabled();
});

it("does not save the already selected language and cancels feedback timers on unmount", async () => {
  context.save.mockResolvedValue(undefined);
  const view = render(<LanguageSelector appearance="rounded" />);
  fireEvent.click(screen.getByRole("button", { name: "Application language" }));
  fireEvent.click(screen.getByRole("option", { name: "English" }));
  expect(context.save).not.toHaveBeenCalled();
  choosePolish();
  await act(async () => {});
  expect(screen.getByRole("status")).toHaveTextContent("Saved");
  view.unmount();
  expect(vi.getTimerCount()).toBe(0);
});
