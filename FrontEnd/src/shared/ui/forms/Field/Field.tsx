import type { InputHTMLAttributes, ReactNode, TextareaHTMLAttributes } from "react";
import styles from "./Field.module.css";

type LabeledFieldProps = {
  id: string;
  label: ReactNode;
  children: ReactNode;
  error?: string;
  errorId?: string;
  className?: string;
};

type ErrorPillProps = {
  id?: string;
  children: ReactNode;
  className?: string;
};

type TextInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, "className"> & {
  className?: string;
};

type TextAreaProps = Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, "className"> & {
  className?: string;
};

function withOptionalClass(baseClassName: string, className?: string) {
  return className ? `${baseClassName} ${className}` : baseClassName;
}

export function TextInput({ className, type = "text", ...props }: TextInputProps) {
  return <input {...props} type={type} className={withOptionalClass(styles.input, className)} />;
}

export function TextArea({ className, rows = 5, ...props }: TextAreaProps) {
  return <textarea {...props} rows={rows} className={withOptionalClass(styles.textArea, className)} />;
}

export function ErrorPill({ id, children, className }: ErrorPillProps) {
  return (
    <div id={id} className={withOptionalClass(styles.fieldError, className)}>
      {children}
    </div>
  );
}

export function LabeledField({ id, label, children, error, errorId, className }: LabeledFieldProps) {
  const resolvedErrorId = errorId ?? `${id}-error`;

  return (
    <div className={withOptionalClass(styles.field, className)}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      {children}
      {error ? <ErrorPill id={resolvedErrorId}>{error}</ErrorPill> : null}
    </div>
  );
}
