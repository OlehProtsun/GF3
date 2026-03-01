import { useState } from "react";
import type { PropsWithChildren } from "react";
import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { isDev } from "@shared/lib/isDev";

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        retry: 1,
        refetchOnWindowFocus: false,
      },
      mutations: {
        retry: 0,
      },
    },
    queryCache: new QueryCache({
      onError: (error, query) => {
        if (isDev) {
          console.error("[Query error]", query.queryKey, error);
        }
      },
    }),
    mutationCache: new MutationCache({
      onError: (error, _variables, _context, mutation) => {
        if (isDev) {
          console.error("[Mutation error]", mutation.options.mutationKey, error);
        }
      },
    }),
  });
}

export function QueryProvider({ children }: PropsWithChildren) {
  const [queryClient] = useState(createQueryClient);

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      {isDev ? <ReactQueryDevtools /> : null}
    </QueryClientProvider>
  );
}
