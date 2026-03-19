import type { CSSProperties, ReactNode } from "react";
import { DetailItem, DetailList } from "@shared/ui/components/DetailList";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./ProfileSummaryCard.module.css";

export type ProfileSummaryDetail = {
  key?: string;
  label: ReactNode;
  value: ReactNode;
};

type ProfileSummaryCardProps = {
  sectionTitle: ReactNode;
  icon?: ReactNode;
  headerMeta?: ReactNode;
  headerRightSlot?: ReactNode;
  statusContent?: ReactNode;
  avatar?: ReactNode;
  name?: ReactNode;
  subtitle?: ReactNode;
  details?: ProfileSummaryDetail[];
  contentAfterIdentity?: ReactNode;
  detailColumns?: 1 | 2 | 3;
  actions?: ReactNode;
  className?: string;
  style?: CSSProperties;
};

export function ProfileSummaryCard({
  sectionTitle,
  icon,
  headerMeta,
  headerRightSlot,
  statusContent,
  avatar,
  name,
  subtitle,
  details = [],
  contentAfterIdentity,
  detailColumns = 2,
  actions,
  className,
  style,
}: ProfileSummaryCardProps) {
  const cardClassName = [styles.card, className ?? ""].filter(Boolean).join(" ");
  const hasProfileContent =
    avatar != null ||
    name != null ||
    subtitle != null ||
    details.length > 0 ||
    contentAfterIdentity != null ||
    actions != null;
  const identityClassName = [
    styles.identityBlock,
    avatar == null ? styles.identityBlockNoAvatar : "",
  ]
    .filter(Boolean)
    .join(" ");

  const resolvedHeaderRightSlot =
    headerMeta != null || headerRightSlot != null ? (
      <div className={styles.headerRightStack}>
        {headerMeta != null ? <span className={styles.headerMetaBadge}>{headerMeta}</span> : null}
        {headerRightSlot}
      </div>
    ) : null;

  return (
    <CardSection
      className={cardClassName}
      style={style}
      title={sectionTitle}
      icon={icon}
      headerRightSlot={resolvedHeaderRightSlot}
    >
      <div className={styles.summaryLayout}>
        {statusContent}

        {hasProfileContent ? (
          <>
            <div className={identityClassName}>
              {avatar != null ? (
                <div className={styles.avatar} aria-hidden="true">
                  {avatar}
                </div>
              ) : null}

              <div className={styles.identityContent}>
                {name != null ? <h2 className={styles.name}>{name}</h2> : null}
                {subtitle != null ? <p className={styles.subtitle}>{subtitle}</p> : null}

                {details.length > 0 ? (
                  <DetailList className={styles.detailList} columns={detailColumns}>
                    {details.map((item, index) => (
                      <DetailItem
                        key={item.key ?? index}
                        label={item.label}
                        value={item.value}
                        className={styles.detailItem}
                        valueClassName={styles.detailValue}
                      />
                    ))}
                  </DetailList>
                ) : null}
              </div>
            </div>

            {contentAfterIdentity}

            {actions ? <div className={styles.actions}>{actions}</div> : null}
          </>
        ) : null}
      </div>
    </CardSection>
  );
}
