import { t } from "@shared/i18n";
import { EmployeePushSettings } from "@entities/employee-push/ui/EmployeePushSettings";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { LanguageSelector } from "@shared/i18n/LanguageSelector";
import { useEffect, useState } from "react";
import { useAuth } from "@app/providers/AuthProvider";
import {
  useConfirmEmployeePasswordResetMutation,
  useEmployeeProfileQuery,
  useSendEmployeePasswordResetCodeMutation,
  useUpdateEmployeeProfileMutation,
} from "@features/employee-profile/api/queries";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { pushErrorAlertFromError } from "@shared/ui/feedback/error-alerts/errorAlerts";
import { AccountSettingsIcon, CheckIcon, ChevronRightIcon, EmployeeIcon, LogoutIcon, SaveIcon } from "@shared/ui/icons";
import sharedStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import styles from "./EmployeeAccountPage.module.css";
import { useMyRegulationHistoryQuery } from "@entities/regulations";
import { RegulationHistoryCard } from "@entities/regulations/ui/RegulationHistoryCard";

function getFallbackInitials(value?: string | null) {
  const parts = value?.trim().split(/\s+/).filter(Boolean) ?? [];
  const initials = parts
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");

  return initials || "GF";
}

function normalizeEditableValue(value?: string | null) {
  const trimmedValue = value?.trim();
  return trimmedValue ? trimmedValue : null;
}

function runMutation<TData, TVariables>(
  mutate: (variables: TVariables, callbacks?: { onSuccess?: (data: TData) => void; onError?: (error: unknown) => void }) => void,
  variables: TVariables,
) {
  return new Promise<TData>((resolve, reject) => {
    mutate(variables, {
      onSuccess: resolve,
      onError: reject,
    });
  });
}

