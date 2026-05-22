import { Suspense, lazy } from "react";
import type { ComponentType } from "react";
import { BrowserRouter, Navigate, useLocation } from "react-router-dom";
import { EmployeeWorkspaceLayout } from "@app/layouts/employee-workspace-layout";
import { OverlaySidebarLayout } from "@app/layouts/overlay-sidebar-layout";
import { useAuth } from "@app/providers/AuthProvider";
import { renderMatched } from "@shared/lib/react-router-dom";
import { RouteFallback } from "./RouteFallback";

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

  return lazy(loadComponent);
}

const HomePage = lazyPage(() => import("@pages/home"), (module) => module.HomePage);
const EmployeeNotificationsPage = lazyPage(
  () => import("@pages/employee-notifications"),
  (module) => module.EmployeeNotificationsPage,
);
const EmployeeAvailabilityPage = lazyPage(
  () => import("@pages/employee-availability"),
  (module) => module.EmployeeAvailabilityPage,
);
const EmployeeSchedulePage = lazyPage(
  () => import("@pages/employee-schedule"),
  (module) => module.EmployeeSchedulePage,
);
const EmployeeSwapPage = lazyPage(
  () => import("@pages/employee-swap"),
  (module) => module.EmployeeSwapPage,
);
const EmployeeAccountPage = lazyPage(
  () => import("@pages/employee-account"),
  (module) => module.EmployeeAccountPage,
);
const LoginPage = lazyPage(() => import("@pages/login"), (module) => module.LoginPage);
const PasswordRecoveryPage = lazyPage(
  () => import("@pages/password-recovery"),
  (module) => module.PasswordRecoveryPage,
);
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
const ManagerAccountPage = lazyPage(() => import("@pages/manager-account"), (module) => module.ManagerAccountPage);
const EmployeeListPage = lazyPage(() => import("@pages/employee-list"), (module) => module.EmployeeListPage);
const EmployeeProfilePage = lazyPage(() => import("@pages/employee-profile"), (module) => module.EmployeeProfilePage);
const EmployeeEditPage = lazyPage(() => import("@pages/employee-edit"), (module) => module.EmployeeEditPage);

const managerRoutes = [
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
  { path: "/manager-profile", element: <ManagerAccountPage /> },
  { path: "/employee/new", element: <EmployeeEditPage /> },
  { path: "/employee/:employeeId/edit", element: <EmployeeEditPage /> },
  { path: "/employee/:employeeId", element: <EmployeeProfilePage /> },
  { path: "/employee", element: <EmployeeListPage /> },
  { path: "/", element: <HomePage /> },
] as const;

const employeeRoutes = [
  { path: "/profile", element: <EmployeeAccountPage /> },
  { path: "/notifications", element: <EmployeeNotificationsPage /> },
  { path: "/swap", element: <EmployeeSwapPage /> },
  { path: "/schedule", element: <EmployeeSchedulePage /> },
  { path: "/availability", element: <EmployeeAvailabilityPage /> },
  { path: "/", element: <EmployeeNotificationsPage /> },
] as const;

function RoutedContent() {
  const { pathname } = useLocation();
  const { session, status } = useAuth();
  const isPublicAuthRoute = pathname === "/login" || pathname === "/password-recovery";

  if (status === "loading") {
    return <RouteFallback />;
  }

  if (status === "unauthenticated") {
    if (pathname === "/login") {
      return <LoginPage />;
    }

    if (pathname === "/password-recovery") {
      return <PasswordRecoveryPage />;
    }

    return <Navigate to="/login" />;
  }

  if (isPublicAuthRoute) {
    return <Navigate to="/" />;
  }

  const matched = renderMatched(pathname, session?.role === "manager" ? managerRoutes : employeeRoutes);
  return matched ?? <Navigate to="/" />;
}

function RoutedShell() {
  const { pathname } = useLocation();
  const { session, status } = useAuth();
  const shouldUseLayout = status === "authenticated" && pathname !== "/login" && pathname !== "/password-recovery";
  const content = (
    <Suspense fallback={<RouteFallback />}>
      <RoutedContent />
    </Suspense>
  );

  if (!shouldUseLayout) {
    return content;
  }

  if (session?.role === "employee") {
    return <EmployeeWorkspaceLayout>{content}</EmployeeWorkspaceLayout>;
  }

  return <OverlaySidebarLayout>{content}</OverlaySidebarLayout>;
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <RoutedShell />
    </BrowserRouter>
  );
}
