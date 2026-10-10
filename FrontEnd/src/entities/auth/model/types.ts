export type AuthRole = "manager" | "employee";
export type ManagerWorkspaceMode = "choose" | "pc" | "phone";

export type AuthSession = {
  workspaceMode?: ManagerWorkspaceMode | null;
  role: AuthRole;
  isSystemManager?: boolean;
  userName: string;
  displayName: string;
  managerId?: number | null;
  employeeId?: number | null;
};

export type AuthLoginResult = {
  accessToken: string;
  expiresAtUtc: string;
  session: AuthSession;
};

export type LoginInput = {
  username: string;
  password: string;
};

export type PasswordResetCodeDispatch = {
  deliveryHint: string;
  expiresAtUtc: string;
};

export type SendPasswordResetCodeInput = {
  username: string;
};

export type CompleteForgotPasswordResetInput = {
  username: string;
  code: string;
  newPassword: string;
};
