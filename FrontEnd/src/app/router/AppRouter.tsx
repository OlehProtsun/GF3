import { Suspense, lazy, useEffect } from "react";
import type { ComponentType, LazyExoticComponent } from "react";
import { BrowserRouter, Navigate, useLocation } from "react-router-dom";
import { OverlaySidebarLayout } from "@app/layouts/overlay-sidebar-layout";
import { renderMatched } from "@shared/lib/react-router-dom";
import { RouteFallback } from "./RouteFallback";

type PreloadablePage = LazyExoticComponent<ComponentType> & {
  preload: () => Promise<void>;
};

type IdleWindow = Window & typeof globalThis & {
  requestIdleCallback?: (callback: () => void, options?: { timeout: number }) => number;
  cancelIdleCallback?: (handle: number) => void;
};

function lazyPage<TModule>(
  load: () => Promise<TModule>,
  select: (module: TModule) => ComponentType,
) {
  let modulePromise: Promise<{ default: ComponentType }> | undefined;

  const loadComponent = () => {
    modulePromise ??= load().then((module) => ({
      default: select(module),
    }));

    return modulePromise;
  };

  const component = lazy(loadComponent) as PreloadablePage;
  component.preload = () => loadComponent().then(() => undefined);

  return component;
}

const HomePage = lazyPage(() => import("@pages/home"), (module) => module.HomePage);
const ShopListPage = lazyPage(() => import("@pages/shop-list"), (module) => module.ShopListPage);
const ShopProfilePage = lazyPage(() => import("@pages/shop-profile"), (module) => module.ShopProfilePage);
const ShopEditPage = lazyPage(() => import("@pages/shop-edit"), (module) => module.ShopEditPage);
const AvailabilityPage = lazyPage(() => import("@pages/availability"), (module) => module.AvailabilityPage);
const AvailabilityEditPage = lazyPage(() => import("@pages/availability-edit"), (module) => module.AvailabilityEditPage);
const AvailabilityProfilePage = lazyPage(() => import("@pages/availability-profile"), (module) => module.AvailabilityProfilePage);
const ContainerPage = lazyPage(() => import("@pages/container"), (module) => module.ContainerPage);
const ContainerGraphProfilePage = lazyPage(
  () => import("@pages/container-graph-profile"),
  (module) => module.ContainerGraphProfilePage,
);
const ContainerGraphEditPage = lazyPage(
  () => import("@pages/container-graph-edit"),
  (module) => module.ContainerGraphEditPage,
);
const InformationPage = lazyPage(() => import("@pages/information"), (module) => module.InformationPage);
const DataBasePage = lazyPage(() => import("@pages/database"), (module) => module.DataBasePage);
const EmployeeListPage = lazyPage(() => import("@pages/employee-list"), (module) => module.EmployeeListPage);
const EmployeeProfilePage = lazyPage(() => import("@pages/employee-profile"), (module) => module.EmployeeProfilePage);
const EmployeeEditPage = lazyPage(() => import("@pages/employee-edit"), (module) => module.EmployeeEditPage);

const preloadablePages: readonly PreloadablePage[] = [
  HomePage,
  ShopListPage,
  ShopProfilePage,
  ShopEditPage,
  AvailabilityPage,
  AvailabilityEditPage,
  AvailabilityProfilePage,
  ContainerPage,
  ContainerGraphProfilePage,
  ContainerGraphEditPage,
  InformationPage,
  DataBasePage,
  EmployeeListPage,
  EmployeeProfilePage,
  EmployeeEditPage,
] as const;

const routes = [
  { path: "/shop/new", element: <ShopEditPage /> },
  { path: "/shop/:shopId/edit", element: <ShopEditPage /> },
  { path: "/shop/:shopId", element: <ShopProfilePage /> },
  { path: "/shop", element: <ShopListPage /> },
  { path: "/availability/new", element: <AvailabilityEditPage /> },
  { path: "/availability/:availabilityId/edit", element: <AvailabilityEditPage /> },
  { path: "/availability/:availabilityId", element: <AvailabilityProfilePage /> },
  { path: "/availability", element: <AvailabilityPage /> },
  { path: "/container/:containerId/graphs/new", element: <ContainerGraphEditPage /> },
  { path: "/container/:containerId/graphs/:graphId/edit", element: <ContainerGraphEditPage /> },
  { path: "/container/:containerId/graphs/:graphId", element: <ContainerGraphProfilePage /> },
  { path: "/container", element: <ContainerPage /> },
  { path: "/information", element: <InformationPage /> },
  { path: "/database", element: <DataBasePage /> },
  { path: "/employee/new", element: <EmployeeEditPage /> },
  { path: "/employee/:employeeId/edit", element: <EmployeeEditPage /> },
  { path: "/employee/:employeeId", element: <EmployeeProfilePage /> },
  { path: "/employee", element: <EmployeeListPage /> },
  { path: "/", element: <HomePage /> },
] as const;

function useWarmRouteChunks() {
  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    const connection = (navigator as Navigator & {
      connection?: {
        saveData?: boolean;
      };
    }).connection;

    if (connection?.saveData) {
      return;
    }

    const warmRouteChunks = () => {
      void Promise.allSettled(preloadablePages.map((page) => page.preload()));
    };

    const idleWindow = window as IdleWindow;
    const idleHandle = idleWindow.requestIdleCallback?.(warmRouteChunks, { timeout: 1200 });

    if (idleHandle !== undefined) {
      return () => {
        idleWindow.cancelIdleCallback?.(idleHandle);
      };
    }

    const timeoutId = window.setTimeout(warmRouteChunks, 320);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, []);
}

function RoutedContent() {
  const { pathname } = useLocation();
  useWarmRouteChunks();

  return renderMatched(pathname, routes) ?? <Navigate to="/" />;
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <OverlaySidebarLayout>
        <Suspense fallback={<RouteFallback />}>
          <RoutedContent />
        </Suspense>
      </OverlaySidebarLayout>
    </BrowserRouter>
  );
}
