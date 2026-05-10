/* eslint-disable react-refresh/only-export-components */
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
import { isRequestCanceledError } from "@shared/api/httpClient";

type QueryKey = readonly unknown[];
type QueryFilters = { queryKey?: QueryKey };
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
};

type QueryClientOptions = {
  defaultOptions?: {
    queries?: { staleTime?: number; retry?: number; refetchOnWindowFocus?: boolean };
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

async function runWithRetry<TData>(queryFn: () => Promise<TData>, retry: number): Promise<TData> {
  let attempt = 0;

  while (true) {
    try {
      return await queryFn();
    } catch (error) {
      if (isRequestCanceledError(error) || attempt >= retry) {
        throw error;
      }

      attempt += 1;
    }
  }
}

export class QueryClient {
  private listeners: Set<(event: QueryEvent) => void> = new Set();
  private bumps: Map<string, number> = new Map();
  private records: Map<string, CacheRecord> = new Map();
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

  trackQuery(queryKey: QueryKey): void {
    this.ensureRecord(queryKey);
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
    const matchingKeys = [...this.records.entries()]
      .filter(([, record]) => isQueryKeyPrefix(filters.queryKey, record.queryKey))
      .map(([key]) => key);

    if (matchingKeys.length === 0 && filters.queryKey) {
      const fallbackKey = hashQueryKey(filters.queryKey);
      this.ensureRecord(filters.queryKey);
      matchingKeys.push(fallbackKey);
    }

    matchingKeys.forEach((key) => {
      const record = this.records.get(key);
      if (record) {
        record.updatedAt = 0;
        if (record.promise) {
          record.invalidatedWhileFetching = true;
        }
      }

      this.bumps.set(key, this.getBump(key) + 1);
      this.listeners.forEach((listener) => listener({ type: "invalidate", key }));
    });
  }

  setQueryData<TData>(queryKey: QueryKey, data: TData): void {
    const key = hashQueryKey(queryKey);
    const record = this.ensureRecord(queryKey);

    record.data = data;
    record.updatedAt = Date.now();

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

    const promise = runWithRetry(
      () => queryFn({ signal: controller.signal }),
      retry,
    )
      .then((data) => {
        const activeRecord = this.records.get(key);

        if (activeRecord?.promise === promise && !activeRecord.invalidatedWhileFetching) {
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
        activeRecord.promise = undefined;
        activeRecord.controller = undefined;
        activeRecord.invalidatedWhileFetching = false;

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

    record.invalidatedWhileFetching = true;
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
  if (!hasData) {
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
  const staleTime = options.staleTime ?? client.defaults?.queries?.staleTime ?? 0;
  const retry = options.retry ?? client.defaults?.queries?.retry ?? 0;
  const initialState = client.getQueryState<TData>(options.queryKey);
  const queryFnRef = useRef(options.queryFn);
  const queryKeyRef = useRef(options.queryKey);
  const lastSyncRef = useRef<{ key: string; bump: number } | null>(null);
  const [data, setData] = useState<TData | undefined>(initialState.data);
  const [error, setError] = useState<unknown>(null);
  const [isLoading, setIsLoading] = useState(options.enabled !== false && initialState.data === undefined);
  const [isFetching, setIsFetching] = useState(false);
  const [bump, setBump] = useState(client.getBump(keyString));

  useEffect(() => {
    queryFnRef.current = options.queryFn;
    queryKeyRef.current = options.queryKey;
    client.trackQuery(options.queryKey);
  }, [client, options.queryFn, options.queryKey]);

  useEffect(() => {
    setBump(client.getBump(keyString));
    lastSyncRef.current = null;
  }, [client, keyString]);

  useEffect(() => {
    return client.subscribe((event) => {
      if (event.key !== keyString) {
        return;
      }

      if (event.type === "invalidate") {
        setBump(client.getBump(event.key));
        return;
      }

      setData(event.data as TData);
      setError(null);
      setIsLoading(false);
      setIsFetching(false);
    });
  }, [client, keyString]);

  useEffect(() => {
    client.trackQuery(options.queryKey);

    const snapshot = client.getQueryState<TData>(options.queryKey);
    setData(snapshot.data);
    setError(null);
    setIsFetching(false);

    if (options.enabled === false) {
      setIsLoading(false);
      setIsFetching(false);
      return;
    }

    setIsLoading(snapshot.data === undefined);
  }, [client, keyString, options.enabled, options.queryKey]);

  useEffect(() => {
    if (options.enabled === false) {
      return;
    }

    const snapshot = client.getQueryState<TData>(queryKeyRef.current);
    const hasData = snapshot.data !== undefined;
    const wasInvalidated = lastSyncRef.current?.key === keyString && lastSyncRef.current.bump !== bump;

    if (!wasInvalidated && !isStale(snapshot.updatedAt, staleTime, hasData)) {
      setIsLoading(false);
      setIsFetching(false);
      lastSyncRef.current = { key: keyString, bump };
      return;
    }

    let isActive = true;
    setError(null);
    setIsFetching(true);
    setIsLoading(!hasData);

    client
      .fetchQuery(queryKeyRef.current, queryFnRef.current, { retry })
      .catch((reason) => {
        if (!isActive || isRequestCanceledError(reason)) {
          return;
        }

        setError(reason);
        client.queryCache?.config.onError?.(reason, { queryKey: queryKeyRef.current });
      })
      .finally(() => {
        if (!isActive) {
          return;
        }

        setIsFetching(false);
        setIsLoading(false);
        lastSyncRef.current = { key: keyString, bump };
      });

    return () => {
      isActive = false;

      if (options.cancelOnUnmount === true) {
        client.cancelQuery(queryKeyRef.current);
      }
    };
  }, [bump, client, keyString, options.cancelOnUnmount, options.enabled, retry, staleTime]);

  return {
    data,
    error,
    isLoading,
    isFetching,
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
