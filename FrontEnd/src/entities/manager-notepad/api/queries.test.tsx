import { act, renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { setAuthAccessToken } from "@shared/api/httpClient";
import { queryKeys } from "@shared/api/queryKeys";
import type { ManagerNote, ManagerNotepad } from "../model/types";
import { managerNotepadApi } from "./managerNotepadApi";
import {
  useCreateManagerNoteMutation, useDeleteManagerNoteMutation, useManagerNotepadQuery,
  useRestoreManagerNoteVersionMutation, useUpdateManagerNoteMetadataMutation, useUpdateManagerNoteMutation,
} from "./queries";

const note: ManagerNote = {
  id: 7, ownerManagerId: 1, title: "Note", content: "Private text", color: "slate",
  createdAtUtc: "", updatedAtUtc: "", visibility: "private", revision: 2,
  isPinned: false, isLocked: false, folderId: null, tagIds: [], sharedWithManagerIds: [], canEdit: true, canManage: true,
};
const panel = (notes: ManagerNote[] = []): ManagerNotepad => ({
  notes, state: { isPinned: false, isExpanded: true, height: 520 }, tags: [], folders: [],
});
const currentKey = queryKeys.managerNotepad.current();
const versionsKey = queryKeys.managerNotepad.versions(note.id);
const payload = { title: note.title, content: note.content, color: "slate" as const };

function fixture() {
  const client = new QueryClient();
  const wrapper = ({ children }: PropsWithChildren) => <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  return { client, wrapper };
}

afterEach(() => {
  setAuthAccessToken(null);
  vi.useRealTimers();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

describe("manager notepad queries", () => {
  it("refreshes shared notes on expiry, visible focus and 15-second polling, then cleans up", async () => {
    vi.useFakeTimers();
    vi.spyOn(document, "hidden", "get").mockReturnValue(false);
    const { client, wrapper } = fixture();
    const refresh = vi.spyOn(client, "invalidateQueries");
    const get = vi.spyOn(managerNotepadApi, "get").mockResolvedValueOnce(panel([{
      ...note, visibility: "public", activeEditor: { displayName: "Editor", expiresAtMs: Date.now() + 2_000 },
    }])).mockResolvedValue(panel([note]));
    const query = renderHook(({ enabled }) => useManagerNotepadQuery(enabled, true), { initialProps: { enabled: true }, wrapper });
    await act(async () => {});
    expect(get).toHaveBeenCalledTimes(1);
    await act(() => vi.advanceTimersByTimeAsync(2_149));
    expect(refresh).not.toHaveBeenCalled();
    await act(() => vi.advanceTimersByTimeAsync(1));
    expect(refresh).toHaveBeenCalledExactlyOnceWith({ queryKey: currentKey });
    await act(async () => { window.dispatchEvent(new Event("focus")); });
    expect(refresh).toHaveBeenCalledTimes(2);
    await act(() => vi.advanceTimersByTimeAsync(12_850));
    expect(refresh).toHaveBeenCalledTimes(3);
    query.rerender({ enabled: false });
    await act(async () => {
      window.dispatchEvent(new Event("focus"));
      await vi.advanceTimersByTimeAsync(30_000);
    });
    expect(refresh).toHaveBeenCalledTimes(3);
    query.unmount();
  });

  it("updates and invalidates current notes and versions while retaining a newer cached revision", async () => {
    const { client, wrapper } = fixture();
    client.setQueryData(currentKey, panel());
    const invalidate = vi.spyOn(client, "invalidateQueries");
    vi.spyOn(managerNotepadApi, "createNote").mockResolvedValue(note);
    vi.spyOn(managerNotepadApi, "updateNote").mockResolvedValue({ ...note, revision: 1, content: "Stale" });
    vi.spyOn(managerNotepadApi, "updateMetadata").mockResolvedValue({ ...note, revision: 3, isPinned: true });
    vi.spyOn(managerNotepadApi, "restoreVersion").mockResolvedValue({ ...note, revision: 4, content: "Restored" });
    vi.spyOn(managerNotepadApi, "deleteNote").mockResolvedValue();
    const create = renderHook(useCreateManagerNoteMutation, { wrapper });
    const update = renderHook(useUpdateManagerNoteMutation, { wrapper });
    const metadata = renderHook(useUpdateManagerNoteMetadataMutation, { wrapper });
    const restore = renderHook(useRestoreManagerNoteVersionMutation, { wrapper });
    const remove = renderHook(useDeleteManagerNoteMutation, { wrapper });
    const cached = () => client.getQueryState<ManagerNotepad>(currentKey).data?.notes;
    await act(async () => { await create.result.current.mutateAsync(payload); });
    expect(cached()).toEqual([note]);
    expect(invalidate).toHaveBeenLastCalledWith({ queryKey: currentKey });
    await act(async () => { await update.result.current.mutateAsync({ noteId: note.id, payload, token: "lease" }); });
    expect(cached()).toEqual([note]);
    expect(invalidate).toHaveBeenLastCalledWith({ queryKey: versionsKey });
    await act(async () => { await metadata.result.current.mutateAsync({ noteId: note.id, payload: { expectedRevision: 2, isPinned: true } }); });
    expect(cached()?.[0]).toMatchObject({ revision: 3, isPinned: true });
    expect(invalidate).toHaveBeenLastCalledWith({ queryKey: currentKey });
    await act(async () => { restore.result.current.mutate({ noteId: note.id, versionId: 1, expectedRevision: 3, token: "lease" }); });
    expect(cached()?.[0]).toMatchObject({ revision: 4, content: "Restored" });
    expect(invalidate).toHaveBeenLastCalledWith({ queryKey: versionsKey });
    client.setQueryData(versionsKey, [{ id: 1 }]);
    await act(async () => { remove.result.current.mutate({ noteId: note.id }); });
    expect(cached()).toEqual([]);
    expect(client.getQueryState(versionsKey).data).toBeUndefined();
  });

  it("adds the lease only to session-aware requests and rejects old writes after an auth change", async () => {
    const { client, wrapper } = fixture();
    setAuthAccessToken("first-manager");
    let complete!: (response: Response) => void;
    const fetch = vi.fn<typeof globalThis.fetch>()
      .mockResolvedValueOnce(Response.json(note))
      .mockResolvedValueOnce(Response.json(note))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(Response.json(note))
      .mockImplementationOnce(() => new Promise(resolve => { complete = resolve; }));
    vi.stubGlobal("fetch", fetch);
    await managerNotepadApi.editSession(note.id, "lease", true);
    await managerNotepadApi.editSession(note.id, "lease", false);
    await managerNotepadApi.releaseEditSession(note.id, "lease", true);
    await managerNotepadApi.getNote(note.id);
    expect(fetch.mock.calls.map(([, init]) => init?.method)).toEqual(["POST", "PUT", "DELETE", "GET"]);
    expect(fetch.mock.calls.map(([, init]) => new Headers(init?.headers).get("X-Notepad-Edit-Session"))).toEqual(["lease", "lease", "lease", null]);
    expect(fetch.mock.calls[2][1]?.keepalive).toBe(true);

    const invalidate = vi.spyOn(client, "invalidateQueries");
    const update = renderHook(useUpdateManagerNoteMutation, { wrapper });
    let write!: Promise<ManagerNote>;
    await act(async () => { write = update.result.current.mutateAsync({ noteId: note.id, payload, token: "lease" }); });
    expect(new Headers(fetch.mock.calls[4][1]?.headers).get("Authorization")).toBe("Bearer first-manager");
    setAuthAccessToken(null);
    client.clear();
    setAuthAccessToken("second-manager");
    client.setQueryData(currentKey, panel());
    await act(async () => {
      complete(Response.json(note));
      await expect(write).rejects.toThrow();
    });
    expect(client.getQueryState(currentKey).data).toEqual(panel());
    expect(invalidate).not.toHaveBeenCalled();
    expect(fetch).toHaveBeenCalledTimes(5);
  });
});
