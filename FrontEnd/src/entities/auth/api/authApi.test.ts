import { beforeEach, describe, expect, it, vi } from "vitest";
import { authApi } from "./authApi";

const request = vi.hoisted(() => vi.fn());
vi.mock("@shared/api/httpClient", () => ({ request }));

describe("auth session privilege mapping", () => {
  beforeEach(() => request.mockReset());

  it.each([true, false, undefined, null, "true", 1])("maps login and restoration privilege %s with strict boolean checking", async privilege => {
    const session = { role: "manager", userName: "manager", displayName: "Synthetic manager", isSystemManager: privilege };
    request.mockResolvedValueOnce({ accessToken: "synthetic-token", expiresAtUtc: "2099-01-01T00:00:00Z", session });
    const login = await authApi.login({ username: "manager", password: "123456" });
    expect(login.session.isSystemManager).toBe(privilege === true);
    request.mockResolvedValueOnce(session);
    expect((await authApi.session()).isSystemManager).toBe(privilege === true);
  });
});
