import { useEffect, useMemo, useState } from "react";
import type { ChangeEvent } from "react";
import { useAuth } from "@app/providers/AuthProvider";
import {
  useCreateManagerMutation,
  useDeleteManagerMutation,
  useManagerListQuery,
  useManagerProfileQuery,
  useUpdateManagerProfileMutation,
  type ManagerProfileDto,
} from "@features/manager-profile/api";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { pushErrorAlertFromError } from "@shared/ui/feedback/error-alerts/errorAlerts";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./ManagerAccountPage.module.css";

type ManagerFormState = {
  displayName: string;
  userName: string;
  recoveryEmail: string;
  newPassword: string;
};

type NewManagerFormState = {
  displayName: string;
  userName: string;
  recoveryEmail: string;
  password: string;
};

const emptyManagerForm: ManagerFormState = {
  displayName: "",
  userName: "",
  recoveryEmail: "",
  newPassword: "",
};

const emptyNewManagerForm: NewManagerFormState = {
  displayName: "",
  userName: "",
  recoveryEmail: "",
  password: "",
};

const PASSWORD_VALIDATION_MESSAGE = "Password must contain exactly 6 digits.";

function isValidNumericPassword(value: string) {
  return /^\d{6}$/.test(value);
}

const lastOnlineFormatter = new Intl.DateTimeFormat(undefined, {
  month: "short",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
});

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

function getInitials(value?: string | null) {
  const initials = value
    ?.trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map(part => part[0]?.toUpperCase() ?? "")
    .join("");

  return initials || "M";
}

function normalize(value: string) {
  return value.trim();
}

function isSameManager(manager: ManagerProfileDto, currentManagerId: number | null, currentUserName: string) {
  return (
    currentManagerId === manager.id ||
    normalize(manager.userName).toLowerCase() === normalize(currentUserName).toLowerCase()
  );
}

function formatLastOnline(value?: string | null) {
  if (!value) {
    return "No activity yet";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "Unknown activity";
  }

  return `Last online ${lastOnlineFormatter.format(date)}`;
}

function getLastOnlineTime(value?: string | null) {
  if (!value) {
    return 0;
  }

  const time = new Date(value).getTime();
  return Number.isFinite(time) ? time : 0;
}

