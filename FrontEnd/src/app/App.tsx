import { EmployeesPage } from "@pages/employees/ui/EmployeesPage";

function Navigation() {
  return (
    <nav>
      <a href="/employees">Employees</a>
    </nav>
  );
}

export function App() {
  const path = window.location.pathname;

  return (
    <>
      <Navigation />
      {path === "/employees" ? <EmployeesPage /> : <EmployeesPage />}
    </>
  );
}
