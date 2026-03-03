import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useEmployeesQuery } from "@entities/employees/model/queries";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./EmployeeListPage.module.css";

export function EmployeeListPage() {
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const employeesQuery = useEmployeesQuery(query);
  const employees = employeesQuery.data ?? [];

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
        <div className={styles.actionsRow}>
          <button type="button" onClick={() => navigate("/employee/new")}>Add New</button>
        </div>

        <div className={styles.badge}>Total: {employees.length}</div>

        {employeesQuery.isLoading ? <p>Loading...</p> : null}
        {employeesQuery.error ? <p className={styles.error}>Could not load employees.</p> : null}

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
                className={styles.clickable}
                onDoubleClick={() => navigate(`/employee/${employee.id}`)}
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
