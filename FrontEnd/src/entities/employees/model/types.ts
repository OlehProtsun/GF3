export type EmploymentContractType = "zlecenie" | "o_prace";

export type Employee = {
  contractType?: EmploymentContractType;
  id: number;
  firstName: string;
  lastName: string;
  phone?: string | null;
  email?: string | null;
  username?: string | null;
  hasLoginAccount: boolean;
  isOnline: boolean;
  lastLoginAtUtc?: string | null;
};

export type EmployeePresenceUpdate = {
  employeeId: number;
  isOnline: boolean;
  changedAtUtc: string;
  lastLoginAtUtc?: string | null;
};

export type SaveEmployeeInput = {
  contractType?: EmploymentContractType;
  firstName: string;
  lastName: string;
  phone?: string;
  email?: string;
  username?: string;
  password?: string;
};

export type EmployeesListParams = {
  search?: string;
  refreshKey?: string;
};
