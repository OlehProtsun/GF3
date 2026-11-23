import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { CommunicationGif, CommunicationPhoto } from "./CommunicationGif";

const gif = vi.hoisted(() => vi.fn());
vi.mock("../api/communicationsApi", () => ({ communicationsApi: { gif } }));

beforeEach(() => {
  setLanguage("en");
  gif.mockReset();
  vi.spyOn(URL, "createObjectURL").mockReturnValue("blob:test-media");
  vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => undefined);
});
afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals(); });

test("retrying the same media key in another message clears the old message failure", async () => {
  gif.mockRejectedValueOnce(new Error("fetch failed")).mockResolvedValueOnce({ blob: new Blob(["new"]) });
  const { container, rerender, unmount } = render(<CommunicationPhoto id={1} attachmentKey="shared-key" />);
  expect(await screen.findByText("The photo could not be loaded.")).toBeVisible();
  rerender(<CommunicationPhoto id={2} attachmentKey="shared-key" />);
  await waitFor(() => expect(container.querySelector("img")).not.toBeNull());
  fireEvent.load(container.querySelector("img")!);
  await userEvent.click(screen.getByRole("button", { name: "Enlarge photo" }));
  const zoom = screen.getByRole("dialog", { name: "Photo attachment" });
  expect(zoom).toBeVisible();
  await userEvent.click(screen.getByRole("button", { name: "Close" }));
  expect(zoom).not.toBeVisible();
  expect(gif.mock.calls.map(([id, key]) => [id, key])).toEqual([[1, "shared-key"], [2, "shared-key"]]);
  unmount();
  expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:test-media");
});

test("reduced motion delays the GIF fetch until the employee chooses playback", async () => {
  vi.stubGlobal("matchMedia", () => ({ matches: true }));
  gif.mockResolvedValue({ blob: new Blob(["animated"]) });
  render(<CommunicationGif id={1} attachmentKey="animation" employee />);
  expect(gif).not.toHaveBeenCalled();
  await userEvent.click(screen.getByRole("button", { name: "Show animation" }));
  await waitFor(() => expect(gif).toHaveBeenCalledOnce());
  await userEvent.click(screen.getByRole("button", { name: "Hide animation" }));
  expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:test-media");
});
