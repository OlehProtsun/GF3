import { useMemo, useState } from "react";
import type { FormEvent } from "react";
import {
  useCreateEmployeeMutation,
  useDeleteEmployeeMutation,
  useEmployeesListQuery,
  useUpdateEmployeeMutation,
} from "@entities/employees";
import type { Employee, SaveEmployeeInput } from "@entities/employees";
import { ApiError } from "@shared/api/httpClient";

type EmployeeFormState = {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
};

const initialFormState: EmployeeFormState = {
  firstName: "",
  lastName: "",
  email: "",
  phone: "",
};

function toPayload(form: EmployeeFormState): SaveEmployeeInput {
  return {
    firstName: form.firstName,
    lastName: form.lastName,
    email: form.email,
    phone: form.phone,
  };
}

function toFormState(employee: Employee): EmployeeFormState {
  return {
    firstName: employee.firstName,
    lastName: employee.lastName,
    email: employee.email ?? "",
    phone: employee.phone ?? "",
  };
}

export function EmployeesPage() {
  const [search, setSearch] = useState("");
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<EmployeeFormState>(initialFormState);

  const employeesQuery = useEmployeesListQuery({ search });
  const createMutation = useCreateEmployeeMutation();
  const updateMutation = useUpdateEmployeeMutation();
  const deleteMutation = useDeleteEmployeeMutation();

  const employees = employeesQuery.data ?? [];
  const normalizedSearch = search.trim().toLowerCase();

  const filteredEmployees = useMemo(() => {
    if (!normalizedSearch) return employees;

    return employees.filter((employee) => {
      const fullName = `${employee.firstName} ${employee.lastName}`.toLowerCase();
      return (
        fullName.includes(normalizedSearch)
        || employee.email?.toLowerCase().includes(normalizedSearch)
        || employee.phone?.toLowerCase().includes(normalizedSearch)
      );
    });
  }, [employees, normalizedSearch]);

  const resetForm = () => {
    setForm(initialFormState);
    setEditingId(null);
  };

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (editingId === null) {
      createMutation.mutate(toPayload(form), { onSuccess: resetForm });
      return;
    }

    updateMutation.mutate(
      { id: editingId, payload: toPayload(form) },
      { onSuccess: resetForm },
    );
  };

  const onEdit = (employee: Employee) => {
    setEditingId(employee.id);
    setForm(toFormState(employee));
  };

  const onDelete = (employee: Employee) => {
    const shouldDelete = window.confirm(`Delete employee \"${employee.firstName} ${employee.lastName}\"?`);
    if (!shouldDelete) return;
    deleteMutation.mutate(employee.id);
  };

  const mutationError = (createMutation.error ?? updateMutation.error ?? deleteMutation.error) as ApiError | null;

  return (
    <main>
      <h1>Employees</h1>

      <form onSubmit={onSubmit}>
        <h2>{editingId === null ? "Add employee" : `Edit employee #${editingId}`}</h2>
        <input
          placeholder="First name"
          value={form.firstName}
          onChange={(event) => setForm((prev) => ({ ...prev, firstName: event.target.value }))}
          required
        />
        <input
          placeholder="Last name"
          value={form.lastName}
          onChange={(event) => setForm((prev) => ({ ...prev, lastName: event.target.value }))}
          required
        />
        <input
          placeholder="Email"
          value={form.email}
          onChange={(event) => setForm((prev) => ({ ...prev, email: event.target.value }))}
        />
        <input
          placeholder="Phone"
          value={form.phone}
          onChange={(event) => setForm((prev) => ({ ...prev, phone: event.target.value }))}
        />

        <button type="submit" disabled={createMutation.isPending || updateMutation.isPending}>
          {editingId === null ? "Add" : "Save"}
        </button>
        {editingId !== null ? (
          <button type="button" onClick={resetForm}>Cancel</button>
        ) : null}
      </form>

      <input
        placeholder="Search by name/email/phone"
        value={search}
        onChange={(event) => setSearch(event.target.value)}
      />

      {mutationError ? <p className="error">Operation failed: {mutationError.message}</p> : null}

      {employeesQuery.isLoading ? <p>Loading employees...</p> : null}
      {employeesQuery.error ? <p className="error">Failed to load employees.</p> : null}
      {!employeesQuery.isLoading && !employeesQuery.error && filteredEmployees.length === 0 ? (
        <p>No employees found.</p>
      ) : null}

      {filteredEmployees.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>First Name</th>
              <th>Last Name</th>
              <th>Email</th>
              <th>Phone</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredEmployees.map((employee) => (
              <tr key={employee.id}>
                <td>{employee.id}</td>
                <td>{employee.firstName}</td>
                <td>{employee.lastName}</td>
                <td>{employee.email ?? "-"}</td>
                <td>{employee.phone ?? "-"}</td>
                <td>
                  <button type="button" onClick={() => onEdit(employee)}>Edit</button>
                  <button type="button" onClick={() => onDelete(employee)} disabled={deleteMutation.isPending}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </main>
  );
}
