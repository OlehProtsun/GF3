import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryProvider } from "./QueryProvider";
import { useQuery } from "@tanstack/react-query";
import { publicHolidaySettingsApi } from "@entities/public-holidays/api/publicHolidaySettingsApi";
import { regulationsApi } from "@entities/regulations/api/regulationsApi";
import { useAcceptRegulationMutation } from "@entities/regulations/api/queries";
import type { RegulationDocument } from "@entities/regulations/model/types";
import { usePublicHolidayVisibility } from "./PublicHolidayVisibilityProvider";

const auth = vi.hoisted(() => ({ session: { role: "employee", employeeId: 1, managerId: null, workspaceMode: undefined as string | undefined } }));
vi.mock("./AuthProvider", () => ({ useAuth: () => auth }));
vi.mock("@entities/public-holidays/api/publicHolidaySettingsApi", () => ({ publicHolidaySettingsApi: { get: vi.fn() } }));
vi.mock("@entities/regulations/api/regulationsApi", () => ({ regulationsApi: { pending: vi.fn(), accept: vi.fn() } }));

beforeEach(() => {
  auth.session.role = "employee"; auth.session.workspaceMode = undefined;
  vi.mocked(regulationsApi.pending).mockReset().mockResolvedValue([]);
  vi.mocked(publicHolidaySettingsApi.get).mockReset().mockResolvedValue({ showPublicHolidays: false });
});
afterEach(() => { cleanup(); auth.session.employeeId = 1; });

function Data({ load }: { load: (context: { signal: AbortSignal }) => Promise<string> }) {
  const query = useQuery({ queryKey: ["employee", "private"], queryFn: load, staleTime: 60_000 });
  return <div data-testid="private-data">{query.data ?? "loading"}</div>;
}

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
