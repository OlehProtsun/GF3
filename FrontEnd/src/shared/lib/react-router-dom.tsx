/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import type { AnchorHTMLAttributes, PropsWithChildren, ReactNode } from "react";

type NavigateTo = string | number;

type RouterLocation = {
  pathname: string;
  search: string;
  hash: string;
  key: string;
};

type NavigationBlocker = {
  shouldBlock: (nextLocation: RouterLocation) => boolean;
  onBlocked: () => void;
};

type PendingNavigationAttempt =
  | {
      kind: "push";
      location: RouterLocation;
      replace?: boolean;
    }
  | {
      kind: "delta";
      delta: number;
    };

type RouterContextValue = {
  location: RouterLocation;
  navigate: (to: NavigateTo, options?: { replace?: boolean }) => void;
  setNavigationBlocker: (blocker: NavigationBlocker | null) => void;
  proceedBlockedNavigation: () => void;
  cancelBlockedNavigation: () => void;
};

const RouterContext = createContext<RouterContextValue | null>(null);

let locationSequence = 0;
const ROUTER_HISTORY_INDEX_KEY = "__gf3RouterIndex";

const PARAM_ROUTE_PATTERNS = [
  "/availability/new",
  "/availability/:availabilityId/edit",
  "/availability/:availabilityId",
  "/container/:containerId/graphs/new",
  "/container/:containerId/graphs/:graphId/edit",
  "/container/:containerId/graphs/:graphId",
  "/employee/new",
  "/employee/:employeeId/edit",
  "/employee/:employeeId",
  "/shop/new",
  "/shop/:shopId/edit",
  "/shop/:shopId",
] as const;

function readLocation(): RouterLocation {
  locationSequence += 1;

  return {
    pathname: window.location.pathname,
    search: window.location.search,
    hash: window.location.hash,
    key: String(locationSequence),
  };
}

function resolveLocation(to: string): RouterLocation {
  const url = new URL(to, window.location.href);
  locationSequence += 1;

  return {
    pathname: url.pathname,
    search: url.search,
    hash: url.hash,
    key: String(locationSequence),
  };
}

function toLocationHref(location: RouterLocation) {
  return `${location.pathname}${location.search}${location.hash}`;
}

function getRouterHistoryIndex(state: unknown) {
  if (!state || typeof state !== "object") {
    return null;
  }

  const rawValue = (state as Record<string, unknown>)[ROUTER_HISTORY_INDEX_KEY];
  return typeof rawValue === "number" && Number.isInteger(rawValue) ? rawValue : null;
}

function createRouterHistoryState(index: number) {
  return {
    [ROUTER_HISTORY_INDEX_KEY]: index,
  };
}

function getCurrentHref() {
  return `${window.location.pathname}${window.location.search}${window.location.hash}`;
}

