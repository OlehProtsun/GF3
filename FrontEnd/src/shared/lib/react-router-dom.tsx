import { createContext, useContext, useEffect, useMemo, useState } from "react";
import type { AnchorHTMLAttributes, PropsWithChildren, ReactNode } from "react";

type NavigateTo = string | number;

type RouterContextValue = {
  pathname: string;
  navigate: (to: NavigateTo) => void;
};

const RouterContext = createContext<RouterContextValue | null>(null);

export function BrowserRouter({ children }: PropsWithChildren) {
  const [pathname, setPathname] = useState(window.location.pathname);

  useEffect(() => {
    const onPopState = () => setPathname(window.location.pathname);
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, []);

  const navigate = (to: NavigateTo) => {
    if (typeof to === "number") {
      window.history.go(to);
      return;
    }

    if (to === window.location.pathname) return;

    window.history.pushState({}, "", to);
    setPathname(to);
  };

  const value = useMemo(() => ({ pathname, navigate }), [pathname]);

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
  return { pathname: useRouterContext().pathname };
}

export function useParams<T extends Record<string, string>>() {
  const { pathname } = useRouterContext();
  const parts = pathname.split("/").filter(Boolean);

  const params = {} as Record<string, string>;
  if (parts[0] === "employee" && parts[1] && parts[1] !== "new") {
    params.employeeId = parts[1];
  }

  return params as T;
}

type NavLinkProps = Omit<AnchorHTMLAttributes<HTMLAnchorElement>, "className" | "href"> & {
  to: string;
  className?: string | ((params: { isActive: boolean }) => string);
  children: ReactNode;
};

export function NavLink({ to, className, children, ...rest }: NavLinkProps) {
  const { pathname, navigate } = useRouterContext();
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
  if (path === pathname) return true;
  const pathParts = path.split("/").filter(Boolean);
  const currentParts = pathname.split("/").filter(Boolean);
  if (pathParts.length !== currentParts.length) return false;

  return pathParts.every((part, index) => part.startsWith(":") || part === currentParts[index]);
}

export function renderMatched(pathname: string, routes: Array<{ path: string; element: ReactNode }>) {
  const matched = routes.find((route) => matchPath(pathname, route.path));
  if (!matched) return null;

  return matched.element;
}
