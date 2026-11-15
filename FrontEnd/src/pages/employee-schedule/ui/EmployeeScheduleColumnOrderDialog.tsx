import { t } from "@shared/i18n";
import { useEffect, useId, useRef, useState } from "react";
import {
  closestCenter, DndContext, DragOverlay, KeyboardSensor, MouseSensor, pointerWithin, TouchSensor, useSensor, useSensors,
  type CollisionDetection, type DragEndEvent, type UniqueIdentifier,
} from "@dnd-kit/core";
import { arrayMove, SortableContext, sortableKeyboardCoordinates, useSortable, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import type { GraphMatrixColumn } from "@entities/containers/model/graphWorkspace";
import { IosButton } from "@shared/ui/components/IosButton";
import { CheckIcon, CloseIcon } from "@shared/ui/icons";
import styles from "./EmployeeScheduleColumnOrderDialog.module.css";

type EmployeeScheduleColumnOrderDialogProps = {
  open: boolean;
  columns: GraphMatrixColumn[];
  defaultColumns: GraphMatrixColumn[];
  activeEmployeeId: number | null;
  onCancel: () => void;
  onSave: (columnOrder: number[]) => void;
};

function columnIds(columns: GraphMatrixColumn[]) {
  return columns.map(column => column.employeeId);
}

function arraysEqual(left: number[], right: number[]) {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

const detectTarget: CollisionDetection = args => args.pointerCoordinates ? pointerWithin(args) : closestCenter(args);

function ColumnLabel({ position, label }: { position: number; label: string }) {
  return <>
    <span className={styles.position}>{position}</span>
    <span className={styles.employeeName}>{label}</span>
  </>;
}

function SortableColumn({ column, position, highlighted, disabled }: {
  column: GraphMatrixColumn;
  position: number;
  highlighted: boolean;
  disabled: boolean;
}) {
  const { attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging, over } =
    useSortable({ id: column.employeeId, disabled });

  return (
    <div
      ref={setNodeRef}
      className={[styles.row, highlighted && styles.activeRow, isDragging && styles.placeholder].filter(Boolean).join(" ")}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      role="listitem"
    >
      <ColumnLabel position={position} label={column.label} />
      <button
        ref={setActivatorNodeRef}
        {...attributes}
        {...listeners}
        type="button"
        className={styles.handle}
        aria-label={t("Reorder {0}", column.label)}
        disabled={disabled}
      >
        <span className={styles.grip} aria-hidden="true" />
      </button>
      {isDragging && over && <span className={styles.dropText} aria-hidden="true">{t("Drop here")}</span>}
    </div>
  );
}

export function EmployeeScheduleColumnOrderDialog({ open, ...props }: EmployeeScheduleColumnOrderDialogProps) {
  return open ? <ColumnOrderDialog {...props} /> : null;
}

function ColumnOrderDialog({
  columns,
  defaultColumns,
  activeEmployeeId,
  onCancel,
  onSave,
}: Omit<EmployeeScheduleColumnOrderDialogProps, "open">) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const descriptionId = useId();
  const [draftColumns, setDraftColumns] = useState(columns);
  const [activeId, setActiveId] = useState<UniqueIdentifier | null>(null);
  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 6 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 250, tolerance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates, scrollBehavior: "auto" }),
  );

  useEffect(() => {
    const dialog = dialogRef.current;
    const previousFocus = document.activeElement;
    dialog?.showModal();
    return () => {
      dialog?.close();
      if (previousFocus instanceof HTMLElement && previousFocus.isConnected) previousFocus.focus({ preventScroll: true });
    };
  }, []);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      event.preventDefault();
      if (activeId === null) onCancel();
    };
    window.addEventListener("keydown", handleKeyDown, true);
    return () => window.removeEventListener("keydown", handleKeyDown, true);
  }, [activeId, onCancel]);

  const handleDragEnd = ({ active, over }: DragEndEvent) => {
    setActiveId(null);
    if (!over || active.id === over.id) return;
    setDraftColumns(current => {
      const sourceIndex = current.findIndex(column => column.employeeId === active.id);
      const targetIndex = current.findIndex(column => column.employeeId === over.id);
      return sourceIndex >= 0 && targetIndex >= 0 ? arrayMove(current, sourceIndex, targetIndex) : current;
    });
  };

  const currentOrder = columnIds(columns);
  const draftOrder = columnIds(draftColumns);
  const defaultOrder = columnIds(defaultColumns);
  const hasChanges = !arraysEqual(currentOrder, draftOrder);
  const isDefaultOrder = arraysEqual(defaultOrder, draftOrder);
  const activeIndex = draftColumns.findIndex(column => column.employeeId === activeId);
  const activeColumn = draftColumns[activeIndex];
  const columnName = (id: UniqueIdentifier) => draftColumns.find(column => column.employeeId === id)?.label ?? "";

  return (
    <dialog
      ref={dialogRef}
      className={styles.overlay}
      aria-modal="true"
      aria-labelledby={titleId}
      aria-describedby={descriptionId}
      onCancel={event => {
        event.preventDefault();
        if (activeId === null) onCancel();
      }}
      onClick={event => {
        if (event.target === event.currentTarget && activeId === null) onCancel();
      }}
    >
      <DndContext
        sensors={sensors}
        collisionDetection={detectTarget}
        onDragStart={({ active }) => setActiveId(active.id)}
        onDragEnd={handleDragEnd}
        onDragCancel={() => setActiveId(null)}
        accessibility={{
          screenReaderInstructions: { draggable: t("Press Space or Enter to pick up or drop a column. Use the arrow keys to move it. Press Escape to cancel.") },
          announcements: {
            onDragStart: ({ active }) => t("Picked up {0}.", columnName(active.id)),
            onDragOver: ({ active, over }) => over ? t("Moving {0} to position {1}.", columnName(active.id), draftColumns.findIndex(column => column.employeeId === over.id) + 1) : undefined,
            onDragEnd: ({ active, over }) => over ? t("Placed {0} at position {1}.", columnName(active.id), draftColumns.findIndex(column => column.employeeId === over.id) + 1) : t("Reordering canceled."),
            onDragCancel: () => t("Reordering canceled."),
          },
        }}
      >
        <div className={styles.dialog}>
          <header className={styles.header}>
            <div className={styles.titleBlock}>
              <span className={styles.eyebrow}>{t("Column order")}</span>
              <h2 id={titleId}>{t("Customize schedule")}</h2>
              <p id={descriptionId}>{t("Hold and drag a handle to change the column order.")}</p>
            </div>

            <button type="button" className={styles.closeButton} aria-label={t("Close column order dialog")} onClick={onCancel}>
              <CloseIcon size={18} />
            </button>
          </header>

          <SortableContext items={draftOrder} strategy={verticalListSortingStrategy}>
            <div className={styles.list} role="list" aria-label={t("Schedule columns")}>
              {draftColumns.map((column, index) => (
                <SortableColumn key={column.employeeId} column={column} position={index + 1}
                  highlighted={column.employeeId === activeEmployeeId} disabled={draftColumns.length < 2} />
              ))}
            </div>
          </SortableContext>

          <footer className={styles.footer}>
            <button
              type="button"
              className={styles.resetButton}
              disabled={isDefaultOrder || activeId !== null}
              onClick={() => setDraftColumns(defaultColumns)}
            >
              {t("Reset default")}</button>
            <span className={styles.footerActions}>
              <IosButton label={t("Cancel")} variant="secondary" size="compact" onClick={onCancel} />
              <IosButton
                label={t("Apply")}
                size="compact"
                icon={<CheckIcon size={15} />}
                disabled={!hasChanges || activeId !== null}
                onClick={() => onSave(draftOrder)}
              />
            </span>
          </footer>
        </div>
        <DragOverlay dropAnimation={null}>
          {activeColumn && (
            <div className={`${styles.row} ${styles.dragPreview}`} aria-hidden="true">
              <ColumnLabel position={activeIndex + 1} label={activeColumn.label} />
              <span className={styles.handle}><span className={styles.grip} /></span>
            </div>
          )}
        </DragOverlay>
      </DndContext>
    </dialog>
  );
}
