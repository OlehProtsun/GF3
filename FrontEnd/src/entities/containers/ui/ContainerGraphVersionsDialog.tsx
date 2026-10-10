import { dateTimeFormat, t } from "@shared/i18n";
import { useState, type CSSProperties } from "react";
import type { GraphVersionDto } from "@entities/containers/api/dto";
import {
  useCheckoutGraphVersionMutation,
  useDeleteGraphVersionMutation,
  useGraphVersionsQuery,
} from "@entities/containers/api/queries";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { CloseIcon, SwapHistoryIcon, SwapOffersIcon } from "@shared/ui/icons";
import styles from "./ContainerGraphVersionsDialog.module.css";

type ContainerGraphVersionsDialogProps = {
  open: boolean;
  containerId: number | null;
  graphId: number | null;
  hasUnsavedChanges: boolean;
  onCancel: () => void;
  onCheckoutComplete: () => void;
};

type VersionPosition = { x: number; y: number };

const X_SPACING = 124;
const Y_SPACING = 98;
const X_OFFSET = 150;
const Y_OFFSET = 56;

function formatCommitDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : dateTimeFormat("en-GB", {
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    }).format(date);
}

function getAuthorInitial(authorName: string) {
  return authorName.trim().charAt(0).toLocaleUpperCase() || "?";
}

function buildVersionLayout(versions: GraphVersionDto[]) {
  const versionById = new Map(versions.map(version => [version.id, version]));
  const depthById = new Map<number, number>();
  const resolveDepth = (version: GraphVersionDto, visiting = new Set<number>()): number => {
    const cached = depthById.get(version.id);
    if (cached !== undefined) {
      return cached;
    }

    if (!version.parentVersionId || visiting.has(version.id)) {
      depthById.set(version.id, 0);
      return 0;
    }

    visiting.add(version.id);
    const parent = versionById.get(version.parentVersionId);
    const depth = parent ? resolveDepth(parent, visiting) + 1 : 0;
    depthById.set(version.id, depth);
    return depth;
  };
  const branchNames = [...new Set(versions.map(version => version.branchName))];
  const branchIndexByName = new Map(branchNames.map((branchName, index) => [branchName, index]));
  const positions = new Map<number, VersionPosition>();

  versions.forEach(version => {
    positions.set(version.id, {
      x: X_OFFSET + resolveDepth(version) * X_SPACING,
      y: Y_OFFSET + (branchIndexByName.get(version.branchName) ?? 0) * Y_SPACING,
    });
  });

  const maxDepth = Math.max(0, ...versions.map(version => depthById.get(version.id) ?? 0));
  return {
    branchNames,
    positions,
    width: Math.max(760, X_OFFSET + maxDepth * X_SPACING + 110),
    height: Math.max(210, Y_OFFSET + Math.max(0, branchNames.length - 1) * Y_SPACING + 70),
  };
}

