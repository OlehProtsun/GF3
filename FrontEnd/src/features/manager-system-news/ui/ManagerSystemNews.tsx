import { dateTimeFormat, t } from "@shared/i18n";
import { useEffect, useMemo, useState, type ChangeEvent, type FormEvent } from "react";
import {
  systemNewsApi,
  useCreateSystemNewsMutation,
  useDeleteSystemNewsMutation,
  useMarkAllSystemNewsReadMutation,
  useMarkSystemNewsReadMutation,
  useSystemNewsQuery,
  useUpdateSystemNewsMutation,
  type SaveSystemNewsInput,
  type SystemNewsAudience,
  type SystemNewsMessage,
} from "@entities/system-news";
import { ApiError, getErrorMessage } from "@shared/api/httpClient";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { CloseIcon, NewsIcon, PlusIcon, SaveIcon, SettingsIcon } from "@shared/ui/icons";
import styles from "./ManagerSystemNews.module.css";

const emptyDraft: SaveSystemNewsInput = { title: "", body: "", audience: "all", imageUrl: null, videoUrl: null };

function formatDate(value: string) {
  return dateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

export function getYoutubeEmbedUrl(value: string | null) {
  if (!value) return null;
  try {
    const url = new URL(value);
    const id = url.hostname === "youtu.be" ? url.pathname.slice(1).split("/")[0] :
      url.pathname.startsWith("/shorts/") || url.pathname.startsWith("/embed/") ? url.pathname.split("/")[2] : url.searchParams.get("v");
    return id && /^[\w-]{6,20}$/.test(id) ? `https://www.youtube-nocookie.com/embed/${id}` : null;
  } catch { return null; }
}

function NewsCard({ message, onRead }: { message: SystemNewsMessage; onRead: (id: number) => void }) {
  const embedUrl = getYoutubeEmbedUrl(message.videoUrl);
  return (
    <article className={[styles.newsCard, !message.isRead ? styles.newsCardUnread : ""].filter(Boolean).join(" ")}>
      <div className={styles.cardHeading}>
        <div>
          <strong>{message.title}</strong>
          <time dateTime={message.createdAtUtc}>{formatDate(message.createdAtUtc)}</time>
        </div>
        {!message.isRead ? <span className={styles.unreadDot} aria-label={t("Unread")} /> : null}
      </div>
      <p>{message.body}</p>
      {message.imageUrl ? <img className={styles.newsImage} src={message.imageUrl} alt="" /> : null}
      {embedUrl ? (
        <div className={styles.videoFrame}>
          <iframe src={embedUrl} title={t("{0} video", message.title)} allow="accelerometer; autoplay; encrypted-media; picture-in-picture" allowFullScreen />
        </div>
      ) : null}
      <button type="button" className={styles.readButton} disabled={message.isRead} onClick={() => onRead(message.id)}>
        {message.isRead ? t("Read") : t("Mark as read")}
      </button>
    </article>
  );
}

export function ManagerSystemNews() {
  const newsQuery = useSystemNewsQuery();
  const markRead = useMarkSystemNewsReadMutation();
  const markAllRead = useMarkAllSystemNewsReadMutation();
  const createNews = useCreateSystemNewsMutation();
  const updateNews = useUpdateSystemNewsMutation();
  const deleteNews = useDeleteSystemNewsMutation();
  const [isOpen, setIsOpen] = useState(false);
  const [showAccess, setShowAccess] = useState(false);
  const [showAdmin, setShowAdmin] = useState(false);
  const [password, setPassword] = useState("");
  const [accessError, setAccessError] = useState<string | null>(null);
  const [adminMessages, setAdminMessages] = useState<SystemNewsMessage[]>([]);
  const [draft, setDraft] = useState<SaveSystemNewsInput>(emptyDraft);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const messages = newsQuery.data ?? [];
  const unreadCount = useMemo(() => messages.filter(item => !item.isRead).length, [messages]);

  useEffect(() => {
    const closeForOtherOverlay = (event: Event) => {
      if ((event as CustomEvent<string>).detail !== "news") setIsOpen(false);
    };
    window.addEventListener("gf3:manager-overlay-open", closeForOtherOverlay);
    return () => window.removeEventListener("gf3:manager-overlay-open", closeForOtherOverlay);
  }, []);

  const openNews = () => {
    window.dispatchEvent(new CustomEvent("gf3:manager-overlay-open", { detail: "news" }));
    setIsOpen(true);
  };

  const refreshAdmin = async () => setAdminMessages(await systemNewsApi.adminList());
  const unlock = async (event: FormEvent) => {
    event.preventDefault();
    setAccessError(null);
    try {
      const data = await systemNewsApi.unlockAdmin(password.trim());
      setAdminMessages(data);
      setPassword("");
      setShowAccess(false);
      setShowAdmin(true);
    } catch (error) {
      setAccessError(error instanceof ApiError ? error.message : t("Could not verify the developer password."));
    }
  };

  const startEdit = (message: SystemNewsMessage) => {
    setEditingId(message.id);
    setDraft({ title: message.title, body: message.body, audience: message.audience, imageUrl: message.imageUrl, videoUrl: message.videoUrl });
    setActionError(null);
  };
  const resetDraft = () => { setEditingId(null); setDraft(emptyDraft); setActionError(null); };
  const save = (event: FormEvent) => {
    event.preventDefault();
    const options = {
      onSuccess: async () => { await refreshAdmin(); resetDraft(); void newsQuery.refetch(); },
      onError: (error: unknown) => setActionError(getErrorMessage(error, t("Could not save this news update."))),
    };
    if (editingId === null) createNews.mutate(draft, options);
    else updateNews.mutate({ messageId: editingId, payload: draft }, options);
  };
  const confirmDelete = () => {
    if (deleteId === null) return;
    deleteNews.mutate(deleteId, {
      onSuccess: async () => { setDeleteId(null); await refreshAdmin(); if (editingId === deleteId) resetDraft(); void newsQuery.refetch(); },
      onError: error => { setDeleteId(null); setActionError(getErrorMessage(error, t("Could not delete this update."))); },
    });
  };
  const loadImage = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;
    if (!file.type.startsWith("image/") || file.size > 2_000_000) {
      setActionError(t("Choose an image up to 2 MB."));
      return;
    }
    const reader = new FileReader();
    reader.onload = () => setDraft(current => ({ ...current, imageUrl: String(reader.result) }));
    reader.onerror = () => setActionError(t("Could not read this image."));
    reader.readAsDataURL(file);
  };

  const isSaving = createNews.isPending || updateNews.isPending;
  return (
    <>
      <button type="button" className={[styles.openTab, isOpen ? styles.openTabHidden : ""].filter(Boolean).join(" ")} onClick={openNews} aria-label={t("Open system news")} title={t("Open system news")}>
        <NewsIcon size={17} />{unreadCount > 0 ? <span className={styles.triggerDot} aria-label={t("{0} unread", unreadCount)} /> : null}
      </button>
      <aside className={[styles.panel, isOpen ? styles.panelOpen : styles.panelClosed].join(" ")} aria-label={t("System news")} aria-hidden={!isOpen}>
        <header className={styles.header}>
          <div className={styles.heading}><span><NewsIcon size={19} /></span><div><strong>{t("News")}</strong><small>{unreadCount ? t("{0} unread", unreadCount) : t("{0} updates", messages.length)}</small></div></div>
          <div className={styles.headerActions}>
            <button type="button" aria-label={t("News settings")} onClick={() => setShowAccess(true)}><SettingsIcon size={16} /></button>
            <button type="button" aria-label={t("Close system news")} onClick={() => setIsOpen(false)}><CloseIcon size={15} /></button>
          </div>
        </header>
        <div className={styles.toolbar}>
          <span>{t("System updates")}</span>
          {unreadCount > 0 ? <button type="button" onClick={() => markAllRead.mutate(undefined)}>{t("Mark all read")}</button> : null}
        </div>
        <div className={styles.newsList}>
          {newsQuery.isLoading ? <div className={styles.empty}>{t("Loading updates...")}</div> : null}
          {newsQuery.error ? <div className={styles.error}>{getErrorMessage(newsQuery.error, t("Could not load system news."))}</div> : null}
          {!newsQuery.isLoading && !newsQuery.error && messages.length === 0 ? <div className={styles.empty}><NewsIcon size={28} /><strong>{t("No updates yet")}</strong><span>{t("New system announcements will appear here.")}</span></div> : null}
          {messages.map(message => <NewsCard key={message.id} message={message} onRead={id => markRead.mutate(id)} />)}
        </div>
      </aside>

      {showAccess ? (
        <div className={styles.modalBackdrop} role="dialog" aria-modal="true" aria-label={t("Developer access")}>
          <form className={styles.accessDialog} onSubmit={unlock}>
            <span className={styles.modalIcon}><SettingsIcon size={22} /></span>
            <h2>{t("News settings")}</h2><p>{t("Enter the developer password configured on the GF3 host.")}</p>
            <label><span>{t("Developer password")}</span><input type="password" autoFocus autoComplete="off" value={password} onChange={event => { setPassword(event.target.value); setAccessError(null); }} /></label>
            {accessError ? <div className={styles.error}>{accessError}</div> : null}
            <div className={styles.modalActions}><button type="button" onClick={() => setShowAccess(false)}>{t("Cancel")}</button><button type="submit" disabled={!password.trim()}>{t("Unlock")}</button></div>
          </form>
        </div>
      ) : null}

      {showAdmin ? (
        <div className={styles.modalBackdrop} role="dialog" aria-modal="true" aria-label={t("Manage system news")}>
          <section className={styles.adminWindow}>
            <header><div><span>{t("Developer tools")}</span><h2>{t("System news")}</h2></div><button type="button" aria-label={t("Close news manager")} onClick={() => setShowAdmin(false)}><CloseIcon size={17} /></button></header>
            <div className={styles.adminBody}>
              <div className={styles.adminList}>
                <button type="button" className={styles.newAdminButton} onClick={resetDraft}><PlusIcon size={15} />{t("New update")}</button>
                {adminMessages.map(message => <button type="button" key={message.id} className={editingId === message.id ? styles.adminItemActive : ""} onClick={() => startEdit(message)}><strong>{message.title}</strong><span>{message.audience} · {formatDate(message.createdAtUtc)}</span></button>)}
              </div>
              <form className={styles.editor} onSubmit={save}>
                <div className={styles.editorTitle}><div><span>{editingId === null ? t("Create") : t("Edit")}</span><h3>{editingId === null ? t("New update") : t("Update message")}</h3></div>{editingId !== null ? <button type="button" className={styles.deleteButton} onClick={() => setDeleteId(editingId)}>{t("Delete")}</button> : null}</div>
                <label><span>{t("Title")}</span><input maxLength={160} value={draft.title} onChange={event => setDraft(current => ({ ...current, title: event.target.value }))} /></label>
                <label><span>{t("Text")}</span><textarea maxLength={10_000} rows={7} value={draft.body} onChange={event => setDraft(current => ({ ...current, body: event.target.value }))} /></label>
                <label><span>{t("Visible to")}</span><select value={draft.audience} onChange={event => setDraft(current => ({ ...current, audience: event.target.value as SystemNewsAudience }))}><option value="all">{t("Everyone")}</option><option value="managers">{t("Managers only")}</option><option value="employees">{t("Employees only")}</option></select></label>
                <div className={styles.mediaGrid}>
                  <label><span>{t("Image URL")}</span><input value={draft.imageUrl?.startsWith("data:") ? t("Uploaded image") : draft.imageUrl ?? ""} disabled={draft.imageUrl?.startsWith("data:")} placeholder="https://..." onChange={event => setDraft(current => ({ ...current, imageUrl: event.target.value || null }))} /></label>
                  <label className={styles.fileButton}><span>{t("Upload image")}</span><input type="file" accept="image/*" onChange={loadImage} /></label>
                </div>
                {draft.imageUrl ? <div className={styles.imagePreview}><img src={draft.imageUrl} alt={t("Preview")} /><button type="button" onClick={() => setDraft(current => ({ ...current, imageUrl: null }))}>{t("Remove")}</button></div> : null}
                <label><span>{t("YouTube link")}</span><input maxLength={500} placeholder="https://youtube.com/watch?v=..." value={draft.videoUrl ?? ""} onChange={event => setDraft(current => ({ ...current, videoUrl: event.target.value || null }))} /></label>
                {actionError ? <div className={styles.error}>{actionError}</div> : null}
                <div className={styles.editorActions}><button type="button" onClick={resetDraft}>{t("Clear")}</button><button type="submit" disabled={isSaving || !draft.title.trim() || !draft.body.trim()}><SaveIcon size={15} />{isSaving ? t("Saving...") : t("Save update")}</button></div>
              </form>
            </div>
          </section>
        </div>
      ) : null}
      <ConfirmDialog open={deleteId !== null} title={t("Delete update?")} message={t("This system news message and its read history will be permanently deleted.")} confirmText={t("Delete")} onConfirm={confirmDelete} onCancel={() => setDeleteId(null)} confirmDisabled={deleteNews.isPending} cancelDisabled={deleteNews.isPending} />
    </>
  );
}
