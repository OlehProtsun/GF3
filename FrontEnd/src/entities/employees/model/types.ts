export type Employee = {
  id: number;
  firstName: string;
  lastName: string;
  phone?: string | null;
  email?: string | null;
};

export type SaveEmployeeInput = {
  firstName: string;
  lastName: string;
  phone?: string;
  email?: string;
};

export type EmployeesListParams = {
  search?: string;
};
