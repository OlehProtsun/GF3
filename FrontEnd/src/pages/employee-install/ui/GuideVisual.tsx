import { useContext, useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { ChevronRightIcon, CloseIcon, PlusIcon, ShareIcon } from "@shared/ui/icons";
import captures from "../model/guideCaptures.json";
import type { GuideStep } from "../model/guideContent";
import { GuideLanguageContext, useGuideTranslation } from "../model/guideLanguage";
import { GuideInterfaceIllustration } from "./GuideInterfaceIllustration";
import styles from "./EmployeeInstallPage.module.css";

type Capture = { src: string; width: number; height: number; highlight?: number[] };
const guideCaptures: Partial<Record<string, Capture>> = captures;

function DemoCrop() {
  const capture = guideCaptures.login;
  return capture && <img className={styles.browserPage} src={`/guide/${capture.src}`}
    width={capture.width} height={capture.height} alt="" loading="lazy" decoding="async" />;
}

function Illustration({ step }: { step: GuideStep }) {
  const t = useGuideTranslation();
  const icon = <img src="/icons/gf-icon-192.png" width={36} height={36} alt="" />;

  function row(label: string, artwork?: ReactNode, active = false) {
    return <span className={styles.browserRow} data-highlight={active || undefined}>
      {artwork}<span>{t(label)}</span>
    </span>;
  }

  switch (step.visual) {
    case "ios-open":
    case "ios-share":
      return (
        <span className={styles.browserMock}>
          <span className={styles.browserTitle}>Safari</span>
          <span className={styles.browserToolbar}>
            <span className={styles.address} data-highlight={step.visual.endsWith("-open") || undefined}>GF</span>
            <span className={styles.browserControl} data-highlight={step.visual === "ios-share" || undefined} aria-label={t("Share")}><ShareIcon size={21} /></span>
          </span>
          {step.visual.endsWith("-open") && <DemoCrop />}
        </span>
      );
    case "ios-page-menu":
      return <span className={styles.browserMock}>
        <span className={styles.browserTitle}>Safari</span>
        <span className={styles.browserToolbar}>
          <span className={styles.address}>GF</span>
          <span className={styles.browserControl} data-highlight aria-hidden="true">•••</span>
        </span>
        <span className={styles.browserAlternative}>
          {row("Page Menu")}<ChevronRightIcon size={15} />{row("Share", <ShareIcon size={17} />, true)}
        </span>
      </span>;
    case "ios-add":
    case "ios-help":
      return <span className={styles.browserMock}>
        <span className={styles.browserTitle}>{t("Share actions")}</span>
        {row(step.control ?? step.title, <PlusIcon size={18} />, true)}
      </span>;
    case "ios-web":
    case "ios-confirm":
      return <span className={styles.browserMock}>
        <span className={styles.browserToolbar}>
          {row("GF", icon)}
          {row("Add", undefined, step.visual.endsWith("-confirm"))}
        </span>
        {step.visual === "ios-web" && row("Open as Web App", <span className={styles.switchExample} aria-hidden="true" />, true)}
      </span>;
    case "android-launch":
      return <span className={styles.browserMock}>
        <span className={styles.browserTitle}>{t("App launcher")}</span>
        <span className={styles.launcherTile} data-highlight>{icon}<strong>GF</strong></span>
      </span>;
    default:
      return <GuideInterfaceIllustration visual={step.visual} />;
  }
}

function Screenshot({ step, capture, eager = false }: { step: GuideStep; capture: Capture; eager?: boolean }) {
  const t = useGuideTranslation();
  const highlight = capture.highlight;
  const highlightStyle = highlight && {
    left: highlight[0] + "%", top: highlight[1] + "%", width: highlight[2] + "%", height: highlight[3] + "%",
  };
  return <span className={styles.capture} style={{ "--capture-ratio": capture.width / capture.height } as CSSProperties}>
    <img src={`/guide/${capture.src}`} width={capture.width} height={capture.height}
      alt={t("GF screenshot showing {0}", t(step.control ?? step.title))}
      loading={eager ? "eager" : "lazy"} decoding="async" />
    {highlightStyle && <span className={styles.highlight} style={highlightStyle} aria-hidden="true" />}
    {step.visual === "schedule-order-drag" && highlight && (
      <span className={styles.dragDirection} style={{ top: (highlight[1] + highlight[3] / 2) + "%" }} aria-hidden="true">↕</span>
    )}
    {step.visual === "android-menu" && <span className={styles.scrollDirection} aria-hidden="true">↓</span>}
  </span>;
}

function ImagePreview({ step, capture, onClose }: { step: GuideStep; capture?: Capture; onClose: () => void }) {
  const t = useGuideTranslation();
  const language = useContext(GuideLanguageContext);
  const dialogRef = useRef<HTMLDialogElement>(null);
  useLayoutEffect(() => {
    const dialog = dialogRef.current;
    if (dialog && !dialog.open) dialog.showModal();
    return () => { if (dialog?.open) dialog.close(); };
  }, []);
  return createPortal(
    <dialog ref={dialogRef} className={styles.imageDialog} lang={language}
      aria-label={t(capture ? "Screenshot preview" : "Interface illustration")}
      onClose={event => { if (!event.currentTarget.open) onClose(); }}
      onClick={event => { if (event.target === event.currentTarget) dialogRef.current?.close(); }}>
      <button type="button" className={styles.closePreview} aria-label={t("Close")} autoFocus
        onClick={() => dialogRef.current?.close()}><CloseIcon size={23} /></button>
      <div className={styles.previewContent}>
        {capture ? <Screenshot step={step} capture={capture} eager /> : <Illustration step={step} />}
      </div>
      <p className={styles.previewCaption}>{t(step.title)}</p>
    </dialog>,
    document.body,
  );
}

export function GuideVisual({ step }: { step: GuideStep }) {
  const t = useGuideTranslation();
  const [expanded, setExpanded] = useState(false);
  const capture = guideCaptures[step.visual];
  return <>
    <button type="button" className={styles.visual} aria-haspopup="dialog"
      aria-label={t("Enlarge visual for {0}", t(step.title))} onClick={() => setExpanded(true)}>
      {capture ? <Screenshot step={step} capture={capture} /> : <Illustration step={step} />}
    </button>
    {!capture && <small className={styles.illustrationLabel}>{t("Interface illustration")}</small>}
    {expanded && <ImagePreview step={step} capture={capture} onClose={() => setExpanded(false)} />}
  </>;
}
