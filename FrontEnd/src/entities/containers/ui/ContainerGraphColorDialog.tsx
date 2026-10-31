import { t } from "@shared/i18n";
import { useEffect, useId, useMemo, useState, type MouseEvent } from "react";
import {
  formatBindKeyFromKeyboardEvent,
  isBindNavigationKey,
  isCommonEditorShortcut,
  isModifierOnlyKey,
} from "@entities/availability-binds";
import type { ManagerGraphFillColorBindDto, ManagerGraphTextColorBindDto } from "@entities/containers/api/dto";
import { useSyncedDraft } from "@shared/lib/useSyncedDraft";
import { IosButton } from "@shared/ui/components/IosButton";
import { LabeledField, TextInput } from "@shared/ui/forms/Field";
import { BindIcon, CheckIcon, CloseIcon } from "@shared/ui/icons";
import styles from "./ContainerGraphColorDialog.module.css";

type ColorDialogMode = "fill" | "text";

type ColorOption = {
  label: string;
  value: string;
};

type ColorGroup = {
  label: string;
  options: ColorOption[];
};

type ContainerGraphColorDialogProps = {
  open: boolean;
  mode: ColorDialogMode | null;
  value: string;
  fillColorBinds: ManagerGraphFillColorBindDto[];
  textColorBinds: ManagerGraphTextColorBindDto[];
  reservedValueBindKeys: ReadonlySet<string>;
  isFillColorBindBusy: boolean;
  isTextColorBindBusy: boolean;
  onCancel: () => void;
  onSave: (value: string) => void;
  onBindFillColor: (key: string, fillColor: string) => Promise<void>;
  onDeleteFillColorBind: (id: number) => Promise<void>;
  onBindTextColor: (key: string, textColor: string) => Promise<void>;
  onDeleteTextColorBind: (id: number) => Promise<void>;
};

const COLOR_GROUPS: ColorGroup[] = [
  {
    get label() { return t("Neutrals"); },
    options: [
      { get label() { return t("Slate"); }, value: "#f8fafc" },
      { get label() { return t("Cloud"); }, value: "#e2e8f0" },
      { get label() { return t("Ash"); }, value: "#cbd5e1" },
      { get label() { return t("Ink"); }, value: "#1e293b" },
      { get label() { return t("Graphite"); }, value: "#0f172a" },
    ],
  },
  {
    get label() { return t("Cool"); },
    options: [
      { get label() { return t("Ice"); }, value: "#dbeafe" },
      { get label() { return t("Sky"); }, value: "#bae6fd" },
      { get label() { return t("Mint"); }, value: "#bbf7d0" },
      { get label() { return t("Aqua"); }, value: "#99f6e4" },
      { get label() { return t("Lavender"); }, value: "#ddd6fe" },
    ],
  },
  {
    get label() { return t("Warm"); },
    options: [
      { get label() { return t("Cream"); }, value: "#fef3c7" },
      { get label() { return t("Peach"); }, value: "#fed7aa" },
      { get label() { return t("Rose"); }, value: "#fecdd3" },
      { get label() { return t("Lilac"); }, value: "#f5d0fe" },
      { get label() { return t("Coral"); }, value: "#fdba74" },
    ],
  },
  {
    get label() { return t("Accent"); },
    options: [
      { get label() { return t("Blue"); }, value: "#2563eb" },
      { get label() { return t("Indigo"); }, value: "#4f46e5" },
      { get label() { return t("Emerald"); }, value: "#059669" },
      { get label() { return t("Amber"); }, value: "#d97706" },
      { get label() { return t("Rose"); }, value: "#e11d48" },
    ],
  },
];

function normalizeHexColor(value: string) {
  const trimmedValue = value.trim();
  const rawValue = trimmedValue.startsWith("#") ? trimmedValue.slice(1) : trimmedValue;

  if (/^[0-9a-fA-F]{3}$/.test(rawValue)) {
    return `#${rawValue.split("").map(symbol => `${symbol}${symbol}`).join("").toLowerCase()}`;
  }

  if (/^[0-9a-fA-F]{6}$/.test(rawValue)) {
    return `#${rawValue.toLowerCase()}`;
  }

  return null;
}

function getPreviewColor(mode: ColorDialogMode, value: string) {
  return normalizeHexColor(value) ?? (mode === "fill" ? "#dbeafe" : "#0f172a");
}

