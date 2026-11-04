import { t } from "@shared/i18n";
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
  const onlineCount = employees.filter(employee => employee.isOnline).length;

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
        title={t("Employee List")}
        subtitle={t("Browse and search employee records")}
        backTo="/"
        rightSlot={
          <IosButton label={t("Add New")} icon={<PlusIcon size={18} />} onClick={handleAddEmployee} />
        }
        searchMeta={t("Total: {0} | Online: {1}", employees.length, onlineCount)}
        search={{
          value: query,
          onChange: setQuery,
          placeholder: t("Search employee"),
          ariaLabel: t("Search employee"),
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
