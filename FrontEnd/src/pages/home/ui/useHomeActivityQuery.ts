import { useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { managerHomeActivityApi, type HomeActivityTab, type ManagerHomeActivityPage } from "../api/managerHomeActivityApi";

type ActivityData = { scope: string; pages: ManagerHomeActivityPage[] };

// The repository's query shim has no infinite-query API. Keep paging in its existing cache
// so realtime invalidation refreshes every loaded page and never combines different filters.
export function useHomeActivityQuery(year: number, month: number, tab: HomeActivityTab) {
  const client = useQueryClient();
  const queryKey = [...queryKeys.home.activity(), year, month, tab];
  const scope = `${year}:${month}:${tab}`;
  const nextRequest = useRef(new Set<string>());
  const [nextByScope, setNextByScope] = useState<Record<string, { loading: boolean; error: unknown }>>({});
  const next = nextByScope[scope];
  const query = useQuery<ActivityData>({
    queryKey, staleTime: 30_000, cancelOnUnmount: true,
    queryFn: async ({ signal }) => {
      const pageCount = client.getQueryState<ActivityData>(queryKey).data?.pages.length ?? 1;
      const pages: ManagerHomeActivityPage[] = [];
      let offset = 0;
      for (let i = 0; i < pageCount; i++) {
        const page = await managerHomeActivityApi.list({ year, month, tab, offset, limit: 7 }, signal);
        pages.push(page);
        if (!page.hasMore) break;
        offset = page.nextOffset;
      }
      return { scope, pages };
    },
  });
  const data = query.data?.scope === scope ? query.data : undefined;
  const lastPage = data?.pages.at(-1);
  async function fetchNextPage() {
    if (!lastPage?.hasMore || query.isFetching || nextRequest.current.has(scope)) return;
    nextRequest.current.add(scope);
    setNextByScope(previous => ({ ...previous, [scope]: { loading: true, error: null } }));
    let error: unknown = null;
    try {
      await client.fetchQuery(queryKey, async ({ signal }) => {
        const page = await managerHomeActivityApi.list({ year, month, tab, offset: lastPage.nextOffset, limit: 7 }, signal);
        return { scope, pages: [...data!.pages, page] };
      }, { retry: 0 });
    } catch (caught) {
      if (!(caught instanceof Error && caught.name === "AbortError")) error = caught;
    } finally {
      nextRequest.current.delete(scope);
      setNextByScope(previous => ({ ...previous, [scope]: { loading: false, error } }));
    }
  }
  return { data, error: query.error, isError: query.isError, isLoading: query.isLoading || (!data && !query.isError),
    isFetching: query.isFetching, hasNextPage: lastPage?.hasMore ?? false,
    isFetchingNextPage: next?.loading ?? false,
    nextPageError: next?.error ?? null,
    fetchNextPage, refetch: () => client.invalidateQueries({ queryKey }),
  };
}
