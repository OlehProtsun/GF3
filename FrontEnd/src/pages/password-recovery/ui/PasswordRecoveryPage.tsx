import { useEffect, useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { authApi } from "@entities/auth";
import { ApiError, getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { LabeledField, TextInput } from "@shared/ui/forms/Field";
import styles from "@pages/login/ui/LoginPage.module.css";

type RecoveryFormErrors = {
  username?: string;
  recoveryEmail?: string;
  code?: string;
  newPassword?: string;
};

function getInitialUsername(search: string) {
  return new URLSearchParams(search).get("username")?.trim() ?? "";
}

function getValidationMessage(error: unknown, field: string) {
  if (!(error instanceof ApiError)) {
    return undefined;
  }

  return error.validationErrors?.[field]?.find((message) => message.trim().length > 0);
}

function hasFormErrors(errors: RecoveryFormErrors) {
  return Object.values(errors).some((message) => Boolean(message));
}

export function PasswordRecoveryPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [username, setUsername] = useState(() => getInitialUsername(location.search));
  const [passwordCode, setPasswordCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [deliveryHint, setDeliveryHint] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [formErrors, setFormErrors] = useState<RecoveryFormErrors>({});
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSendingCode, setIsSendingCode] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);

  useEffect(() => {
    const htmlElement = document.documentElement;
    const bodyElement = document.body;
    const rootElement = document.getElementById("root");

    htmlElement.classList.add("login-page-active");
    bodyElement.classList.add("login-page-active");
    rootElement?.classList.add("login-page-active");

    return () => {
      htmlElement.classList.remove("login-page-active");
      bodyElement.classList.remove("login-page-active");
      rootElement?.classList.remove("login-page-active");
    };
  }, []);

  const normalizedUsername = username.trim();
  const normalizedCode = passwordCode.trim();
  const canSendCode = normalizedUsername.length > 0 && !isSendingCode && !isConfirming;
  const canConfirmPassword =
    normalizedUsername.length > 0 &&
    normalizedCode.length === 6 &&
    /^\d{6}$/.test(newPassword.trim()) &&
    !isSendingCode &&
    !isConfirming;

  const handleSendCode = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!normalizedUsername) {
      setFormErrors({ username: "Enter the username for this account." });
      setSubmitError(null);
      return;
    }

    setIsSendingCode(true);
    setSubmitError(null);
    setFormErrors({});
    setSuccessMessage(null);

    try {
      const result = await authApi.sendPasswordResetCode({ username: normalizedUsername });
      setDeliveryHint(result.deliveryHint);
      setPasswordCode("");
      setNewPassword("");
      setSuccessMessage(`Code sent to ${result.deliveryHint}.`);
    } catch (error) {
      const nextErrors: RecoveryFormErrors = {
        username: getValidationMessage(error, "username"),
        recoveryEmail: getValidationMessage(error, "recoveryEmail"),
      };

      setFormErrors(nextErrors);
      setSubmitError(hasFormErrors(nextErrors) ? null : getErrorMessage(error, "Could not send the password code."));
    } finally {
      setIsSendingCode(false);
    }
  };

  const handleConfirmPassword = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const nextErrors: RecoveryFormErrors = {};
    if (!normalizedUsername) {
      nextErrors.username = "Enter the username for this account.";
    }
    if (normalizedCode.length !== 6) {
      nextErrors.code = "Enter the 6-digit code from your email.";
    }
    if (!/^\d{6}$/.test(newPassword.trim())) {
      nextErrors.newPassword = "Password must contain exactly 6 digits.";
    }

    if (Object.keys(nextErrors).length > 0) {
      setFormErrors(nextErrors);
      setSubmitError(null);
      return;
    }

    setIsConfirming(true);
    setSubmitError(null);
    setFormErrors({});
    setSuccessMessage(null);

    try {
      await authApi.confirmPasswordReset({
        username: normalizedUsername,
        code: normalizedCode,
        newPassword,
      });

      setPasswordCode("");
      setNewPassword("");
      setDeliveryHint(null);
      setSuccessMessage("Password updated. You can sign in now.");
    } catch (error) {
      const nextErrors: RecoveryFormErrors = {
        username: getValidationMessage(error, "username"),
        recoveryEmail: getValidationMessage(error, "recoveryEmail"),
        code: getValidationMessage(error, "code"),
        newPassword: getValidationMessage(error, "newPassword"),
      };

      setFormErrors(nextErrors);
      setSubmitError(hasFormErrors(nextErrors) ? null : getErrorMessage(error, "Could not update the password."));
    } finally {
      setIsConfirming(false);
    }
  };

  const handleCodeChange = (value: string) => {
    setPasswordCode(value.replace(/\D/g, "").slice(0, 6));
  };

  return (
    <div className={styles.page}>
      <div className={styles.frame}>
        <section className={styles.card}>
          <div className={styles.cardHeader}>
            <span className={styles.cardEyebrow}>Password recovery</span>
            <h2 className={styles.cardTitle}>Reset password</h2>
            <p className={styles.cardSubtitle}>A code will be sent to the recovery email saved on your account.</p>
          </div>

          {submitError ? <ErrorBanner className={styles.banner}>{submitError}</ErrorBanner> : null}
          {formErrors.recoveryEmail ? <ErrorBanner className={styles.banner}>{formErrors.recoveryEmail}</ErrorBanner> : null}
          {successMessage ? <div className={styles.successBanner}>{successMessage}</div> : null}

          <form className={styles.form} onSubmit={handleSendCode}>
            <LabeledField id="recovery-username" label="Username" error={formErrors.username}>
              <TextInput
                id="recovery-username"
                autoComplete="username"
                value={username}
                placeholder="Example User"
                onChange={(event) => {
                  setUsername(event.target.value);
                  setDeliveryHint(null);
                }}
              />
            </LabeledField>

            <div className={styles.actions}>
              <IosButton
                label={isSendingCode ? "Sending..." : deliveryHint ? "Send code again" : "Send code"}
                type="submit"
                disabled={!canSendCode}
                className={styles.submitButton}
              />
            </div>
          </form>

          {deliveryHint ? <div className={styles.helperText}>Last code sent to {deliveryHint}.</div> : null}

          <form className={styles.form} onSubmit={handleConfirmPassword}>
            <LabeledField id="recovery-code" label="Code" error={formErrors.code}>
              <TextInput
                id="recovery-code"
                inputMode="numeric"
                maxLength={6}
                value={passwordCode}
                placeholder="123456"
                onChange={(event) => handleCodeChange(event.target.value)}
              />
            </LabeledField>

            <LabeledField id="recovery-new-password" label="New password" error={formErrors.newPassword}>
              <TextInput
                id="recovery-new-password"
                type="password"
                inputMode="numeric"
                pattern="[0-9]*"
                minLength={6}
                maxLength={6}
                autoComplete="654321"
                value={newPassword}
                placeholder="Exactly 6 digits"
                onChange={(event) => setNewPassword(event.target.value.replace(/\D/g, "").slice(0, 6))}
              />
            </LabeledField>

            <div className={styles.actions}>
              <IosButton
                label={isConfirming ? "Updating..." : "Change password"}
                type="submit"
                disabled={!canConfirmPassword}
                className={styles.submitButton}
              />
            </div>
          </form>

          <div className={styles.secondaryActions}>
            <button type="button" className={styles.textButton} onClick={() => navigate("/login")}>
              Back to sign in
            </button>
          </div>
        </section>
      </div>
    </div>
  );
}
