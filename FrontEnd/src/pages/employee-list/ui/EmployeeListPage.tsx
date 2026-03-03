import { useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useEmployeesQuery } from "@entities/employees/model/queries";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./EmployeeListPage.module.css";

export function EmployeeListPage() {
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState("");
  const [searchText, setSearchText] = useState("");
  const employeesQuery = useEmployeesQuery(searchText);
  const employees = employeesQuery.data ?? [];

  const onSearch = (event: FormEvent) => {
    event.preventDefault();
    setSearchText(searchInput);
  };

  return (
    <div>
      <PageHeader title="Employee List" subtitle="Browse and search employee records" backTo="/" />
      <section className={styles.card}>
        <form className={styles.toolbar} onSubmit={onSearch}>
          <input value={searchInput} onChange={(event) => setSearchInput(event.target.value)} placeholder="Search employee" />
          <button type="submit">Search</button>
          <button type="button" onClick={() => navigate("/employee/new")}>Add New</button>
        </form>

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
