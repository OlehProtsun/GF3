import { request } from "@shared/api/httpClient";
import type {
  AuthLoginResult,
  AuthSession,
  CompleteForgotPasswordResetInput,
  LoginInput,
  PasswordResetCodeDispatch,
  SendPasswordResetCodeInput,
} from "@entities/auth/model/types";

type SessionDto = {
  role: AuthSession["role"];
  userName: string;
  displayName: string;
  employeeId?: number | null;
};

type LoginResponseDto = {
  accessToken: string;
  expiresAtUtc: string;
  session: SessionDto;
};

function toSessionModel(dto: SessionDto): AuthSession {
  return {
    role: dto.role,
    userName: dto.userName,
    displayName: dto.displayName,
    employeeId: dto.employeeId ?? null,
  };
}

function toLoginResultModel(dto: LoginResponseDto): AuthLoginResult {
  return {
    accessToken: dto.accessToken,
    expiresAtUtc: dto.expiresAtUtc,
    session: toSessionModel(dto.session),
  };
}

function toLoginDto(input: LoginInput) {
  return {
    username: input.username.trim(),
    password: input.password,
  };
}

export const authApi = {
  login: async (input: LoginInput) =>
    toLoginResultModel(await request<LoginResponseDto>("auth/login", { method: "POST", body: toLoginDto(input) })),
  session: async () => toSessionModel(await request<SessionDto>("auth/session")),
  logout: () => request<void>("auth/logout", { method: "POST" }),
  sendPasswordResetCode: (input: SendPasswordResetCodeInput) =>
    request<PasswordResetCodeDispatch>("auth/password/send-code", {
      method: "POST",
      body: {
        username: input.username.trim(),
      },
    }),
  confirmPasswordReset: (input: CompleteForgotPasswordResetInput) =>
    request<void>("auth/password/confirm", {
      method: "POST",
      body: {
        username: input.username.trim(),
        code: input.code.trim(),
        newPassword: input.newPassword,
      },
    }),
};
