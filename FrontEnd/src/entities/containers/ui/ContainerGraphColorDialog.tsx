import { useEffect, useId, useState, type MouseEvent } from "react";
import { IosButton } from "@shared/ui/components/IosButton";
import { LabeledField, TextInput } from "@shared/ui/forms/Field";
import { CheckIcon, CloseIcon } from "@shared/ui/icons";
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
  onCancel: () => void;
  onSave: (value: string) => void;
};

const COLOR_GROUPS: ColorGroup[] = [
  {
    label: "Neutrals",
    options: [
      { label: "Slate", value: "#f8fafc" },
      { label: "Cloud", value: "#e2e8f0" },
      { label: "Ash", value: "#cbd5e1" },
      { label: "Ink", value: "#1e293b" },
      { label: "Graphite", value: "#0f172a" },
    ],
  },
  {
    label: "Cool",
    options: [
      { label: "Ice", value: "#dbeafe" },
      { label: "Sky", value: "#bae6fd" },
      { label: "Mint", value: "#bbf7d0" },
      { label: "Aqua", value: "#99f6e4" },
      { label: "Lavender", value: "#ddd6fe" },
    ],
  },
  {
    label: "Warm",
    options: [
      { label: "Cream", value: "#fef3c7" },
      { label: "Peach", value: "#fed7aa" },
      { label: "Rose", value: "#fecdd3" },
      { label: "Lilac", value: "#f5d0fe" },
      { label: "Coral", value: "#fdba74" },
    ],
  },
  {
    label: "Accent",
    options: [
      { label: "Blue", value: "#2563eb" },
      { label: "Indigo", value: "#4f46e5" },
      { label: "Emerald", value: "#059669" },
      { label: "Amber", value: "#d97706" },
      { label: "Rose", value: "#e11d48" },
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
  onCancel,
  onSave,
}: ContainerGraphColorDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const [draftValue, setDraftValue] = useState(value);
  const [inputError, setInputError] = useState<string | undefined>();

  useEffect(() => {
    if (!open || mode === null) {
      return;
    }

    setDraftValue(value);
    setInputError(undefined);
  }, [mode, open, value]);

  useEffect(() => {
    if (!open || mode === null) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onCancel();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [mode, onCancel, open]);

  if (!open || mode === null) {
    return null;
  }

  const normalizedDraftValue = normalizeHexColor(draftValue);
  const resolvedPreviewColor = getPreviewColor(mode, draftValue);
  const title = mode === "fill" ? "Choose Fill Color" : "Choose Text Color";
  const description =
    mode === "fill"
      ? "Pick a clean background color for the selected schedule cells."
      : "Pick a readable text color for the selected schedule cells.";
  const saveLabel = mode === "fill" ? "Use Fill Color" : "Use Text Color";
  const displayValue = (normalizedDraftValue ?? draftValue).toUpperCase();

  const submit = () => {
    if (!normalizedDraftValue) {
      setInputError("Enter a valid hex color like #2563EB.");
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
    setDraftValue(nextValue);
    setInputError(undefined);
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
          <span className={styles.previewLabel}>Preview</span>
          <div
            className={styles.previewCell}
            style={mode === "fill" ? { backgroundColor: resolvedPreviewColor } : { color: resolvedPreviewColor }}
          >
            <span className={styles.previewDay}>12</span>
            <span className={styles.previewShift}>Shift 09:00 - 18:00</span>
          </div>
        </div>

        <div className={styles.paletteBlock}>
          <div className={styles.paletteHeader}>
            <span className={styles.paletteTitle}>Palette</span>
            <span className={styles.paletteHint}>Choose a swatch or enter your own hex color.</span>
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

        <LabeledField id={`graph-${mode}-color`} label="Hex color" error={inputError} className={styles.hexField}>
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
                setDraftValue(event.target.value);
                if (inputError) {
                  setInputError(undefined);
                }
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
          <IosButton label="Cancel" variant="secondary" icon={<CloseIcon size={16} />} onClick={onCancel} />
          <IosButton label={saveLabel} icon={<CheckIcon size={16} />} onClick={submit} />
        </div>
      </div>
    </div>
  );
}
