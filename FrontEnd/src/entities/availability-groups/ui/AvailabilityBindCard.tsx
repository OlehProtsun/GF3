import { t } from "@shared/i18n";
import type { FocusEvent, KeyboardEvent, ReactNode } from "react";
import {
  formatBindKeyFromKeyboardEvent,
  isCommonEditorShortcut,
  isModifierOnlyKey,
} from "@entities/availability-binds";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { BindIcon, CloseIcon, PlusIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import { IosButton } from "@shared/ui/components/IosButton";
import styles from "./AvailabilityBindCard.module.css";

type AvailabilityBindRow = {
  clientId: string;
  id: number | null;
  key: string;
  value: string;
  isActive: boolean;
};

type AvailabilityBindCardProps = {
  binds: AvailabilityBindRow[];
  selectedBindClientId: string | null;
  isLoading: boolean;
  isBusy: boolean;
  errorMessage?: string;
  headerRightSlot?: ReactNode;
  onSelectedBindChange: (clientId: string | null) => void;
  onBindFieldChange: (clientId: string, patch: Partial<Pick<AvailabilityBindRow, "key" | "value" | "isActive">>) => void;
  onBindCommit: (clientId: string) => void;
  onAddBind: () => void;
  onDeleteBind: () => void;
};

export function AvailabilityBindCard({
  binds,
  selectedBindClientId,
  isLoading,
  isBusy,
  errorMessage,
  headerRightSlot,
  onSelectedBindChange,
  onBindFieldChange,
  onBindCommit,
  onAddBind,
  onDeleteBind,
}: AvailabilityBindCardProps) {
  const shouldCaptureKeyStroke = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.ctrlKey || event.altKey || event.metaKey) {
      return true;
    }

    if (/^F\d{1,2}$/i.test(event.key)) {
      return true;
    }

    return [
      " ",
      "Spacebar",
      "ArrowUp",
      "ArrowDown",
      "ArrowLeft",
      "ArrowRight",
      "Home",
      "End",
      "PageUp",
      "PageDown",
      "Insert",
    ].includes(event.key);
  };

  const handleRowBlur = (event: FocusEvent<HTMLDivElement>, clientId: string) => {
    if (event.relatedTarget instanceof Node && event.currentTarget.contains(event.relatedTarget)) {
      return;
    }

    onBindCommit(clientId);
  };

  const handleKeyCapture = (event: KeyboardEvent<HTMLInputElement>, clientId: string) => {
    if (event.key === "Enter") {
      event.preventDefault();
      onBindCommit(clientId);
      return;
    }

    if (event.key === "Tab" || event.key === "Escape") {
      return;
    }

    if (isCommonEditorShortcut(event) || isModifierOnlyKey(event.key)) {
      return;
    }

    if (!shouldCaptureKeyStroke(event)) {
      return;
    }

    const nextKey = formatBindKeyFromKeyboardEvent(event);
    if (!nextKey) {
      return;
    }

    event.preventDefault();
    onBindFieldChange(clientId, { key: nextKey });
  };

  const bindCountLabel = t("{0} bind{1}", binds.length, binds.length === 1 ? "" : "s");

  return (
    <CardSection
      className={styles.card}
      title={
        <span className={styles.titleWrap}>
          <span>{t("Bind Information")}</span>
          <span className={styles.titleMeta}>{bindCountLabel}</span>
        </span>
      }
      icon={<BindIcon size={18} />}
      headerRightSlot={headerRightSlot}
    >
      <div className={styles.layout}>
        {errorMessage ? (
          <ErrorBanner
            className={styles.errorMessageWrap}
            bannerClassName={styles.errorMessage}
            textClassName={styles.errorMessageText}
          >
            {errorMessage}
          </ErrorBanner>
        ) : null}

        <div className={styles.placeholderTable}>
          <div className={styles.headerRow}>
            <span>{t("Key")}</span>
            <span>{t("Value")}</span>
            <span>{t("Active")}</span>
          </div>

          {isLoading ? (
            <div className={styles.emptyRow}>{t("Loading bind information...")}</div>
          ) : binds.length > 0 ? (
            <div className={styles.body}>
              {binds.map(bind => {
                const isSelected = bind.clientId === selectedBindClientId;

                return (
                  <div
                    key={bind.clientId}
                    className={`${styles.bindRow} ${isSelected ? styles.bindRowSelected : ""}`}
                    onClick={() => onSelectedBindChange(bind.clientId)}
                    onFocus={() => onSelectedBindChange(bind.clientId)}
                    onBlur={event => handleRowBlur(event, bind.clientId)}
                  >
                    <input
                      className={styles.keyInput}
                      value={bind.key}
                      placeholder={t("Press shortcut or type manually...")}
                      spellCheck={false}
                      autoComplete="off"
                      onFocus={() => onSelectedBindChange(bind.clientId)}
                      onChange={event => onBindFieldChange(bind.clientId, { key: event.target.value })}
                      onKeyDown={event => handleKeyCapture(event, bind.clientId)}
                    />

                    <input
                      className={styles.valueInput}
                      value={bind.value}
                      placeholder="08:00 - 16:00 / + / -"
                      onFocus={() => onSelectedBindChange(bind.clientId)}
                      onChange={event => onBindFieldChange(bind.clientId, { value: event.target.value })}
                      onKeyDown={event => {
                        if (event.key === "Enter") {
                          event.preventDefault();
                          onBindCommit(bind.clientId);
                        }
                      }}
                    />

                    <label className={styles.toggleCell}>
                      <input
                        type="checkbox"
                        checked={bind.isActive}
                        onFocus={() => onSelectedBindChange(bind.clientId)}
                        onChange={event => {
                          onBindFieldChange(bind.clientId, { isActive: event.target.checked });
                          onBindCommit(bind.clientId);
                        }}
                      />
                      <span className={styles.toggleTrack}>
                        <span className={styles.toggleThumb} />
                      </span>
                    </label>
                  </div>
                );
              })}
            </div>
          ) : (
            <div className={styles.emptyRow}>{t("No binds yet. Add as many rows as you need.")}</div>
          )}
        </div>

        <div className={styles.actions}>
          <IosButton label={t("Delete")} icon={<CloseIcon size={16} />} variant="secondary" customColor="#dc2626" customBorderColor="#dc2626" onClick={onDeleteBind} disabled={isBusy || !selectedBindClientId} />
          <IosButton label={t("Add")} icon={<PlusIcon size={16} />} onClick={onAddBind} disabled={isBusy} />
        </div>
      </div>
    </CardSection>
  );
}
