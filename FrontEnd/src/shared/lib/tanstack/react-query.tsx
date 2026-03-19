import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import type { PropsWithChildren } from "react";

type QueryKey = readonly unknown[];
type QueryFilters = { queryKey?: QueryKey };

type QueryOptions<TData> = {
  queryKey: QueryKey;
  queryFn: (context: { signal: AbortSignal }) => Promise<TData>;
  enabled?: boolean;
  cancelOnUnmount?: boolean;
};

type MutationOptions<TData, TVariables> = {
  mutationFn: (variables: TVariables) => Promise<TData>;
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
  mutationKey?: QueryKey;
};

type QueryClientOptions = {
  defaultOptions?: {
    queries?: { staleTime?: number; retry?: number; refetchOnWindowFocus?: boolean };
    mutations?: { retry?: number };
  };
  queryCache?: QueryCache;
  mutationCache?: MutationCache;
};

export class QueryCache {
  public readonly config;
  constructor(config: { onError?: (error: unknown, query: { queryKey: QueryKey }) => void } = {}) {
    this.config = config;
  }
}

export class MutationCache {
  public readonly config;
  constructor(
    config: {
      onError?: (error: unknown, variables: unknown, context: unknown, mutation: { options: { mutationKey?: QueryKey } }) => void;
    } = {},
  ) {
    this.config = config;
  }
}

export class QueryClient {
  private listeners: Set<(key: string) => void> = new Set();
  private bumps: Map<string, number> = new Map();
  public readonly defaults;
  public readonly queryCache;
  public readonly mutationCache;

  constructor(options: QueryClientOptions = {}) {
    this.defaults = options.defaultOptions;
    this.queryCache = options.queryCache;
    this.mutationCache = options.mutationCache;
  }

  subscribe(listener: (key: string) => void): () => void {
    this.listeners.add(listener);
    return () => {
      this.listeners.delete(listener);
    };
  }

  getBump(key: string): number {
    return this.bumps.get(key) ?? 0;
  }

  invalidateQueries(filters: QueryFilters): void {
    const key = JSON.stringify(filters.queryKey ?? []);
    this.bumps.set(key, this.getBump(key) + 1);
    this.listeners.forEach((listener) => listener(key));
  }
}

const QueryClientContext = createContext<QueryClient | null>(null);

export function QueryClientProvider({ client, children }: PropsWithChildren<{ client: QueryClient }>) {
  return <QueryClientContext.Provider value={client}>{children}</QueryClientContext.Provider>;
}

export function useQueryClient(): QueryClient {
  const client = useContext(QueryClientContext);
  if (!client) throw new Error("QueryClientProvider is missing");
  return client;
}

export function useQuery<TData>(options: QueryOptions<TData>) {
  const client = useQueryClient();
  const keyString = useMemo(() => JSON.stringify(options.queryKey), [options.queryKey]);
  const queryFnRef = useRef(options.queryFn);
  const queryKeyRef = useRef(options.queryKey);
  const [data, setData] = useState<TData | undefined>(undefined);
  const [error, setError] = useState<unknown>(null);
  const [isLoading, setIsLoading] = useState(options.enabled !== false);
  const [bump, setBump] = useState(client.getBump(keyString));

  queryFnRef.current = options.queryFn;
  queryKeyRef.current = options.queryKey;

  useEffect(() => {
    return client.subscribe((updatedKey) => {
      if (updatedKey === keyString) {
        setBump(client.getBump(updatedKey));
      }
    });
  }, [client, keyString]);

  // We refetch only when query key / invalidation changes.
  // Before this fix, inline queryFn identity changed each render and caused a refetch loop + UI freeze.
  useEffect(() => {
    if (options.enabled === false) {
      setIsLoading(false);
      return;
    }

    const controller = new AbortController();
    let isActive = true;
    setIsLoading(true);
    setError(null);

    queryFnRef
      .current({ signal: controller.signal })
      .then((value) => {
        if (!isActive || controller.signal.aborted) {
          return;
        }

        setData(value);
        setError(null);
      })
      .catch((reason) => {
        if (!isActive || controller.signal.aborted) {
          return;
        }

        setError(reason);
        client.queryCache?.config.onError?.(reason, { queryKey: queryKeyRef.current });
      })
      .finally(() => {
        if (!isActive || controller.signal.aborted) {
          return;
        }

        setIsLoading(false);
      });

    return () => {
      isActive = false;
      if (options.cancelOnUnmount !== false) {
        controller.abort();
      }
    };
  }, [bump, keyString, options.cancelOnUnmount, options.enabled, client]);

  return {
    data,
    error,
    isLoading,
    isFetching: isLoading,
    isError: Boolean(error),
  };
}

export function useMutation<TData, TVariables>(options: MutationOptions<TData, TVariables>) {
  const client = useQueryClient();
  const [isPending, setIsPending] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const mutate = useCallback(
    (variables: TVariables, callbacks?: { onSuccess?: (data: TData) => void; onError?: (error: unknown) => void }) => {
      setIsPending(true);
      setError(null);

      options
        .mutationFn(variables)
        .then((data) => {
          options.onSuccess?.(data, variables);
          callbacks?.onSuccess?.(data);
        })
        .catch((reason) => {
          setError(reason);
          options.onError?.(reason, variables);
          callbacks?.onError?.(reason);
          client.mutationCache?.config.onError?.(reason, variables, null, { options: { mutationKey: options.mutationKey } });
        })
        .finally(() => setIsPending(false));
    },
    [client, options],
  );

  return { mutate, isPending, error };
}

