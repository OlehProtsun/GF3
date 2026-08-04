import { useMemo, useState, type FormEvent } from "react";
import {
  regulationsApi,
  useAdminRegulationAcceptancesQuery,
  useAdminRegulationsQuery,
  useCreateRegulationMutation,
  useDeleteRegulationMutation,
  usePublishRegulationMutation,
  useUpdateRegulationMutation,
  type RegulationDocument,
} from "@entities/regulations";
import { getErrorMessage } from "@shared/api/httpClient";
import { IosButton } from "@shared/ui/components/IosButton";
import styles from "./RegulationsAdminPanel.module.css";

type FormState = { title: string; version: string; message: string; pdf: File | null };
const emptyForm: FormState = { title: "", version: "", message: "", pdf: null };
const dateFormatter = new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" });

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

export function RegulationsAdminPanel() {
  const documentsQuery = useAdminRegulationsQuery();
  const acceptancesQuery = useAdminRegulationAcceptancesQuery();
  const createMutation = useCreateRegulationMutation();
  const updateMutation = useUpdateRegulationMutation();
  const publishMutation = usePublishRegulationMutation();
  const deleteMutation = useDeleteRegulationMutation();
  const [editing, setEditing] = useState<RegulationDocument | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [historySearch, setHistorySearch] = useState("");
  const isBusy = createMutation.isPending || updateMutation.isPending || publishMutation.isPending || deleteMutation.isPending;
  const acceptances = useMemo(() => {
    const search = historySearch.trim().toLowerCase();
    if (!search) return acceptancesQuery.data ?? [];
    return (acceptancesQuery.data ?? []).filter(item =>
      [item.displayName, item.username, item.accountRole, item.regulationTitle, item.regulationVersion]
        .some(value => value.toLowerCase().includes(search)),
    );
  }, [acceptancesQuery.data, historySearch]);

  const resetForm = () => {
    setEditing(null);
    setForm(emptyForm);
    setError(null);
  };

  const editDocument = (document: RegulationDocument) => {
    if (document.isPublished) return;
    setEditing(document);
    setForm({ title: document.title, version: document.version, message: document.message, pdf: null });
    setError(null);
    setFeedback(null);
  };

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    const input = { ...form };
    const callbacks = {
      onSuccess: () => {
        setFeedback(editing ? "Draft updated." : "Draft created.");
        resetForm();
      },
      onError: (submitError: unknown) => setError(getErrorMessage(submitError, "Could not save the regulation.")),
    };
    if (editing) {
      updateMutation.mutate({ documentId: editing.id, input }, callbacks);
    } else {
      createMutation.mutate(input, callbacks);
    }
  };

  const publish = (document: RegulationDocument) => {
    if (!window.confirm(`Publish ${document.title} (${document.version})? Users will be required to accept it.`)) return;
    setError(null);
    publishMutation.mutate(document.id, {
      onSuccess: () => setFeedback("Regulation published. It is now pending for managers and employees."),
      onError: publishError => setError(getErrorMessage(publishError, "Could not publish the regulation.")),
    });
  };

  const remove = (document: RegulationDocument) => {
    if (!window.confirm(`Delete draft ${document.title} (${document.version})?`)) return;
    deleteMutation.mutate(document.id, {
      onSuccess: () => { if (editing?.id === document.id) resetForm(); setFeedback("Draft deleted."); },
      onError: deleteError => setError(getErrorMessage(deleteError, "Could not delete the draft.")),
    });
  };

  const download = async (document: RegulationDocument) => {
    try {
      const file = await regulationsApi.adminDownloadPdf(document.id);
      downloadBlob(file.blob, file.fileName ?? document.pdfFileName);
    } catch (downloadError) {
      setError(getErrorMessage(downloadError, "Could not download the PDF."));
    }
  };

  return (
    <section className={styles.panel} aria-labelledby="regulations-admin-title">
      <div className={styles.header}>
        <div>
          <span className={styles.eyebrow}>Developer protected</span>
          <h2 id="regulations-admin-title">Regulations &amp; acceptance</h2>
          <p>Create immutable PDF versions, publish notices, and audit every user acceptance.</p>
        </div>
        <span className={styles.summary}>{documentsQuery.data?.length ?? 0} versions · {acceptancesQuery.data?.length ?? 0} acceptances</span>
      </div>

      {error ? <div className={styles.error} role="alert">{error}</div> : null}
      {feedback ? <div className={styles.success}>{feedback}</div> : null}

      <div className={styles.grid}>
        <form className={styles.form} onSubmit={submit}>
          <div className={styles.formHeader}>
            <h3>{editing ? `Edit draft ${editing.version}` : "New regulation version"}</h3>
            {editing ? <button type="button" onClick={resetForm}>Cancel edit</button> : null}
          </div>
          <label>Title<input value={form.title} maxLength={160} required onChange={event => setForm(current => ({ ...current, title: event.target.value }))} /></label>
          <label>Version<input value={form.version} maxLength={80} required placeholder="2026.1" onChange={event => setForm(current => ({ ...current, version: event.target.value }))} /></label>
          <label>Pop-up message<textarea value={form.message} maxLength={4000} required rows={5} onChange={event => setForm(current => ({ ...current, message: event.target.value }))} /></label>
          <label>PDF file<input type="file" accept="application/pdf,.pdf" required={!editing} onChange={event => setForm(current => ({ ...current, pdf: event.target.files?.[0] ?? null }))} /></label>
          {editing && !form.pdf ? <small>Current PDF stays unchanged unless a replacement is selected.</small> : null}
          <IosButton label={isBusy ? "Saving..." : editing ? "Update draft" : "Create draft"} type="submit" disabled={isBusy} />
        </form>

        <div className={styles.documents}>
          <h3>Document versions</h3>
          {documentsQuery.isLoading ? <div className={styles.empty}>Loading documents...</div> : (documentsQuery.data ?? []).length === 0 ? (
            <div className={styles.empty}>No regulation versions yet.</div>
          ) : (documentsQuery.data ?? []).map(document => (
            <article key={document.id} className={styles.document}>
              <div className={styles.documentTop}>
                <div><strong>{document.title}</strong><span>Version {document.version}</span></div>
                <span className={document.isPublished ? styles.published : styles.draft}>{document.isPublished ? "Published" : "Draft"}</span>
              </div>
              <p>{document.message}</p>
              <small>{document.pdfFileName} · SHA-256 {document.pdfSha256.slice(0, 14)}… · {document.acceptanceCount} accepted</small>
              <div className={styles.actions}>
                <button type="button" onClick={() => void download(document)}>PDF</button>
                {!document.isPublished ? <button type="button" onClick={() => editDocument(document)}>Edit</button> : null}
                {!document.isPublished ? <button type="button" onClick={() => publish(document)} disabled={isBusy}>Publish</button> : null}
                {!document.isPublished ? <button type="button" className={styles.danger} onClick={() => remove(document)} disabled={isBusy}>Delete</button> : null}
              </div>
            </article>
          ))}
        </div>
      </div>

      <div className={styles.history}>
        <div className={styles.historyHeader}>
          <div><h3>Acceptance history</h3><p>Managers and employees, including preserved account snapshots.</p></div>
          <input value={historySearch} placeholder="Search person, role or version" onChange={event => setHistorySearch(event.target.value)} />
        </div>
        <div className={styles.tableWrap}>
          <table><thead><tr><th>User</th><th>Role</th><th>Regulation</th><th>Accepted</th></tr></thead>
            <tbody>{acceptances.map(item => <tr key={item.id}>
              <td><strong>{item.displayName}</strong><small>@{item.username} · #{item.accountId}</small></td>
              <td>{item.accountRole}</td><td>{item.regulationTitle}<small>Version {item.regulationVersion}</small></td>
              <td>{dateFormatter.format(new Date(item.acceptedAtUtc))}</td>
            </tr>)}</tbody>
          </table>
          {!acceptancesQuery.isLoading && acceptances.length === 0 ? <div className={styles.empty}>No matching acceptances.</div> : null}
        </div>
      </div>
    </section>
  );
}
