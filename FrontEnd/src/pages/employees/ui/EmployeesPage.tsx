import { useEmployeesListQuery } from "@entities/employees";

export function EmployeesPage() {
  const { data, isLoading, error } = useEmployeesListQuery();

  if (isLoading) {
    return <main><h1>Employees</h1><p>Loading...</p></main>;
  }

  if (error) {
    return <main><h1>Employees</h1><p>Failed to load employees.</p></main>;
  }

  if (!data || data.length === 0) {
    return <main><h1>Employees</h1><p>No employees found.</p></main>;
  }

  return (
    <main>
      <h1>Employees</h1>
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>First Name</th>
            <th>Last Name</th>
            <th>Email</th>
            <th>Phone</th>
          </tr>
        </thead>
        <tbody>
          {data.map((employee) => (
            <tr key={employee.id}>
              <td>{employee.id}</td>
              <td>{employee.firstName}</td>
              <td>{employee.lastName}</td>
              <td>{employee.email ?? "-"}</td>
              <td>{employee.phone ?? "-"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </main>
  );
}
