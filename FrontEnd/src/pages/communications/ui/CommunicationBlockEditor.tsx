import { closestCenter, DndContext, KeyboardSensor, MouseSensor, TouchSensor, useSensor, useSensors } from "@dnd-kit/core";
import { arrayMove, SortableContext, sortableKeyboardCoordinates, useSortable, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { numberFormat, t } from "@shared/i18n";
import { CloseIcon, NoteIcon, PlusIcon } from "@shared/ui/icons";
import { useId, type ChangeEvent } from "react";
import {
  CommunicationGif, CommunicationPhoto, maxCommunicationBlocks, maxCommunicationGifBytes,
  maxCommunicationUploadBytes, type CommunicationDraftBlock,
} from "@entities/communications";
import styles from "./CommunicationsPage.module.css";

const kilobytes = numberFormat(undefined, { style: "unit", unit: "kilobyte", maximumFractionDigits: 1 });
const accept = { photo: ".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp", gif: ".gif,image/gif" };

type MediaType = keyof typeof accept;

function BlockIcon({ type }: { type: CommunicationDraftBlock["type"] }) {
  if (type === "text") return <NoteIcon size={16} />;
  return (
    <svg aria-hidden="true" width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7"
      strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="16" rx="4" />
      {type === "gif" ? <path d="m10 8 6 4-6 4z" /> : <><circle cx="9" cy="9" r="1.5" /><path d="m4 17 5-4 3 3 4-5 4 4" /></>}
    </svg>
  );
}

function FilePicker({ type, disabled, label, onFile }: {
  type: MediaType; disabled: boolean; label: string; onFile: (file: File) => void;
}) {
  function onChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.[0];
    event.currentTarget.value = "";
    if (file) onFile(file);
  }
  return (
    <label className={styles.uploadOption}>
      <input type="file" accept={accept[type]} disabled={disabled} onChange={onChange} />
      <BlockIcon type={type} />
      {label}
    </label>
  );
}

function SortableBlock({ block, messageId, index, count, disabled, onText, onFile, onMove, onRemove }: {
  block: CommunicationDraftBlock; messageId?: number; index: number; count: number; disabled: boolean;
  onText: (text: string) => void; onFile: (file: File) => void; onMove: (offset: number) => void; onRemove: () => void;
}) {
  const { setNodeRef, setActivatorNodeRef, attributes, listeners, transform, transition, isDragging, isOver } =
    useSortable({ id: block.id, disabled });
  const label = t(block.type === "text" ? "Text" : block.type === "photo" ? "Photo" : "GIF");
  const inputId = useId();
  const Media = block.type === "photo" ? CommunicationPhoto : CommunicationGif;
  const size = block.file?.size ?? block.media?.size ?? 0;
  return (
    <li ref={setNodeRef} className={styles.block} data-dragging={isDragging || undefined} data-over={isOver && !isDragging || undefined}
      style={{ transform: CSS.Transform.toString(transform), transition }}>
      <button type="button" className={styles.grip} ref={setActivatorNodeRef} {...attributes} {...listeners}
        aria-label={t("Reorder block {0}", index + 1)} disabled={disabled}>
        <svg width="16" height="18" viewBox="0 0 16 18" aria-hidden="true" fill="currentColor">
          <circle cx="5" cy="4" r="1.4" /><circle cx="11" cy="4" r="1.4" />
          <circle cx="5" cy="9" r="1.4" /><circle cx="11" cy="9" r="1.4" />
          <circle cx="5" cy="14" r="1.4" /><circle cx="11" cy="14" r="1.4" />
        </svg>
      </button>
      <div className={styles.blockContent}>
        <div className={styles.blockHeader}>
          <label htmlFor={block.type === "text" ? inputId : undefined}><BlockIcon type={block.type} />{label}</label>
          <div className={styles.blockActions}>
            <button type="button" aria-label={t("Move up")} title={t("Move up")} disabled={disabled || index === 0} onClick={() => onMove(-1)}>↑</button>
            <button type="button" aria-label={t("Move down")} title={t("Move down")} disabled={disabled || index === count - 1} onClick={() => onMove(1)}>↓</button>
            <button type="button" aria-label={t("Remove")} title={t("Remove")} disabled={disabled} onClick={onRemove}><CloseIcon size={15} /></button>
          </div>
        </div>
        {block.type === "text" ? (
          <textarea id={inputId} rows={3} maxLength={4000} value={block.text ?? ""} disabled={disabled}
            placeholder={t("Write the update employees need to see when they sign in.")} onChange={event => onText(event.target.value)} />
        ) : (
          <div className={styles.blockMedia}>
            <div className={styles.attachmentThumb}>
              <Media id={messageId} attachmentKey={block.media?.key} file={block.file} thumbnail />
            </div>
            <div className={styles.blockFile}>
              <strong>{block.file?.name ?? block.media?.fileName}</strong>
              <span>{kilobytes.format(size / 1024)}</span>
              <FilePicker type={block.type} label={t("Replace")} disabled={disabled} onFile={onFile} />
            </div>
          </div>
        )}
      </div>
    </li>
  );
}

