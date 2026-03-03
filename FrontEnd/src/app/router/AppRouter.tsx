import { BrowserRouter, Navigate, useLocation } from "react-router-dom";
import { OverlaySidebarLayout } from "@app/layouts/overlay-sidebar-layout";
import { HomePage } from "@pages/home";
import { ShopPage } from "@pages/shop";
import { AvailabilityPage } from "@pages/availability";
import { ContainerPage } from "@pages/container";
import { InformationPage } from "@pages/information";
import { DataBasePage } from "@pages/database";
import { EmployeeListPage } from "@pages/employee-list";
import { EmployeeProfilePage } from "@pages/employee-profile";
import { EmployeeEditPage } from "@pages/employee-edit";
import { matchPath } from "@shared/lib/react-router-dom";

function RoutedContent() {
  const { pathname } = useLocation();

  if (pathname === "/") return <HomePage />;
  if (pathname === "/shop") return <ShopPage />;
  if (pathname === "/availability") return <AvailabilityPage />;
  if (pathname === "/container") return <ContainerPage />;
  if (pathname === "/information") return <InformationPage />;
  if (pathname === "/database") return <DataBasePage />;
  if (pathname === "/employee") return <EmployeeListPage />;
  if (pathname === "/employee/new") return <EmployeeEditPage />;
  if (matchPath(pathname, "/employee/:employeeId/edit")) return <EmployeeEditPage />;
  if (matchPath(pathname, "/employee/:employeeId")) return <EmployeeProfilePage />;

  return <Navigate to="/" />;
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <OverlaySidebarLayout>
        <RoutedContent />
      </OverlaySidebarLayout>
    </BrowserRouter>
  );
}
