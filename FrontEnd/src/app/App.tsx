import { useEffect, useState } from "react";
import { DashboardPage } from "@pages/dashboard/ui/DashboardPage";
import { EmployeesPage } from "@pages/employees/ui/EmployeesPage";

function getPathname() {
  return window.location.pathname;
}

function navigate(pathname: string) {
  if (window.location.pathname === pathname) return;
  window.history.pushState({}, "", pathname);
  window.dispatchEvent(new PopStateEvent("popstate"));
}

export function App() {
  const [pathname, setPathname] = useState(getPathname);

  useEffect(() => {
    const onPopState = () => setPathname(getPathname());
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, []);

  return (
    <>
      <nav>
        <a
          href="/"
          onClick={(event) => {
            event.preventDefault();
            navigate("/");
          }}
        >
          Dashboard
        </a>{" "}
        |{" "}
        <a
          href="/employees"
          onClick={(event) => {
            event.preventDefault();
            navigate("/employees");
          }}
        >
          Employees
        </a>
      </nav>

      {pathname === "/employees" ? <EmployeesPage /> : <DashboardPage />}
    </>
  );
}
