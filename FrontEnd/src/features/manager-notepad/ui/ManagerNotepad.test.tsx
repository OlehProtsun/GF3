import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { managerNotepadApi, type ManagerNote, type ManagerNotepad as NotepadResponse } from "@entities/manager-notepad";
import { setAuthAccessToken } from "@shared/api/httpClient";
import { setLanguage } from "@shared/i18n";
import { ManagerNotepad } from "./ManagerNotepad";

function note(id: number, overrides: Partial<ManagerNote> = {}): ManagerNote {
  return {
    id, ownerManagerId: 1, title: `Note ${id}`, content: "", color: "slate",
    createdAtUtc: "2026-11-26T10:00:00Z", updatedAtUtc: "2026-11-26T10:00:00Z", visibility: "private",
    isPinned: false, isLocked: false, folderId: null, tagIds: [], sharedWithManagerIds: [],
    revision: 1, canEdit: true, canManage: true, ...overrides,
  };
}

let data: NotepadResponse;

beforeEach(() => {
  setLanguage("en");
  data = { notes: [], folders: [], tags: [], state: { isExpanded: true, isPinned: false, height: 520 } };
  vi.spyOn(managerNotepadApi, "get").mockImplementation(async () => data);
  vi.spyOn(managerNotepadApi, "saveState").mockResolvedValue();
});
afterEach(() => vi.restoreAllMocks());

function mount(client = new QueryClient()) {
  return render(<QueryClientProvider client={client}><ManagerNotepad /></QueryClientProvider>);
}

const panel = () => screen.findByRole("dialog", { name: "Manager notepad" });

