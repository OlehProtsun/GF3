import type { ReactNode } from "react";
import { IosButton } from "@shared/ui/components/IosButton";
import { CloseIcon, InformationIcon, PlusIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./ContainerGraphManualColumnsCard.module.css";

type ManualColumnSummary = {
  columnId: number;
  label: string;
};

type ContainerGraphManualColumnsCardProps = {
  columns: ManualColumnSummary[];
  headerRightSlot?: ReactNode;
  onAddColumn: () => void;
  onDeleteColumn: (columnId: number) => void;
};

export function ContainerGraphManualColumnsCard({
  columns,
  headerRightSlot,
  onAddColumn,
  onDeleteColumn,
}: ContainerGraphManualColumnsCardProps) {
  const columnsCountLabel = `${columns.length} column${columns.length === 1 ? "" : "s"}`;

  return (
    <CardSection
      className={styles.card}
      title={(
        <span className={styles.titleWrap}>
          <span>Manual Columns</span>
          <span className={styles.titleMeta}>{columnsCountLabel}</span>
        </span>
      )}
      icon={<InformationIcon size={18} />}
      headerRightSlot={headerRightSlot}
    >
      <div className={styles.layout}>
        <p className={styles.description}>
          Add extra free-form columns and edit their header and cells directly inside Schedule Matrix.
        </p>

        {columns.length === 0 ? (
          <div className={styles.emptyState}>No manual columns yet. Add one when you need an extra editable column.</div>
        ) : (
          <div className={styles.list}>
            {columns.map((column, index) => (
              <div key={column.columnId} className={styles.item}>
                <div className={styles.itemText}>
                  <span className={styles.itemTitle}>{column.label.trim() || `Untitled column ${index + 1}`}</span>
                  <span className={styles.itemHint}>Edit header and values in the grid</span>
                </div>

                <button
                  type="button"
                  className={styles.deleteButton}
                  aria-label={`Delete ${column.label.trim() || `manual column ${index + 1}`}`}
                  onClick={() => onDeleteColumn(column.columnId)}
                >
                  <CloseIcon size={14} />
                </button>
              </div>
            ))}
          </div>
        )}

        <div className={styles.actions}>
          <IosButton label="Add Column" icon={<PlusIcon size={16} />} onClick={onAddColumn} />
        </div>
      </div>
    </CardSection>
  );
}
