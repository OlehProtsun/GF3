export type EmployeeProfileDto = {
  contractType?: "zlecenie" | "o_prace";
  employeeId: number;
  username: string;
  displayName: string;
  recoveryEmail?: string | null;
  phone?: string | null;
};

export type UpdateEmployeeProfileDto = {
  recoveryEmail?: string;
  phone?: string;
};

export type PasswordResetCodeDispatchDto = {
  deliveryHint: string;
  expiresAtUtc: string;
};

export type CompletePasswordResetDto = {
  code: string;
  newPassword: string;
};
