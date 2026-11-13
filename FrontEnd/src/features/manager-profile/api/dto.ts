import type { AuthLoginResult } from "@entities/auth";

export type ManagerProfileDto = {
  id: number;
  userName: string;
  displayName: string;
  recoveryEmail?: string | null;
  lastLoginAtUtc?: string | null;
  isOnline: boolean;
  isSystem: boolean;
  createdAtUtc: string;
};

export type UpdateManagerProfileDto = {
  userName: string;
  displayName: string;
  recoveryEmail?: string;
  newPassword?: string;
};

export type CreateManagerDto = {
  userName: string;
  displayName: string;
  recoveryEmail?: string;
  password: string;
};

export type ManagerProfileUpdateResponseDto = AuthLoginResult & {
  profile: ManagerProfileDto;
};
