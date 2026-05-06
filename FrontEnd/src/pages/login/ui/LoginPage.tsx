import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { getErrorMessage } from "@shared/api/httpClient";
import { IosButton } from "@shared/ui/components/IosButton";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { LabeledField, TextInput } from "@shared/ui/forms/Field";
import styles from "./LoginPage.module.css";

export function LoginPage() {
  const { bootstrapError, login } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

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

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);
    setIsSubmitting(true);

    try {
      await login({ username, password });
    } catch (error) {
      setSubmitError(getErrorMessage(error, "Could not sign you in."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenPasswordRecovery = () => {
    const normalizedUsername = username.trim();
    const recoveryPath = normalizedUsername
      ? `/password-recovery?username=${encodeURIComponent(normalizedUsername)}`
      : "/password-recovery";

    navigate(recoveryPath);
  };

  return (
    <div className={styles.page}>
      <div className={styles.frame}>
        <section className={styles.card}>
          <div className={styles.cardHeader}>
            <span className={styles.cardEyebrow}>Access to GF</span>
            <h2 className={styles.cardTitle}>Sign in</h2>
            <p className={styles.cardSubtitle}>Use the credentials created for your role.</p>
          </div>

          {bootstrapError ? <ErrorBanner className={styles.banner}>{bootstrapError}</ErrorBanner> : null}
          {submitError ? <ErrorBanner className={styles.banner}>{submitError}</ErrorBanner> : null}

          <form className={styles.form} onSubmit={handleSubmit}>
            <LabeledField id="login-username" label="Username">
              <TextInput
                id="login-username"
                autoComplete="username"
                value={username}
                placeholder="Example User"
                onChange={(event) => setUsername(event.target.value)}
              />
            </LabeledField>

            <LabeledField id="login-password" label="Password">
              <TextInput
                id="login-password"
                type="password"
                autoComplete="current-password"
                value={password}
                placeholder="********"
                onChange={(event) => setPassword(event.target.value)}
              />
            </LabeledField>

            <div className={styles.inlineRecoveryAction}>
              <button type="button" className={styles.textButton} onClick={handleOpenPasswordRecovery}>
                Forgot password?
              </button>
            </div>

            <div className={styles.actions}>
              <IosButton
                label={isSubmitting ? "Signing in..." : "Sign in"}
                type="submit"
                disabled={isSubmitting || !username.trim() || !password}
                className={styles.submitButton}
              />
            </div>
          </form>
        </section>
      </div>
    </div>
  );
}
