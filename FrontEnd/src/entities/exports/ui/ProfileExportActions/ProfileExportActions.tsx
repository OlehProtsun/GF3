import { CodeIcon, ExcelIcon } from "@shared/ui/icons";
import styles from "./ProfileExportActions.module.css";

type ProfileExportActionsProps = {
  excelLabel?: string;
  codeLabel?: string;
  isExcelPending?: boolean;
  isCodePending?: boolean;
  disabled?: boolean;
  className?: string;
  onExportExcel: () => void;
  onExportCode: () => void;
};

function joinClassNames(...values: Array<string | false | null | undefined>) {
  return values.filter(Boolean).join(" ");
}

export function ProfileExportActions({
  excelLabel = "Export",
  codeLabel = "Export",
  isExcelPending = false,
  isCodePending = false,
  disabled = false,
  className,
  onExportExcel,
  onExportCode,
}: ProfileExportActionsProps) {
  const isBusy = isExcelPending || isCodePending;
  const buttonsDisabled = disabled || isBusy;

  return (
    <div className={joinClassNames(styles.actions, className)}>
      <span className={styles.label}>Export</span>

      <div className={styles.buttonRow}>
        <button
          type="button"
          aria-label={isExcelPending ? "Preparing Excel export" : excelLabel}
          title={isExcelPending ? "Preparing Excel export" : excelLabel}
          disabled={buttonsDisabled}
          className={joinClassNames(
            styles.actionButton,
            styles.excelButton,
            isExcelPending && styles.actionButtonPending,
          )}
          onClick={onExportExcel}
        >
          <ExcelIcon size={17} />
        </button>

        <button
          type="button"
          aria-label={isCodePending ? "Preparing code export" : codeLabel}
          title={isCodePending ? "Preparing code export" : codeLabel}
          disabled={buttonsDisabled}
          className={joinClassNames(
            styles.actionButton,
            styles.codeButton,
            isCodePending && styles.actionButtonPending,
          )}
          onClick={onExportCode}
        >
          <CodeIcon size={17} />
        </button>
      </div>
    </div>
  );
}
