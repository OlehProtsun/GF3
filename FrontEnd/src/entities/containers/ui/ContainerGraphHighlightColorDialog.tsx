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
  { label: "Soft blue", value: "#DBEAFE" },
  { label: "Sky", value: "#BAE6FD" },
  { label: "Mint", value: "#BBF7D0" },
  { label: "Aqua", value: "#99F6E4" },
  { label: "Lavender", value: "#DDD6FE" },
  { label: "Cream", value: "#FEF3C7" },
  { label: "Amber", value: "#FDE68A" },
  { label: "Peach", value: "#FED7AA" },
  { label: "Rose", value: "#FECDD3" },
  { label: "Lilac", value: "#F5D0FE" },
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
          <button type="button" aria-label="Close color dialog" onClick={onCancel}><CloseIcon size={16} /></button>
        </header>
        <div className={styles.palette} aria-label="Highlight colors">
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
        {!normalizedDraft ? <p className={styles.error}>Enter a color like #BBF7D0.</p> : null}
        <footer className={styles.actions}>
          <IosButton label="Cancel" variant="secondary" size="compact" onClick={onCancel} />
          <IosButton label={isSaving ? "Saving..." : "Use color"} size="compact" icon={<CheckIcon size={15} />}
            disabled={!normalizedDraft || isSaving} onClick={() => normalizedDraft && onSave(normalizedDraft)} />
        </footer>
      </div>
    </div>,
    document.body,
  );
}
