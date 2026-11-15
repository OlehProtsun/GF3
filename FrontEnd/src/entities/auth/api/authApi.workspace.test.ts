import { afterEach, expect, test, vi } from "vitest";
import { authApi } from "./authApi";
afterEach(() => vi.restoreAllMocks());
test.each(["choose", "pc", "phone", undefined])("session maps signed mode %s and legacy", async mode => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ role: "manager", userName: "manager", displayName: "Manager", workspaceMode: mode }));
  expect((await authApi.session()).workspaceMode).toBe(mode ?? "pc");
});
test("unknown manager mode fails safely and employee never gets manager mode", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json({ role: "manager", workspaceMode: "unsafe" })).mockResolvedValueOnce(Response.json({ role: "employee", workspaceMode: "phone" }));
  await expect(authApi.session()).rejects.toThrow("Invalid manager workspace mode"); expect((await authApi.session()).workspaceMode).toBeNull();
});