describe("manager notepad", () => {
  it("uses server counts, sections, sort and search", async () => {
    data.notes = [
      note(1, { title: "Pinned zebra", isPinned: true }),
      note(2, { title: "Zeta", content: "<b>Meeting</b>" }),
      note(3, { title: "Alpha", content: "Meeting", updatedAtUtc: "2026-11-25T10:00:00Z" }),
      note(4, { title: "Published", visibility: "public" }),
      note(5, { title: "Shared checklist", visibility: "custom", canEdit: false, canManage: false }),
    ];
    mount();
    const notepad = within(await panel());
    expect(notepad.getByRole("button", { name: "Private 3" })).toHaveAttribute("aria-pressed", "true");
    expect(notepad.getByRole("heading", { name: "Pinned" })).toBeVisible();
    fireEvent.click(notepad.getByRole("button", { name: "Sort notes" }));
    fireEvent.click(screen.getByRole("menuitem", { name: "Title — A to Z" }));
    expect(notepad.getAllByRole("article").map(item => item.querySelector("strong")?.textContent)).toEqual(["Pinned zebra", "Alpha", "Zeta"]);
    fireEvent.change(notepad.getByRole("textbox", { name: "Search notes" }), { target: { value: "MEETING" } });
    expect(notepad.getAllByRole("article")).toHaveLength(2);
    fireEvent.click(notepad.getByRole("button", { name: "Clear note search" }));
    fireEvent.click(notepad.getByRole("button", { name: "Public 2" }));
    expect(notepad.getByText("Published")).toBeVisible();
    expect(notepad.getByText("Shared checklist")).toBeVisible();
    expect(notepad.queryByText("Pinned zebra")).toBeNull();
  });

  it("opens a blank draft without creating a record and keeps manual save", async () => {
    const create = vi.spyOn(managerNotepadApi, "createNote").mockImplementation(async request => {
      const created = note(7, request);
      data = { ...data, notes: [created] };
      return created;
    });
    mount();
    const notepad = within(await panel());
    expect(notepad.getByText("No private notes yet.")).toBeVisible();
    fireEvent.click(notepad.getByRole("button", { name: "New" }));
    expect(screen.getAllByRole("menuitem").map(item => item.textContent)).toEqual(["New note"]);
    fireEvent.click(screen.getByRole("menuitem", { name: "New note" }));
    expect(create).not.toHaveBeenCalled();
    fireEvent.change(notepad.getByRole("textbox", { name: "Note title" }), { target: { value: "New draft" } });
    fireEvent.click(notepad.getByRole("button", { name: "Save" }));
    expect(await notepad.findByText("Saved")).toBeVisible();
    expect(create).toHaveBeenCalledExactlyOnceWith({ title: "New draft", content: "", color: "slate" });
  });

  it("retains a failed draft when the user keeps editing", async () => {
    vi.spyOn(managerNotepadApi, "createNote").mockRejectedValue(new Error("Offline"));
    mount();
    const notepad = within(await panel());
    fireEvent.click(notepad.getByRole("button", { name: "New" }));
    fireEvent.click(screen.getByRole("menuitem", { name: "New note" }));
    const title = notepad.getByRole("textbox", { name: "Note title" });
    fireEvent.change(title, { target: { value: "Keep my work" } });
    fireEvent.click(notepad.getByRole("button", { name: "Notes" }));
    const prompt = within(await screen.findByRole("dialog", { name: "Discard unsaved changes?" }));
    fireEvent.click(prompt.getByRole("button", { name: "Keep editing" }));
    expect(title).toHaveValue("Keep my work");
    expect(notepad.getByText("Not saved")).toBeVisible();
    expect(notepad.getByRole("button", { name: "Retry" })).toBeVisible();
  });

  it("shows read-only shared notes and clears them on access refresh", async () => {
    const shared = note(5, { title: "Shared checklist", visibility: "custom", canEdit: false, canManage: false, content: "<b>Readers</b>" });
    data.notes = [shared];
    vi.spyOn(managerNotepadApi, "getNote").mockResolvedValue(shared);
    mount();
    const notepad = within(await panel());
    fireEvent.click(notepad.getByRole("button", { name: "Public 1" }));
    fireEvent.click(notepad.getByRole("button", { name: /Shared checklist/ }));
    expect(await notepad.findByText("Read only")).toBeVisible();
    expect(notepad.getByRole("textbox", { name: "Note content" })).toHaveAttribute("contenteditable", "false");
    expect(notepad.queryByRole("button", { name: "Note actions" })).toBeNull();
    expect(notepad.queryByRole("button", { name: "Save" })).toBeNull();
    data = { ...data, notes: [] };
    fireEvent(window, new Event("focus"));
    expect(await notepad.findByText("This note is no longer available.")).toBeVisible();
    expect(notepad.queryByText("Readers")).toBeNull();
  });

  it("saves custom recipient selection and confirms owner deletion", async () => {
    const current = note(1, { title: "To share" });
    data.notes = [current];
    vi.spyOn(managerNotepadApi, "getShareTargets").mockResolvedValue([{ id: 9, displayName: "Recipient" }]);
    const patch = vi.spyOn(managerNotepadApi, "updateMetadata").mockImplementation(async () => {
      const updated = { ...current, visibility: "custom" as const, sharedWithManagerIds: [9], revision: 2 };
      data = { ...data, notes: [updated] };
      return updated;
    });
    const remove = vi.spyOn(managerNotepadApi, "deleteNote").mockImplementation(async () => { data = { ...data, notes: [] }; });
    mount();
    const notepad = within(await panel());
    fireEvent.click(notepad.getByRole("button", { name: /To share/ }));
    fireEvent.click(notepad.getByRole("button", { name: "Private" }));
    fireEvent.click(await notepad.findByRole("button", { name: "Custom" }));
    fireEvent.click(await notepad.findByRole("checkbox", { name: "Recipient" }));
    fireEvent.click(notepad.getByRole("button", { name: "Save changes" }));
    await waitFor(() => expect(patch).toHaveBeenCalledWith(1, expect.objectContaining({
      visibility: "custom", sharedWithManagerIds: [9], expectedRevision: 1,
    })));
    await waitFor(() => expect(notepad.getByRole("button", { name: "Save changes" })).toBeDisabled());
    fireEvent.click(notepad.getByRole("button", { name: "Note" }));
    fireEvent.click(await notepad.findByRole("button", { name: "Note actions" }));
    fireEvent.click(screen.getByRole("menuitem", { name: "Delete" }));
    const dialog = within(await screen.findByRole("dialog", { name: "Delete note?" }));
    expect(remove).not.toHaveBeenCalled();
    fireEvent.click(dialog.getByRole("button", { name: "Delete" }));
    await waitFor(() => expect(remove).toHaveBeenCalledWith(1));
  });

  it("persists title-only edits, panel pinning and pointer resizing", async () => {
    const original = note(7, { title: "Release checklist", content: "Verify the schedule.", color: "blue" });
    data.notes = [original];
    const update = vi.spyOn(managerNotepadApi, "updateNote").mockImplementation(async (_, changes) => {
      const saved = { ...original, ...changes, revision: 2 };
      data = { ...data, notes: [saved] };
      return saved;
    });
    mount();
    const notepad = within(await panel());
    fireEvent.click(notepad.getByRole("button", { name: /Release checklist/ }));
    const title = notepad.getByRole("textbox", { name: "Note title" });
    expect(title.parentElement).toContainElement(notepad.getByText("Saved"));
    fireEvent.change(title, { target: { value: "Updated checklist" } });
    fireEvent.click(notepad.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(update).toHaveBeenCalledWith(7, {
      title: "Updated checklist", content: "Verify the schedule.", color: "blue", expectedRevision: 1,
    }));
    fireEvent.click(notepad.getByRole("button", { name: "Pin notepad" }));
    expect(managerNotepadApi.saveState).toHaveBeenLastCalledWith({ isExpanded: true, isPinned: true, height: 520 });
    const resize = notepad.getByRole("separator", { name: "Resize notepad" });
    fireEvent.pointerDown(resize, { clientY: 500 });
    fireEvent.pointerMove(window, { clientY: 620 });
    fireEvent.pointerUp(window);
    expect(managerNotepadApi.saveState).toHaveBeenLastCalledWith({ isExpanded: true, isPinned: true, height: 640 });
  });

  it("allows title-only edits when legacy plain text fits the stored limit", async () => {
    const content = "X".repeat(19_998) + " &";
    const original = note(1, { title: "Legacy note", content });
    data.notes = [original];
    const update = vi.spyOn(managerNotepadApi, "updateNote").mockResolvedValue({ ...original, title: "Renamed", revision: 2 });
    mount();
    const notepad = within(await panel());
    fireEvent.click(notepad.getByText("Legacy note"));
    fireEvent.change(notepad.getByRole("textbox", { name: "Note title" }), { target: { value: "Renamed" } });
    fireEvent.click(notepad.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(update).toHaveBeenCalledWith(1, expect.objectContaining({ title: "Renamed", content })));
  });

  it("collapses an unpinned panel on an outside click", async () => {
    mount();
    await panel();
    fireEvent.pointerDown(document.body);
    await waitFor(() => expect(managerNotepadApi.saveState).toHaveBeenCalledWith({ isExpanded: false, isPinned: false, height: 520 }));
    expect(screen.getByRole("button", { name: "Open notepad" })).toBeVisible();
  });

  it("retains formatting commands and bottom collapse", async () => {
    data.notes = [note(7, { title: "Release checklist" })];
    const execCommand = vi.fn();
    const descriptor = Object.getOwnPropertyDescriptor(document, "execCommand");
    Object.defineProperty(document, "execCommand", { configurable: true, value: execCommand });
    try {
      mount();
      const notepad = within(await panel());
      fireEvent.click(notepad.getByRole("button", { name: /Release checklist/ }));
      expect(notepad.getByRole("button", { name: "Notes" }).closest("header")).not.toBeNull();
      expect(notepad.queryByText("Edit note")).toBeNull();
      expect(notepad.queryByRole("button", { name: "Align left" })).toBeNull();
      fireEvent.click(notepad.getByRole("button", { name: "Bold" }));
      expect(execCommand).toHaveBeenCalledWith("bold", false, undefined);
      fireEvent.click(notepad.getByRole("button", { name: "Text color" }));
      const colors = within(notepad.getByRole("dialog", { name: "Choose text color" }));
      fireEvent.click(colors.getByRole("button", { name: "Text color #dc2626" }));
      expect(execCommand).toHaveBeenCalledWith("foreColor", false, "#dc2626");
      fireEvent.click(notepad.getByRole("button", { name: "Bulleted list" }));
      fireEvent.click(notepad.getByRole("button", { name: "Numbered list" }));
      expect(execCommand).toHaveBeenCalledWith("insertUnorderedList", false, undefined);
      expect(execCommand).toHaveBeenCalledWith("insertOrderedList", false, undefined);
      fireEvent.click(notepad.getByRole("button", { name: "Collapse notepad" }));
      await waitFor(() => expect(managerNotepadApi.saveState).toHaveBeenLastCalledWith({ isExpanded: false, isPinned: false, height: 520 }));
    } finally {
      if (descriptor) Object.defineProperty(document, "execCommand", descriptor);
      else Reflect.deleteProperty(document, "execCommand");
    }
  });

  it("keeps typed and deleted formatting through a background refetch", async () => {
    const original = note(7, { title: "Release checklist", content: "Original", color: "blue" });
    data.notes = [original];
    const update = vi.spyOn(managerNotepadApi, "updateNote").mockResolvedValue({ ...original, revision: 2, content: "First lin<br>Second line" });
    mount();
    const notepad = within(await panel());
    fireEvent.click(notepad.getByRole("button", { name: /Release checklist/ }));
    const editor = notepad.getByRole("textbox", { name: "Note content" });
    editor.innerHTML = "First line<br>Second line";
    fireEvent.input(editor);
    expect(editor.innerHTML).toBe("First line<br>Second line");
    editor.innerHTML = "First lin<br>Second line";
    fireEvent.input(editor);
    expect(editor.innerHTML).toBe("First lin<br>Second line");
    fireEvent.click(notepad.getByRole("button", { name: "Save" }));
    await waitFor(() => expect(update).toHaveBeenCalledWith(7, {
      title: "Release checklist", content: "First lin<br>Second line", color: "blue", expectedRevision: 1,
    }));
    expect(editor.innerHTML).toBe("First lin<br>Second line");
  });

  it("attempts a final save and recovers a failed draft when the workspace remounts", async () => {
    const client = new QueryClient();
    setAuthAccessToken("notepad-test-session");
    const create = vi.spyOn(managerNotepadApi, "createNote").mockRejectedValue(new Error("Offline"));
    try {
      const workspace = mount(client);
      const notepad = within(await panel());
      fireEvent.click(notepad.getByRole("button", { name: "New" }));
      fireEvent.click(screen.getByRole("menuitem", { name: "New note" }));
      fireEvent.change(notepad.getByRole("textbox", { name: "Note title" }), { target: { value: "Recovered draft" } });
      workspace.unmount();
      expect(create).toHaveBeenCalledTimes(1);

      mount(client);
      const reopened = within(await panel());
      expect(await reopened.findByRole("textbox", { name: "Note title" })).toHaveValue("Recovered draft");
      expect(reopened.getByRole("button", { name: "Retry" })).toBeVisible();
      expect(create).toHaveBeenCalledTimes(1);
    } finally {
      setAuthAccessToken(null);
    }
  });

  it("shows a fetch error and allows retry", async () => {
    vi.mocked(managerNotepadApi.get).mockRejectedValueOnce(new Error("Offline"));
    mount();
    fireEvent.click(screen.getByRole("button", { name: "Open notepad" }));
    const notepad = within(await panel());
    expect(await notepad.findByRole("alert")).toHaveTextContent("Offline");
    fireEvent.click(notepad.getByRole("button", { name: "Retry" }));
    expect(await notepad.findByText("No private notes yet.")).toBeVisible();
  });
});
