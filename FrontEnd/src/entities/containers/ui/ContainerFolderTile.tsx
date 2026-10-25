import { useId, useRef, useState, type CSSProperties } from "react";
import { t } from "@shared/i18n";
import { PinIcon } from "@shared/ui/icons";
import { useContainerGraphsQuery } from "../api/queries";
import { getContainerDisplayName, getGraphMonthYearLabel } from "../model/presentation";
import type { Container } from "../model/types";
import styles from "./ContainerFolderTile.module.css";

type ContainerFolderTileProps = {
  container: Container;
  isPinned: boolean;
  onTogglePin: (containerId: number) => void;
  onContainerOpen: (containerId: number) => void;
  onGraphOpen: (containerId: number, graphId: number) => void;
};

export function ContainerFolderTile({ container, isPinned, onTogglePin, onContainerOpen }: ContainerFolderTileProps) {
  const [isPreviewOpen, setIsPreviewOpen] = useState(false);
  const pointerActivation = useRef(false);
  const previewId = useId();
  const graphs = useContainerGraphsQuery(container.id, false);
  const documents = graphs.data ? graphs.data.slice(0, 3) : [null, null, null];
  const displayName = getContainerDisplayName(container);
  const note = container.note?.trim();

  return (
    <article
      className={styles.tile}
      data-variant={Math.abs(container.id) % 5}
      data-preview-open={isPreviewOpen}
      onPointerEnter={event => {
        if (event.pointerType === "mouse" || event.pointerType === "pen") setIsPreviewOpen(true);
      }}
      onPointerLeave={event => {
        if (event.pointerType === "mouse" || event.pointerType === "pen") setIsPreviewOpen(false);
      }}
      onPointerDownCapture={() => { pointerActivation.current = true; }}
      onPointerUpCapture={() => { pointerActivation.current = false; }}
      onPointerCancel={() => { pointerActivation.current = false; }}
      onFocus={event => {
        if (!pointerActivation.current && !event.currentTarget.contains(event.relatedTarget)) setIsPreviewOpen(true);
      }}
      onBlur={event => {
        // WebKit can blur the previous control without focusing the clicked button.
        if (!pointerActivation.current && !event.currentTarget.contains(event.relatedTarget)) setIsPreviewOpen(false);
      }}
      onKeyDown={event => {
        if (event.key === "Escape") {
          setIsPreviewOpen(false);
          event.stopPropagation();
        }
      }}
    >
      <div className={styles.back} aria-hidden="true" />
      <button type="button" className={styles.front} aria-label={t("Open {0} profile", displayName)} onClick={() => onContainerOpen(container.id)}>
        <span className={styles.heading}>
          <span className={styles.name} title={displayName}>{displayName}</span>
          <span className={styles.badge}>ID {container.id}</span>
        </span>
        <span className={styles.description}>{note || t("Schedule workspace for container planning and statistics.")}</span>
        <span className={styles.status}>{note ? t("Note available") : t("No note yet")}</span>
      </button>
      <div className={styles.actions}>
        <button type="button" className={styles.action} aria-label={t(isPinned ? "Unpin {0}" : "Pin {0}", displayName)} aria-pressed={isPinned} onClick={() => onTogglePin(container.id)}>
          <PinIcon size={18} />
        </button>
        <button type="button" className={styles.action} aria-label={t("Schedules")} aria-expanded={isPreviewOpen} aria-controls={previewId} onClick={() => setIsPreviewOpen(open => !open)}>
          <span className={styles.dots} aria-hidden="true">⋮</span>
        </button>
      </div>
      <div id={previewId} className={styles.preview} aria-hidden="true" inert>
        {documents.map((graph, index) => (
          <div
            key={graph?.id ?? `paper-${index}`}
            className={styles.sheet}
            style={{ "--sheet-order": index, "--sheet-delay": `${index * 35}ms` } as CSSProperties}
          >
            {graph ? <span className={styles.sheetName}>{graph.name.trim() || getGraphMonthYearLabel(graph)}</span> : <span className={styles.paperLine} />}
            <span className={styles.paperLine} />
          </div>
        ))}
      </div>
    </article>
  );
}