export function BrowserRouter({ children }: PropsWithChildren) {
  const [location, setLocation] = useState<RouterLocation>(() => readLocation());
  const locationRef = useRef(location);
  const historyIndexRef = useRef(getRouterHistoryIndex(window.history.state) ?? 0);
  const navigationBlockerRef = useRef<NavigationBlocker | null>(null);
  const pendingNavigationRef = useRef<PendingNavigationAttempt | null>(null);
  const bypassNextPopStateRef = useRef(false);

  const updateLocation = useCallback((nextLocation: RouterLocation) => {
    locationRef.current = nextLocation;
    setLocation(nextLocation);
  }, []);

  const commitPushNavigation = useCallback((nextLocation: RouterLocation, replace = false) => {
    const nextHistoryIndex = historyIndexRef.current + (replace ? 0 : 1);
    historyIndexRef.current = nextHistoryIndex;
    if (replace) {
      window.history.replaceState(createRouterHistoryState(nextHistoryIndex), "", toLocationHref(nextLocation));
    } else {
      window.history.pushState(createRouterHistoryState(nextHistoryIndex), "", toLocationHref(nextLocation));
    }
    updateLocation(nextLocation);
  }, [updateLocation]);

  const cancelBlockedNavigation = useCallback(() => {
    pendingNavigationRef.current = null;
  }, []);

  const proceedBlockedNavigation = useCallback(() => {
    const pendingNavigation = pendingNavigationRef.current;
    if (!pendingNavigation) {
      return;
    }

    pendingNavigationRef.current = null;

    if (pendingNavigation.kind === "push") {
      commitPushNavigation(pendingNavigation.location, pendingNavigation.replace);
      return;
    }

    if (pendingNavigation.delta === 0) {
      return;
    }

    bypassNextPopStateRef.current = true;
    window.history.go(pendingNavigation.delta);
  }, [commitPushNavigation]);

  const setNavigationBlocker = useCallback((blocker: NavigationBlocker | null) => {
    navigationBlockerRef.current = blocker;
  }, []);

  useEffect(() => {
    const existingHistoryIndex = getRouterHistoryIndex(window.history.state);
    if (existingHistoryIndex === null) {
      window.history.replaceState(createRouterHistoryState(historyIndexRef.current), "", getCurrentHref());
    } else {
      historyIndexRef.current = existingHistoryIndex;
    }
  }, []);

  useEffect(() => {
    locationRef.current = location;
  }, [location]);

  useEffect(() => {
    const onPopState = (event: PopStateEvent) => {
      const nextLocation = readLocation();
      const nextHistoryIndex = getRouterHistoryIndex(event.state);

      if (bypassNextPopStateRef.current) {
        bypassNextPopStateRef.current = false;
        if (nextHistoryIndex !== null) {
          historyIndexRef.current = nextHistoryIndex;
        }

        updateLocation(nextLocation);
        return;
      }

      const blocker = navigationBlockerRef.current;
      if (blocker?.shouldBlock(nextLocation)) {
        const delta = nextHistoryIndex !== null ? nextHistoryIndex - historyIndexRef.current : 0;
        pendingNavigationRef.current = { kind: "delta", delta };
        blocker.onBlocked();

        if (delta !== 0) {
          bypassNextPopStateRef.current = true;
          window.history.go(-delta);
        } else {
          window.history.replaceState(createRouterHistoryState(historyIndexRef.current), "", toLocationHref(locationRef.current));
        }

        return;
      }

      if (nextHistoryIndex !== null) {
        historyIndexRef.current = nextHistoryIndex;
      }

      updateLocation(nextLocation);
    };

    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, [updateLocation]);

  const navigate = useCallback((to: NavigateTo, options?: { replace?: boolean }) => {
    if (typeof to === "number") {
      if (to === 0) {
        return;
      }

      const blocker = navigationBlockerRef.current;
      if (blocker?.shouldBlock(locationRef.current)) {
        pendingNavigationRef.current = { kind: "delta", delta: to };
        blocker.onBlocked();
        return;
      }

      window.history.go(to);
      return;
    }

    const nextLocation = resolveLocation(to);
    const nextHref = toLocationHref(nextLocation);

    if (nextHref === toLocationHref(locationRef.current)) {
      return;
    }

    const blocker = navigationBlockerRef.current;
    if (blocker?.shouldBlock(nextLocation)) {
      pendingNavigationRef.current = { kind: "push", location: nextLocation, replace: options?.replace };
      blocker.onBlocked();
      return;
    }

    commitPushNavigation(nextLocation, options?.replace);
  }, [commitPushNavigation]);

  const value = useMemo(
    () => ({
      location,
      navigate,
      setNavigationBlocker,
      proceedBlockedNavigation,
      cancelBlockedNavigation,
    }),
    [cancelBlockedNavigation, location, navigate, proceedBlockedNavigation, setNavigationBlocker],
  );

  return <RouterContext.Provider value={value}>{children}</RouterContext.Provider>;
}

function useRouterContext() {
  const context = useContext(RouterContext);
  if (!context) {
    throw new Error("BrowserRouter is missing");
  }

  return context;
}

export function useNavigate() {
  return useRouterContext().navigate;
}

export function useLocation() {
  return useRouterContext().location;
}

export function useNavigationBlocker(
  enabled: boolean,
  shouldBlock: (nextLocation: RouterLocation) => boolean,
  onBlocked: () => void,
) {
  const { setNavigationBlocker, proceedBlockedNavigation, cancelBlockedNavigation } = useRouterContext();

  useEffect(() => {
    if (!enabled) {
      cancelBlockedNavigation();
      setNavigationBlocker(null);
      return;
    }

    setNavigationBlocker({
      shouldBlock,
      onBlocked,
    });

    return () => {
      setNavigationBlocker(null);
    };
  }, [cancelBlockedNavigation, enabled, onBlocked, setNavigationBlocker, shouldBlock]);

  return {
    proceedBlockedNavigation,
    cancelBlockedNavigation,
  };
}

function extractRouteParams(pathname: string, path: string) {
  const pathParts = path.split("/").filter(Boolean);
  const currentParts = pathname.split("/").filter(Boolean);

  if (pathParts.length !== currentParts.length) {
    return null;
  }

  const params: Record<string, string> = {};

  for (let index = 0; index < pathParts.length; index += 1) {
    const routePart = pathParts[index];
    const currentPart = currentParts[index];

    if (!currentPart) {
      return null;
    }

    if (routePart.startsWith(":")) {
      params[routePart.slice(1)] = decodeURIComponent(currentPart);
      continue;
    }

    if (routePart !== currentPart) {
      return null;
    }
  }

  return params;
}

export function useParams<T extends Record<string, string | undefined>>() {
  const { pathname } = useRouterContext().location;

  for (const pattern of PARAM_ROUTE_PATTERNS) {
    const params = extractRouteParams(pathname, pattern);
    if (params) {
      return params as T;
    }
  }

  return {} as T;
}

type NavLinkProps = Omit<AnchorHTMLAttributes<HTMLAnchorElement>, "className" | "href"> & {
  to: string;
  className?: string | ((params: { isActive: boolean }) => string);
  children: ReactNode;
};

export function NavLink({ to, className, children, ...rest }: NavLinkProps) {
  const { location, navigate } = useRouterContext();
  const { pathname } = location;
  const isActive = pathname === to;
  const resolvedClassName = typeof className === "function" ? className({ isActive }) : className;

  return (
    <a
      {...rest}
      href={to}
      className={resolvedClassName}
      onClick={(event) => {
        event.preventDefault();
        navigate(to);
      }}
    >
      {children}
    </a>
  );
}

type NavigateProps = {
  to: string;
  replace?: boolean;
};

export function Navigate({ to, replace = false }: NavigateProps) {
  const navigate = useNavigate();

  useEffect(() => {
    navigate(to, { replace });
  }, [navigate, to, replace]);

  return null;
}

export function Outlet() {
  return null;
}

export function Routes({ children }: PropsWithChildren) {
  return <>{children}</>;
}

export function Route() {
  return null;
}

export function matchPath(pathname: string, path: string): boolean {
  return extractRouteParams(pathname, path) !== null;
}

export function renderMatched(pathname: string, routes: ReadonlyArray<{ path: string; element: ReactNode }>) {
  const matched = routes.find((route) => matchPath(pathname, route.path));
  if (!matched) return null;

  return matched.element;
}

