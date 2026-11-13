import { t } from "@shared/i18n";
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
const PHONE_VIEWPORT_CONTENT = "width=device-width, initial-scale=1.0, viewport-fit=cover";

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
  const { bootstrapError, passwordChanged, login } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState(() => readStoredValue(USERNAME_STORAGE_KEY));
  const [passwordMode, setPasswordMode] = useState<PasswordMode>(readStoredPasswordMode);
  const [password, setPassword] = useState("");
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [failedPinAttempts, setFailedPinAttempts] = useState(0);
  const [isPinDialogOpen, setIsPinDialogOpen] = useState(false);
  const pinDialogRef = useRef<HTMLDialogElement | null>(null);
  const submitInFlightRef = useRef(false);
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

  const openPinDialog = () => {
    if (passwordMode !== "phone" || isSubmitting || !username.trim()) return;
    updatePassword("");
    setSubmitError(null);
    lastPinPointerActionRef.current = null;
    // WebKit does not focus tapped buttons; give native close() a return target.
    pinDialogRef.current?.parentElement?.querySelector<HTMLButtonElement>(`.${styles.pinLaunchButton}`)?.focus();
    setIsPinDialogOpen(true);
  };

  const closePinDialog = () => {
    if (submitInFlightRef.current) return;
    setIsPinDialogOpen(false);
    updatePassword("");
    setSubmitError(null);
    lastPinPointerActionRef.current = null;
  };

  useEffect(() => {
    const dialog = pinDialogRef.current;
    if (!dialog) return;
    if (isPinDialogOpen && passwordMode === "phone") {
      if (!dialog.open) dialog.showModal();
    } else if (dialog.open) dialog.close();
    return () => {
      if (dialog.open) dialog.close();
    };
  }, [isPinDialogOpen, passwordMode]);

  const submitCredentials = async (candidatePassword: string) => {
    if (isSubmitting) return;
    if (!username.trim()) {
      setSubmitError(t("Enter your username first."));
      return;
    }
    if (!isValidPassword(candidatePassword)) {
      setSubmitError(PASSWORD_VALIDATION_MESSAGE);
      return;
    }

    if (submitInFlightRef.current) return;
    submitInFlightRef.current = true;
    setSubmitError(null);
    setIsSubmitting(true);
    try {
      await login({ username, password: candidatePassword });
    } catch (error) {
      setSubmitError(getErrorMessage(error, t("Could not sign you in.")));
      if (passwordMode === "phone") {
        updatePassword("");
        setFailedPinAttempts((current) => current + 1);
      }
    } finally {
      submitInFlightRef.current = false;
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
    setIsPinDialogOpen(false);
    lastPinPointerActionRef.current = null;
    updatePassword("");
    setSubmitError(null);
    persistValue(PASSWORD_MODE_STORAGE_KEY, mode);
    if (mode === "phone" && document.activeElement instanceof HTMLElement) document.activeElement.blur();
  };

  const handlePinDigit = (digit: string) => {
    const currentPassword = passwordRef.current;
    if (submitInFlightRef.current || isSubmitting || !username.trim() || currentPassword.length >= PASSWORD_LENGTH) return;
    const nextPassword = `${currentPassword}${digit}`;
    updatePassword(nextPassword);
    setSubmitError(null);
    if (nextPassword.length === PASSWORD_LENGTH) void submitCredentials(nextPassword);
  };

  const handlePinDelete = () => {
    if (submitInFlightRef.current || isSubmitting) return;
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
      <span className={styles.modeSwitch} role="group" aria-label={t("Password input mode")}>
        {(["pc", "phone"] as const).map((mode) => (
          <button
            key={mode}
            type="button"
            className={`${styles.modeButton} ${passwordMode === mode ? styles.modeButtonActive : ""}`}
            aria-pressed={passwordMode === mode}
            onClick={() => handlePasswordModeChange(mode)}
          >
            {mode === "pc" ? "PC" : t("Phone")}
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
              <span className={styles.cardEyebrow}>{t("Access to GF")}</span>
              <img className={styles.brandLogo} src="/gf-favicon.svg" alt="" aria-hidden="true" />
            </div>
            <h2 className={styles.cardTitle}>{t("Sign in")}</h2>
            <p className={styles.cardSubtitle}>{t("Use the credentials created for your role.")}</p>
          </div>

          {passwordChanged ? (
            <div role="status" className={styles.successBanner}>
              {t("Password changed successfully. Sign in with your new password.")}
            </div>
          ) : null}
          {bootstrapError ? <ErrorBanner className={styles.banner}>{bootstrapError}</ErrorBanner> : null}
          {passwordMode === "pc" && submitError ? <ErrorBanner className={styles.banner}>{submitError}</ErrorBanner> : null}

          <form className={styles.form} onSubmit={handleSubmit}>
            <LabeledField id="login-username" label={t("Username")}>
              <TextInput
                id="login-username"
                autoComplete="username"
                value={username}
                placeholder={t("Example User")}
                onChange={(event) => handleUsernameChange(event.target.value)}
              />
            </LabeledField>

            <LabeledField id="login-password" label={t("Password")}>
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
                  <span className={styles.passwordHint}>{t("Exactly 6 digits")}</span>
                </>
              ) : (
                <>
                  <IosButton label={t("Enter password")} type="button" onClick={openPinDialog}
                    disabled={isSubmitting || !username.trim()} className={`${styles.submitButton} ${styles.pinLaunchButton}`} />
                  <span className={styles.pinHint}>
                    {username.trim() ? t("Exactly 6 digits") : t("Enter username to unlock keypad")}
                  </span>
                </>
              )}
            </LabeledField>

            <div className={styles.inlineRecoveryAction}>
              <button type="button" className={styles.textButton} onClick={handleOpenPasswordRecovery}>{t("Forgot password?")}</button>
            </div>

            {passwordMode === "pc" ? (
              <div className={styles.actions}>
                <IosButton
                  label={isSubmitting ? t("Signing in...") : t("Sign in")}
                  type="submit"
                  disabled={isSubmitting || !username.trim() || !isValidPassword(password)}
                  className={styles.submitButton}
                />
              </div>
            ) : null}
          </form>

          <p className={styles.supportText}>{t("contact us")} <a href="mailto:support@app-gf.com">support@app-gf.com</a></p>
          <nav className={styles.legalLinks} aria-label={t("Legal documents")}>
            <a href="/legal/index.html">{t("Legal documents")}</a>
            <a href="/legal/regulamin.html">{t("Terms")}</a>
            <a href="/legal/polityka-prywatnosci.html">{t("Privacy policy")}</a>
            <a href="/legal/pliki-cookies.html">{t("Cookies")}</a>
          </nav>
        </section>
      </div>
      {passwordMode === "phone" ? (
        <dialog ref={pinDialogRef} className={styles.pinDialog} aria-labelledby="login-pin-dialog-title"
          onClose={closePinDialog}
          onCancel={(event) => { if (submitInFlightRef.current) event.preventDefault(); }}
          onKeyDown={(event) => {
            if (!isPinDialogOpen || submitInFlightRef.current) return;
            if (/^[0-9]$/.test(event.key)) {
              event.preventDefault();
              handlePinDigit(event.key);
            } else if (event.key === "Backspace") {
              event.preventDefault();
              handlePinDelete();
            }
          }}>
          <div className={styles.pinDialogContent} aria-label={t("Password PIN entry")}>
            <button type="button" autoFocus className={styles.pinClose} aria-label={t("Close")}
              disabled={isSubmitting} onClick={closePinDialog}>{t("Close")}</button>
            <h2 id="login-pin-dialog-title" className={styles.pinTitle}>{t("Enter your 6-digit PIN")}</h2>
            <div
              key={failedPinAttempts}
              className={`${styles.pinDots} ${failedPinAttempts ? styles.pinDotsError : ""}`}
              role="status"
              aria-label={t("{0} of {1} digits entered", password.length, PASSWORD_LENGTH)}
            >
              {Array.from({ length: PASSWORD_LENGTH }, (_, index) => (
                <span key={index} className={`${styles.pinDot} ${index < password.length ? styles.pinDotFilled : ""}`} aria-hidden="true" />
              ))}
            </div>

            <div className={styles.pinKeypad} aria-label={t("Numeric keypad")}>
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
                aria-label={t("Delete last digit")}
                disabled={isSubmitting || password.length === 0}
                onPointerDown={(event) => handlePinPointerDown(event, "delete", handlePinDelete)}
                onClick={() => handlePinClick("delete", handlePinDelete)}
              >
                {t("Delete")}</button>
            </div>
            <div className={styles.pinFeedback} aria-live="polite">
              {isSubmitting ? <span role="status">{t("Checking…")}</span> : submitError ?
                <span role="alert" className={styles.pinError}>{submitError}</span> : <span>{t("Exactly 6 digits")}</span>}
            </div>
          </div>
        </dialog>
      ) : null}
    </div>
  );
}
