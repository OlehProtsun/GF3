import { t } from "@shared/i18n";
import type { UrlopStatus } from "../model/types";
export function UrlopStatusLabel({ status }: { status: UrlopStatus }) {
  return <span data-urlop-status={status}>{t(status === "pending" ? "Pending" : status === "approved" ? "Approved" : "Rejected")}</span>;
}
