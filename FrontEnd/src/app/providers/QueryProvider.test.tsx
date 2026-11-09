import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryProvider } from "./QueryProvider";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { graphEditorSaveMutationKey } from "@shared/api/graphSaveTraffic";
import { ApiError } from "@shared/api/httpClient";
import { pushErrorAlertFromError } from "@shared/ui/feedback/error-alerts/errorAlerts";
import { publicHolidaySettingsApi } from "@entities/public-holidays/api/publicHolidaySettingsApi";
import { regulationsApi } from "@entities/regulations/api/regulationsApi";
import { useAcceptRegulationMutation } from "@entities/regulations/api/queries";
import type { RegulationDocument } from "@entities/regulations/model/types";
import { usePublicHolidayVisibility } from "./PublicHolidayVisibilityProvider";

const auth = vi.hoisted(() => ({ session: { role: "employee", employeeId: 1, managerId: null, workspaceMode: undefined as string | undefined } }));
vi.mock("./AuthProvider", () => ({ useAuth: () => auth }));
vi.mock("@entities/public-holidays/api/publicHolidaySettingsApi", () => ({ publicHolidaySettingsApi: { get: vi.fn() } }));
vi.mock("@entities/regulations/api/regulationsApi", () => ({ regulationsApi: { pending: vi.fn(), accept: vi.fn() } }));
vi.mock("@shared/ui/feedback/error-alerts/errorAlerts", () => ({ pushErrorAlertFromError: vi.fn() }));

beforeEach(() => {
  auth.session.role = "employee"; auth.session.workspaceMode = undefined;
  vi.mocked(regulationsApi.pending).mockReset().mockResolvedValue([]);
  vi.mocked(publicHolidaySettingsApi.get).mockReset().mockResolvedValue({ showPublicHolidays: false });
});
afterEach(() => { cleanup(); auth.session.employeeId = 1; });

it("delays rate-limited reads and suppresses cached and repeated uncached toasts", async () => {
  vi.useFakeTimers();
  const log = vi.spyOn(console, "error").mockImplementation(() => {});
  vi.mocked(pushErrorAlertFromError).mockClear();
  const limited = new ApiError({ status: 429, message: "Limited", retryAfterMs: 7000 });
  const cached = vi.fn().mockResolvedValueOnce("cached").mockRejectedValue(limited);
  const uncached = vi.fn().mockRejectedValue(limited);
  const ordinary = new ApiError({ status: 500, message: "Failed" });
  const unrelated = vi.fn().mockRejectedValue(ordinary);
  function Harness() {
    const client = useQueryClient();
    const result = useQuery({ queryKey: ["cached"], queryFn: cached });
    useQuery({ queryKey: ["uncached-1"], queryFn: uncached });
    useQuery({ queryKey: ["uncached-2"], queryFn: uncached });
    useQuery({ queryKey: ["ordinary"], queryFn: unrelated });
    return <button onClick={() => client.invalidateQueries({ queryKey: ["cached"] })}>{result.data}</button>;
  }
  try {
    render(<QueryProvider><Harness /></QueryProvider>);
    await act(async () => {});
    fireEvent.click(screen.getByRole("button", { name: "cached" }));
    await act(async () => vi.advanceTimersByTimeAsync(6999));
    expect(uncached).toHaveBeenCalledTimes(2);
    expect(cached).toHaveBeenCalledTimes(2);
    await act(async () => vi.advanceTimersByTimeAsync(1));
    expect(uncached).toHaveBeenCalledTimes(4);
    expect(cached).toHaveBeenCalledTimes(3);
    expect(screen.getByRole("button", { name: "cached" })).toBeVisible();
    expect(vi.mocked(pushErrorAlertFromError).mock.calls.map(([error]) => error)).toEqual([ordinary, limited]);
  } finally { vi.useRealTimers(); log.mockRestore(); }
});

function Data({ load }: { load: (context: { signal: AbortSignal }) => Promise<string> }) {
  const query = useQuery({ queryKey: ["employee", "private"], queryFn: load, staleTime: 60_000 });
  return <div data-testid="private-data">{query.data ?? "loading"}</div>;
}

