import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { managerNotepadApi, type ManagerNote } from "@entities/manager-notepad";
import { ApiError } from "@shared/api/httpClient";
import { usePublicNoteSession } from "./usePublicNoteSession";

const note: ManagerNote = {
  id: 7, ownerManagerId: 1, title: "Public", content: "Shared", color: "slate",
  createdAtUtc: "", updatedAtUtc: "", visibility: "public", revision: 1,
  isPinned: false, isLocked: false, folderId: null, tagIds: [], sharedWithManagerIds: [], canEdit: true, canManage: true,
};

function deferred() {
  let resolve!: (value: ManagerNote) => void;
  const promise = new Promise<ManagerNote>(done => { resolve = done; });
  return { promise, resolve };
}

function mount(id: number | null = note.id) {
  const acquired = vi.fn();
  const changed = vi.fn();
  return { ...renderHook(({ noteId }) => usePublicNoteSession(noteId, acquired, changed), { initialProps: { noteId: id } }), acquired, changed };
}

beforeEach(() => {
  vi.useFakeTimers();
  vi.spyOn(managerNotepadApi, "releaseEditSession").mockResolvedValue();
});
afterEach(() => { vi.useRealTimers(); vi.restoreAllMocks(); });

describe("public note edit session", () => {
  it("opens only after acquisition and renews each tab with its own token every twenty seconds", async () => {
    const first = deferred();
    const edit = vi.spyOn(managerNotepadApi, "editSession").mockImplementationOnce(() => first.promise).mockResolvedValue(note);
    const hook = mount(null);
    expect(hook.result.current.status).toBeNull();
    expect(edit).not.toHaveBeenCalled();
    hook.rerender({ noteId: note.id });
    expect(hook.result.current.status).toBe("connecting");
    expect(hook.result.current.token).toBeUndefined();
    const other = mount();
    const firstToken = edit.mock.calls[0][1];
    const secondToken = edit.mock.calls[1][1];
    expect(firstToken).not.toBe(secondToken);
    await act(async () => { first.resolve(note); });
    expect(hook.result.current).toMatchObject({ status: "active", token: firstToken });
    expect(other.result.current).toMatchObject({ status: "active", token: secondToken });
    expect(hook.acquired).toHaveBeenCalledExactlyOnceWith(note);
    await act(() => vi.advanceTimersByTimeAsync(19_999));
    expect(edit).toHaveBeenCalledTimes(2);
    await act(() => vi.advanceTimersByTimeAsync(1));
    expect(edit).toHaveBeenCalledWith(note.id, firstToken, false);
    expect(edit).toHaveBeenCalledWith(note.id, secondToken, false);
  });

  it.each([[409, "busy"], [404, "unavailable"]] as const)("maps initial HTTP %s and lets the user retry with a new token", async (status, state) => {
    const edit = vi.spyOn(managerNotepadApi, "editSession")
      .mockRejectedValueOnce(new ApiError({ status, message: "Unavailable" })).mockResolvedValue(note);
    const hook = mount();
    await act(async () => {});
    expect(hook.result.current.status).toBe(state);
    expect(hook.result.current.token).toBeUndefined();
    act(() => hook.result.current.retry());
    await act(async () => {});
    expect(hook.result.current.status).toBe("active");
    expect(edit.mock.calls[1][1]).not.toBe(edit.mock.calls[0][1]);
  });

  it.each([404, 409, 500])("drops a previously active lease after renewal HTTP %s", async status => {
    const edit = vi.spyOn(managerNotepadApi, "editSession").mockResolvedValueOnce(note)
      .mockRejectedValueOnce(new ApiError({ status, message: "Lost lease" }));
    const hook = mount();
    await act(async () => {});
    const token = hook.result.current.token;
    await act(() => vi.advanceTimersByTimeAsync(20_000));
    expect(hook.result.current.status).toBe("lost");
    expect(hook.result.current.token).toBeUndefined();
    expect(managerNotepadApi.releaseEditSession).toHaveBeenCalledWith(note.id, token, false);
    act(() => window.dispatchEvent(new Event("online")));
    await act(() => vi.advanceTimersByTimeAsync(60_000));
    expect(edit).toHaveBeenCalledTimes(2);
    expect(hook.changed).toHaveBeenCalled();
  });

  it("discards a delayed initial response past the monotonic client deadline", async () => {
    const pending = deferred();
    const edit = vi.spyOn(managerNotepadApi, "editSession").mockReturnValue(pending.promise);
    const hook = mount();
    await act(() => vi.advanceTimersByTimeAsync(55_001));
    await act(async () => { pending.resolve(note); });
    expect(hook.result.current.status).toBe("lost");
    expect(hook.result.current.token).toBeUndefined();
    expect(hook.acquired).not.toHaveBeenCalled();
    expect(managerNotepadApi.releaseEditSession).toHaveBeenCalledWith(note.id, edit.mock.calls[0][1], false);
  });

  it("ignores in-flight acquire/renew responses when changing notes, invalidating or hiding the page", async () => {
    const initial = deferred();
    const renewal = deferred();
    const edit = vi.spyOn(managerNotepadApi, "editSession").mockReturnValueOnce(initial.promise)
      .mockResolvedValueOnce({ ...note, id: 8 }).mockReturnValueOnce(renewal.promise);
    const hook = mount();
    hook.rerender({ noteId: 8 });
    await act(async () => { initial.resolve(note); });
    expect(hook.acquired).toHaveBeenCalledExactlyOnceWith({ ...note, id: 8 });
    expect(managerNotepadApi.releaseEditSession).toHaveBeenCalledWith(note.id, edit.mock.calls[0][1], true);
    const token = hook.result.current.token;
    act(() => {
      window.dispatchEvent(new Event("online"));
      window.dispatchEvent(new Event("online"));
    });
    expect(edit).toHaveBeenCalledTimes(3);
    expect(edit).toHaveBeenLastCalledWith(8, token, false);
    act(() => window.dispatchEvent(new Event("pagehide")));
    expect(hook.result.current.status).toBe("lost");
    act(() => hook.result.current.invalidate());
    await act(async () => { renewal.resolve({ ...note, id: 8 }); });
    expect(hook.result.current.token).toBeUndefined();
    expect(managerNotepadApi.releaseEditSession).toHaveBeenCalledWith(8, token, true);
    hook.unmount();
    await act(async () => {
      window.dispatchEvent(new Event("online"));
      await vi.advanceTimersByTimeAsync(80_000);
    });
    expect(edit).toHaveBeenCalledTimes(3);
  });
});