export function ManagerAccountPage() {
  const { replaceLoginResult, session } = useAuth();
  const profileQuery = useManagerProfileQuery();
  const managerListQuery = useManagerListQuery();
  const updateProfileMutation = useUpdateManagerProfileMutation();
  const createManagerMutation = useCreateManagerMutation();
  const deleteManagerMutation = useDeleteManagerMutation();

  const profile = profileQuery.data;
  const managers = managerListQuery.data ?? [];
  const currentManagerId = profile?.id ?? session?.managerId ?? null;
  const currentUserName = profile?.userName ?? session?.userName ?? "";
  const [form, setForm] = useState<ManagerFormState>(emptyManagerForm);
  const [newManagerForm, setNewManagerForm] = useState<NewManagerFormState>(emptyNewManagerForm);
  const [deleteTarget, setDeleteTarget] = useState<ManagerProfileDto | null>(null);
  const [inlineError, setInlineError] = useState<string | null>(null);
  const [inlineSuccess, setInlineSuccess] = useState<string | null>(null);
  const initials = getInitials(profile?.displayName ?? session?.displayName);
  const isSystemManager = profile?.isSystem ?? false;
  const hasPasswordDraft = !isSystemManager && Boolean(normalize(form.newPassword));
  const isBusy = updateProfileMutation.isPending || createManagerMutation.isPending || deleteManagerMutation.isPending;
  const managerPresenceItems = useMemo(() => {
    return [...managers]
      .map(manager => {
        const isCurrent = isSameManager(manager, currentManagerId, currentUserName);

        return {
          manager,
          isCurrent,
          isOnline: manager.isOnline || isCurrent,
        };
      })
      .sort((left, right) => {
        if (left.isCurrent !== right.isCurrent) {
          return left.isCurrent ? -1 : 1;
        }

        if (left.isOnline !== right.isOnline) {
          return left.isOnline ? -1 : 1;
        }

        return getLastOnlineTime(right.manager.lastLoginAtUtc) - getLastOnlineTime(left.manager.lastLoginAtUtc);
      });
  }, [currentManagerId, currentUserName, managers]);
  const onlineManagerCount = managerPresenceItems.filter(item => item.isOnline).length;
  const hasProfileChanges = useMemo(() => {
    if (!profile) {
      return false;
    }

    return (
      normalize(form.displayName) !== profile.displayName ||
      (!isSystemManager && normalize(form.userName) !== profile.userName) ||
      normalize(form.recoveryEmail) !== normalize(profile.recoveryEmail ?? "") ||
      hasPasswordDraft
    );
  }, [form.displayName, form.recoveryEmail, form.userName, hasPasswordDraft, isSystemManager, profile]);

  useEffect(() => {
    if (!profile) {
      return;
    }

    setForm({
      displayName: profile.displayName,
      userName: profile.userName,
      recoveryEmail: profile.recoveryEmail ?? "",
      newPassword: "",
    });
  }, [profile]);

  const updateForm = (field: keyof ManagerFormState) => (event: ChangeEvent<HTMLInputElement>) => {
    const value = field === "newPassword" ? event.target.value.replace(/\D/g, "").slice(0, 6) : event.target.value;
    setForm(current => ({ ...current, [field]: value }));
  };

  const updateNewManagerForm = (field: keyof NewManagerFormState) => (event: ChangeEvent<HTMLInputElement>) => {
    const value = field === "password" ? event.target.value.replace(/\D/g, "").slice(0, 6) : event.target.value;
    setNewManagerForm(current => ({ ...current, [field]: value }));
  };

  const handleSaveProfile = async () => {
    try {
      if (!normalize(form.displayName) || !normalize(form.userName)) {
        setInlineError("Display name and username are required.");
        setInlineSuccess(null);
        return;
      }

      if (hasPasswordDraft && !isValidNumericPassword(form.newPassword)) {
        setInlineError(PASSWORD_VALIDATION_MESSAGE);
        setInlineSuccess(null);
        return;
      }

      const result = await runMutation(updateProfileMutation.mutate, {
        displayName: form.displayName,
        userName: form.userName,
        recoveryEmail: form.recoveryEmail,
        newPassword: isSystemManager ? "" : form.newPassword,
      });

      replaceLoginResult(result);
      setForm(current => ({ ...current, newPassword: "" }));
      setInlineError(null);
      setInlineSuccess("Manager profile updated.");
    } catch (error) {
      const message = getErrorMessage(error, "Could not update manager profile.");
      setInlineError(message);
      setInlineSuccess(null);
      pushErrorAlertFromError(error, "Could not update manager profile.");
    }
  };

  const handleCreateManager = async () => {
    try {
      if (!normalize(newManagerForm.displayName) || !normalize(newManagerForm.userName) || !normalize(newManagerForm.password)) {
        setInlineError("Display name, username and password are required for a new manager.");
        setInlineSuccess(null);
        return;
      }


      if (!isValidNumericPassword(newManagerForm.password)) {
        setInlineError(PASSWORD_VALIDATION_MESSAGE);
        setInlineSuccess(null);
        return;
      }

      const created = await runMutation(createManagerMutation.mutate, newManagerForm);
      setNewManagerForm(emptyNewManagerForm);
      setInlineError(null);
      setInlineSuccess(`Manager account created for ${created.displayName}.`);
    } catch (error) {
      const message = getErrorMessage(error, "Could not create manager account.");
      setInlineError(message);
      setInlineSuccess(null);
      pushErrorAlertFromError(error, "Could not create manager account.");
    }
  };

  const handleDeleteManager = async () => {
    if (!deleteTarget) {
      return;
    }

    try {
      await runMutation(deleteManagerMutation.mutate, deleteTarget.id);
      setInlineError(null);
      setInlineSuccess(`Manager account deleted for ${deleteTarget.displayName}.`);
      setDeleteTarget(null);
    } catch (error) {
      const message = getErrorMessage(error, "Could not delete manager account.");
      setInlineError(message);
      setInlineSuccess(null);
      pushErrorAlertFromError(error, "Could not delete manager account.");
    }
  };

  return (
    <div className={styles.page}>
      <PageHeader title="Manager profile" subtitle="Account access and manager team" backTo={-1} />

      <section className={styles.panel}>
        <div className={styles.profileHeader}>
          <span className={styles.avatar} aria-hidden="true">
            {initials}
          </span>
          <div className={styles.profileTitle}>
            <span className={styles.eyebrow}>Current manager</span>
            <strong>{profile?.displayName ?? session?.displayName ?? "Manager"}</strong>
            <span>@{profile?.userName ?? session?.userName ?? "manager"}</span>
          </div>
        </div>

        {inlineError ? <ErrorBanner>{inlineError}</ErrorBanner> : null}
        {profileQuery.error ? <ErrorBanner>Could not load manager profile.</ErrorBanner> : null}
        {inlineSuccess ? <div className={styles.successBanner}>{inlineSuccess}</div> : null}

        {profileQuery.isLoading && !profile ? (
          <p className={styles.stateText}>Loading manager profile...</p>
        ) : (
          <div className={styles.grid}>
            <div className={styles.profileColumn}>
              <div className={styles.card}>
                <div className={styles.cardHeader}>
                  <div>
                    <span className={styles.eyebrow}>Profile access</span>
                    <h2>Sign-in details</h2>
                  </div>
                  <span className={styles.badge}>{isSystemManager ? "System account" : "Editable"}</span>
                </div>

                <div className={styles.formGrid}>
                  <label className={styles.field}>
                    <span>Display name</span>
                    <input
                      type="text"
                      value={form.displayName}
                      onChange={updateForm("displayName")}
                      placeholder="Manager name"
                      autoComplete="name"
                    />
                  </label>

                  <label className={styles.field}>
                    <span>Username</span>
                    <input
                      type="text"
                      value={form.userName}
                      onChange={updateForm("userName")}
                      placeholder="manager"
                      autoComplete="username"
                      disabled={isSystemManager}
                    />
                  </label>

                  <label className={styles.field}>
                    <span>Recovery email</span>
                    <input
                      type="email"
                      value={form.recoveryEmail}
                      onChange={updateForm("recoveryEmail")}
                      placeholder="manager@example.com"
                      autoComplete="email"
                    />
                  </label>

                  <label className={styles.field}>
                    <span>New password</span>
                    <input
                      type="password"
                      inputMode="numeric"
                      pattern="[0-9]*"
                      minLength={6}
                      maxLength={6}
                      value={form.newPassword}
                      onChange={updateForm("newPassword")}
                      placeholder={isSystemManager ? "Managed through server environment" : "Exactly 6 digits"}
                      autoComplete="654321"
                      disabled={isSystemManager}
                    />
                  </label>
                </div>

                <div className={styles.actions}>
                  <IosButton
                    label={updateProfileMutation.isPending ? "Saving..." : "Save changes"}
                    onClick={() => void handleSaveProfile()}
                    disabled={isBusy || !profile || !hasProfileChanges}
                    className={styles.primaryButton}
                  />
                </div>
              </div>

              <div className={styles.card}>
                <div className={styles.cardHeader}>
                  <div>
                    <span className={styles.eyebrow}>Manager online</span>
                    <h2>Active managers</h2>
                  </div>
                  <span className={styles.badge}>{onlineManagerCount} online</span>
                </div>

                <div className={styles.onlineList}>
                  {managerListQuery.isLoading && managerPresenceItems.length === 0 ? (
                    <p className={styles.stateText}>Loading manager activity...</p>
                  ) : managerPresenceItems.length === 0 ? (
                    <p className={styles.stateText}>No manager activity yet.</p>
                  ) : (
                    managerPresenceItems.map(({ manager, isOnline }) => (
                      <article key={manager.id} className={styles.onlineItem}>
                        <span
                          className={`${styles.onlineDot} ${isOnline ? styles.onlineDotActive : styles.onlineDotIdle}`}
                          aria-hidden="true"
                        />
                        <div className={styles.onlineDetails}>
                          <strong>{manager.displayName}</strong>
                          <span>@{manager.userName}</span>
                        </div>
                        <span className={isOnline ? styles.onlineNow : styles.lastOnline}>
                          {isOnline ? "Online now" : formatLastOnline(manager.lastLoginAtUtc)}
                        </span>
                      </article>
                    ))
                  )}
                </div>
              </div>
            </div>

            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <div>
                  <span className={styles.eyebrow}>Manager team</span>
                  <h2>Accounts in database</h2>
                </div>
                <span className={styles.badge}>{managers.length}</span>
              </div>

              <div className={styles.managerList}>
                {managerListQuery.isLoading && managers.length === 0 ? (
                  <p className={styles.stateText}>Loading managers...</p>
                ) : managers.length === 0 ? (
                  <p className={styles.stateText}>No manager accounts yet.</p>
                ) : (
                  managers.map(manager => {
                    const isCurrent = isSameManager(manager, currentManagerId, currentUserName);

                    return (
                      <article key={manager.id} className={styles.managerItem}>
                        <span className={styles.managerInitials}>{getInitials(manager.displayName)}</span>
                        <div className={styles.managerDetails}>
                          <strong>{manager.displayName}</strong>
                          <span>@{manager.userName}</span>
                          <span>{manager.recoveryEmail ?? "No recovery email"}</span>
                        </div>
                        <div className={styles.managerActions}>
                          {manager.isSystem ? (
                            <span className={styles.currentPill}>{isCurrent ? "You / System" : "System"}</span>
                          ) : isCurrent ? (
                            <span className={styles.currentPill}>You</span>
                          ) : (
                            <button
                              type="button"
                              className={styles.deleteManagerButton}
                              onClick={() => setDeleteTarget(manager)}
                              disabled={isBusy}
                            >
                              Delete
                            </button>
                          )}
                        </div>
                      </article>
                    );
                  })
                )}
              </div>

              <div className={styles.createPanel}>
                <span className={styles.eyebrow}>Add manager</span>
                <div className={styles.formGrid}>
                  <label className={styles.field}>
                    <span>Display name</span>
                    <input
                      type="text"
                      value={newManagerForm.displayName}
                      onChange={updateNewManagerForm("displayName")}
                      placeholder="New manager"
                      autoComplete="off"
                    />
                  </label>

                  <label className={styles.field}>
                    <span>Username</span>
                    <input
                      type="text"
                      value={newManagerForm.userName}
                      onChange={updateNewManagerForm("userName")}
                      placeholder="manager.two"
                      autoComplete="off"
                    />
                  </label>

                  <label className={styles.field}>
                    <span>Recovery email</span>
                    <input
                      type="email"
                      value={newManagerForm.recoveryEmail}
                      onChange={updateNewManagerForm("recoveryEmail")}
                      placeholder="new.manager@example.com"
                      autoComplete="off"
                    />
                  </label>

                  <label className={styles.field}>
                    <span>Temporary password</span>
                    <input
                      type="password"
                      inputMode="numeric"
                      pattern="[0-9]*"
                      minLength={6}
                      maxLength={6}
                      value={newManagerForm.password}
                      onChange={updateNewManagerForm("password")}
                      placeholder="Exactly 6 digits"
                      autoComplete="654321"
                    />
                  </label>
                </div>

                <div className={styles.actions}>
                  <IosButton
                    label={createManagerMutation.isPending ? "Creating..." : "Create manager"}
                    variant="secondary"
                    onClick={() => void handleCreateManager()}
                    disabled={isBusy}
                    className={styles.primaryButton}
                  />
                </div>
              </div>
            </div>
          </div>
        )}
      </section>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete manager"
        message={`Delete ${deleteTarget?.displayName ?? "this manager"}? This removes their access to the manager workflow.`}
        onCancel={() => {
          if (!deleteManagerMutation.isPending) {
            setDeleteTarget(null);
          }
        }}
        onConfirm={() => void handleDeleteManager()}
        confirmText={deleteManagerMutation.isPending ? "Deleting..." : "Delete"}
        confirmDisabled={deleteManagerMutation.isPending}
        cancelDisabled={deleteManagerMutation.isPending}
      />
    </div>
  );
}
