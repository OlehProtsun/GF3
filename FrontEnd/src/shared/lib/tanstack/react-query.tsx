/* eslint-disable react-refresh/only-export-components */
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  useSyncExternalStore,
} from "react";
import type { PropsWithChildren } from "react";
import { ApiError, getAuthGeneration, isRequestCanceledError, RequestCanceledError } from "@shared/api/httpClient";
import { waitForReadRetry } from "@shared/api/retry429Read";

type QueryKey = readonly unknown[];
type QueryFilters = {
  queryKey?: QueryKey;
  exact?: boolean;
  refetchType?: "none";
  predicate?: (query: { queryKey: QueryKey }) => boolean;
};
type QueryEvent =
  | { type: "invalidate"; key: string }
  | { type: "setData"; key: string; data: unknown };

type QueryOptions<TData> = {
  queryKey: QueryKey;
  queryFn: (context: { signal: AbortSignal }) => Promise<TData>;
  enabled?: boolean;
  cancelOnUnmount?: boolean;
  staleTime?: number;
  retry?: number;
};

type MutationOptions<TData, TVariables> = {
  mutationFn: (variables: TVariables) => Promise<TData>;
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
  mutationKey?: QueryKey;
  meta?: Record<string, unknown>;
};

type QueryClientOptions = {
  defaultOptions?: {
    queries?: { staleTime?: number; retry?: number; retryDelay?: (attempt: number, error: unknown) => number; refetchOnWindowFocus?: boolean };
    mutations?: { retry?: number };
  };
  queryCache?: QueryCache;
  mutationCache?: MutationCache;
};

type CacheRecord = {
  queryKey: QueryKey;
  data?: unknown;
  updatedAt: number;
  promise?: Promise<unknown>;
  controller?: AbortController;
  invalidatedWhileFetching?: boolean;
  staleOnSettle?: boolean;
  dataRevision?: number;
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
      onError?: (error: unknown, variables: unknown, context: unknown, mutation: {
        options: { mutationKey?: QueryKey };
        meta?: Record<string, unknown>;
      }) => void;
    } = {},
  ) {
    this.config = config;
  }
}

function hashQueryKey(queryKey: QueryKey): string {
  return JSON.stringify(queryKey);
}

function isQueryKeyPrefix(prefix: QueryKey | undefined, queryKey: QueryKey): boolean {
  if (!prefix || prefix.length === 0) {
    return true;
  }

  if (prefix.length > queryKey.length) {
    return false;
  }

  return prefix.every((segment, index) => Object.is(segment, queryKey[index]));
}

async function runWithRetry<TData>(
  queryFn: () => Promise<TData>,
  retry: number,
  signal: AbortSignal,
  retryDelay?: (attempt: number, error: unknown) => number,
): Promise<TData> {
  let attempt = 0;
  const generation = getAuthGeneration();
  const ensureCurrent = () => {
    if (signal.aborted || getAuthGeneration() !== generation) throw new RequestCanceledError();
  };

  while (true) {
    ensureCurrent();
    try {
      const data = await queryFn();
      ensureCurrent();
      return data;
    } catch (error) {
      if (isRequestCanceledError(error)) throw error;
      ensureCurrent();
      if ((!retryDelay && error instanceof ApiError && error.status === 429) || attempt >= retry) {
        throw error;
      }

      const delay = retryDelay?.(attempt, error) ?? 0;
      if (delay > 0) await waitForReadRetry(delay, signal);
      attempt += 1;
    }
  }
}

export class QueryClient {
  private mutationCount = 0;
  private mutationListeners = new Set<() => void>();
  getMutationCount = () => this.mutationCount;
  subscribeMutations = (listener: () => void) => {
    this.mutationListeners.add(listener);
    return () => { this.mutationListeners.delete(listener); };
  };
  changeMutationCount(delta: number) {
    this.mutationCount += delta;
    this.mutationListeners.forEach(listener => listener());
  }
  private listeners: Set<(event: QueryEvent) => void> = new Set();
  private bumps: Map<string, number> = new Map();
  private records: Map<string, CacheRecord> = new Map();
  private observers = new Map<string, number>();
  public readonly defaults;
  public readonly queryCache;
  public readonly mutationCache;