it("lets the editor report a failed save once while other mutation errors still notify", async () => {
  const error = new ApiError({ status: 429, message: "Limited", retryAfterMs: 1000 });
  const report = vi.fn();
  const failure = vi.fn().mockRejectedValue(error);
  const consoleError = vi.spyOn(console, "error").mockImplementation(() => {});
  vi.mocked(pushErrorAlertFromError).mockClear();
  function Save() {
    const editor = useMutation({ mutationKey: graphEditorSaveMutationKey, mutationFn: failure });
    const ordinary = useMutation({ mutationFn: failure });
    return <>
      <button onClick={() => editor.mutate(undefined, { onError: report })}>Save editor</button>
      <button onClick={() => ordinary.mutate(undefined)}>Save another form</button>
    </>;
  }
  try {
    render(<QueryProvider><Save /></QueryProvider>);
    fireEvent.click(screen.getByRole("button", { name: "Save editor" }));
    await waitFor(() => expect(report).toHaveBeenCalledExactlyOnceWith(error));
    expect(pushErrorAlertFromError).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Save another form" }));
    await waitFor(() => expect(pushErrorAlertFromError).toHaveBeenCalledOnce());
    expect(failure).toHaveBeenCalledTimes(2);
  } finally {
    consoleError.mockRestore();
  }
});

describe("account query isolation", () => {
  it("does not load holiday settings while the manager is choosing a workspace", async () => {
    auth.session.role = "manager"; auth.session.workspaceMode = "choose";
    const view = render(<QueryProvider><div>Choose workspace</div></QueryProvider>);
    await act(async () => {});
    expect(publicHolidaySettingsApi.get).not.toHaveBeenCalled();
    expect(regulationsApi.pending).not.toHaveBeenCalled();
    auth.session.workspaceMode = "pc";
    view.rerender(<QueryProvider><div>PC workspace</div></QueryProvider>);
    await waitFor(() => expect(publicHolidaySettingsApi.get).toHaveBeenCalledOnce());
  });

  it("waits for required document acceptance, then loads holiday settings immediately", async () => {
    vi.mocked(regulationsApi.pending).mockResolvedValue([{ id: 9 } as RegulationDocument]);
    vi.mocked(regulationsApi.accept).mockResolvedValue({} as Awaited<ReturnType<typeof regulationsApi.accept>>);
    function Acceptance() {
      const mutation = useAcceptRegulationMutation();
      const visible = usePublicHolidayVisibility();
      return <><button onClick={() => mutation.mutate(9)}>Accept documents</button><output>{String(visible)}</output></>;
    }
    render(<QueryProvider><Acceptance /></QueryProvider>);
    await waitFor(() => expect(regulationsApi.pending).toHaveBeenCalledOnce());
    expect(publicHolidaySettingsApi.get).not.toHaveBeenCalled();
    expect(screen.getByText("true")).toBeVisible();
    vi.mocked(regulationsApi.pending).mockResolvedValue([]);
    fireEvent.click(screen.getByRole("button", { name: "Accept documents" }));
    await screen.findByText("false");
    expect(publicHolidaySettingsApi.get).toHaveBeenCalledOnce();
  });
  it("never exposes a fresh cached response after switching accounts", async () => {
    const first = vi.fn().mockResolvedValue("private A");
    const second = vi.fn().mockResolvedValue("private B");
    const view = render(<QueryProvider><Data load={first} /></QueryProvider>);
    await screen.findByText("private A");
    auth.session.employeeId = 2;
    view.rerender(<QueryProvider><Data load={second} /></QueryProvider>);
    expect(screen.queryByText("private A")).toBeNull();
    await screen.findByText("private B");
    expect(second).toHaveBeenCalledOnce();
  });

  it("aborts old requests and ignores a late response from the previous account", async () => {
    let resolveFirst!: (value: string) => void;
    let oldSignal!: AbortSignal;
    const first = ({ signal }: { signal: AbortSignal }) => {
      oldSignal = signal;
      return new Promise<string>(resolve => { resolveFirst = resolve; });
    };
    const view = render(<QueryProvider><Data load={first} /></QueryProvider>);
    await waitFor(() => expect(oldSignal).toBeDefined());
    auth.session.employeeId = 2;
    view.rerender(<QueryProvider><Data load={() => Promise.resolve("private B")} /></QueryProvider>);
    await screen.findByText("private B");
    expect(oldSignal.aborted).toBe(true);
    await act(async () => { resolveFirst("private A"); });
    expect(screen.getByTestId("private-data")).toHaveTextContent("private B");
  });
});
