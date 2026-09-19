import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryProvider } from "./QueryProvider";
import { useQuery } from "@tanstack/react-query";

const auth = vi.hoisted(() => ({ session: { role: "employee", employeeId: 1, managerId: null } }));
vi.mock("./AuthProvider", () => ({ useAuth: () => auth }));
afterEach(() => { cleanup(); auth.session.employeeId = 1; });

function Data({ load }: { load: (context: { signal: AbortSignal }) => Promise<string> }) {
  const query = useQuery({ queryKey: ["employee", "private"], queryFn: load, staleTime: 60_000 });
  return <div data-testid="private-data">{query.data ?? "loading"}</div>;
}

describe("account query isolation", () => {
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