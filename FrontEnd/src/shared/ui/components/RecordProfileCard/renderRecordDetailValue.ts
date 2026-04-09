import { createElement } from "react";
import styles from "./RecordProfileCard.module.css";

export function renderRecordDetailValue(value?: string | null, href?: string) {
  if (!value) {
    return createElement("span", { className: styles.mutedValue }, "Not provided");
  }

  if (!href) {
    return value;
  }

  return createElement("a", { className: styles.valueLink, href }, value);
}
