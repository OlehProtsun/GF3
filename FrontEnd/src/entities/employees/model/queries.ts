import { useMemo } from "react";
import { useEmployeesListQuery } from "@entities/employees/api/queries";

export function useEmployeesQuery(searchText: string) {
  const query = useEmployeesListQuery({ search: searchText });

  const data = useMemo(() => {
    const employees = query.data ?? [];
    const search = searchText.trim().toLowerCase();
    if (!search) return employees;

    return employees.filter((employee) =>
      `${employee.firstName} ${employee.lastName}`.toLowerCase().includes(search),
    );
  }, [query.data, searchText]);

  return { ...query, data };
}