export function CommunicationBlockEditor({ blocks, messageId, disabled, onChange, onError }: {
  blocks: CommunicationDraftBlock[]; messageId?: number; disabled: boolean;
  onChange: (blocks: CommunicationDraftBlock[]) => void; onError: (error: string) => void;
}) {
  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 6 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 220, tolerance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );
  const cannotAdd = disabled || blocks.length >= maxCommunicationBlocks;

  function chooseFile(type: MediaType, file: File, id?: string) {
    const formats = accept[type].split(",");
    if (!file.size || (!formats.includes(file.type) && !formats.some(format => format.startsWith(".") && file.name.toLowerCase().endsWith(format)))) {
      onError(t("Unsupported file format"));
      return;
    }
    if (file.size > maxCommunicationGifBytes) {
      onError(t("File too large"));
      return;
    }
    const total = blocks.reduce((sum, block) => sum + (block.id === id ? 0 : block.file?.size ?? 0), file.size);
    if (total > maxCommunicationUploadBytes) {
      onError(t("New attachments cannot exceed 8 MB per save."));
      return;
    }
    onChange(id
      ? blocks.map(block => block.id === id ? { id, type, file } : block)
      : [...blocks, { id: crypto.randomUUID(), type, file }]);
  }

  return (
    <section className={styles.contentEditor} aria-label={t("Message Content")}>
      <div className={styles.contentHeading}><h2>{t("Message Content")}</h2><span>{blocks.length}/{maxCommunicationBlocks}</span></div>
      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={({ active, over }) => {
        const from = blocks.findIndex(block => block.id === active.id);
        const to = blocks.findIndex(block => block.id === over?.id);
        if (from >= 0 && to >= 0 && from !== to) onChange(arrayMove(blocks, from, to));
      }}>
        <SortableContext items={blocks} strategy={verticalListSortingStrategy}>
          <ol className={styles.blockList} aria-label={t("Content order")}>
            {blocks.map((block, index) => (
              <SortableBlock key={block.id} block={block} index={index} count={blocks.length} messageId={messageId}
                disabled={disabled} onText={text => onChange(blocks.map(item => item.id === block.id ? { ...item, text } : item))}
                onFile={file => { if (block.type !== "text") chooseFile(block.type, file, block.id); }}
                onMove={offset => onChange(arrayMove(blocks, index, index + offset))}
                onRemove={() => onChange(blocks.filter(item => item.id !== block.id))} />
            ))}
          </ol>
        </SortableContext>
      </DndContext>
      <div className={styles.uploadOptions}>
        <button type="button" className={styles.uploadOption} disabled={cannotAdd}
          onClick={() => onChange([...blocks, { id: crypto.randomUUID(), type: "text", text: "" }])}>
          <PlusIcon size={16} />{t("Add text")}
        </button>
        <FilePicker type="photo" label={t("Add photo")} disabled={cannotAdd} onFile={file => chooseFile("photo", file)} />
        <FilePicker type="gif" label={t("Add GIF")} disabled={cannotAdd} onFile={file => chooseFile("gif", file)} />
      </div>
    </section>
  );
}