export function ContainerGraphVersionsDialog({
  open,
  containerId,
  graphId,
  hasUnsavedChanges,
  onCancel,
  onCheckoutComplete,
}: ContainerGraphVersionsDialogProps) {
  const versionsQuery = useGraphVersionsQuery(containerId, graphId, open);
  const checkoutMutation = useCheckoutGraphVersionMutation();
  const deleteMutation = useDeleteGraphVersionMutation();
  const [selectedVersionId, setSelectedVersionId] = useState<number | null>(null);
  const [checkoutTarget, setCheckoutTarget] = useState<GraphVersionDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<GraphVersionDto | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const versions = versionsQuery.data?.versions ?? [];
  const selectedVersion = versions.find(version => version.id === selectedVersionId)
    ?? versions.find(version => version.isCurrent)
    ?? versions.at(-1)
    ?? null;
  const layout = buildVersionLayout(versions);
  const isBusy = checkoutMutation.isPending || deleteMutation.isPending;

  if (!open) {
    return null;
  }

  const handleCheckout = () => {
    if (!checkoutTarget || containerId === null || graphId === null) {
      return;
    }

    setActionError(null);
    checkoutMutation.mutate(
      { containerId, graphId, versionId: checkoutTarget.id },
      {
        onSuccess: () => {
          setCheckoutTarget(null);
          onCheckoutComplete();
        },
        onError: error => {
          setActionError(getErrorMessage(error, t("Could not checkout this commit.")));
          setCheckoutTarget(null);
        },
      },
    );
  };

  const handleDelete = () => {
    if (!deleteTarget || containerId === null || graphId === null) {
      return;
    }

    setActionError(null);
    deleteMutation.mutate(
      { containerId, graphId, versionId: deleteTarget.id },
      {
        onSuccess: () => {
          setSelectedVersionId(null);
          setDeleteTarget(null);
        },
        onError: error => {
          setActionError(getErrorMessage(error, t("Could not delete this commit.")));
          setDeleteTarget(null);
        },
      },
    );
  };

  return (
    <div className={styles.overlay} role="presentation" onMouseDown={event => event.target === event.currentTarget && !isBusy && onCancel()}>
      <section className={styles.dialog} role="dialog" aria-modal="true" aria-labelledby="graph-versions-title">
        <header className={styles.header}>
          <div className={styles.titleBlock}>
            <span className={styles.eyebrow}><SwapHistoryIcon size={14} />  {t("Version control")}</span>
            <h2 id="graph-versions-title" className={styles.title}>{t("Schedule history")}</h2>
            <p className={styles.description}>{t("Every successful Save creates an immutable commit. Checkout an older point and the next change starts a new branch automatically.")}</p>
          </div>
          <button type="button" className={styles.closeButton} aria-label={t("Close version control")} disabled={isBusy} onClick={onCancel}>
            <CloseIcon size={18} />
          </button>
        </header>

        {actionError ? <ErrorBanner>{actionError}</ErrorBanner> : null}

        <div className={styles.treeShell}>
          {versionsQuery.isLoading ? <div className={styles.state}>{t("Loading commit tree...")}</div> : null}
          {versionsQuery.isError ? <div className={styles.state}>{t("Could not load schedule commits.")}</div> : null}
          {!versionsQuery.isLoading && !versionsQuery.isError && versions.length === 0 ? (
            <div className={styles.state}>{t("The first commit will appear after this schedule is saved.")}</div>
          ) : null}
          {versions.length > 0 ? (
            <div className={styles.treeScroller}>
              <div className={styles.treeCanvas} style={{ width: layout.width, height: layout.height } as CSSProperties}>
                <svg
                  className={styles.connections}
                  data-testid="version-connections"
                  width={layout.width}
                  height={layout.height}
                  aria-hidden="true"
                >
                  {versions.flatMap(version => {
                    if (!version.parentVersionId) {
                      return [];
                    }

                    const parent = layout.positions.get(version.parentVersionId);
                    const child = layout.positions.get(version.id);
                    if (!parent || !child) {
                      return [];
                    }

                    const bend = Math.max(28, (child.x - parent.x) * 0.48);
                    return [(
                      <path
                        key={`${version.parentVersionId}-${version.id}`}
                        d={`M ${parent.x + 22} ${parent.y} C ${parent.x + bend} ${parent.y}, ${child.x - bend} ${child.y}, ${child.x - 22} ${child.y}`}
                        className={version.isCurrent ? styles.currentConnection : styles.connection}
                      />
                    )];
                  })}
                </svg>

                {layout.branchNames.map((branchName, index) => (
                  <span key={branchName} className={styles.branchLabel} style={{ top: Y_OFFSET + index * Y_SPACING - 14 }}>
                    {branchName}
                  </span>
                ))}

                {versions.map(version => {
                  const position = layout.positions.get(version.id) ?? { x: X_OFFSET, y: Y_OFFSET };
                  const isSelected = selectedVersion?.id === version.id;
                  return (
                    <button
                      key={version.id}
                      type="button"
                      className={`${styles.commitButton} ${version.isCurrent ? styles.commitCurrent : ""} ${isSelected ? styles.commitSelected : ""}`}
                      style={{ left: position.x, top: position.y }}
                      aria-label={t("Commit {0}, {1}{2}", version.versionNumber, version.branchName, version.isCurrent ? t(", current") : "")}
                      aria-pressed={isSelected}
                      onClick={() => setSelectedVersionId(version.id)}
                    >
                      <span className={styles.commitDot} aria-hidden="true">{getAuthorInitial(version.authorName)}</span>
                      <span className={styles.commitNumber}>#{version.versionNumber}</span>
                    </button>
                  );
                })}
              </div>
            </div>
          ) : null}
        </div>

        {selectedVersion ? (
          <section className={styles.commitDetails} aria-label={t("Commit {0} details", selectedVersion.versionNumber)}>
            <div className={styles.commitIdentity}>
              <div className={styles.commitTitleRow}>
                <strong>{t("Commit #")}{selectedVersion.versionNumber}</strong>
                <span className={styles.branchChip}>{selectedVersion.branchName}</span>
                {selectedVersion.isCurrent ? <span className={styles.currentChip}>{t("Current")}</span> : null}
              </div>
              <span>{formatCommitDate(selectedVersion.createdAtUtc)} · {selectedVersion.authorName}</span>
            </div>
            <div className={styles.commitStats}>
              <span><strong>{selectedVersion.employeeCount}</strong>  {t("employees")}</span>
              <span><strong>{selectedVersion.slotCount}</strong>  {t("shifts")}</span>
              <span><strong>{selectedVersion.cellStyleCount}</strong>  {t("styles")}</span>
            </div>
            <div className={styles.actions}>
              <button
                type="button"
                className={`${styles.actionButton} ${styles.deleteAction}`}
                disabled={selectedVersion.isCurrent || isBusy}
                onClick={() => setDeleteTarget(selectedVersion)}
              >
                <span className={styles.actionIcon}><CloseIcon size={14} /></span>
                {t("Delete")}</button>
              <button
                type="button"
                className={`${styles.actionButton} ${styles.switchAction}`}
                disabled={selectedVersion.isCurrent || isBusy}
                onClick={() => setCheckoutTarget(selectedVersion)}
              >
                <span className={styles.actionIcon}><SwapOffersIcon size={15} /></span>
                {t("Switch")}
              </button>
            </div>
          </section>
        ) : null}
      </section>

      <ConfirmDialog
        open={checkoutTarget !== null}
        title={t("Checkout commit #{0}", checkoutTarget?.versionNumber ?? "")}
        message={hasUnsavedChanges
          ? t("This will replace the current schedule with the selected commit and discard the unsaved editor draft. The next Save will create a new branch.")
          : t("This will replace the current schedule with the selected commit. The next Save from that point will create a new branch.")}
        confirmText={checkoutMutation.isPending ? t("Checking out...") : t("Checkout")}
        confirmDisabled={checkoutMutation.isPending}
        cancelDisabled={checkoutMutation.isPending}
        onCancel={() => setCheckoutTarget(null)}
        onConfirm={handleCheckout}
      />
      <ConfirmDialog
        open={deleteTarget !== null}
        title={t("Delete commit #{0}", deleteTarget?.versionNumber ?? "")}
        message={t("Delete this immutable snapshot? Any child commits will stay available and reconnect to its parent. This action cannot be undone.")}
        confirmText={deleteMutation.isPending ? t("Deleting...") : t("Delete")}
        confirmDisabled={deleteMutation.isPending}
        cancelDisabled={deleteMutation.isPending}
        variant="warning"
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
      />
    </div>
  );
}
