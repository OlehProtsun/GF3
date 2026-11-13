import { t } from "@shared/i18n";
import { useEffect, useState } from "react";
import type { PropsWithChildren } from "react";
import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { ApiError, isRequestCanceledError } from "@shared/api/httpClient";
import { graphEditorSaveMutationKey } from "@shared/api/graphSaveTraffic";
import { isDev } from "@shared/lib/isDev";
import { pushErrorAlertFromError } from "@shared/ui/feedback/error-alerts/errorAlerts";
import { useAuth } from "./AuthProvider";
import { PublicHolidayVisibilityProvider } from "./PublicHolidayVisibilityProvider";
import { usePendingRegulationsQuery } from "@entities/regulations/api/queries";

function createQueryClient() {
  let lastRateLimitToast = -Infinity;
  const client = new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        retry: 1,
        retryDelay: (_, error) => error instanceof ApiError && error.status === 429 ? error.retryAfterMs ?? 3000 : 0,
        refetchOnWindowFocus: false,
      },
      mutations: {
        retry: 0,
      },
    },
    queryCache: new QueryCache({
      onError: (error, query) => {
        if (isRequestCanceledError(error)) {
          return;
        }
        if (error instanceof ApiError && error.status === 429) {
          const now = Date.now();
          if (client.getQueryState(query.queryKey).data !== undefined || now - lastRateLimitToast < 5000) return;
          lastRateLimitToast = now;
        }

        pushErrorAlertFromError(error, t("Could not load data from the server."));

        if (isDev) {
          console.error("[Query error]", query.queryKey, error);
        }
      },
    }),
    mutationCache: new MutationCache({
      onError: (error, _variables, _context, mutation) => {
        if (isRequestCanceledError(error) || mutation.options.mutationKey === graphEditorSaveMutationKey) {
          return;
        }

        pushErrorAlertFromError(error, t("Could not complete the requested action."));

        if (isDev) {
          console.error("[Mutation error]", mutation.options.mutationKey, error);
        }
      },
    }),
  });
  return client;
}

export function QueryProvider({ children }: PropsWithChildren) {
  const { session } = useAuth();
  const accountKey = session ? `${session.role}:${session.managerId ?? session.employeeId}` : "guest";

  const holidaySettingsEnabled = Boolean(session) && !(session?.role === "manager" && session.workspaceMode === "choose");
  return <AccountQueryProvider key={accountKey} accountKey={accountKey} enabled={holidaySettingsEnabled}>{children}</AccountQueryProvider>;
}

function AccountQueryProvider({ children, accountKey, enabled }: PropsWithChildren<{ accountKey: string; enabled: boolean }>) {
  const [queryClient] = useState(createQueryClient);
  useEffect(() => () => queryClient.clear(), [queryClient]);

  return (
    <QueryClientProvider client={queryClient}>
      <AccountHolidayVisibility accountKey={accountKey} enabled={enabled}>{children}</AccountHolidayVisibility>
      {isDev ? <ReactQueryDevtools /> : null}
    </QueryClientProvider>
  );
}

function AccountHolidayVisibility({ children, accountKey, enabled }: PropsWithChildren<{ accountKey: string; enabled: boolean }>) {
  const pending = usePendingRegulationsQuery(accountKey, enabled);
  return (
    <PublicHolidayVisibilityProvider enabled={enabled && pending.data?.length === 0 && !pending.isError}>
      {children}
    </PublicHolidayVisibilityProvider>
  );
}
