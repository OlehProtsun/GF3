import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useEmployeesQuery } from "@entities/employees/model/queries";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./EmployeeListPage.module.css";

export function EmployeeListPage() {
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [selectedId, setSelectedId] = useState<string | number | null>(null);
  const employeesQuery = useEmployeesQuery(query);
  const employees = employeesQuery.data ?? [];

  const hasEmployees = (employees?.length ?? 0) > 0;

  // показуємо помилку тільки коли:
  // - реально error
  // - НЕ йде завантаження/рефетч
  // - і при цьому немає даних
  const showEmployeesError =
    Boolean(employeesQuery.error) && !employeesQuery.isFetching && !hasEmployees;


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

      <section className={styles.card}>
        <div className={styles.cardTopBar}>
          <button
            type="button"
            className={styles.addButton}
            onClick={() => navigate("/employee/new")}
          >
            <svg
              className={styles.addButtonIcon}
              viewBox="0 0 24 24"
              aria-hidden="true"
              focusable="false"
            >
              <path d="M12 5v14M5 12h14" />
            </svg>
            <span>Add New</span>
          </button>

          <div className={styles.totalBadge}>Total: {employees.length}</div>
        </div>

        {employeesQuery.isLoading ? <p>Loading...</p> : null}
        {showEmployeesError ? (
          <div className={styles.errorWrap}>
            <div className={styles.errorBanner} role="alert" aria-live="polite">
              <svg className={styles.errorIcon} viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                <path d="M12 9v4M12 17h.01" />
                <path d="M10.29 3.86 2.17 17.92A2 2 0 0 0 3.9 21h16.2a2 2 0 0 0 1.73-3.08L13.71 3.86a2 2 0 0 0-3.42 0Z" />
              </svg>

              <span className={styles.errorText}>Could not load employees.</span>
            </div>
          </div>
        ) : null}
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
                onClick={() => navigate(`/employee/${employee.id}`)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    setSelectedId(employee.id);
                    navigate(`/employee/${employee.id}`);
                  }
                }}
              >
                <td>{employee.firstName}</td>
                <td>{employee.lastName}</td>
              </tr>            
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}
