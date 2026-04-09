import { useDeferredValue, useMemo } from "react";
import { useEmployeesListQuery } from "@entities/employees/api/queries";

export function useEmployeesQuery(searchText: string, refreshKey?: string) {
  const query = useEmployeesListQuery({ search: searchText, refreshKey });
  const deferredSearchText = useDeferredValue(searchText);

  const data = useMemo(() => {
    const employees = query.data ?? [];
    const search = deferredSearchText.trim().toLowerCase();
    if (!search) return employees;

    return employees.filter((employee) =>
      `${employee.firstName} ${employee.lastName}`.toLowerCase().includes(search),
    );
  }, [deferredSearchText, query.data]);

  return { ...query, data };
}
