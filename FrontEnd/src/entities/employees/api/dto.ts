export type EmployeeDto = {
  id: number;
  firstName: string;
  lastName: string;
  phone?: string | null;
  email?: string | null;
};

export type SaveEmployeeDto = {
  firstName: string;
  lastName: string;
  phone?: string;
  email?: string;
};
