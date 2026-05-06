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
import sharedStyles from "@pages/shared/EmployeeWorkspacePage.module.css";
import styles from "./EmployeeAccountPage.module.css";

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
  const { session, logout } = useAuth();
  const profileQuery = useEmployeeProfileQuery();
  const updateProfileMutation = useUpdateEmployeeProfileMutation();
  const sendPasswordCodeMutation = useSendEmployeePasswordResetCodeMutation();
  const confirmPasswordMutation = useConfirmEmployeePasswordResetMutation();

  const profile = profileQuery.data;
  const displayName = profile?.displayName ?? session?.displayName ?? "Employee";
  const userName = profile?.username ?? session?.userName ?? "employee";
  const initials = getFallbackInitials(displayName);

  const [isEditing, setIsEditing] = useState(false);
  const [recoveryEmail, setRecoveryEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [passwordCode, setPasswordCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [inlineError, setInlineError] = useState<string | null>(null);
  const [inlineSuccess, setInlineSuccess] = useState<string | null>(null);
  const [codeDeliveryHint, setCodeDeliveryHint] = useState<string | null>(null);

  useEffect(() => {
    if (isEditing) {
      return;
    }

    setRecoveryEmail(profile?.recoveryEmail ?? "");
    setPhone(profile?.phone ?? "");
  }, [profile?.phone, profile?.recoveryEmail, isEditing]);

  const normalizedStoredEmail = normalizeEditableValue(profile?.recoveryEmail);
  const normalizedStoredPhone = normalizeEditableValue(profile?.phone);
  const normalizedDraftEmail = normalizeEditableValue(recoveryEmail);
  const normalizedDraftPhone = normalizeEditableValue(phone);
  const hasUnsavedChanges = normalizedStoredEmail !== normalizedDraftEmail || normalizedStoredPhone !== normalizedDraftPhone;
  const hasRecoveryEmailForPassword = isEditing ? Boolean(normalizedDraftEmail) : Boolean(normalizedStoredEmail);
  const isBusy =
    updateProfileMutation.isPending ||
    sendPasswordCodeMutation.isPending ||
    confirmPasswordMutation.isPending;
  const panelFooterClassName = [
    styles.panelFooter,
    isEditing ? styles.panelFooterEditing : "",
  ]
    .filter(Boolean)
    .join(" ");

  const handleOpenEdit = () => {
    setRecoveryEmail(profile?.recoveryEmail ?? "");
    setPhone(profile?.phone ?? "");
    setPasswordCode("");
    setNewPassword("");
    setCodeDeliveryHint(null);
    setInlineError(null);
    setInlineSuccess(null);
    setIsEditing(true);
  };

  const handleCancelEdit = () => {
    setRecoveryEmail(profile?.recoveryEmail ?? "");
    setPhone(profile?.phone ?? "");
    setPasswordCode("");
    setNewPassword("");
    setCodeDeliveryHint(null);
    setInlineError(null);
    setInlineSuccess(null);
    setIsEditing(false);
  };

  const persistProfileChanges = async (closeOnSuccess: boolean) => {
    const updatedProfile = await runMutation(updateProfileMutation.mutate, {
      recoveryEmail,
      phone,
    });

    if (normalizeEditableValue(updatedProfile.recoveryEmail) !== normalizedStoredEmail) {
      setPasswordCode("");
      setNewPassword("");
      setCodeDeliveryHint(null);
    }

    setInlineError(null);
    setInlineSuccess("Profile updated.");

    if (closeOnSuccess) {
      setIsEditing(false);
    }

    return updatedProfile;
  };

  const handleSaveProfile = async () => {
    try {
      await persistProfileChanges(true);
    } catch (error) {
      const message = getErrorMessage(error, "Could not update your profile.");
      setInlineError(message);
      setInlineSuccess(null);
      pushErrorAlertFromError(error, "Could not update your profile.");
    }
  };

  const handleSendPasswordCode = async () => {
    try {
      if (!hasRecoveryEmailForPassword) {
        setInlineError("Add a recovery email before requesting a password code.");
        setInlineSuccess(null);
        return;
      }

      if (hasUnsavedChanges) {
        await persistProfileChanges(false);
      }

      const result = await runMutation(sendPasswordCodeMutation.mutate, undefined);
      setCodeDeliveryHint(result.deliveryHint);
      setInlineError(null);
      setInlineSuccess(`Code sent to ${result.deliveryHint}.`);
    } catch (error) {
      const message = getErrorMessage(error, "Could not send the password code.");
      setInlineError(message);
      setInlineSuccess(null);
      pushErrorAlertFromError(error, "Could not send the password code.");
    }
  };

  const handleConfirmPasswordReset = async () => {
    try {
      await runMutation(confirmPasswordMutation.mutate, {
        code: passwordCode,
        newPassword,
      });

      setPasswordCode("");
      setNewPassword("");
      setInlineError(null);
      setInlineSuccess("Password updated.");
    } catch (error) {
      const message = getErrorMessage(error, "Could not update the password.");
      setInlineError(message);
      setInlineSuccess(null);
      pushErrorAlertFromError(error, "Could not update the password.");
    }
  };

  return (
    <div className={sharedStyles.page}>
      <section className={`${sharedStyles.panel} ${sharedStyles.panelAccent} ${styles.profilePanel}`}>
        <div className={styles.panelHeader}>
          <div className={styles.panelIntro}>
            <span className={sharedStyles.panelEyebrow}>Profile</span>

            <div className={styles.profileSummaryBlock}>
              <span className={sharedStyles.profileAvatar} aria-hidden="true">
                {initials}
              </span>

              <div className={sharedStyles.profileIdentity}>
                <strong className={sharedStyles.profileName}>{displayName}</strong>
                <span className={sharedStyles.profileHandle}>@{userName}</span>
              </div>
            </div>

            <h2 className={sharedStyles.panelTitle}>Account details and recovery</h2>
            <p className={sharedStyles.panelText}>Update your recovery contacts and confirm password changes with a code sent to email.</p>
          </div>

          {!isEditing ? (
            <div className={styles.headerActions}>
              <IosButton
                label="Edit"
                size="compact"
                className={styles.profileButton}
                onClick={handleOpenEdit}
                disabled={profileQuery.isLoading || !profile}
              />
            </div>
          ) : null}
        </div>

        {inlineError ? <ErrorBanner>{inlineError}</ErrorBanner> : null}
        {profileQuery.error ? (
          <ErrorBanner bannerClassName={styles.errorBanner}>Could not load your profile right now.</ErrorBanner>
        ) : null}
        {inlineSuccess ? <div className={styles.successBanner}>{inlineSuccess}</div> : null}

        {profileQuery.isLoading && !profile ? (
          <p className={sharedStyles.panelText}>Loading your profile...</p>
        ) : profile ? (
          <div className={styles.contentGrid}>
            <div className={styles.infoGrid}>
              <div className={styles.infoCard}>
                <span className={styles.cardLabel}>Login</span>
                <span className={styles.cardValue}>@{profile.username}</span>
              </div>

              <div className={styles.infoCard}>
                <span className={styles.cardLabel}>Display name</span>
                <span className={styles.cardValue}>{profile.displayName}</span>
              </div>

              <div className={styles.infoCard}>
                <span className={styles.cardLabel}>Recovery email</span>
                {isEditing ? (
                  <label className={styles.field}>
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
                  <span className={styles.cardValue}>{profile.recoveryEmail ?? "Not added yet"}</span>
                )}
              </div>

              <div className={styles.infoCard}>
                <span className={styles.cardLabel}>Phone</span>
                {isEditing ? (
                  <label className={styles.field}>
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
                  <span className={styles.cardValue}>{profile.phone ?? "Not added yet"}</span>
                )}
              </div>
            </div>

            <div className={styles.passwordCard}>
              <div className={styles.passwordHeader}>
                <div>
                  <span className={styles.cardLabel}>Password change</span>
                  <strong className={styles.passwordTitle}>Email verification</strong>
                </div>
                <span className={styles.passwordHint}>{hasRecoveryEmailForPassword ? "6-digit code" : "Recovery email required"}</span>
              </div>

              <p className={styles.passwordText}>
                {hasRecoveryEmailForPassword
                  ? "We will send a one-time code to your recovery email. Save any changed contact details before continuing."
                  : "Add and save a recovery email first, then request a one-time password code here."}
              </p>

              {codeDeliveryHint ? <div className={styles.passwordNotice}>Last code sent to {codeDeliveryHint}.</div> : null}

              {isEditing ? (
                <>
                  <div className={styles.passwordActions}>
                    <IosButton
                      label={sendPasswordCodeMutation.isPending ? "Sending..." : "Send code"}
                      size="compact"
                      className={styles.profileButton}
                      onClick={() => void handleSendPasswordCode()}
                      disabled={isBusy || !hasRecoveryEmailForPassword}
                    />
                    {!hasRecoveryEmailForPassword ? (
                      <span className={styles.passwordMeta}>Recovery email is required.</span>
                    ) : hasUnsavedChanges ? (
                      <span className={styles.passwordMeta}>Unsaved profile changes will be saved first.</span>
                    ) : null}
                  </div>

                  <div className={styles.passwordFormGrid}>
                    <label className={styles.field}>
                      <span className={styles.fieldLabel}>Code</span>
                      <input
                        type="text"
                        inputMode="numeric"
                        maxLength={6}
                        className={styles.input}
                        value={passwordCode}
                        onChange={(event) => setPasswordCode(event.target.value)}
                        placeholder="123456"
                      />
                    </label>

                    <label className={styles.field}>
                      <span className={styles.fieldLabel}>New password</span>
                      <input
                        type="password"
                        className={styles.input}
                        value={newPassword}
                        onChange={(event) => setNewPassword(event.target.value)}
                        placeholder="Minimum 6 characters"
                        autoComplete="new-password"
                      />
                    </label>
                  </div>

                  <div className={styles.passwordActions}>
                    <IosButton
                      label={confirmPasswordMutation.isPending ? "Updating..." : "Change password"}
                      size="compact"
                      className={styles.profileButton}
                      onClick={() => void handleConfirmPasswordReset()}
                      disabled={isBusy || !passwordCode.trim() || !newPassword.trim()}
                    />
                  </div>
                </>
              ) : (
                <div className={styles.passwordReadonly}>Use Edit to update recovery details and change the password by email code.</div>
              )}
            </div>
          </div>
        ) : (
          <p className={sharedStyles.panelText}>Your profile is not available yet.</p>
        )}

        {profile ? (
          <div className={panelFooterClassName}>
            {isEditing ? (
              <div className={styles.footerPrimaryActions}>
                <IosButton label="Cancel" variant="secondary" size="compact" className={styles.profileButton} onClick={handleCancelEdit} disabled={isBusy} />
                <IosButton
                  label={updateProfileMutation.isPending ? "Saving..." : "Save profile"}
                  size="compact"
                  className={styles.profileButton}
                  onClick={() => void handleSaveProfile()}
                  disabled={isBusy || !hasUnsavedChanges}
                />
              </div>
            ) : null}

            <IosButton
              label="Log out"
              variant="secondary"
              size="compact"
              className={[styles.profileButton, styles.logoutButton].join(" ")}
              onClick={() => void logout()}
              disabled={isBusy}
            />
          </div>
        ) : null}
      </section>
    </div>
  );
}
