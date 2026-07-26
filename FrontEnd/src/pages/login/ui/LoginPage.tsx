import { useEffect, useRef, useState, type FormEvent, type PointerEvent as ReactPointerEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@app/providers/AuthProvider";
import { getErrorMessage } from "@shared/api/httpClient";
import { isValidPassword, PASSWORD_LENGTH, PASSWORD_VALIDATION_MESSAGE, sanitizePassword } from "@shared/lib/passwordPolicy";
import { IosButton } from "@shared/ui/components/IosButton";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { LabeledField, TextInput } from "@shared/ui/forms/Field";
import styles from "./LoginPage.module.css";

type PasswordMode = "pc" | "phone";

const USERNAME_STORAGE_KEY = "gf3.auth.last-username";
const PASSWORD_MODE_STORAGE_KEY = "gf3.auth.password-mode";
const KEYPAD_DIGITS = ["1", "2", "3", "4", "5", "6", "7", "8", "9"];
const PHONE_VIEWPORT_CONTENT = "width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover";

function readStoredValue(key: string) {
  if (typeof window === "undefined") return "";
  try {
    return window.localStorage.getItem(key) ?? "";
  } catch {
    return "";
  }
}

function persistValue(key: string, value: string) {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    // Login still works when browser storage is unavailable.
  }
}

function readStoredPasswordMode(): PasswordMode {
  return readStoredValue(PASSWORD_MODE_STORAGE_KEY) === "phone" ? "phone" : "pc";
}

