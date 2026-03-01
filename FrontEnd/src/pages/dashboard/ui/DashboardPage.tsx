import { EmployeesPanel } from "@entities/employees/ui/EmployeesPanel";
import { ShopsPanel } from "@entities/shops/ui/ShopsPanel";

export function DashboardPage() {
  return (
    <main>
      <h1>GF3 API integration demo</h1>
      <p>Employees and Shops are now loaded from the ASP.NET Core API via TanStack Query.</p>
      <div className="layout">
        <EmployeesPanel />
        <ShopsPanel />
      </div>
    </main>
  );
}
