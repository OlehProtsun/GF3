import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, test, vi } from "vitest";
import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
  useMutation,
  useQuery,
  useQueryClient,
} from "./react-query";

function QueryHarness({
  queryKey = ["items"],
  queryFn,
  enabled = true,
  cancelOnUnmount = false,
}: {
  queryKey?: readonly unknown[];
  queryFn: ({ signal }: { signal: AbortSignal }) => Promise<string>;
  enabled?: boolean;
  cancelOnUnmount?: boolean;
}) {
  const client = useQueryClient();
  const query = useQuery({
    queryKey,
    queryFn,
    enabled,
    cancelOnUnmount,
  });

  return (
    <section>
      <output aria-label="data">{query.data ?? ""}</output>
      <output aria-label="loading">{String(query.isLoading)}</output>
      <output aria-label="fetching">{String(query.isFetching)}</output>
      <output aria-label="error">{query.error instanceof Error ? query.error.message : ""}</output>
      <button type="button" onClick={() => client.invalidateQueries({ queryKey })}>
        invalidate
      </button>
      <button type="button" onClick={() => client.setQueryData(queryKey, "manual")}>
        set data
      </button>
    </section>
  );
}

function MutationHarness({
  mutationFn,
  onSuccess,
  onError,
}: {
  mutationFn: (value: string) => Promise<string>;
  onSuccess?: (data: string, value: string) => void;
  onError?: (error: unknown, value: string) => void;
}) {
  const mutation = useMutation({
    mutationKey: ["save"],
    mutationFn,
    onSuccess,
    onError,
  });

  return (
    <section>
      <output aria-label="pending">{String(mutation.isPending)}</output>
      <output aria-label="mutation-error">{mutation.error instanceof Error ? mutation.error.message : ""}</output>
      <button type="button" onClick={() => mutation.mutate("ok")}>
        save ok
      </button>
      <button type="button" onClick={() => mutation.mutate("bad")}>
        save bad
      </button>
    </section>
  );
}

function renderWithClient(client: QueryClient, element: React.ReactNode) {
  return render(
    <QueryClientProvider client={client}>
      {element}
    </QueryClientProvider>,
  );
}

