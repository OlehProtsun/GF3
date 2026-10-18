import { afterEach, describe, expect, it, vi } from "vitest";
import { fetchLatestBuildId, isNewBuild } from "./appUpdate";

afterEach(() => vi.unstubAllGlobals());
describe("release metadata", () => {
  it.each([["a", "a", false], ["a", "b", true], ["a", null, false], ["", "b", false], ["a", " ", false]])("compares %s with %s", (current, remote, expected) => {
    expect(isNewBuild(current as string, remote as string | null)).toBe(expected);
  });
  it("uses unauthenticated same-origin no-store fetch", async () => {
    const fetch = vi.fn().mockResolvedValue(new Response('{"buildId":"release-2"}', { headers: { "Content-Type": "application/json; charset=utf-8" } }));
    vi.stubGlobal("fetch", fetch);
    const signal = new AbortController().signal;
    expect(await fetchLatestBuildId(signal)).toBe("release-2");
    expect(fetch).toHaveBeenCalledWith("/version.json", { cache: "no-store", redirect: "error", headers: { Accept: "application/json" }, signal });
  });
  it.each([
    [404, "application/json", '{}'], [500, "application/json", '{}'],
    [200, "text/html", '{"buildId":"other"}'], [200, "application/json", 'null'],
    [200, "application/json", '{"buildId":3}'], [200, "application/json", '{"buildId":" "}'],
    [200, "application/json", '{}'], [200, "application/json", 'invalid'],
  ])("ignores status=%s type=%s body=%s", async (status, type, body) => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(body, { status, headers: { "Content-Type": type } })));
    expect(await fetchLatestBuildId()).toBeNull();
  });
  it.each([new Error("offline"), new DOMException("aborted", "AbortError")])("ignores network and cancellation failures", async error => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(error));
    expect(await fetchLatestBuildId()).toBeNull();
  });
});
