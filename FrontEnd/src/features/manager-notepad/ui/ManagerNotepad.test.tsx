import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { ManagerNotepad } from "./ManagerNotepad";

const mocks = vi.hoisted(() => ({
  query: vi.fn(),
  createMutate: vi.fn(),
  updateMutate: vi.fn(),
  deleteMutate: vi.fn(),
  stateMutate: vi.fn(),
  execCommand: vi.fn(),
}));

vi.mock("@entities/manager-notepad", () => ({
  useManagerNotepadQuery: () => mocks.query(),
  useCreateManagerNoteMutation: () => ({ mutate: mocks.createMutate, isPending: false }),
  useUpdateManagerNoteMutation: () => ({ mutate: mocks.updateMutate, isPending: false }),
  useDeleteManagerNoteMutation: () => ({ mutate: mocks.deleteMutate, isPending: false }),
  useSaveManagerNotepadStateMutation: () => ({ mutate: mocks.stateMutate, isPending: false }),
}));

beforeEach(() => {
  mocks.createMutate.mockReset();
  mocks.updateMutate.mockReset();
  mocks.deleteMutate.mockReset();
  mocks.stateMutate.mockReset();
  mocks.execCommand.mockReset();
  Object.defineProperty(document, "execCommand", {
    configurable: true,
    value: mocks.execCommand,
  });
  mocks.query.mockReturnValue({
    data: {
      notes: [{
        id: 7,
        title: "Release checklist",
        content: "Verify the schedule.",
        color: "blue",
        createdAtUtc: "2026-07-20T10:00:00Z",
        updatedAtUtc: "2026-07-20T11:00:00Z",
      }],
      state: { isExpanded: true, isPinned: false, height: 520 },
    },
    isLoading: false,
    error: null,
  });
});

describe("ManagerNotepad", () => {
  test("opens a note, saves edits, pins and persists a resized height", async () => {
    const user = userEvent.setup();
    render(<ManagerNotepad />);

    await user.click(await screen.findByRole("button", { name: /Release checklist/i }));
    const titleInput = screen.getByRole("textbox", { name: "Note title" });
    expect(titleInput.parentElement).toContainElement(screen.getByText("Saved"));
    await user.clear(titleInput);
    await user.type(titleInput, "Updated checklist");
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(mocks.updateMutate).toHaveBeenCalledWith(
      {
        noteId: 7,
        payload: {
          title: "Updated checklist",
          content: "Verify the schedule.",
          color: "blue",
        },
      },
      expect.objectContaining({ onSuccess: expect.any(Function), onError: expect.any(Function) }),
    );

    await user.click(screen.getByRole("button", { name: "Pin notepad" }));
    expect(mocks.stateMutate).toHaveBeenCalledWith(
      { isExpanded: true, isPinned: true, height: 520 },
      expect.objectContaining({ onError: expect.any(Function) }),
    );

    const resizeHandle = screen.getByRole("separator", { name: "Resize notepad" });
    fireEvent.pointerDown(resizeHandle, { clientY: 500 });
    fireEvent.pointerMove(window, { clientY: 620 });
    fireEvent.pointerUp(window);
    expect(mocks.stateMutate).toHaveBeenLastCalledWith(
      { isExpanded: true, isPinned: true, height: 640 },
      expect.objectContaining({ onError: expect.any(Function) }),
    );
  });

  test("an unpinned notepad collapses when the manager clicks outside", async () => {
    render(<ManagerNotepad />);
    await screen.findByRole("complementary", { name: "Manager notepad" });

    fireEvent.pointerDown(document.body);

    await waitFor(() => expect(mocks.stateMutate).toHaveBeenCalledWith(
      { isExpanded: false, isPinned: false, height: 520 },
      expect.objectContaining({ onError: expect.any(Function) }),
    ));
    expect(screen.getByRole("button", { name: "Open notepad" })).toBeInTheDocument();
  });

  test("formats selected text from the side toolbar and collapses from the bottom button", async () => {
    const user = userEvent.setup();
    render(<ManagerNotepad />);

    await user.click(await screen.findByRole("button", { name: /Release checklist/i }));
    await user.click(screen.getByRole("button", { name: "Bold" }));
    expect(mocks.execCommand).toHaveBeenCalledWith("bold", false, undefined);

    await user.click(screen.getByRole("button", { name: "Text color" }));
    expect(screen.getByRole("dialog", { name: "Choose text color" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Text color #dc2626" }));
    expect(mocks.execCommand).toHaveBeenCalledWith("foreColor", false, "#dc2626");

    await user.click(screen.getByRole("button", { name: "Collapse notepad" }));
    expect(mocks.stateMutate).toHaveBeenLastCalledWith(
      { isExpanded: false, isPinned: false, height: 520 },
      expect.objectContaining({ onError: expect.any(Function) }),
    );
  });

  test("keeps typed and deleted rich text stable across React rerenders", async () => {
    const user = userEvent.setup();
    render(<ManagerNotepad />);

    await user.click(await screen.findByRole("button", { name: /Release checklist/i }));
    const editor = screen.getByRole("textbox", { name: "Note content" });

    editor.innerHTML = "First line<br>Second line";
    fireEvent.input(editor);
    await waitFor(() => expect(editor.innerHTML).toBe("First line<br>Second line"));

    editor.innerHTML = "First lin<br>Second line";
    fireEvent.input(editor);
    await waitFor(() => expect(editor.innerHTML).toBe("First lin<br>Second line"));

    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(mocks.updateMutate).toHaveBeenCalledWith(
      {
        noteId: 7,
        payload: {
          title: "Release checklist",
          content: "First lin<br>Second line",
          color: "blue",
        },
      },
      expect.objectContaining({ onSuccess: expect.any(Function), onError: expect.any(Function) }),
    );
  });

  test("keeps navigation under the Notepad label and exposes working list commands only", async () => {
    const user = userEvent.setup();
    render(<ManagerNotepad />);

    await user.click(await screen.findByRole("button", { name: /Release checklist/i }));
    const backButton = screen.getByRole("button", { name: "Notes" });
    expect(backButton.closest("header")).not.toBeNull();
    expect(screen.queryByText("Edit note")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Align left" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Bulleted list" }));
    await user.click(screen.getByRole("button", { name: "Numbered list" }));
    expect(mocks.execCommand).toHaveBeenCalledWith("insertUnorderedList", false, undefined);
    expect(mocks.execCommand).toHaveBeenCalledWith("insertOrderedList", false, undefined);
  });
});
