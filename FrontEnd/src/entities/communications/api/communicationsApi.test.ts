import { beforeEach, expect, test, vi } from "vitest";
import { communicationsApi } from "./communicationsApi";

const request = vi.hoisted(() => vi.fn());
vi.mock("@shared/api/httpClient", () => ({ request, requestFile: vi.fn() }));

const text = { title: " Fixture ", body: " Body ", visibleFromUtc: "2026-11-23T10:00:00Z", deadlineAtUtc: "2026-11-23T11:00:00Z" };
beforeEach(() => request.mockReset());

test("uses browser FormData boundaries and keeps order, retained keys and replacement files", () => {
  const file = new File(["fixture"], "photo.png", { type: "image/png" });
  const retained = { key: "gif-key", fileName: "animation.gif", size: 50, contentType: "image/gif" };
  communicationsApi.create({ ...text, blocks: [{ id: "text", type: "text", text: " Body " }, { id: "photo", type: "photo", file }] });
  communicationsApi.update({ id: 19, ...text, blocks: [{ id: "gif", type: "gif", media: retained }, { id: "photo", type: "photo", file }] });
  expect(request.mock.calls.map(([path, init]) => [path, init.method])).toEqual([
    ["communications/with-gif", "POST"], ["communications/19/with-gif", "PUT"],
  ]);
  for (const [, options] of request.mock.calls) {
    expect(options).not.toHaveProperty("headers");
    expect(options.body).toBeInstanceOf(FormData);
    expect(options.body.get("Block-photo")).toBe(file);
  }
  expect(JSON.parse(request.mock.calls[0][1].body.get("Blocks"))).toEqual([
    { id: "text", type: "text", text: "Body" }, { id: "photo", type: "photo", mediaKey: null },
  ]);
  expect(JSON.parse(request.mock.calls[1][1].body.get("Blocks"))).toEqual([
    { id: "gif", type: "gif", mediaKey: "gif-key" }, { id: "photo", type: "photo", mediaKey: null },
  ]);
  expect(request.mock.calls[1][1].body.get("Block-gif")).toBeNull();
});

test("keeps legacy JSON routes for plain messages", () => {
  communicationsApi.create(text);
  communicationsApi.update({ id: 19, ...text, removeGif: true });
  expect(request).toHaveBeenNthCalledWith(1, "communications", {
    method: "POST", body: { ...text, title: "Fixture", body: "Body" },
  });
  expect(request).toHaveBeenNthCalledWith(2, "communications/19", {
    method: "PUT", body: { ...text, title: "Fixture", body: "Body", removeGif: true },
  });
});
