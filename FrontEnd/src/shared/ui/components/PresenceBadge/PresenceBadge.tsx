import styles from "./PresenceBadge.module.css";

type PresenceBadgeTone = "online" | "offline" | "inactive";
type PresenceBadgeSize = "sm" | "md";

type PresenceBadgeProps = {
  label: string;
  tone: PresenceBadgeTone;
  size?: PresenceBadgeSize;
};

export function PresenceBadge({ label, tone, size = "md" }: PresenceBadgeProps) {
  const badgeClassName = [
    styles.badge,
    styles[`tone${tone[0].toUpperCase()}${tone.slice(1)}`],
    size === "sm" ? styles.badgeSm : styles.badgeMd,
  ].join(" ");

  return (
    <span className={badgeClassName} title={label}>
      <span className={styles.dot} aria-hidden="true" />
      <span>{label}</span>
    </span>
  );
}
