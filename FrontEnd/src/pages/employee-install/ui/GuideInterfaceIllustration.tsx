import { useGuideTranslation } from "../model/guideLanguage";
import styles from "./EmployeeInstallPage.module.css";

type TimelineTone = "offered" | "remaining" | "accepted";

function SwapTimeline({ title, parts }: {
  title: string;
  parts: readonly (readonly [start: string, end: string, label: string, tone: TimelineTone])[];
}) {
  const t = useGuideTranslation();
  return (
    <span className={styles.swapTimeline}>
      <strong>{t(title)}</strong>
      <span className={styles.swapTimelineParts}>
        {parts.map(([start, end, label, tone]) => (
          <span key={start} className={styles.swapTimelinePart} data-tone={tone}>
            <span className={styles.swapTimelineBar} aria-hidden="true" />
            <b>{start}–{end}</b><span>{t(label)}</span>
          </span>
        ))}
      </span>
    </span>
  );
}

export function GuideInterfaceIllustration({ visual }: { visual: string }) {
  const t = useGuideTranslation();
  switch (visual) {
    case "swap-timeline-part":
      return <span className={styles.guideMock}>
        <strong>{t("Original shift")} · 09:00–21:00</strong>
        <SwapTimeline title="Offered interval" parts={[
          ["09:00", "15:00", "Offered", "offered"], ["15:00", "21:00", "Not offered", "remaining"],
        ]} />
      </span>;
    case "swap-timeline-modes":
      return <span className={styles.guideMock}>
        <SwapTimeline title="Only the exact shift" parts={[["09:00", "21:00", "Offered", "offered"]]} />
        <span>{t("Recipient must take the whole offered interval.")}</span>
        <SwapTimeline title="Allow custom time" parts={[
          ["09:00", "15:00", "Accepted", "accepted"], ["15:00", "21:00", "Remaining offer", "remaining"],
        ]} />
        <span>{t("Recipient can take a valid part of the offer.")}</span>
      </span>;
    case "swap-timeline-before":
      return <span className={styles.guideMock}>
        <SwapTimeline title="Offered interval" parts={[["09:00", "21:00", "Allow custom time", "offered"]]} />
        <SwapTimeline title="Accepted" parts={[
          ["09:00", "15:00", "Accepted", "accepted"], ["15:00", "21:00", "Remaining offer", "remaining"],
        ]} />
      </span>;
    case "pdf-download-example":
      return <span className={styles.pdfExample}>
        <span>{t("PDF is ready. Tap PDF again to share, or download it.")}</span>
        <span className={styles.pdfControl} data-highlight>{t("Download PDF")}</span>
      </span>;
    default:
      return null;
  }
}
