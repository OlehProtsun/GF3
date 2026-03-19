import { useEffect, useState } from "react";
import type { KeyboardEvent } from "react";
import styles from "./NumberStepperInput.module.css";

type NumberStepperInputProps = {
  value: number;
  onChange: (value: number) => void;
  min?: number;
  max?: number;
  step?: number;
  disabled?: boolean;
  className?: string;
  id?: string;
  ariaLabel: string;
};

function clampValue(value: number, min?: number, max?: number) {
  if (min !== undefined && value < min) {
    return min;
  }

  if (max !== undefined && value > max) {
    return max;
  }

  return value;
}

export function NumberStepperInput({
  value,
  onChange,
  min,
  max,
  step = 1,
  disabled = false,
  className,
  id,
  ariaLabel,
}: NumberStepperInputProps) {
  const [draftValue, setDraftValue] = useState(String(value));

  useEffect(() => {
    setDraftValue(String(value));
  }, [value]);

  const commitValue = (rawValue: string) => {
    const sanitizedValue = rawValue.replace(/[^\d]/g, "");
    const fallbackValue = clampValue(value, min, max);

    if (!sanitizedValue) {
      setDraftValue(String(fallbackValue));
      return;
    }

    const parsedValue = Number(sanitizedValue);
    if (!Number.isFinite(parsedValue)) {
      setDraftValue(String(fallbackValue));
      return;
    }

    const nextValue = clampValue(parsedValue, min, max);
    setDraftValue(String(nextValue));

    if (nextValue !== value) {
      onChange(nextValue);
    }
  };

  const applyStep = (direction: -1 | 1) => {
    const parsedDraftValue = Number(draftValue);
    const baseValue = Number.isFinite(parsedDraftValue) ? parsedDraftValue : value;
    const nextValue = clampValue(baseValue + direction * step, min, max);

    setDraftValue(String(nextValue));

    if (nextValue !== value) {
      onChange(nextValue);
    }
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter") {
      event.preventDefault();
      commitValue(draftValue);
      return;
    }

    if (event.key === "Escape") {
      event.preventDefault();
      setDraftValue(String(value));
      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();
      applyStep(1);
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();
      applyStep(-1);
    }
  };

  const canDecrement = min === undefined || value > min;
  const canIncrement = max === undefined || value < max;
  const rootClassName = [styles.root, className].filter(Boolean).join(" ");

  return (
    <div className={rootClassName}>
      <button
        type="button"
        className={styles.stepButton}
        onClick={() => applyStep(-1)}
        disabled={disabled || !canDecrement}
        aria-label={`Decrease ${ariaLabel}`}
      >
        -
      </button>

      <input
        id={id}
        className={styles.input}
        type="text"
        inputMode="numeric"
        pattern="[0-9]*"
        value={draftValue}
        onChange={event => setDraftValue(event.target.value.replace(/[^\d]/g, ""))}
        onBlur={() => commitValue(draftValue)}
        onKeyDown={handleKeyDown}
        disabled={disabled}
        aria-label={ariaLabel}
      />

      <button
        type="button"
        className={styles.stepButton}
        onClick={() => applyStep(1)}
        disabled={disabled || !canIncrement}
        aria-label={`Increase ${ariaLabel}`}
      >
        +
      </button>
    </div>
  );
}