  constructor(options: QueryClientOptions = {}) {
    this.defaults = options.defaultOptions;
    this.queryCache = options.queryCache;
    this.mutationCache = options.mutationCache;
  }

  private ensureRecord(queryKey: QueryKey): CacheRecord {
    const key = hashQueryKey(queryKey);
    const existing = this.records.get(key);

    if (existing) {
      if (existing.queryKey !== queryKey) {
        existing.queryKey = queryKey;
      }

      return existing;
    }

    const created: CacheRecord = {
      queryKey,
      updatedAt: 0,
    };

    this.records.set(key, created);
    return created;
  }

  subscribe(listener: (event: QueryEvent) => void): () => void {
    this.listeners.add(listener);
    return () => {
      this.listeners.delete(listener);
    };
  }

  observeQuery(queryKey: QueryKey, cancelOnRelease = false): () => void {
    const key = hashQueryKey(queryKey);
    this.observers.set(key, (this.observers.get(key) ?? 0) + 1);
    return () => {
      const remaining = (this.observers.get(key) ?? 1) - 1;
      if (remaining) {
        this.observers.set(key, remaining);
        return;
      }
      this.observers.delete(key);
      if (cancelOnRelease) this.cancelQuery(queryKey);
    };
  }

  getBump(key: string): number {
    return this.bumps.get(key) ?? 0;
  }

  getQueryState<TData>(queryKey: QueryKey): { data: TData | undefined; updatedAt: number } {
    const record = this.ensureRecord(queryKey);

    return {
      data: record.data as TData | undefined,
      updatedAt: record.updatedAt,
    };
  }

  invalidateQueries(filters: QueryFilters): void {
    const exactKey = filters.exact && filters.queryKey ? hashQueryKey(filters.queryKey) : undefined;
    const matchingKeys = [...this.records.entries()]
      .filter(([key, record]) => (exactKey === undefined ? isQueryKeyPrefix(filters.queryKey, record.queryKey) : key === exactKey) &&
        (!filters.predicate || filters.predicate(record)))
      .map(([key]) => key);

    if (matchingKeys.length === 0 && filters.queryKey && !filters.predicate) {
      const fallbackKey = hashQueryKey(filters.queryKey);
      this.ensureRecord(filters.queryKey);
      matchingKeys.push(fallbackKey);
    }

    matchingKeys.forEach((key) => {
      const record = this.records.get(key);
      if (record) {
        record.updatedAt = 0;
        if (record.promise) {
          if (filters.refetchType === "none") record.staleOnSettle = true;
          else record.invalidatedWhileFetching = true;
        }
      }

      if (filters.refetchType === "none") return;
      this.bumps.set(key, this.getBump(key) + 1);
      this.listeners.forEach((listener) => listener({ type: "invalidate", key }));
    });
  }

  setQueryData<TData>(queryKey: QueryKey, data: TData): void {
    const key = hashQueryKey(queryKey);
    const record = this.ensureRecord(queryKey);

    record.data = data;
    record.updatedAt = Date.now();
    record.dataRevision = (record.dataRevision ?? 0) + 1;

    this.listeners.forEach((listener) => listener({ type: "setData", key, data }));
  }

