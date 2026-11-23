import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { communicationsApi } from "@entities/communications/api/communicationsApi";
import type { CommunicationMessageDto } from "@entities/communications";
import { setLanguage } from "@shared/i18n";
import { CommunicationsPage } from "./CommunicationsPage";

vi.mock("@app/providers/AuthProvider", () => ({
  useAuth: () => ({ session: { displayName: "Fixture Manager" } }),
}));

beforeEach(() => {
  setLanguage("en");
  vi.spyOn(URL, "createObjectURL").mockReturnValue("blob:communications-draft");
  vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => undefined);
});

afterEach(() => vi.restoreAllMocks());

function mount(messages: CommunicationMessageDto[] = []) {
  vi.spyOn(communicationsApi, "listForManager").mockResolvedValue(messages);
  render(<BrowserRouter><QueryClientProvider client={new QueryClient()}><CommunicationsPage /></QueryClientProvider></BrowserRouter>);
}

const photo = new File(["fixture"], "photo.png", { type: "image/png" });
const titleField = () => screen.getByRole("textbox", { name: "Title" });
const bodyField = () => screen.getByRole("textbox", { name: "Text" });

test("requires text and keeps the attachment draft after a rejected upload", async () => {
  const user = userEvent.setup();
  let rejectUpload!: (error: Error) => void;
  const create = vi.spyOn(communicationsApi, "create").mockImplementation(() => new Promise((_, reject) => {
    rejectUpload = reject;
  }));
  mount();
  await user.type(titleField(), "Update");
  await user.upload(screen.getByLabelText("Add photo"), photo);
  await user.click(screen.getByRole("button", { name: "Create" }));
  expect(screen.getByText("Title and message are required.")).toBeVisible();
  expect(create).not.toHaveBeenCalled();

  await user.type(bodyField(), "Fixture body");
  await user.click(screen.getByRole("button", { name: "Create" }));
  expect(screen.getByRole("button", { name: "Saving..." })).toBeDisabled();
  expect(create).toHaveBeenCalledWith(expect.objectContaining({
    title: "Update", body: "Fixture body",
    blocks: [
      expect.objectContaining({ type: "text", text: "Fixture body" }),
      expect.objectContaining({ type: "photo", file: photo }),
    ],
  }));
  await act(async () => rejectUpload(new Error("Upload was rejected")));
  expect(await screen.findByText("Upload was rejected")).toBeVisible();
  expect(screen.getByText("photo.png")).toBeVisible();
  expect(bodyField()).toHaveValue("Fixture body");
  expect(screen.getByRole("button", { name: "Create" })).toBeEnabled();
});

test("edits an existing message, retains its media key and clears the editor on success", async () => {
  const user = userEvent.setup();
  const message: CommunicationMessageDto = {
    id: 17, title: "Published", body: "Text",
    visibleFromUtc: "2026-01-01T00:00:00Z", deadlineAtUtc: "2099-01-03T00:00:00Z",
    createdAtUtc: "2026-01-01T00:00:00Z", createdByManagerName: "Fixture Manager", isActive: true,
    blocks: [
      { id: "text", type: "text", text: "Text" },
      { id: "photo", type: "photo", media: { key: "retained", fileName: "existing.webp", size: 10 } },
    ],
  };
  vi.spyOn(communicationsApi, "gif").mockResolvedValue({ blob: new Blob(["preview"]), contentType: "image/webp" });
  const update = vi.spyOn(communicationsApi, "update").mockResolvedValue({ ...message, title: "Revised", body: "Text!" });
  mount([message]);
  await user.click(await screen.findByRole("button", { name: "Edit" }));
  expect(bodyField()).toHaveValue("Text");
  await user.clear(titleField());
  await user.type(titleField(), "Revised");
  await user.type(bodyField(), "!");
  await user.click(screen.getByRole("button", { name: "Save changes" }));
  expect(update).toHaveBeenCalledWith(expect.objectContaining({
    id: 17, title: "Revised", body: "Text!",
    blocks: [{ id: "text", type: "text", text: "Text!" }, message.blocks![1]],
  }));
  expect(await screen.findByText(/Communication "Revised" was updated/)).toBeVisible();
  expect(titleField()).toHaveValue("");
  expect(bodyField()).toHaveValue("");
  expect(screen.getByRole("button", { name: "Create" })).toBeEnabled();
});