describe("react-query shim", () => {
  test("QueryClient retries failed fetches, stores data, and notifies subscribers", async () => {
    const events: string[] = [];
    const client = new QueryClient({
      defaultOptions: {
        queries: { retry: 1 },
      },
    });
    client.subscribe(event => events.push(`${event.type}:${event.key}`));
    let attempts = 0;

    const result = await client.fetchQuery(["profile", 1], async () => {
      attempts += 1;
      if (attempts === 1) {
        throw new Error("temporary");
      }

      return "Ada";
    });

    expect(result).toBe("Ada");
    expect(attempts).toBe(2);
    expect(client.getQueryState<string>(["profile", 1]).data).toBe("Ada");
    expect(events).toEqual(['setData:["profile",1]']);

    client.invalidateQueries({ queryKey: ["profile"] });

    expect(client.getQueryState<string>(["profile", 1]).updatedAt).toBe(0);
    expect(events).toContain('invalidate:["profile",1]');
  });

  test("QueryClient dedupes in-flight fetches and aborts canceled queries", async () => {
    const client = new QueryClient();
    let resolveFetch: (value: string) => void = () => undefined;
    let observedSignal: AbortSignal | null = null;
    const queryFn = vi.fn(({ signal }: { signal: AbortSignal }) => {
      observedSignal = signal;
      return new Promise<string>(resolve => {
        resolveFetch = resolve;
      });
    });

    const first = client.fetchQuery(["slow"], queryFn);
    const second = client.fetchQuery(["slow"], queryFn);

    expect(first).toBe(second);
    expect(queryFn).toHaveBeenCalledTimes(1);

    resolveFetch("done");
    await expect(first).resolves.toBe("done");

    const cancelPromise = client.fetchQuery(["cancel"], ({ signal }) => new Promise<string>(() => {
      observedSignal = signal;
    }));
    client.cancelQuery(["cancel"]);

    expect(observedSignal?.aborted).toBe(true);
    await expect(Promise.race([
      cancelPromise.then(() => "resolved"),
      Promise.resolve("tick"),
    ])).resolves.toBe("tick");
  });

  test("useQuery loads, reacts to manual data, and refetches after invalidation", async () => {
    const user = userEvent.setup();
    const client = new QueryClient();
    let fetchCount = 0;

    renderWithClient(
      client,
      <QueryHarness queryFn={async () => {
        fetchCount += 1;
        return `server-${fetchCount}`;
      }} />,
    );

    expect(screen.getByLabelText("loading")).toHaveTextContent("true");

    await waitFor(() => {
      expect(screen.getByLabelText("data")).toHaveTextContent("server-1");
    });

    await user.click(screen.getByRole("button", { name: "set data" }));
    expect(screen.getByLabelText("data")).toHaveTextContent("manual");

    await user.click(screen.getByRole("button", { name: "invalidate" }));

    await waitFor(() => {
      expect(screen.getByLabelText("data")).toHaveTextContent("server-2");
    });
    expect(screen.getByLabelText("fetching")).toHaveTextContent("false");
  });

  test("useQuery reports errors through query cache and skips disabled queries", async () => {
    const onError = vi.fn();
    const queryFn = vi.fn(async () => {
      throw new Error("load failed");
    });
    const client = new QueryClient({
      queryCache: new QueryCache({ onError }),
    });
    const { rerender } = renderWithClient(client, <QueryHarness queryFn={queryFn} enabled={false} />);

    expect(screen.getByLabelText("loading")).toHaveTextContent("false");
    expect(queryFn).not.toHaveBeenCalled();

    rerender(
      <QueryClientProvider client={client}>
        <QueryHarness queryFn={queryFn} enabled />
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByLabelText("error")).toHaveTextContent("load failed");
    });
    expect(onError).toHaveBeenCalledTimes(1);
    expect(onError.mock.calls[0][1]).toEqual({ queryKey: ["items"] });
  });

  test("useQuery cancels in-flight work on unmount when requested", async () => {
    const client = new QueryClient();
    let signal: AbortSignal | null = null;

    const { unmount } = renderWithClient(
      client,
      <QueryHarness
        cancelOnUnmount
        queryFn={({ signal: nextSignal }) => {
          signal = nextSignal;
          return new Promise<string>(() => undefined);
        }}
      />,
    );

    await waitFor(() => {
      expect(signal).not.toBeNull();
    });

    unmount();

    expect(signal?.aborted).toBe(true);
  });

  test("useMutation calls success and error callbacks plus mutation cache errors", async () => {
    const user = userEvent.setup();
    const onSuccess = vi.fn();
    const onError = vi.fn();
    const cacheError = vi.fn();
    const client = new QueryClient({
      mutationCache: new MutationCache({ onError: cacheError }),
    });

    renderWithClient(
      client,
      <MutationHarness
        mutationFn={async value => {
          if (value === "bad") {
            throw new Error("save failed");
          }

          return "saved";
        }}
        onSuccess={onSuccess}
        onError={onError}
      />,
    );

    await user.click(screen.getByRole("button", { name: "save ok" }));
    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalledWith("saved", "ok");
    });
    expect(screen.getByLabelText("pending")).toHaveTextContent("false");

    await user.click(screen.getByRole("button", { name: "save bad" }));
    await waitFor(() => {
      expect(screen.getByLabelText("mutation-error")).toHaveTextContent("save failed");
    });
    expect(onError).toHaveBeenCalledTimes(1);
    expect(cacheError).toHaveBeenCalledTimes(1);
    expect(cacheError.mock.calls[0][3]).toEqual({ options: { mutationKey: ["save"] } });
  });
});