  fetchQuery<TData>(
    queryKey: QueryKey,
    queryFn: (context: { signal: AbortSignal }) => Promise<TData>,
    options: { retry?: number } = {},
  ): Promise<TData> {
    const key = hashQueryKey(queryKey);
    const record = this.ensureRecord(queryKey);

    if (record.promise) {
      if (record.controller?.signal.aborted) {
        record.promise = undefined;
        record.controller = undefined;
      } else {
        return record.promise as Promise<TData>;
      }
    }

    if (record.controller?.signal.aborted) {
      record.controller = undefined;
      record.promise = undefined;
    }

    if (record.promise) {
      return record.promise as Promise<TData>;
    }

    const controller = new AbortController();
    const retry = Math.max(0, options.retry ?? this.defaults?.queries?.retry ?? 0);
    record.invalidatedWhileFetching = false;
    record.staleOnSettle = false;
    const dataRevision = record.dataRevision;

    const promise = runWithRetry(
      () => queryFn({ signal: controller.signal }),
      retry,
      controller.signal,
      this.defaults?.queries?.retryDelay,
    )
      .then((data) => {
        const activeRecord = this.records.get(key);

        if (activeRecord?.promise === promise && !activeRecord.invalidatedWhileFetching && activeRecord.dataRevision === dataRevision) {
          this.setQueryData(queryKey, data);
        }

        return data;
      })
      .finally(() => {
        const activeRecord = this.records.get(key);

        if (!activeRecord || activeRecord.promise !== promise) {
          return;
        }

        const shouldRefetch = activeRecord.invalidatedWhileFetching;
        if (activeRecord.staleOnSettle) activeRecord.updatedAt = 0;
        activeRecord.promise = undefined;
        activeRecord.controller = undefined;
        activeRecord.invalidatedWhileFetching = false;
        activeRecord.staleOnSettle = false;

        if (shouldRefetch) {
          activeRecord.updatedAt = 0;
          this.bumps.set(key, this.getBump(key) + 1);
          this.listeners.forEach((listener) => listener({ type: "invalidate", key }));
        }
      });

    record.promise = promise;
    record.controller = controller;

    return promise;
  }

  cancelQuery(queryKey: QueryKey): void {
    const record = this.records.get(hashQueryKey(queryKey));

    if (!record) {
      return;
    }

    const controller = record.controller;
    record.promise = undefined;
    record.controller = undefined;
    controller?.abort();
  }

  clear(): void {
    const records = [...this.records.values()];
    this.records.clear();
    this.bumps.clear();
    records.forEach(record => record.controller?.abort());
  }
}

const QueryClientContext = createContext<QueryClient | null>(null);

export function QueryClientProvider({ client, children }: PropsWithChildren<{ client: QueryClient }>) {
  return <QueryClientContext.Provider value={client}>{children}</QueryClientContext.Provider>;
}

export function useQueryClient(): QueryClient {
  const client = useContext(QueryClientContext);

  if (!client) {
    throw new Error("QueryClientProvider is missing");
  }

  return client;
}

function isStale(updatedAt: number, staleTime: number, hasData: boolean): boolean {
  if (!hasData || updatedAt === 0) {
    return true;
  }

  if (staleTime <= 0) {
    return true;
  }

  return Date.now() - updatedAt >= staleTime;
}

