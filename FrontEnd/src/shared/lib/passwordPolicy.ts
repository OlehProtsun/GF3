export const PASSWORD_LENGTH = 6;
export const PASSWORD_VALIDATION_MESSAGE = "Password must contain exactly 6 digits.";

export function sanitizePassword(value: string): string {
  return value.replace(/\D/g, "").slice(0, PASSWORD_LENGTH);
}

export function isValidPassword(value: string): boolean {
  return /^\d{6}$/.test(value);
}
