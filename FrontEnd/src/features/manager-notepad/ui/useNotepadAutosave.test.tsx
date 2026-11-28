import { act, renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { managerNotepadApi, type ManagerNote } from "@entities/manager-notepad";
import { ApiError } from "@shared/api/httpClient";
import { useNotepadAutosave } from "./useNotepadAutosave";

const savedNote: ManagerNote = {
  id: 7, ownerManagerId: 1, title: "Draft", content: "<b>Content</b>", color: "slate",
  createdAtUtc: "", updatedAtUtc: "", visibility: "private", revision: 1,
  isPinned: false, isLocked: false, folderId: null, tagIds: [], sharedWithManagerIds: [], canEdit: true, canManage: true,
};

function mount(input: Partial<Parameters<typeof useNotepadAutosave>[0]> = {}) {
  const client = new QueryClient();
  const props = {
    noteId: null, revision: null, title: "Draft", content: "<b>Content</b>", color: "slate" as const,
    dirty: true, enabled: true, onSaved: vi.fn(), onError: vi.fn(), ...input,
  };
  const wrapper = ({ children }: PropsWithChildren) => <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  return { ...renderHook(useNotepadAutosave, { initialProps: props, wrapper }), props };
}

beforeEach(() => vi.useFakeTimers());
afterEach(() => { vi.useRealTimers(); vi.restoreAllMocks(); });

describe("notepad autosave", () => {
  it("waits 1200 ms after the last edit and does not save an untouched draft", async () => {
    const create = vi.spyOn(managerNotepadApi, "createNote").mockResolvedValue(savedNote);
    const hook = mount({ dirty: false, title: "", content: "" });
    await act(() => vi.advanceTimersByTimeAsync(2000));
    expect(create).not.toHaveBeenCalled();
    hook.rerender({ ...hook.props, dirty: true, title: "Draft" });
    await act(() => vi.advanceTimersByTimeAsync(1199));
    expect(create).not.toHaveBeenCalled();
    hook.rerender({ ...hook.props, dirty: true, title: "Latest" });
    await act(() => vi.advanceTimersByTimeAsync(1200));
    expect(create).toHaveBeenCalledExactlyOnceWith({ title: "Latest", content: "", color: "slate" });
    expect(hook.props.onSaved).toHaveBeenCalledWith(savedNote, expect.objectContaining({ title: "Latest" }));
  });

  it("flushes before navigation and reuses the acknowledged revision for a second in-flight edit", async () => {
    let complete!: (note: ManagerNote) => void;
    const update = vi.spyOn(managerNotepadApi, "updateNote")
      .mockImplementationOnce(() => new Promise(resolve => { complete = resolve; }))
      .mockResolvedValueOnce({ ...savedNote, revision: 3, title: "Latest" });
    const hook = mount({ noteId: 7, revision: 1 });
    let flushed!: Promise<boolean>;
    await act(async () => { flushed = hook.result.current.flush(); });
    expect(update).toHaveBeenCalledTimes(1);
    expect(hook.result.current.isSaving).toBe(true);
    hook.rerender({ ...hook.props, title: "Latest" });
    await act(() => vi.advanceTimersByTimeAsync(2400));
    expect(update).toHaveBeenCalledTimes(1);
    await act(async () => { complete({ ...savedNote, revision: 2 }); expect(await flushed).toBe(true); });
    expect(update).toHaveBeenNthCalledWith(1, 7, expect.objectContaining({ expectedRevision: 1, title: "Draft" }), undefined);
    expect(update).toHaveBeenNthCalledWith(2, 7, expect.objectContaining({ expectedRevision: 2, title: "Latest" }), undefined);
  });

  it("uses one explicit phone save with the lease and current revision", async () => {
    const update = vi.spyOn(managerNotepadApi, "updateNote").mockResolvedValue({ ...savedNote, revision: 2 });
    const hook = mount({ noteId: 7, revision: 1, explicitSave: true, sessionRequired: true });
    await act(() => vi.advanceTimersByTimeAsync(5_000));
    await act(async () => { expect(await hook.result.current.save()).toBe(false); });
    expect(update).not.toHaveBeenCalled();
    hook.rerender({ ...hook.props, sessionToken: "tab-lease", revision: 3 });
    await act(() => vi.advanceTimersByTimeAsync(5_000));
    expect(update).not.toHaveBeenCalled();
    await act(async () => { expect(await hook.result.current.save()).toBe(true); });
    expect(update).toHaveBeenCalledExactlyOnceWith(7, expect.objectContaining({ expectedRevision: 3 }), "tab-lease");
  });

  it("updates only metadata and distinguishes an edit-session conflict from a revision conflict", async () => {
    const conflict = new ApiError({ status: 409, message: "Lease expired", details: { code: "notepad_edit_session_conflict" } });
    const patch = vi.spyOn(managerNotepadApi, "updateMetadata").mockRejectedValue(conflict);
    const update = vi.spyOn(managerNotepadApi, "updateNote");
    const hook = mount({ noteId: 7, revision: 2, dirty: false, sessionRequired: true, sessionToken: "old-token" });
    await act(async () => {
      expect(await hook.result.current.save(false, { visibility: "custom", sharedWithManagerIds: [9] })).toBe(false);
    });
    expect(patch).toHaveBeenCalledExactlyOnceWith(7, { visibility: "custom", sharedWithManagerIds: [9], expectedRevision: 2 }, "old-token");
    expect(update).not.toHaveBeenCalled();
    expect(hook.result.current.error).toEqual({ kind: "error", cause: conflict });
    expect(hook.props.onSaved).not.toHaveBeenCalled();
  });

  it("never applies a save acknowledgment to a different selected note", async () => {
    let complete!: (note: ManagerNote) => void;
    const update = vi.spyOn(managerNotepadApi, "updateNote").mockImplementationOnce(() => new Promise(resolve => { complete = resolve; }));
    const hook = mount({ noteId: 7, revision: 1, explicitSave: true });
    let saving!: Promise<boolean>;
    await act(async () => { saving = hook.result.current.save(); });
    hook.rerender({ ...hook.props, noteId: 8, revision: 3, title: "Another note" });
    await act(async () => { complete({ ...savedNote, revision: 2 }); expect(await saving).toBe(false); });
    expect(update).toHaveBeenCalledTimes(1);
    expect(hook.props.onSaved).not.toHaveBeenCalled();
  });

  it("keeps a revision conflict and prevents manual or automatic overwrite", async () => {
    const conflict = new ApiError({ status: 409, message: "Conflict" });
    const update = vi.spyOn(managerNotepadApi, "updateNote").mockRejectedValue(conflict);
    const hook = mount({ noteId: 7, revision: 1 });
    await act(async () => { expect(await hook.result.current.flush()).toBe(false); });
    expect(hook.result.current.error).toEqual({ kind: "conflict", cause: conflict });
    await act(async () => {
      expect(await hook.result.current.save(true)).toBe(false);
      await vi.advanceTimersByTimeAsync(3000);
    });
    expect(update).toHaveBeenCalledTimes(1);
    expect(hook.props.onSaved).not.toHaveBeenCalled();
  });

  it("retains failed edits until explicit retry succeeds", async () => {
    const create = vi.spyOn(managerNotepadApi, "createNote").mockRejectedValueOnce(new Error("Offline")).mockResolvedValueOnce(savedNote);
    const hook = mount();
    await act(() => vi.advanceTimersByTimeAsync(1200));
    expect(hook.result.current.error?.kind).toBe("error");
    await act(async () => { expect(await hook.result.current.flush()).toBe(false); await vi.advanceTimersByTimeAsync(2400); });
    expect(create).toHaveBeenCalledTimes(1);
    await act(async () => { expect(await hook.result.current.save(true)).toBe(true); });
    expect(hook.result.current.error).toBeNull();
    expect(hook.props.onSaved).toHaveBeenCalledTimes(1);
  });

  it("cancels queued writes when changing selection and blocks invalid dirty navigation", async () => {
    const update = vi.spyOn(managerNotepadApi, "updateNote").mockResolvedValue(savedNote);
    const hook = mount({ noteId: 4, revision: 1 });
    await act(() => vi.advanceTimersByTimeAsync(1100));
    hook.rerender({ ...hook.props, noteId: 5, content: "a".repeat(20_001) });
    await act(async () => { expect(await hook.result.current.flush()).toBe(false); await vi.advanceTimersByTimeAsync(1300); });
    expect(update).not.toHaveBeenCalled();
  });
});