export function useQuery<TData>(options: QueryOptions<TData>) {
  const client = useQueryClient();
  const keyString = useMemo(() => hashQueryKey(options.queryKey), [options.queryKey]);
  const enabled = options.enabled !== false;
  const staleTime = options.staleTime ?? client.defaults?.queries?.staleTime ?? 0;
  const retry = options.retry ?? client.defaults?.queries?.retry ?? 0;
  const binding = useMemo(() => ({ client, keyString }), [client, keyString]);
  const requestRef = useRef({ binding, queryKey: options.queryKey, queryFn: options.queryFn });
  useLayoutEffect(() => {
    requestRef.current = { binding, queryKey: options.queryKey, queryFn: options.queryFn };
  }, [binding, options.queryKey, options.queryFn]);

  const [status, setStatus] = useState<{ binding: typeof binding; bump: number; fetching: boolean; error: unknown }>();
  const subscribe = useCallback((notify: () => void) => client.subscribe(event => {
    if (event.key !== keyString) return;
    if (event.type === "setData") {
      setStatus(current => current?.binding === binding && current.error ? { ...current, error: null } : current);
    }
    notify();
  }), [binding, client, keyString]);
  const readData = () => client.getQueryState<TData>(options.queryKey).data;
  const readBump = () => client.getBump(keyString);
  const data = useSyncExternalStore(subscribe, readData, readData);
  const bump = useSyncExternalStore(subscribe, readBump, readBump);

  useEffect(() => {
    if (enabled) return client.observeQuery(requestRef.current.queryKey, options.cancelOnUnmount);
  }, [binding, client, enabled, options.cancelOnUnmount]);

  useEffect(() => {
    if (!enabled) return;
    const { queryKey, queryFn } = requestRef.current;
    const snapshot = client.getQueryState<TData>(queryKey);
    if (!isStale(snapshot.updatedAt, staleTime, snapshot.data !== undefined)) return;
    let isActive = true;
    setStatus({ binding, bump, fetching: true, error: null });
    client.fetchQuery(queryKey, queryFn, { retry }).then(
      () => {
        if (isActive) setStatus({ binding, bump, fetching: false, error: null });
      },
      reason => {
        if (!isActive) return;
        const error = isRequestCanceledError(reason) ? null : reason;
        setStatus({ binding, bump, fetching: false, error });
        if (error) client.queryCache?.config.onError?.(error, { queryKey });
      },
    );
    return () => { isActive = false; };
  }, [binding, bump, client, enabled, retry, staleTime]);

  const refetchWithSignal = useCallback((signal: AbortSignal) => {
    const { binding: currentBinding, queryKey, queryFn } = requestRef.current;
    if (signal.aborted || binding !== currentBinding) return Promise.reject(signal.reason ?? new RequestCanceledError());
    return client.fetchQuery(queryKey, async ({ signal: querySignal }) => {
      const controller = new AbortController();
      const abortFromCaller = () => controller.abort(signal.reason);
      const abortFromQuery = () => controller.abort(querySignal.reason);
      signal.addEventListener("abort", abortFromCaller);
      querySignal.addEventListener("abort", abortFromQuery);
      try {
        if (querySignal.aborted) abortFromQuery();
        if (signal.aborted) abortFromCaller();
        const data = await queryFn({ signal: controller.signal });
        if (controller.signal.aborted) throw controller.signal.reason ?? new RequestCanceledError();
        return data;
      } finally {
        signal.removeEventListener("abort", abortFromCaller);
        querySignal.removeEventListener("abort", abortFromQuery);
      }
    }, { retry: 0 });
  }, [binding, client]);

  const currentStatus = enabled && status?.binding === binding && status.bump === bump ? status : undefined;
  const error = currentStatus?.error ?? null;
  return {
    data,
    error,
    isLoading: enabled && data === undefined && (currentStatus?.fetching ?? true),
    isFetching: currentStatus?.fetching ?? false,
    isError: Boolean(error),
    refetchWithSignal,
  };
}

export function useMutation<TData, TVariables>(options: MutationOptions<TData, TVariables>) {
  const client = useQueryClient();
  const [isPending, setIsPending] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const mutate = useCallback(
    (variables: TVariables, callbacks?: { onSuccess?: (data: TData) => void; onError?: (error: unknown) => void }) => {
      setIsPending(true);
      client.changeMutationCount(1);
      setError(null);

      let mutation: Promise<TData>;
      try {
        mutation = options.mutationFn(variables);
      } catch (reason) {
        mutation = Promise.reject(reason);
      }
      mutation
        .then((data) => {
          options.onSuccess?.(data, variables);
          callbacks?.onSuccess?.(data);
        })
        .catch((reason) => {
          setError(reason);
          options.onError?.(reason, variables);
          callbacks?.onError?.(reason);
          client.mutationCache?.config.onError?.(reason, variables, null, { options: { mutationKey: options.mutationKey }, meta: options.meta });
        })
        .finally(() => { client.changeMutationCount(-1); setIsPending(false); });
    },
    [client, options],
  );

  return { mutate, isPending, error };
}

export function useIsMutating() {
  const client = useQueryClient();
  return useSyncExternalStore(client.subscribeMutations, client.getMutationCount, client.getMutationCount);
}