export function LoginPage() {
  const { bootstrapError, login } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState(() => readStoredValue(USERNAME_STORAGE_KEY));
  const [passwordMode, setPasswordMode] = useState<PasswordMode>(readStoredPasswordMode);
  const [password, setPassword] = useState("");
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [failedPinAttempts, setFailedPinAttempts] = useState(0);
  const passwordRef = useRef("");
  const lastPinPointerActionRef = useRef<{ key: string; at: number } | null>(null);

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

  useEffect(() => {
    if (passwordMode !== "phone") return;

    const htmlElement = document.documentElement;
    const bodyElement = document.body;
    const rootElement = document.getElementById("root");
    const viewportMeta = document.querySelector<HTMLMetaElement>('meta[name="viewport"]');
    const previousViewportContent = viewportMeta?.getAttribute("content") ?? null;
    const previousBodyStyles = {
      position: bodyElement.style.position,
      inset: bodyElement.style.inset,
      width: bodyElement.style.width,
      overflow: bodyElement.style.overflow,
      top: bodyElement.style.top,
    };
    const scrollY = window.scrollY;
    const preventGesture = (event: Event) => event.preventDefault();
    const preventPinch = (event: TouchEvent) => {
      if (event.touches.length > 1) event.preventDefault();
    };

    htmlElement.classList.add("login-phone-mode");
    bodyElement.classList.add("login-phone-mode");
    rootElement?.classList.add("login-phone-mode");
    bodyElement.style.position = "fixed";
    bodyElement.style.inset = "0";
    bodyElement.style.width = "100%";
    bodyElement.style.overflow = "hidden";
    bodyElement.style.top = `-${scrollY}px`;
    viewportMeta?.setAttribute("content", PHONE_VIEWPORT_CONTENT);
    document.addEventListener("gesturestart", preventGesture, { passive: false });
    document.addEventListener("gesturechange", preventGesture, { passive: false });
    document.addEventListener("touchmove", preventPinch, { passive: false });

    return () => {
      htmlElement.classList.remove("login-phone-mode");
      bodyElement.classList.remove("login-phone-mode");
      rootElement?.classList.remove("login-phone-mode");
      Object.assign(bodyElement.style, previousBodyStyles);
      if (viewportMeta) {
        if (previousViewportContent === null) viewportMeta.removeAttribute("content");
        else viewportMeta.setAttribute("content", previousViewportContent);
      }
      document.removeEventListener("gesturestart", preventGesture);
      document.removeEventListener("gesturechange", preventGesture);
      document.removeEventListener("touchmove", preventPinch);
      if (scrollY > 0) window.scrollTo(0, scrollY);
    };
  }, [passwordMode]);

  const updatePassword = (value: string) => {
    passwordRef.current = value;
    setPassword(value);
  };

  const submitCredentials = async (candidatePassword: string) => {
    if (isSubmitting) return;
    if (!username.trim()) {
      setSubmitError("Enter your username first.");
      return;
    }
    if (!isValidPassword(candidatePassword)) {
      setSubmitError(PASSWORD_VALIDATION_MESSAGE);
      return;
    }

    setSubmitError(null);
    setIsSubmitting(true);
    try {
      await login({ username, password: candidatePassword });
    } catch (error) {
      setSubmitError(getErrorMessage(error, "Could not sign you in."));
      if (passwordMode === "phone") {
        updatePassword("");
        setFailedPinAttempts((current) => current + 1);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    void submitCredentials(password);
  };

  const handleUsernameChange = (value: string) => {
    setUsername(value);
    persistValue(USERNAME_STORAGE_KEY, value);
    setSubmitError(null);
  };

  const handlePasswordModeChange = (mode: PasswordMode) => {
    setPasswordMode(mode);
    updatePassword("");
    setSubmitError(null);
    persistValue(PASSWORD_MODE_STORAGE_KEY, mode);
    if (mode === "phone" && document.activeElement instanceof HTMLElement) document.activeElement.blur();
  };

  const handlePinDigit = (digit: string) => {
    const currentPassword = passwordRef.current;
    if (isSubmitting || !username.trim() || currentPassword.length >= PASSWORD_LENGTH) return;
    const nextPassword = `${currentPassword}${digit}`;
    updatePassword(nextPassword);
    setSubmitError(null);
    if (nextPassword.length === PASSWORD_LENGTH) void submitCredentials(nextPassword);
  };

  const handlePinDelete = () => {
    if (isSubmitting) return;
    updatePassword(passwordRef.current.slice(0, -1));
    setSubmitError(null);
  };

  const handlePinPointerDown = (
    event: ReactPointerEvent<HTMLButtonElement>,
    actionKey: string,
    action: () => void,
  ) => {
    if (event.pointerType === "mouse" || event.button !== 0) return;
    event.preventDefault();
    lastPinPointerActionRef.current = { key: actionKey, at: Date.now() };
    action();
  };

  const handlePinClick = (actionKey: string, action: () => void) => {
    const lastPointerAction = lastPinPointerActionRef.current;
    if (lastPointerAction?.key === actionKey && Date.now() - lastPointerAction.at < 750) return;
    action();
  };

  const handleOpenPasswordRecovery = () => {
    const normalizedUsername = username.trim();
    navigate(normalizedUsername ? `/password-recovery?username=${encodeURIComponent(normalizedUsername)}` : "/password-recovery");
  };

  const passwordModeSwitch = (
    <div className={styles.passwordLabelRow}>
      <span className={styles.modeSwitch} role="group" aria-label="Password input mode">
        {(["pc", "phone"] as const).map((mode) => (
          <button
            key={mode}
            type="button"
            className={`${styles.modeButton} ${passwordMode === mode ? styles.modeButtonActive : ""}`}
            aria-pressed={passwordMode === mode}
            onClick={() => handlePasswordModeChange(mode)}
          >
            {mode === "pc" ? "PC" : "Phone"}
          </button>
        ))}
      </span>
    </div>
  );

  return (
    <div className={`${styles.page} ${passwordMode === "phone" ? styles.pagePhone : ""}`}>
      <div className={styles.frame}>
        <section className={styles.card}>
          <div className={styles.cardHeader}>
            <div className={styles.brandRow}>
              <span className={styles.cardEyebrow}>Access to GF</span>
              <img className={styles.brandLogo} src="/gf-favicon.svg" alt="" aria-hidden="true" />
            </div>
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
                onChange={(event) => handleUsernameChange(event.target.value)}
              />
            </LabeledField>

            <LabeledField id="login-password" label="Password">
              {passwordModeSwitch}
              {passwordMode === "pc" ? (
                <>
                  <TextInput
                    id="login-password"
                    type="password"
                    inputMode="numeric"
                    pattern="[0-9]*"
                    minLength={PASSWORD_LENGTH}
                    maxLength={PASSWORD_LENGTH}
                    autoComplete="current-password"
                    value={password}
                    placeholder="••••••"
                    onChange={(event) => {
                      updatePassword(sanitizePassword(event.target.value));
                      setSubmitError(null);
                    }}
                  />
                  <span className={styles.passwordHint}>Exactly 6 digits</span>
                </>
              ) : (
                <div className={styles.pinPanel} aria-label="Password PIN entry">
                  <div
                    key={failedPinAttempts}
                    className={`${styles.pinDots} ${failedPinAttempts ? styles.pinDotsError : ""}`}
                    role="status"
                    aria-label={`${password.length} of ${PASSWORD_LENGTH} digits entered`}
                  >
                    {Array.from({ length: PASSWORD_LENGTH }, (_, index) => (
                      <span key={index} className={`${styles.pinDot} ${index < password.length ? styles.pinDotFilled : ""}`} aria-hidden="true" />
                    ))}
                  </div>

                  <div className={styles.pinKeypad} aria-label="Numeric keypad">
                    {KEYPAD_DIGITS.map((digit) => (
                      <button
                        key={digit}
                        type="button"
                        className={styles.pinKey}
                        aria-label={digit}
                        disabled={isSubmitting || !username.trim()}
                        onPointerDown={(event) => handlePinPointerDown(event, digit, () => handlePinDigit(digit))}
                        onClick={() => handlePinClick(digit, () => handlePinDigit(digit))}
                      >
                        {digit}
                      </button>
                    ))}
                    <span aria-hidden="true" />
                    <button
                      type="button"
                      className={styles.pinKey}
                      aria-label="0"
                      disabled={isSubmitting || !username.trim()}
                      onPointerDown={(event) => handlePinPointerDown(event, "0", () => handlePinDigit("0"))}
                      onClick={() => handlePinClick("0", () => handlePinDigit("0"))}
                    >
                      0
                    </button>
                    <button
                      type="button"
                      className={styles.pinDelete}
                      aria-label="Delete last digit"
                      disabled={isSubmitting || password.length === 0}
                      onPointerDown={(event) => handlePinPointerDown(event, "delete", handlePinDelete)}
                      onClick={() => handlePinClick("delete", handlePinDelete)}
                    >
                      Delete
                    </button>
                  </div>
                  <span className={styles.pinHint}>
                    {isSubmitting ? "Checking…" : username.trim() ? "Enter your 6-digit PIN" : "Enter username to unlock keypad"}
                  </span>
                </div>
              )}
            </LabeledField>

            <div className={styles.inlineRecoveryAction}>
              <button type="button" className={styles.textButton} onClick={handleOpenPasswordRecovery}>Forgot password?</button>
            </div>

            {passwordMode === "pc" ? (
              <div className={styles.actions}>
                <IosButton
                  label={isSubmitting ? "Signing in..." : "Sign in"}
                  type="submit"
                  disabled={isSubmitting || !username.trim() || !isValidPassword(password)}
                  className={styles.submitButton}
                />
              </div>
            ) : null}
          </form>

          <p className={styles.supportText}>contact us <a href="mailto:support@app-gf.com">support@app-gf.com</a></p>
        </section>
      </div>
    </div>
  );
}
