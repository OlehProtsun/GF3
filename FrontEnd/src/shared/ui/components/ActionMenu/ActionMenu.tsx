import { useEffect, useId, useLayoutEffect, useMemo, useRef, type KeyboardEvent, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { useDropdownPosition } from "@shared/lib/useDropdownPosition";
import styles from "./ActionMenu.module.css";

export type ActionMenuItem = {
  id: string;
  label: string;
  icon?: ReactNode;
  description?: string;
  trailingIcon?: ReactNode;
  accent?: boolean;
  destructive?: boolean;
  disabled?: boolean;
  onSelect: () => void;
};

type ActionMenuProps = {
  open: boolean;
  anchor: HTMLElement | null;
  label: string;
  heading?: { title: string; description?: string; icon?: ReactNode; avatar?: string };
  width?: number;
  alignment?: "start" | "end";
  items: readonly ActionMenuItem[];
  onClose: () => void;
};

const enabledItem = '[role="menuitem"]:not(:disabled)';

export function ActionMenu({ open, anchor, label, heading, width = 240, alignment, items, onClose }: ActionMenuProps) {
  const menuId = useId();
  const anchorRef = useMemo(() => ({ current: anchor }), [anchor]);
  const menuRef = useRef<HTMLDivElement>(null);
  const visible = open && Boolean(anchor?.isConnected);
  const position = useDropdownPosition(visible, anchorRef, menuRef, "down", width, alignment);

  useLayoutEffect(() => {
    if (!visible) return;
    const menu = menuRef.current;
    (menu?.querySelector<HTMLElement>(enabledItem) ?? menu)?.focus({ preventScroll: true });
  }, [visible, anchor]);

  useEffect(() => {
    if (!visible) return;
    function dismiss(event: PointerEvent) {
      if (event.target instanceof Node && !anchor?.contains(event.target) && !menuRef.current?.contains(event.target)) {
        onClose();
        if (anchor?.isConnected) anchor.focus({ preventScroll: true });
      }
    }
    document.addEventListener("pointerdown", dismiss);
    return () => document.removeEventListener("pointerdown", dismiss);
  }, [visible, anchor, onClose]);

  function close() {
    onClose();
    if (anchor?.isConnected) anchor.focus({ preventScroll: true });
  }

  function handleKeys(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === "Escape" || event.key === "Tab") {
      if (event.key === "Escape") {
        event.preventDefault();
        event.stopPropagation();
      }
      close();
    } else if (["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) {
      event.preventDefault();
      const buttons = [...event.currentTarget.querySelectorAll<HTMLButtonElement>(enabledItem)];
      const index = buttons.indexOf(document.activeElement as HTMLButtonElement);
      const next = event.key === "Home" ? 0 : event.key === "End" ? buttons.length - 1 :
        event.key === "ArrowDown" ? (index + 1) % buttons.length : (Math.max(index, 0) - 1 + buttons.length) % buttons.length;
      buttons[next]?.focus();
    }
  }

  if (!visible) return null;
  return createPortal(
    <div ref={menuRef} className={styles.menu} data-detailed={heading ? "" : undefined} style={position} role="menu" aria-label={label}
      tabIndex={-1} onKeyDown={handleKeys}>
      <div className={styles.items}>
        {heading && <div className={styles.heading} role="presentation">
          {(heading.icon || heading.avatar) && <span className={styles.headingIcon} data-avatar={heading.avatar ? "" : undefined} aria-hidden="true">
            {heading.avatar || heading.icon}
          </span>}
          <span className={styles.copy}><strong>{heading.title}</strong>{heading.description && <small>{heading.description}</small>}</span>
        </div>}
        {items.map(item => <button key={item.id} type="button" role="menuitem" tabIndex={-1}
          className={styles.item} data-destructive={item.destructive || undefined} data-accent={item.accent || undefined} disabled={item.disabled}
          aria-label={item.description ? item.label : undefined} aria-describedby={item.description ? `${menuId}-${item.id}` : undefined}
          onClick={() => { if (!item.disabled) { close(); item.onSelect(); } }}>
          {item.icon && <span className={styles.itemIcon} aria-hidden="true">{item.icon}</span>}
          {item.description ? <span className={styles.copy}><strong>{item.label}</strong><small id={`${menuId}-${item.id}`}>{item.description}</small></span> : item.label}
          {item.trailingIcon && <span className={styles.trailingIcon} aria-hidden="true">{item.trailingIcon}</span>}
        </button>)}
      </div>
    </div>, document.body,
  );
}
