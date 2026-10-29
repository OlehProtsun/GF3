import { useEffect, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useGraphContractApprovalsQuery, useReviewContractApprovalMutation, type ContractShiftApproval } from "@entities/shift-swap-approvals";
import { queryKeys } from "@shared/api/queryKeys";
import { getErrorMessage } from "@shared/api/httpClient";
import { t, translateServerMessage } from "@shared/i18n";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import styles from "./ContainerGraphContractApprovalsCard.module.css";
export function ContainerGraphContractApprovalsCard({ containerId, graphId, disabled, hasUnsavedChanges, onApproved, onPendingCountChange }: {
  containerId: number | null; graphId: number | null; disabled: boolean; hasUnsavedChanges: boolean;
  onApproved: () => void; onPendingCountChange?: (count: number) => void;
}) {
  const query = useGraphContractApprovalsQuery(containerId, graphId);
  const mutation = useReviewContractApprovalMutation();
  const client = useQueryClient();
  const [confirm, setConfirm] = useState<ContractShiftApproval | null>(null);
  const [error, setError] = useState<string | null>(null);
  const count = query.data?.filter(item => item.status === "pending").length ?? 0;
  useEffect(() => onPendingCountChange?.(count), [count, onPendingCountChange]);
  const review = (item: ContractShiftApproval, action: "approve" | "reject") => {
    if (!containerId || !graphId || disabled || hasUnsavedChanges || mutation.isPending) return;
    setError(null);
    mutation.mutate({ containerId, graphId, id: item.id, action }, {
      onSuccess: () => { setConfirm(null); if (action === "approve" && item.kind === "claim") onApproved(); },
      onError: failure => {
        setConfirm(null); setError(getErrorMessage(failure, t("Could not review this request.")));
        client.invalidateQueries({ queryKey: queryKeys.shiftSwaps.graphApprovals(containerId, graphId) });
      },
    });
  };
  if (!containerId || !graphId) return null;
  const blocked = disabled || hasUnsavedChanges || mutation.isPending;
  return <section className={styles.root} aria-label={t("Contract approvals")}>
    <header className={styles.summary}><strong>{t("Contract approvals")}</strong><span>{t("{0} pending", count)}</span></header>
    <p>{t("Pending requests do not reserve offers. Schedules change only after approval.")}</p>
    {hasUnsavedChanges ? <p role="status">{t("Save your changes before reviewing requests.")}</p> : null}
    {error || query.error ? <ErrorBanner dismissible={false}>{error ?? getErrorMessage(query.error, t("Could not load approval requests."))}</ErrorBanner> : null}
    {query.isError ? <button type="button" onClick={() => client.invalidateQueries({ queryKey: queryKeys.shiftSwaps.graphApprovals(containerId, graphId) })}>{t("Retry")}</button> : null}
    {query.isLoading ? <p role="status">{t("Loading...")}</p> : null}
    {query.data?.length === 0 ? <p>{t("No contract approval requests.")}</p> : null}
    <div className={styles.list}>{query.data?.map(item => <article key={item.id} className={styles.request}>
      <strong>{item.employeeName}</strong>
      <span>{t(item.kind === "claim" ? "Shift pickup approval" : "Monthly minimum exception")}</span>
      <span>{item.shopName} · {item.scheduleName}</span>
      <span>{item.dayOfMonth ? `${item.dayOfMonth}.${item.month}.${item.year}` : `${item.month}.${item.year}`} · {item.fromTime}–{item.toTime}</span>
      {item.requestKind === "exchange" ? <span>{t("Return shift")}: {item.returnFromTime}–{item.returnToTime}</span> : null}
      {item.minimumShopName ? <span>{item.minimumShopName} · {item.minimumMonth}.{item.minimumYear}</span> : null}
      {item.projectedMinutes != null ? <span>{t("Monthly minimum: {0} h; after transfer: {1} h.", item.minimumMinutes == null ? t("Not configured") : item.minimumMinutes / 60, item.projectedMinutes / 60)}</span> : null}
      {item.shiftSwapRequestId ? <span>{t("Related offer")}: #{item.shiftSwapRequestId}</span> : null}
      <span className={styles.status}>{({ pending: t("Pending"), approved: t("Approved"), rejected: t("Rejected"), cancelled: t("Cancelled"), expired: t("Expired") } as const)[item.status]}</span>
      {item.failureReason ? <p>{translateServerMessage(item.failureReason)}</p> : null}
      {item.status === "pending" ? <div className={styles.actions}>
        <button type="button" disabled={blocked} onClick={() => item.kind === "claim" ? setConfirm(item) : review(item, "approve")}>{t("Approve")}</button>
        <button type="button" disabled={blocked} onClick={() => review(item, "reject")}>{t("Reject")}</button>
      </div> : null}
    </article>)}</div>
    <ConfirmDialog open={confirm !== null} title={t("Approve shift transfer?")} message={t("Approval transfers the requested shifts and records schedule history.")}
      confirmText={t("Approve")} confirmDisabled={blocked} cancelDisabled={mutation.isPending}
      onCancel={() => setConfirm(null)} onConfirm={() => { if (confirm) review(confirm, "approve"); }} />
  </section>;
}
