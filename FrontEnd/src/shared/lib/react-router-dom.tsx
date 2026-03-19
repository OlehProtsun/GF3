import { createContext, useContext, useEffect, useMemo, useState } from "react";
import type { AnchorHTMLAttributes, PropsWithChildren, ReactNode } from "react";

type NavigateTo = string | number;

type RouterLocation = {
  pathname: string;
  search: string;
  hash: string;
};

type RouterContextValue = {
  location: RouterLocation;
  navigate: (to: NavigateTo) => void;
};

const RouterContext = createContext<RouterContextValue | null>(null);

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
  return {
    pathname: window.location.pathname,
    search: window.location.search,
    hash: window.location.hash,
  };
}

function resolveLocation(to: string): RouterLocation {
  const url = new URL(to, window.location.href);

  return {
    pathname: url.pathname,
    search: url.search,
    hash: url.hash,
  };
}

function toLocationHref(location: RouterLocation) {
  return `${location.pathname}${location.search}${location.hash}`;
}

export function BrowserRouter({ children }: PropsWithChildren) {
  const [location, setLocation] = useState<RouterLocation>(() => readLocation());

  useEffect(() => {
    const onPopState = () => setLocation(readLocation());
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, []);

  const navigate = (to: NavigateTo) => {
    if (typeof to === "number") {
      window.history.go(to);
      return;
    }

    const nextLocation = resolveLocation(to);
    const nextHref = toLocationHref(nextLocation);

    if (nextHref === toLocationHref(readLocation())) {
      return;
    }

    window.history.pushState({}, "", nextHref);
    setLocation(nextLocation);
  };

  const value = useMemo(() => ({ location, navigate }), [location]);

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
};

export function Navigate({ to }: NavigateProps) {
  const navigate = useNavigate();

  useEffect(() => {
    navigate(to);
  }, [navigate, to]);

  return null;
}

export function Outlet() {
  return null;
}

export function Routes({ children }: PropsWithChildren) {
  return <>{children}</>;
}

export function Route(_props: { path?: string; element?: ReactNode; children?: ReactNode }) {
  return null;
}

export function matchPath(pathname: string, path: string): boolean {
  return extractRouteParams(pathname, path) !== null;
}

export function renderMatched(pathname: string, routes: Array<{ path: string; element: ReactNode }>) {
  const matched = routes.find((route) => matchPath(pathname, route.path));
  if (!matched) return null;

  return matched.element;
}

