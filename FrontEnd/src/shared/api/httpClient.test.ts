import { setLanguage, t } from "@shared/i18n";
import { afterEach, describe, expect, test, vi } from "vitest";
import {
  ApiError,
  RequestCanceledError,
  buildApiUrl,
  getAuthAccessToken,
  getErrorMessage,
  request,
  requestFile,
  setAuthAccessToken,
  subscribeUnauthorized,
} from "./httpClient";

function mockFetch(response: Response | Promise<Response>) {
  const fetchMock = vi.fn().mockResolvedValue(response);
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

function getLastRequest(fetchMock: ReturnType<typeof mockFetch>) {
  const [url, init] = fetchMock.mock.calls.at(-1) ?? [];
  return {
    url: String(url),
    init: init as RequestInit,
    headers: (init as RequestInit).headers as Headers,
  };
}

afterEach(() => {
  setAuthAccessToken(null);
  vi.unstubAllGlobals();
});

describe("httpClient", () => {
  test("retains server retry guidance and trace ID for a localized 429", async () => {
    mockFetch(Response.json({ traceId: "trace", title: "Private upstream error" }, {
      status: 429, headers: { "Retry-After": "7" },
    }));
    await expect(request("profile")).rejects.toMatchObject({
      status: 429, retryAfterMs: 7000, traceId: "trace", message: t("Too many requests. Please try again later."),
    });
  });

  test.each([
    ["Mon, 09 Nov 2026 12:00:05 GMT", 5000], ["Mon, 09 Nov 2026 12:00:00 GMT", 3000],
    ["Monday, 09-Nov-26 12:00:05 GMT", 5000], ["Mon Nov  9 12:00:05 2026", 5000], ["Nov 9 2099", 3000],
    ["0", 1000], ["99999999999999999999999", 60_000], ["", 3000], ["-3", 3000], ["1.2", 3000], ["unknown", 3000], [null, 3000],
  ])("reads Retry-After %s as %s milliseconds from a proxy error", async (header, retryAfterMs) => {
    const now = vi.spyOn(Date, "now").mockReturnValue(Date.parse("2026-11-09T12:00:00Z"));
    try {
      mockFetch(new Response("", { status: 429, headers: header === null ? {} : { "Retry-After": header } }));
      await expect(requestFile("download")).rejects.toMatchObject({ status: 429, retryAfterMs });
    } finally { now.mockRestore(); }
  });

  test.each(["POST", "PUT", "DELETE"] as const)("does not retry a rate-limited %s", async method => {
    const fetchMock = mockFetch(new Response("", { status: 429, headers: { "Retry-After": "1" } }));
    await expect(request("style", { method })).rejects.toMatchObject({ status: 429, retryAfterMs: 1000 });
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  test.each(["en", "pl", "uk"] as const)("normalizes JSON, HTML and empty 429 responses in %s without retries or logout", async language => {
    setLanguage(language);
    const revoked = vi.fn(); const unsubscribe = subscribeUnauthorized(revoked);
    setAuthAccessToken("current-token");
    try {
      for (const body of ["json", "html", "empty"]) {
        const response = body === "json" ? Response.json({ title: "Too many attempts", errors: { username: ["private text"] } }, { status: 429 })
          : new Response(body === "html" ? "<h1>nginx 429</h1>" : "", { status: 429, headers: { "content-type": "text/html" } });
        const fetchMock = mockFetch(response);
        await expect(request("profile")).rejects.toMatchObject({ status: 429, message: t("Too many requests. Please try again later.") });
        expect(fetchMock).toHaveBeenCalledOnce();
      }
      expect(revoked).not.toHaveBeenCalled();
      expect(getAuthAccessToken()).toBe("current-token");
    } finally { unsubscribe(); setLanguage("en"); }
  });

  test.each([200, 401])("discards a late JSON body (%s) without revoking the new session", async (status) => {
    let finish!: (value: unknown) => void;
    const response = Response.json({}, { status });
    const read = vi.spyOn(response, "json").mockImplementation(() => new Promise(resolve => { finish = resolve; }));
    mockFetch(response);
    const revoked = vi.fn();
    const unsubscribe = subscribeUnauthorized(revoked);
    try {
      setAuthAccessToken("old-token");
      const pending = request("profile");
      const rejected = expect(pending).rejects.toBeInstanceOf(RequestCanceledError);
      await vi.waitFor(() => expect(read).toHaveBeenCalled());
      setAuthAccessToken(null);
      setAuthAccessToken("old-token");
      finish({ account: "previous-session" });
      await rejected;
      expect(revoked).not.toHaveBeenCalled();
    } finally { unsubscribe(); }
  });

  test("discards a download body completed after switching accounts", async () => {
    let finish!: (value: Blob) => void;
    const response = new Response("report");
    const read = vi.spyOn(response, "blob").mockImplementation(() => new Promise(resolve => { finish = resolve; }));
    mockFetch(response);
    setAuthAccessToken("old-token");
    const pending = requestFile("export");
    const rejected = expect(pending).rejects.toBeInstanceOf(RequestCanceledError);
    await vi.waitFor(() => expect(read).toHaveBeenCalled());
    setAuthAccessToken("new-token");
    finish(new Blob(["old report"]));
    await rejected;
  });

  test("only signals 401 for requests authenticated with the current session", async () => {
    const revoked = vi.fn();
    const unsubscribe = subscribeUnauthorized(revoked);
    setAuthAccessToken("current-token");
    try {
      for (const options of [{ anonymous: true }, { headers: { Authorization: "Bearer other" } }, {}]) {
        mockFetch(Response.json({ detail: "Unauthorized" }, { status: 401 }));
        await expect(request("profile", options)).rejects.toBeInstanceOf(ApiError);
      }
      expect(revoked).toHaveBeenCalledTimes(1);
    } finally { unsubscribe(); }
  });

  test.each([200, 401])("discards a late %s response from the previous session", async (status) => {
    let finish!: (response: Response) => void;
    mockFetch(new Promise<Response>(resolve => { finish = resolve; }));
    setAuthAccessToken("old-token");
    const pending = request("employees");
    const rejected = expect(pending).rejects.toBeInstanceOf(RequestCanceledError);
    setAuthAccessToken("new-token");
    finish(Response.json({ detail: "old session response" }, { status }));
    await rejected;
    expect(getAuthAccessToken()).toBe("new-token");
  });

  test("discards a download that finishes after logout", async () => {
    let finish!: (response: Response) => void;
    mockFetch(new Promise<Response>(resolve => { finish = resolve; }));
    setAuthAccessToken("old-token");
    const pending = requestFile("export");
    const rejected = expect(pending).rejects.toBeInstanceOf(RequestCanceledError);
    setAuthAccessToken(null);
    finish(new Response("old account data"));
    await rejected;
  });

  test("buildApiUrl appends query values and skips nullish values", () => {
    expect(buildApiUrl("/employees", { search: "Ann Smith", page: 2, active: true, empty: null, missing: undefined }))
      .toBe("/api/employees?search=Ann+Smith&page=2&active=true");
    expect(buildApiUrl("https://example.test/users?existing=1", { next: "yes" }))
      .toBe("https://example.test/users?existing=1&next=yes");
  });

  test("request sends json, accept, query, and bearer headers", async () => {
    setAuthAccessToken("  token-123  ");
    const fetchMock = mockFetch(Response.json({ ok: true }));

    const result = await request<{ ok: boolean }>("/employees", {
      method: "POST",
      query: { includeInactive: false },
      body: { name: "Alex" },
    });
    const sent = getLastRequest(fetchMock);

    expect(result).toEqual({ ok: true });
    expect(sent.url).toBe("/api/employees?includeInactive=false");
    expect(sent.init.method).toBe("POST");
    expect(sent.init.body).toBe(JSON.stringify({ name: "Alex" }));
    expect(sent.headers.get("Accept")).toBe("application/json");
    expect(sent.headers.get("Content-Type")).toBe("application/json");
    expect(sent.headers.get("Authorization")).toBe("Bearer token-123");
    expect(getAuthAccessToken()).toBe("token-123");
  });

  test("request keeps caller authorization and preserves string content type", async () => {
    const fetchMock = mockFetch(new Response("saved", { headers: { "content-type": "text/plain" } }));
    setAuthAccessToken("token-123");

    await request<string>("/notes", {
      method: "PATCH",
      body: "plain text",
      headers: {
        Authorization: "Bearer override",
        "Content-Type": "text/custom",
      },
    });
    const sent = getLastRequest(fetchMock);

    expect(sent.init.body).toBe("plain text");
    expect(sent.headers.get("Authorization")).toBe("Bearer override");
    expect(sent.headers.get("Content-Type")).toBe("text/custom");
  });

  test("request removes content type for FormData bodies", async () => {
    const fetchMock = mockFetch(Response.json({ ok: true }));
    const formData = new FormData();
    formData.set("file", "content");

    await request("/imports", {
      method: "POST",
      body: formData,
      headers: { "Content-Type": "application/json" },
    });
    const sent = getLastRequest(fetchMock);

    expect(sent.init.body).toBe(formData);
    expect(sent.headers.has("Content-Type")).toBe(false);
  });

  test("request returns undefined for 204 responses", async () => {
    mockFetch(new Response(null, { status: 204 }));

    await expect(request<void>("/empty")).resolves.toBeUndefined();
  });

  test("request throws ApiError with problem details and normalized validation errors", async () => {
    mockFetch(Response.json(
      {
        title: "Validation failed",
        traceId: "trace-1",
        errors: {
          UserName: ["Username is required.", ""],
          Password: ["Password is too short."],
        },
      },
      { status: 400 },
    ));

    await expect(request("/login")).rejects.toMatchObject({
      name: "ApiError",
      status: 400,
      message: "Username is required.",
      traceId: "trace-1",
      validationErrors: {
        UserName: ["Username is required."],
        Password: ["Password is too short."],
      },
    });
  });

  test("getErrorMessage prefers ApiError validation messages and falls back safely", () => {
    const apiError = new ApiError({
      status: 422,
      message: "Outer message",
      validationErrors: { field: ["Inner message"] },
    });

    expect(getErrorMessage(apiError)).toBe("Inner message");
    expect(getErrorMessage(new Error("Direct error"))).toBe("Direct error");
    expect(getErrorMessage(null, "Fallback")).toBe("Fallback");
  });

  test("request converts aborted fetches into RequestCanceledError", async () => {
    const controller = new AbortController();
    controller.abort();
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(Object.assign(new Error("aborted"), { name: "AbortError" })));

    await expect(request("/slow", { signal: controller.signal })).rejects.toBeInstanceOf(RequestCanceledError);
  });

  test("requestFile returns blob metadata and decodes content disposition filenames", async () => {
    mockFetch(new Response("report-data", {
      headers: {
        "content-type": "text/csv",
        "content-disposition": "attachment; filename*=UTF-8''May%20report.csv",
      },
    }));

    const file = await requestFile("/exports/report");

    expect(file.fileName).toBe("May report.csv");
    expect(file.contentType).toBe("text/csv");
    await expect(file.blob.text()).resolves.toBe("report-data");
  });

  test("requestFile returns an empty file model for 204 responses", async () => {
    mockFetch(new Response(null, { status: 204, headers: { "content-type": "application/octet-stream" } }));

    const file = await requestFile("/exports/empty");

    expect(file.fileName).toBeNull();
    expect(file.contentType).toBe("application/octet-stream");
    expect(file.blob.size).toBe(0);
  });
});
