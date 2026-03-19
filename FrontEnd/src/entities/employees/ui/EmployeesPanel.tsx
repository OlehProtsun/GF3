import { useState } from "react";
import type { FormEvent } from "react";
import { ApiError } from "@shared/api/httpClient";
import {
  useCreateEmployeeMutation,
  useDeleteEmployeeMutation,
  useEmployeesListQuery,
} from "@entities/employees/api/queries";

export function EmployeesPanel() {
  const employeesQuery = useEmployeesListQuery();
  const createMutation = useCreateEmployeeMutation();
  const deleteMutation = useDeleteEmployeeMutation();

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    createMutation.mutate(
      { firstName, lastName, email, phone },
      {
        onSuccess: () => {
          setFirstName("");
          setLastName("");
          setEmail("");
          setPhone("");
        },
      },
    );
  };

  const createError = createMutation.error as ApiError | null;

  return (
    <section>
      <h2>Employees</h2>
      <form onSubmit={onSubmit}>
        <input placeholder="First name" value={firstName} onChange={(event) => setFirstName(event.target.value)} required />
        <input placeholder="Last name" value={lastName} onChange={(event) => setLastName(event.target.value)} required />
        <input placeholder="Email" value={email} onChange={(event) => setEmail(event.target.value)} />
        <input placeholder="Phone" value={phone} onChange={(event) => setPhone(event.target.value)} />
        <button type="submit" disabled={createMutation.isPending}>
          {createMutation.isPending ? "Saving..." : "Add employee"}
        </button>
      </form>

      {createError ? <p className="error">Create failed: {createError.message}</p> : null}

      {employeesQuery.isLoading ? <p>Loading employees...</p> : null}
      {employeesQuery.error ? <p className="error">Could not load employees.</p> : null}

      <ul>
        {employeesQuery.data?.map((employee) => (
          <li key={employee.id}>
            <strong>
              {employee.firstName} {employee.lastName}
            </strong>{" "}
            <span>{employee.email ?? "no email"}</span>
            <button type="button" onClick={() => deleteMutation.mutate(employee.id)} disabled={deleteMutation.isPending}>
              Delete
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
