import { useCallback, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useEmployeesQuery } from "@entities/employees/model/queries";
import { EmployeeListCard } from "@entities/employees/ui/EmployeeListCard";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { IosButton } from "@shared/ui/components/IosButton";
import { PageHeader } from "@shared/ui/PageHeader";
import { PlusIcon } from "@shared/ui/icons";
import styles from "./EmployeeListPage.module.css";

export function EmployeeListPage() {
  usePageScrollbarHidden();

  const location = useLocation();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const employeesQuery = useEmployeesQuery(query, location.key);
  const employees = employeesQuery.data ?? [];

  const handleAddEmployee = useCallback(() => {
    navigate("/employee/new");
  }, [navigate]);

  const handleEmployeeOpen = useCallback(
    (employeeId: number) => {
      navigate(`/employee/${employeeId}`);
    },
    [navigate]
  );

  return (
    <div className={styles.page}>
      <PageHeader
        title="Employee List"
        subtitle="Browse and search employee records"
        backTo="/"
        rightSlot={
          <IosButton label="Add New" icon={<PlusIcon size={18} />} onClick={handleAddEmployee} />
        }
        searchMeta={`Total: ${employees.length}`}
        search={{
          value: query,
          onChange: setQuery,
          placeholder: "Search employee",
          ariaLabel: "Search employee",
        }}
      />

      <EmployeeListCard
        employees={employees}
        error={employeesQuery.error}
        isLoading={employeesQuery.isLoading}
        searchQuery={query}
        onClearSearch={() => setQuery("")}
        onAddEmployee={handleAddEmployee}
        onEmployeeOpen={handleEmployeeOpen}
      />
    </div>
  );
}
