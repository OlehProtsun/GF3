import { dateTimeFormat, t } from "@shared/i18n";
import { useMemo, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import {
  useCreateCommunicationMutation,
  useDeleteCommunicationMutation,
  useManagerCommunicationsQuery,
  useUpdateCommunicationMutation,
  type CommunicationMessageDto,
} from "@entities/communications";
import { AvailabilityDateTimeField } from "@entities/availability-groups";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { pushErrorAlertFromError } from "@shared/ui/feedback/error-alerts/errorAlerts";
import { NoteIcon, PlusIcon } from "@shared/ui/icons";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./CommunicationsPage.module.css";

type FormState = {
  title: string;
  body: string;
  visibleFromLocal: string;
  visibleToLocal: string;
};

const emptyMessages: CommunicationMessageDto[] = [];
const dateTimeFormatter = dateTimeFormat(undefined, {
  month: "short",
  day: "2-digit",
  year: "numeric",
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

function toDateTimeLocalValue(date: Date) {
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return localDate.toISOString().slice(0, 16);
}

function createInitialForm(): FormState {
  const now = new Date();
  const defaultVisibleTo = new Date(now.getTime() + 24 * 60 * 60 * 1000);
  return {
    title: "",
    body: "",
    visibleFromLocal: toDateTimeLocalValue(now),
    visibleToLocal: toDateTimeLocalValue(defaultVisibleTo),
  };
}

function formatDateTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return t("Unknown date");
  }

  return dateTimeFormatter.format(date);
}

function getDeadlineTone(message: CommunicationMessageDto) {
  const visibleFrom = new Date(message.visibleFromUtc).getTime();
  if (visibleFrom > Date.now()) {
    return t("Scheduled");
  }

  if (!message.isActive) {
    return t("Expired");
  }

  const deadline = new Date(message.deadlineAtUtc).getTime();
  const hoursLeft = (deadline - Date.now()) / 3_600_000;

  if (hoursLeft <= 24) {
    return t("Due soon");
  }

  return t("Active");
}

function getMessageStatusClassName(message: CommunicationMessageDto) {
  if (getDeadlineTone(message) === "Scheduled") {
    return styles.statusScheduled;
  }

  if (!message.isActive) {
    return styles.statusExpired;
  }

  return getDeadlineTone(message) === "Due soon" ? styles.statusSoon : styles.statusActive;
}

export function CommunicationsPage() {
  const communicationsQuery = useManagerCommunicationsQuery();
  const createCommunicationMutation = useCreateCommunicationMutation();
  const updateCommunicationMutation = useUpdateCommunicationMutation();
  const deleteCommunicationMutation = useDeleteCommunicationMutation();
  const messages = communicationsQuery.data ?? emptyMessages;
  const [form, setForm] = useState<FormState>(() => createInitialForm());
  const [inlineError, setInlineError] = useState<string | null>(null);
  const [inlineSuccess, setInlineSuccess] = useState<string | null>(null);
  const [editingMessageId, setEditingMessageId] = useState<number | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<CommunicationMessageDto | null>(null);
  const isSaving = createCommunicationMutation.isPending || updateCommunicationMutation.isPending;

  const activeCount = useMemo(
    () => messages.filter(message => message.isActive).length,
    [messages],
  );

  const updateForm = (field: keyof FormState) => (
    event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) => {
    setForm(current => ({ ...current, [field]: event.target.value }));
  };

  const resetForm = () => {
    setForm(createInitialForm());
    setEditingMessageId(null);
  };

  const beginEdit = (message: CommunicationMessageDto) => {
    setEditingMessageId(message.id);
    setForm({
      title: message.title,
      body: message.body,
      visibleFromLocal: toDateTimeLocalValue(new Date(message.visibleFromUtc)),
      visibleToLocal: toDateTimeLocalValue(new Date(message.deadlineAtUtc)),
    });
    setInlineError(null);
    setInlineSuccess(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const title = form.title.trim();
    const body = form.body.trim();
    const visibleFromDate = new Date(form.visibleFromLocal);
    const visibleToDate = new Date(form.visibleToLocal);

    if (!title || !body) {
      setInlineError(t("Title and message are required."));
      setInlineSuccess(null);
      return;
    }

    if (Number.isNaN(visibleFromDate.getTime())) {
      setInlineError(t("Visible from is required."));
      setInlineSuccess(null);
      return;
    }

    if (Number.isNaN(visibleToDate.getTime()) || visibleToDate.getTime() <= Date.now()) {
      setInlineError(t("Visible to must be in the future."));
      setInlineSuccess(null);
      return;
    }

    if (visibleToDate.getTime() <= visibleFromDate.getTime()) {
      setInlineError(t("Visible to must be later than visible from."));
      setInlineSuccess(null);
      return;
    }

    try {
      const payload = {
        title,
        body,
        visibleFromUtc: visibleFromDate.toISOString(),
        deadlineAtUtc: visibleToDate.toISOString(),
      };
      const saved = editingMessageId === null
        ? await runMutation(createCommunicationMutation.mutate, payload)
        : await runMutation(updateCommunicationMutation.mutate, { id: editingMessageId, ...payload });

      const successVerb = editingMessageId === null ? "created" : "updated";
      resetForm();
      setInlineError(null);
      setInlineSuccess(t("Communication \"{0}\" was {1} and is visible until {2}.", saved.title, successVerb, formatDateTime(saved.deadlineAtUtc)));
    } catch (error) {
      const message = getErrorMessage(error, t("Could not create communication."));
      setInlineError(message);
      setInlineSuccess(null);
      pushErrorAlertFromError(error, t("Could not create communication."));
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) {
      return;
    }

    try {
      await runMutation(deleteCommunicationMutation.mutate, deleteTarget.id);
      if (editingMessageId === deleteTarget.id) {
        resetForm();
      }
      setInlineError(null);
      setInlineSuccess(t("Communication \"{0}\" was deleted.", deleteTarget.title));
      setDeleteTarget(null);
    } catch (error) {
      const message = getErrorMessage(error, t("Could not delete communication."));
      setInlineError(message);
      pushErrorAlertFromError(error, t("Could not delete communication."));
    }
  };

  return (
    <div className={styles.page}>
      <PageHeader title={t("Communications")} subtitle={t("Employee messages with live deadlines")} backTo={-1} />

      <div className={styles.layout}>
        <section className={styles.panel}>
          <div className={styles.panelHeader}>
            <div className={styles.panelTitle}>
              <span className={styles.panelIcon} aria-hidden="true">
                <NoteIcon size={18} />
              </span>
              <div>
                <span className={styles.eyebrow}>{editingMessageId === null ? t("New message") : t("Editing message")}</span>
                <strong>{editingMessageId === null ? t("Create communication") : t("Update communication")}</strong>
              </div>
            </div>
          </div>

          {inlineError ? <ErrorBanner>{inlineError}</ErrorBanner> : null}
          {inlineSuccess ? <div className={styles.successBanner}>{inlineSuccess}</div> : null}

          <form className={styles.form} onSubmit={handleSubmit}>
            <label className={styles.field}>
              <span>{t("Title")}</span>
              <input
                value={form.title}
                onChange={updateForm("title")}
                maxLength={160}
                placeholder={t("Short announcement title")}
              />
            </label>

            <AvailabilityDateTimeField
              id="communication-visible-from"
              label={t("Visible From")}
              value={form.visibleFromLocal}
              defaultTime="09:00"
              onChange={value => setForm(current => ({ ...current, visibleFromLocal: value }))}
            />

            <AvailabilityDateTimeField
              id="communication-visible-to"
              label={t("Visible To")}
              value={form.visibleToLocal}
              defaultTime="23:59"
              onChange={value => setForm(current => ({ ...current, visibleToLocal: value }))}
            />

            <label className={styles.field}>
              <span>{t("Message")}</span>
              <textarea
                value={form.body}
                onChange={updateForm("body")}
                maxLength={4000}
                rows={8}
                placeholder={t("Write the update employees need to see when they sign in.")}
              />
            </label>

            <div className={styles.formFooter}>
              <span>{form.body.trim().length}/4000</span>
              <div className={styles.formActions}>
                {editingMessageId !== null ? (
                  <IosButton
                    type="button"
                    label={t("Cancel edit")}
                    variant="secondary"
                    onClick={resetForm}
                    disabled={isSaving}
                  />
                ) : null}
                <IosButton
                  type="submit"
                  label={isSaving ? t("Saving...") : editingMessageId === null ? t("Create") : t("Save changes")}
                  icon={<PlusIcon size={15} />}
                  disabled={isSaving}
                />
              </div>
            </div>
          </form>
        </section>

        <section className={styles.panel}>
          <div className={styles.panelHeader}>
            <div className={styles.panelTitle}>
              <span className={styles.panelIcon} aria-hidden="true">
                <NoteIcon size={18} />
              </span>
              <div>
                <span className={styles.eyebrow}>{t("Published")}</span>
                <strong>{t("Message board")}</strong>
              </div>
            </div>

            <div className={styles.headerPills}>
              <span className={styles.countPill}>{activeCount}  {t("active")}</span>
              <span className={styles.countPill}>{messages.length}  {t("total")}</span>
            </div>
          </div>

          {communicationsQuery.error ? (
            <div className={styles.stateText}>
              {getErrorMessage(communicationsQuery.error, t("Could not load communications."))}
            </div>
          ) : communicationsQuery.isLoading ? (
            <div className={styles.stateText}>{t("Loading communications...")}</div>
          ) : messages.length === 0 ? (
            <div className={styles.stateText}>{t("No communications have been created yet.")}</div>
          ) : (
            <div className={styles.messageList}>
              {messages.map(message => (
                <article key={message.id} className={styles.messageItem}>
                  <div className={styles.messageTopLine}>
                    <h2>{message.title}</h2>
                    <span className={[styles.statusBadge, getMessageStatusClassName(message)].join(" ")}>
                      {getDeadlineTone(message)}
                    </span>
                  </div>
                  <p>{message.body}</p>
                  <div className={styles.messageMeta}>
                    <span>{t("Visible from")} {formatDateTime(message.visibleFromUtc)}</span>
                    <span>{t("Visible to")} {formatDateTime(message.deadlineAtUtc)}</span>
                    <span>{t("Created by")} {message.createdByManagerName}</span>
                    <span>{formatDateTime(message.createdAtUtc)}</span>
                  </div>
                  <div className={styles.messageActions}>
                    <button type="button" onClick={() => beginEdit(message)} disabled={isSaving}>
                      {t("Edit")}</button>
                    <button
                      type="button"
                      className={styles.deleteButton}
                      onClick={() => setDeleteTarget(message)}
                      disabled={deleteCommunicationMutation.isPending}
                    >
                      {t("Delete")}</button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </div>

      <ConfirmDialog
        open={deleteTarget !== null}
        title={t("Delete communication?")}
        message={deleteTarget ? t("Employees will no longer see \"{0}\".", deleteTarget.title) : ""}
        confirmText={deleteCommunicationMutation.isPending ? t("Deleting...") : t("Delete")}
        onConfirm={() => void handleDelete()}
        onCancel={() => setDeleteTarget(null)}
        confirmDisabled={deleteCommunicationMutation.isPending}
        cancelDisabled={deleteCommunicationMutation.isPending}
      />
    </div>
  );
}