export function ContainerGraphColorDialog({
  open,
  mode,
  value,
  fillColorBinds,
  textColorBinds,
  reservedValueBindKeys,
  isFillColorBindBusy,
  isTextColorBindBusy,
  onCancel,
  onSave,
  onBindFillColor,
  onDeleteFillColorBind,
  onBindTextColor,
  onDeleteTextColorBind,
}: ContainerGraphColorDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const dialogSourceKey = useMemo(
    () => `${open ? "open" : "closed"}:${mode ?? "none"}:${value}`,
    [mode, open, value],
  );
  const initialDialogState = useMemo(
    () => ({
      draftValue: value,
      inputError: undefined as string | undefined,
    }),
    [value],
  );
  const {
    value: dialogState,
    setValue: setDialogState,
  } = useSyncedDraft(dialogSourceKey, initialDialogState);
  const { draftValue, inputError } = dialogState;
  const [isCapturingBind, setIsCapturingBind] = useState(false);
  const [bindError, setBindError] = useState<string | undefined>();
  const isColorBindBusy = mode === "text" ? isTextColorBindBusy : isFillColorBindBusy;

  useEffect(() => {
    setIsCapturingBind(false);
    setBindError(undefined);
  }, [mode, open]);

  useEffect(() => {
    if (!open || mode === null) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (isCapturingBind) {
        event.preventDefault();
        event.stopPropagation();

        if (event.key === "Escape") {
          setIsCapturingBind(false);
          setBindError(undefined);
          return;
        }

        if (isColorBindBusy) {
          return;
        }

        if (isModifierOnlyKey(event.key)) {
          return;
        }

        if (isBindNavigationKey(event.key) || isCommonEditorShortcut(event)) {
          setBindError(t("Choose a non-navigation key that is not a standard editor shortcut."));
          return;
        }

        const nextKey = formatBindKeyFromKeyboardEvent(event);
        const nextColor = normalizeHexColor(draftValue);
        if (!nextKey || !nextColor) {
          setBindError(t("Choose a valid {0} color before binding a key.", mode));
          return;
        }

        if (reservedValueBindKeys.has(nextKey)) {
          setBindError(t("Key '{0}' is already used by a value bind.", nextKey));
          return;
        }

        const conflictingColorBind = (mode === "fill" ? textColorBinds : fillColorBinds)
          .some(bind => bind.key.toUpperCase() === nextKey.toUpperCase());
        if (conflictingColorBind) {
          const conflictingMode = mode === "fill" ? "text" : "fill";
          setBindError(t("Key '{0}' is already used by a {1} color bind.", nextKey, conflictingMode));
          return;
        }

        setBindError(undefined);
        const saveBinding = mode === "fill" ? onBindFillColor : onBindTextColor;
        void saveBinding(nextKey, nextColor.toUpperCase())
          .then(() => setIsCapturingBind(false))
          .catch(() => setBindError(t("Could not save this key binding.")));
        return;
      }

      if (event.key === "Escape") {
        onCancel();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [draftValue, fillColorBinds, isCapturingBind, isColorBindBusy, mode, onBindFillColor, onBindTextColor, onCancel, open, reservedValueBindKeys, textColorBinds]);

  if (!open || mode === null) {
    return null;
  }

  const normalizedDraftValue = normalizeHexColor(draftValue);
  const currentColorBinding = normalizedDraftValue
    ? mode === "fill"
      ? fillColorBinds.find(bind => bind.fillColor.toLowerCase() === normalizedDraftValue)
      : textColorBinds.find(bind => bind.textColor.toLowerCase() === normalizedDraftValue)
    : undefined;
  const resolvedPreviewColor = getPreviewColor(mode, draftValue);
  const title = mode === "fill" ? t("Choose Fill Color") : t("Choose Text Color");
  const description =
    mode === "fill"
      ? t("Pick a clean background color for the selected schedule cells.")
      : t("Pick a readable text color for the selected schedule cells.");
  const saveLabel = mode === "fill" ? t("Use Fill Color") : t("Use Text Color");
  const displayValue = (normalizedDraftValue ?? draftValue).toUpperCase();

  const submit = () => {
    if (!normalizedDraftValue) {
      setDialogState((current) => ({
        ...current,
        inputError: t("Enter a valid hex color like #2563EB."),
      }));
      return;
    }

    onSave(normalizedDraftValue);
  };

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target !== event.currentTarget) {
      return;
    }

    onCancel();
  };

  const handleSwatchSelect = (nextValue: string) => {
    setDialogState({
      draftValue: nextValue,
      inputError: undefined,
    });
  };

  const handleStartBinding = () => {
    if (!normalizedDraftValue) {
      setBindError(t("Choose a valid {0} color before binding a key.", mode));
      return;
    }

    setBindError(undefined);
    setIsCapturingBind(true);
  };

  const handleDeleteBinding = () => {
    if (!currentColorBinding) {
      return;
    }

    setBindError(undefined);
    const deleteBinding = mode === "fill" ? onDeleteFillColorBind : onDeleteTextColorBind;
    void deleteBinding(currentColorBinding.id)
      .catch(() => setBindError(t("Could not remove this key binding.")));
  };

  return (
    <div
      className={styles.overlay}
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      aria-describedby={descriptionId}
      onMouseDown={handleOverlayMouseDown}
    >
      <div className={styles.dialog}>
        <div className={styles.header}>
          <div className={styles.titleBlock}>
            <h3 id={titleId} className={styles.title}>{title}</h3>
            <p id={descriptionId} className={styles.description}>{description}</p>
          </div>

          <div className={styles.currentChip}>
            <span className={styles.currentChipSwatch} style={{ backgroundColor: resolvedPreviewColor }} />
            <span>{displayValue}</span>
          </div>
        </div>

        <div className={styles.previewPanel}>
          <span className={styles.previewLabel}>{t("Preview")}</span>
          <div
            className={styles.previewCell}
            style={mode === "fill" ? { backgroundColor: resolvedPreviewColor } : { color: resolvedPreviewColor }}
          >
            <span className={styles.previewDay}>12</span>
            <span className={styles.previewShift}>{t("Shift 09:00 - 18:00")}</span>
          </div>
        </div>

        <div className={styles.paletteBlock}>
          <div className={styles.paletteHeader}>
            <span className={styles.paletteTitle}>{t("Palette")}</span>
            <span className={styles.paletteHint}>{t("Choose a swatch or enter your own hex color.")}</span>
          </div>

          <div className={styles.paletteSections}>
            {COLOR_GROUPS.map(group => (
              <div key={group.label} className={styles.paletteSection}>
                <span className={styles.paletteSectionLabel}>{group.label}</span>

                <div className={styles.paletteGrid}>
                  {group.options.map(option => {
                    const isSelected = normalizedDraftValue === option.value;

                    return (
                      <button
                        key={`${group.label}-${option.value}`}
                        type="button"
                        className={`${styles.paletteSwatch} ${isSelected ? styles.paletteSwatchSelected : ""}`}
                        aria-label={`${option.label} ${option.value}`}
                        aria-pressed={isSelected}
                        title={`${option.label} ${option.value.toUpperCase()}`}
                        style={{ backgroundColor: option.value }}
                        onClick={() => handleSwatchSelect(option.value)}
                      >
                        {isSelected ? <CheckIcon size={16} /> : null}
                      </button>
                    );
                  })}
                </div>
              </div>
            ))}
          </div>
        </div>

        <LabeledField id={`graph-${mode}-color`} label={t("Hex color")} error={inputError} className={styles.hexField}>
          <div className={styles.hexInputRow}>
            <span className={styles.hexPreview} style={{ backgroundColor: resolvedPreviewColor }} aria-hidden="true" />
            <TextInput
              id={`graph-${mode}-color`}
              className={styles.hexInput}
              value={draftValue.toUpperCase()}
              placeholder={mode === "fill" ? "#DBEAFE" : "#0F172A"}
              spellCheck={false}
              autoCapitalize="characters"
              aria-invalid={inputError ? true : undefined}
              onChange={event => {
                setDialogState((current) => ({
                  draftValue: event.target.value,
                  inputError: current.inputError ? undefined : current.inputError,
                }));
              }}
              onKeyDown={event => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  submit();
                }
              }}
            />
          </div>
        </LabeledField>

        <div className={styles.footer}>
          <div className={styles.bindBlock}>
              <div className={styles.bindActions}>
                <IosButton
                  label={isCapturingBind ? t("Press a key...") : currentColorBinding ? t("Bound: {0}", currentColorBinding.key) : t("Bind Key")}
                  variant="secondary"
                  size="compact"
                  className={`${styles.bindButton} ${isCapturingBind ? styles.bindButtonCapturing : ""}`}
                  icon={<BindIcon size={14} />}
                  disabled={isColorBindBusy}
                  onClick={handleStartBinding}
                />
                {currentColorBinding ? (
                  <IosButton
                    label={t("Unbind")}
                    variant="secondary"
                    size="compact"
                    className={styles.bindButton}
                    disabled={isColorBindBusy || isCapturingBind}
                    onClick={handleDeleteBinding}
                  />
                ) : null}
              </div>
              <span className={bindError ? styles.bindError : styles.bindHint}>
                {bindError ?? (isCapturingBind ? t("Press the shortcut to use for this {0} color. Esc cancels.", mode) : t("The shortcut applies this {0} color to the selected schedule cells.", mode))}
              </span>
            </div>
          <div className={styles.footerActions}>
            <IosButton label={t("Cancel")} variant="secondary" icon={<CloseIcon size={16} />} onClick={onCancel} />
            <IosButton label={saveLabel} icon={<CheckIcon size={16} />} onClick={submit} />
          </div>
        </div>
      </div>
    </div>
  );
}
