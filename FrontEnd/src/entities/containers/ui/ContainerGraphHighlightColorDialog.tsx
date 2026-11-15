import { t } from "@shared/i18n";
import { useEffect, useId, useState, type MouseEvent } from "react";
import { createPortal } from "react-dom";
import { IosButton } from "@shared/ui/components/IosButton";
import { CheckIcon, CloseIcon } from "@shared/ui/icons";
import styles from "./ContainerGraphHighlightColorDialog.module.css";

type ContainerGraphHighlightColorDialogProps = {
  open: boolean;
  value: string;
  eyebrow: string;
  title: string;
  inputLabel: string;
  isSaving: boolean;
  onCancel: () => void;
  onSave: (value: string) => void;
};

const highlightColors = [
  { get label() { return t("Soft blue"); }, value: "#DBEAFE" },
  { get label() { return t("Sky"); }, value: "#BAE6FD" },
  { get label() { return t("Mint"); }, value: "#BBF7D0" },
  { get label() { return t("Aqua"); }, value: "#99F6E4" },
  { get label() { return t("Lavender"); }, value: "#DDD6FE" },
  { get label() { return t("Cream"); }, value: "#FEF3C7" },
  { get label() { return t("Amber"); }, value: "#FDE68A" },
  { get label() { return t("Peach"); }, value: "#FED7AA" },
  { get label() { return t("Rose"); }, value: "#FECDD3" },
  { get label() { return t("Lilac"); }, value: "#F5D0FE" },
];

function normalizeHexColor(value: string) {
  const raw = value.trim().replace(/^#/, "");
  return /^[0-9a-fA-F]{6}$/.test(raw) ? `#${raw.toUpperCase()}` : null;
}

export function ContainerGraphHighlightColorDialog({
  open,
  value,
  eyebrow,
  title,
  inputLabel,
  isSaving,
  onCancel,
  onSave,
}: ContainerGraphHighlightColorDialogProps) {
  const titleId = useId();
  const [draft, setDraft] = useState(value);

  useEffect(() => {
    if (open) setDraft(value);
  }, [open, value]);

  useEffect(() => {
    if (!open) return;
    const handleKeyDown = (event: KeyboardEvent) => event.key === "Escape" && onCancel();
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onCancel, open]);

  if (!open) return null;

  const normalizedDraft = normalizeHexColor(draft);
  const previewColor = normalizedDraft ?? value;
  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) onCancel();
  };

  return createPortal(
    <div className={styles.overlay} role="dialog" aria-modal="true" aria-labelledby={titleId}
      onMouseDown={handleOverlayMouseDown}>
      <div className={styles.dialog}>
        <header className={styles.header}>
          <div><span>{eyebrow}</span><h3 id={titleId}>{title}</h3></div>
          <button type="button" aria-label={t("Close color dialog")} onClick={onCancel}><CloseIcon size={16} /></button>
        </header>
        <div className={styles.palette} aria-label={t("Highlight colors")}>
          {highlightColors.map(option => (
            <button key={option.value} type="button" aria-label={option.label}
              aria-pressed={normalizedDraft === option.value}
              className={normalizedDraft === option.value ? styles.selected : ""}
              style={{ backgroundColor: option.value }} onClick={() => setDraft(option.value)} />
          ))}
        </div>
        <label className={styles.hexControl}>
          <span className={styles.hexSwatch} style={{ backgroundColor: previewColor }} aria-hidden="true" />
          <input value={draft} maxLength={7} aria-label={inputLabel}
            onChange={event => setDraft(event.target.value)} />
        </label>
        {!normalizedDraft ? <p className={styles.error}>{t("Enter a color like #BBF7D0.")}</p> : null}
        <footer className={styles.actions}>
          <IosButton label={t("Cancel")} variant="secondary" size="compact" onClick={onCancel} />
          <IosButton label={isSaving ? t("Saving...") : t("Use color")} size="compact" icon={<CheckIcon size={15} />}
            disabled={!normalizedDraft || isSaving} onClick={() => normalizedDraft && onSave(normalizedDraft)} />
        </footer>
      </div>
    </div>,
    document.body,
  );
}