export function EmployeeAccountPage() {
  useLanguageRevision();
  const { session, logout, completePasswordChange } = useAuth();
  const profileQuery = useEmployeeProfileQuery();
  const updateProfileMutation = useUpdateEmployeeProfileMutation();
  const sendPasswordCodeMutation = useSendEmployeePasswordResetCodeMutation();
  const confirmPasswordMutation = useConfirmEmployeePasswordResetMutation();
  const regulationHistoryQuery = useMyRegulationHistoryQuery(`employee:${session?.employeeId ?? 0}`);

  const profile = profileQuery.data;
  const displayName = profile?.displayName ?? session?.displayName ?? t("Employee");
  const userName = profile?.username ?? session?.userName ?? "employee";
  const initials = getFallbackInitials(displayName);

  const [activeEditor, setActiveEditor] = useState<"account" | "password" | null>(null);
  const isEditingAccount = activeEditor === "account";
  const isChangingPassword = activeEditor === "password";
  const [recoveryEmail, setRecoveryEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [passwordCode, setPasswordCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [inlineError, setInlineError] = useState<string | null>(null);
  const [inlineSuccess, setInlineSuccess] = useState<string | null>(null);
  const [codeDeliveryHint, setCodeDeliveryHint] = useState<string | null>(null);

  useEffect(() => {
    if (isEditingAccount) {
      return;
    }

    setRecoveryEmail(profile?.recoveryEmail ?? "");
    setPhone(profile?.phone ?? "");
  }, [profile?.phone, profile?.recoveryEmail, isEditingAccount]);

  const normalizedStoredEmail = normalizeEditableValue(profile?.recoveryEmail);
  const normalizedStoredPhone = normalizeEditableValue(profile?.phone);
  const normalizedDraftEmail = normalizeEditableValue(recoveryEmail);
  const normalizedDraftPhone = normalizeEditableValue(phone);
  const hasUnsavedChanges = normalizedStoredEmail !== normalizedDraftEmail || normalizedStoredPhone !== normalizedDraftPhone;
  const hasRecoveryEmailForPassword = Boolean(normalizedStoredEmail);
  const isBusy =
    updateProfileMutation.isPending ||
    sendPasswordCodeMutation.isPending ||
    confirmPasswordMutation.isPending;

  const handleOpenAccountEdit = () => {
    if (isBusy || activeEditor) return;
    setRecoveryEmail(profile?.recoveryEmail ?? "");
    setPhone(profile?.phone ?? "");
    setInlineError(null);
    setInlineSuccess(null);
    setActiveEditor("account");
  };

  const handleCancelAccountEdit = () => {
    setRecoveryEmail(profile?.recoveryEmail ?? "");
    setPhone(profile?.phone ?? "");
    setInlineError(null);
    setInlineSuccess(null);
    setActiveEditor(null);
  };

  const handleOpenPasswordEditor = () => {
    if (!hasRecoveryEmailForPassword || isBusy || activeEditor) return;
    setPasswordCode("");
    setNewPassword("");
    setCodeDeliveryHint(null);
    setInlineError(null);
    setInlineSuccess(null);
    setActiveEditor("password");
  };

  const handleCancelPasswordEditor = () => {
    setPasswordCode("");
    setNewPassword("");
    setCodeDeliveryHint(null);
    setInlineError(null);
    setInlineSuccess(null);
    setActiveEditor(null);
  };

  const handleSaveProfile = async () => {
    try {
      await runMutation(updateProfileMutation.mutate, { recoveryEmail, phone });
      setInlineError(null);
      setInlineSuccess(t("Profile updated."));
      setActiveEditor(null);
    } catch (error) {
      const message = getErrorMessage(error, t("Could not update your profile."));
      setInlineError(message);
      setInlineSuccess(null);
      pushErrorAlertFromError(error, t("Could not update your profile."));
    }
  };

  const handleSendPasswordCode = async () => {
    try {
      if (!hasRecoveryEmailForPassword) {
        setInlineError(t("Add a recovery email before requesting a password code."));
        setInlineSuccess(null);
        return;
      }

      const result = await runMutation(sendPasswordCodeMutation.mutate, undefined);
      setCodeDeliveryHint(result.deliveryHint);
      setInlineError(null);
      setInlineSuccess(t("Code sent to {0}.", result.deliveryHint));
    } catch (error) {
      const message = getErrorMessage(error, t("Could not send the password code."));
      setInlineError(message);
      setInlineSuccess(null);
      pushErrorAlertFromError(error, t("Could not send the password code."));
    }
  };

  const handleConfirmPasswordReset = async () => {
    try {
      if (!/^\d{6}$/.test(newPassword)) {
        setInlineError(t("Password must contain exactly 6 digits."));
        setInlineSuccess(null);
        return;
      }

      await runMutation(confirmPasswordMutation.mutate, {
        code: passwordCode,
        newPassword,
      });

      setPasswordCode("");
      setNewPassword("");
      setInlineError(null);
      completePasswordChange();
    } catch (error) {
      const message = getErrorMessage(error, t("Could not update the password."));
      setInlineError(message);
      setInlineSuccess(null);
      pushErrorAlertFromError(error, t("Could not update the password."));
    }
  };

  return (
    <div className={sharedStyles.page}>
      <section className={styles.profilePanel}>
        <div className={styles.panelHeader} data-employee-motion>
          <div className={styles.profileSummaryBlock}>
            <span className={styles.profileAvatar} aria-hidden="true">
              {initials}
            </span>

            <div className={styles.profileIdentity}>
              <span className={styles.profileEyebrow}>{t("Profile")}</span>
              <h1 className={styles.profileName}>{displayName}</h1>
              <span className={styles.profileHandle}>@{userName}</span>
            </div>
          </div>

          <p className={styles.profileDescription}>
            {t("Keep your contact details current and manage password recovery securely.")}</p>
        </div>

        {inlineError || profileQuery.error || inlineSuccess ? (
          <div className={styles.feedbackStack}>
            {inlineError ? <ErrorBanner>{inlineError}</ErrorBanner> : null}
            {profileQuery.error ? (
              <ErrorBanner bannerClassName={styles.errorBanner}>{t("Could not load your profile right now.")}</ErrorBanner>
            ) : null}
            {inlineSuccess ? <div className={styles.successBanner}>{inlineSuccess}</div> : null}
          </div>
        ) : null}

        {profileQuery.isLoading && !profile ? (
          <div className={styles.loadingState}>{t("Loading your profile...")}</div>
        ) : profile ? (
          <div className={styles.contentGrid}>
            <section className={styles.detailsSection} data-employee-motion aria-labelledby="personal-details-heading">
              <div className={styles.sectionHeader}>
                <span className={styles.sectionIcon} aria-hidden="true">
                  <EmployeeIcon size={20} />
                </span>
                <div>
                  <h2 id="personal-details-heading" className={styles.sectionTitle}>{t("Account details")}</h2>
                  <p className={styles.sectionText}>{t("Your account and recovery contacts.")}</p>
                </div>
              </div>

              <div className={styles.detailsList}>
                <div className={styles.languageRow}>
                  <AccountSettingsIcon name="globe" />
                  <LanguageSelector disabled={isBusy} appearance="rounded" dropdownWidth={240} />
                </div>
                <div className={styles.detailRow}>
                  <AccountSettingsIcon name="at" /><span className={styles.cardLabel}>{t("Login")}</span>
                  <span className={styles.cardValue}>@{profile.username}</span>
                </div>

                <div className={styles.detailRow}>
                  <AccountSettingsIcon name="user" /><span className={styles.cardLabel}>{t("Display name")}</span>
                  <span className={styles.cardValue}>{profile.displayName}</span>
                </div>

                <div className={`${styles.detailRow} ${isEditingAccount ? styles.detailRowEditing : ""}`}>
                  {isEditingAccount ? (
                    <label className={styles.field}>
                      <span className={styles.fieldLabel}>{t("Recovery email")}</span>
                      <span className={styles.fieldHint}>{t("Used for secure password recovery.")}</span>
                      <input
                        type="email"
                        className={styles.input}
                        value={recoveryEmail}
                        onChange={(event) => setRecoveryEmail(event.target.value)}
                        placeholder="example@email.com"
                        autoComplete="email"
                      />
                    </label>
                  ) : (
                    <>
                      <AccountSettingsIcon name="mail" /><span className={styles.cardLabel}>{t("Recovery email")}</span>
                      <span className={styles.cardValue}>{normalizedStoredEmail ?? t("Not added yet")}</span>
                    </>
                  )}
                </div>

                <div className={`${styles.detailRow} ${isEditingAccount ? styles.detailRowEditing : ""}`}>
                  {isEditingAccount ? (
                    <label className={styles.field}>
                      <span className={styles.fieldLabel}>{t("Phone")}</span>
                      <span className={styles.fieldHint}>{t("Optional contact number for your manager.")}</span>
                      <input
                        type="tel"
                        className={styles.input}
                        value={phone}
                        onChange={(event) => setPhone(event.target.value)}
                        placeholder="+48 500 000 000"
                        autoComplete="tel"
                      />
                    </label>
                  ) : (
                    <>
                      <AccountSettingsIcon name="phone" /><span className={styles.cardLabel}>{t("Phone")}</span>
                      <span className={styles.cardValue}>{normalizedStoredPhone ?? t("Not added yet")}</span>
                    </>
                  )}
                </div>
              </div>

              <div className={styles.detailsActions}>
                {isEditingAccount ? (
                  <>
                    <button type="button" className={styles.secondaryButton} onClick={handleCancelAccountEdit} disabled={isBusy}>
                      {t("Cancel")}
                    </button>
                    <button type="button" className={styles.primaryButton} onClick={() => void handleSaveProfile()} disabled={isBusy || !hasUnsavedChanges}>
                      <SaveIcon size={17} />
                      {updateProfileMutation.isPending ? t("Saving...") : t("Save profile")}
                    </button>
                  </>
                ) : (
                  <button type="button" className={styles.primaryButton} onClick={handleOpenAccountEdit} disabled={isBusy || isChangingPassword}>
                    <AccountSettingsIcon name="edit" />
                    {t("Edit account details")}
                  </button>
                )}
              </div>
            </section>

            <section className={styles.securitySection} data-employee-motion aria-labelledby="security-heading">
              <div className={styles.sectionHeader}>
                <span className={`${styles.sectionIcon} ${styles.sectionIconSecurity}`} aria-hidden="true">
                  <AccountSettingsIcon name="lock" />
                </span>
                <div>
                  <h2 id="security-heading" className={styles.sectionTitle}>{t("Password & security")}</h2>
                  <p className={styles.sectionText}>{t("Confirm changes with a one-time email code.")}</p>
                </div>
              </div>

              <div className={`${styles.securityStatus} ${!hasRecoveryEmailForPassword ? styles.securityStatusMissing : ""}`}>
                {hasRecoveryEmailForPassword ? <CheckIcon size={20} /> : <AccountSettingsIcon name="mail" />}
                <span>{hasRecoveryEmailForPassword ? t("Recovery email connected") : t("Recovery email required")}</span>
              </div>

              {codeDeliveryHint ? <div className={styles.passwordNotice}>{t("Last code sent to")} {codeDeliveryHint}.</div> : null}

              {isChangingPassword ? (
                <div className={styles.securityEditor}>
                  <div className={styles.passwordActions}>
                    <IosButton
                      label={sendPasswordCodeMutation.isPending ? t("Sending...") : t("Send code")}
                      size="compact"
                      className={styles.profileButton}
                      onClick={() => void handleSendPasswordCode()}
                      disabled={isBusy || !hasRecoveryEmailForPassword}
                    />
                  </div>

                  <div className={styles.passwordFormGrid}>
                    <label className={styles.field}>
                      <span className={styles.fieldLabel}>{t("Verification code")}</span>
                      <input
                        type="text"
                        inputMode="numeric"
                        maxLength={6}
                        className={styles.input}
                        value={passwordCode}
                        onChange={(event) => setPasswordCode(event.target.value)}
                        placeholder="123456"
                        autoComplete="one-time-code"
                      />
                    </label>

                    <label className={styles.field}>
                      <span className={styles.fieldLabel}>{t("New password")}</span>
                      <input
                        type="password"
                        inputMode="numeric"
                        pattern="[0-9]*"
                        minLength={6}
                        maxLength={6}
                        className={styles.input}
                        value={newPassword}
                        onChange={(event) => setNewPassword(event.target.value.replace(/\D/g, "").slice(0, 6))}
                        placeholder={t("Exactly 6 digits")}
                        autoComplete="654321"
                      />
                    </label>
                  </div>

                  <IosButton
                    label={confirmPasswordMutation.isPending ? t("Updating...") : t("Change password")}
                    size="compact"
                    className={`${styles.profileButton} ${styles.changePasswordButton}`}
                    onClick={() => void handleConfirmPasswordReset()}
                    disabled={isBusy || !passwordCode.trim() || !/^\d{6}$/.test(newPassword)}
                  />
                  <button type="button" className={styles.secondaryButton} onClick={handleCancelPasswordEditor} disabled={isBusy}>{t("Cancel")}</button>
                </div>
              ) : (
                <>
                  {!hasRecoveryEmailForPassword ? <p className={styles.passwordMeta}>{t("Add a recovery email first.")}</p> : null}
                  <button type="button" className={styles.passwordTrigger} onClick={handleOpenPasswordEditor} disabled={isBusy || isEditingAccount || !hasRecoveryEmailForPassword}>
                    <AccountSettingsIcon name="key" />
                    <span>{t("Change password")}</span>
                    <ChevronRightIcon size={18} />
                  </button>
                </>
              )}
            </section>
          </div>
        ) : (
          <div className={styles.loadingState}>{t("Your profile is not available yet.")}</div>
        )}

        {profile ? <EmployeePushSettings employeeId={profile.employeeId} appearance="profile" /> : null}

        {profile ? (
          <RegulationHistoryCard
            appearance="profile"
            acceptances={regulationHistoryQuery.data ?? []}
            isLoading={regulationHistoryQuery.isLoading}
          />
        ) : null}

        {profile ? (
          <div className={styles.panelFooter}>
            <button type="button" className={styles.logoutButton} onClick={() => void logout()} disabled={isBusy}>
              <span className={styles.logoutIcon}><LogoutIcon size={22} /></span>
              <span>{t("Log out")}</span>
              <ChevronRightIcon size={18} />
            </button>
          </div>
        ) : null}
      </section>
    </div>
  );
}
