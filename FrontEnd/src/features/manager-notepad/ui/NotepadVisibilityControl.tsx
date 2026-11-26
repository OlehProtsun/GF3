import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { t } from "@shared/i18n";
import { useDropdownPosition } from "@shared/lib/useDropdownPosition";
import { CheckIcon, ShareIcon } from "@shared/ui/icons";
import { NotepadIcon } from "./NotepadIcon";
import { NotepadShareRecipients } from "./NotepadShareRecipients";
import type { NoteAccess } from "./useNotepadAutosave";
import styles from "./NotepadViews.module.css";

const options = [
  { value: "private", label: "Private", icon: "lock", detail: "Only you can read this note." },
  { value: "public", label: "Public", icon: "users", detail: "All signed-in managers can read this note." },
  { value: "custom", label: "Custom", icon: "users", detail: "Selected managers can read this note." },
] as const;

export function NotepadVisibilityControl({ value, expanded, busy, onChange }: {
  value: NoteAccess;
  expanded: boolean;
  busy: boolean;
  onChange: (access: NoteAccess) => void;
}) {
  const [open, setOpen] = useState(false);
  const anchor = useRef<HTMLButtonElement>(null);
  const popover = useRef<HTMLDivElement>(null);
  const visible = open && expanded;
  const position = useDropdownPosition(visible, anchor, popover, "down", 302, "end");
  const selected = options.find(option => option.value === value.visibility) ?? options[0];

  useLayoutEffect(() => {
    if (visible) popover.current?.querySelector<HTMLButtonElement>('[aria-pressed="true"]')?.focus({ preventScroll: true });
  }, [visible]);

  useEffect(() => {
    if (!visible) return;
    const dismiss = (event: PointerEvent) => {
      if (event.target instanceof Node && !anchor.current?.contains(event.target) && !popover.current?.contains(event.target)) {
        setOpen(false);
      }
    };
    document.addEventListener("pointerdown", dismiss);
    return () => document.removeEventListener("pointerdown", dismiss);
  }, [visible]);

  return (
    <>
      <button ref={anchor} type="button" className={styles.visibilityButton} aria-haspopup="dialog"
        aria-label={t(selected.label)} title={t(selected.label)} aria-expanded={visible} disabled={busy} onClick={() => setOpen(!open)}>
        {selected.value === "custom" ? <ShareIcon size={15} /> : <NotepadIcon name={selected.icon} size={15} />}<span className={styles.compactLabel}>{t(selected.label)}</span><NotepadIcon name="chevron" size={13} />
      </button>
      {visible && createPortal(
        <div ref={popover} className={styles.visibilityPopover} style={position} role="dialog" aria-label={t("Visibility")}
          onKeyDown={event => {
            if (event.key !== "Escape") return;
            event.preventDefault();
            event.stopPropagation();
            setOpen(false);
            anchor.current?.focus();
          }}>
          <div className={styles.visibilityContents}>
            <strong>{t("Visibility")}</strong>
            <NotepadVisibilityOptions value={value} busy={busy} onChange={onChange} />
          </div>
        </div>, anchor.current?.closest("[data-manager-notepad-modal]") ?? document.body,
      )}
    </>
  );
}

export function NotepadVisibilityOptions({ value, busy, onChange }: {
  value: NoteAccess; busy: boolean; onChange: (access: NoteAccess) => void;
}) {
  return (
    <>
      {options.map(option => (
        <button key={option.value} type="button" aria-pressed={option.value === value.visibility} disabled={busy}
          onClick={() => onChange({
            visibility: option.value,
            sharedWithManagerIds: option.value === "custom" ? value.sharedWithManagerIds : [],
          })}>
          {option.value === "custom" ? <ShareIcon size={17} /> : <NotepadIcon name={option.icon} size={17} />}
          <span><strong>{t(option.label)}</strong><small>{t(option.detail)}</small></span>
          {option.value === value.visibility && <CheckIcon size={15} />}
        </button>
      ))}
      {value.visibility === "custom" && (
        <NotepadShareRecipients recipients={value.sharedWithManagerIds} busy={busy}
          onChange={ids => onChange({ ...value, sharedWithManagerIds: ids })} />
      )}
    </>
  );
}
