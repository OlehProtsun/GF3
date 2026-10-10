import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useEmployeesQuery } from "@entities/employees/model/queries";
import { EmployeeListCard } from "@entities/employees/ui/EmployeeListCard";
import { queryKeys } from "@shared/api/queryKeys";
import { ManagerPhonePage } from "./ManagerPhonePage";

export function ManagerPhoneEmployeesPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const employees = useEmployeesQuery(query, location.key);
  return <ManagerPhonePage query={query} onQueryChange={setQuery} queries={[employees]} queryKeys={[queryKeys.employees.all]}>
    <EmployeeListCard employees={employees.data ?? []} isLoading={false} searchQuery={query} onClearSearch={() => setQuery("")} onEmployeeOpen={id => navigate(`/employee/${id}`)} />
  </ManagerPhonePage>;
}
