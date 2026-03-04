import { useCallback, useMemo, useState, type KeyboardEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useEmployeesQuery } from "@entities/employees/model/queries";
import { PageHeader } from "@shared/ui/PageHeader";
import { PlusIcon } from "@shared/ui/icons";
import { IosButton } from "@shared/ui/components/IosButton";
import { ListCardSection } from "@shared/ui/components/ListCardSection";
import styles from "./EmployeeListPage.module.css";

export function EmployeeListPage() {
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [selectedId, setSelectedId] = useState<string | number | null>(null);
  const employeesQuery = useEmployeesQuery(query);

  const employees = useMemo(() => employeesQuery.data ?? [], [employeesQuery.data]);
  const hasEmployees = employees.length > 0;

  const handleAddEmployee = useCallback(() => {
    navigate("/employee/new");
  }, [navigate]);

  const handleEmployeeOpen = useCallback(
    (employeeId: string | number) => {
      navigate(`/employee/${employeeId}`);
    },
    [navigate]
  );

  const handleEmployeeKeyDown = useCallback(
    (event: KeyboardEvent<HTMLTableRowElement>, employeeId: string | number) => {
      if (event.key === "Enter" || event.key === " ") {
        event.preventDefault();
        setSelectedId(employeeId);
        handleEmployeeOpen(employeeId);
      }
    },
    [handleEmployeeOpen]
  );

  return (
    <div className={styles.page}>
      <PageHeader
        title="Employee List"
        subtitle="Browse and search employee records"
        backTo="/"
        search={{
          value: query,
          onChange: setQuery,
          placeholder: "Search employee",
          ariaLabel: "Search employee",
        }}
      />

      <ListCardSection
        totalCount={employees.length}
        error={employeesQuery.error}
        isFetching={employeesQuery.isLoading}
        hasData={hasEmployees}
        errorMessage="Could not load employees."
        actionSlot={<IosButton label="Add New" icon={<PlusIcon size={18} />} onClick={handleAddEmployee} />}
      >
        {employeesQuery.isLoading ? <p>Loading...</p> : null}

        <table className={styles.table}>
          <thead>
            <tr>
              <th>First Name</th>
              <th>Last Name</th>
            </tr>
          </thead>
          <tbody>
            {employees.map((employee) => (
              <tr
                key={employee.id}
                tabIndex={0}
                role="button"
                className={`${styles.row} ${styles.clickable} ${
                  selectedId === employee.id ? styles.rowSelected : ""
                }`}
                onMouseDown={() => setSelectedId(employee.id)}
                onClick={() => handleEmployeeOpen(employee.id)}
                onKeyDown={(event) => handleEmployeeKeyDown(event, employee.id)}
              >
                <td>{employee.firstName}</td>
                <td>{employee.lastName}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </ListCardSection>
    </div>
  );
}
