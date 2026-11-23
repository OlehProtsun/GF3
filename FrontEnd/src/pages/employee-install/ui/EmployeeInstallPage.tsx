import { useEffect, useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { languageNames, type Language } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import {
  AccountSettingsIcon, ArrowRightIcon, AvailabilityIcon, ChevronRightIcon, ClockIcon, EmployeeIcon, ScheduleIcon,
  SmartphoneIcon, SwapOffersIcon,
} from "@shared/ui/icons";
import { guideTopics, platformSteps, type GuideStep, type GuideTopic } from "../model/guideContent";
import { guideInstructions } from "../model/guideInstructions";
import { GuideLanguageContext, readGuideLanguage, rememberGuideLanguage, useGuideTranslation } from "../model/guideLanguage";
import { shiftSwapScenarios } from "../model/shiftSwapGuide";
import { GuideLanguageDialog } from "./GuideLanguageDialog";
import { GuideVisual } from "./GuideVisual";
import styles from "./EmployeeInstallPage.module.css";

const topicIcons: Record<GuideTopic["id"], ReactNode> = {
  "getting-started": <SmartphoneIcon size={22} />,
  notifications: <AccountSettingsIcon name="bell" size={22} />,
  schedules: <ScheduleIcon size={22} />,
  "work-hours": <ClockIcon size={22} />,
  "shift-corrections": <AccountSettingsIcon name="edit" size={22} />,
  "shift-swaps": <SwapOffersIcon size={22} />,
  "availability-urlop": <AvailabilityIcon size={22} />,
  profile: <EmployeeIcon size={22} />,
};

function topicUrl(topic: GuideTopic) {
  return `/install?topic=${topic.id}`;
}

function GuideSteps({ steps }: { steps: readonly GuideStep[] }) {
  const t = useGuideTranslation();
  let stepNumber = 0;
  return (
    <ol className={styles.stepList} role="list">
      {steps.map(step => {
        const original = guideInstructions[step.visual];
        const instruction = step.instruction ?? original?.instruction;
        const result = step.result ?? (step.instruction ? undefined : original?.result);
        return (
          <li key={step.visual} className={styles.step} data-password-mode={step.passwordMode}>
            <div className={styles.stepHeading}>
              {step.passwordMode ? (
                <span className={styles.passwordModeIcon} aria-hidden="true">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
                    {step.passwordMode === "pc" ? (
                      <><rect x="2" y="5" width="20" height="14" rx="3" /><path d="M6 9h1m4 0h2m4 0h1M6 13h1m4 0h2m4 0h1m-10 3h8" /></>
                    ) : (
                      <path d="M6 5h.01M12 5h.01M18 5h.01M6 10h.01M12 10h.01M18 10h.01M6 15h.01M12 15h.01M18 15h.01M12 20h.01" strokeWidth="3" />
                    )}
                  </svg>
                </span>
              ) : (
                <span className={styles.stepNumber} aria-hidden="true">{++stepNumber}</span>
              )}
              <strong>{t(step.title)}</strong>
            </div>
            {instruction && <p>{t(instruction)}</p>}
            <GuideVisual step={step} />
            {step.extraVisuals?.map(visual => (
              <GuideVisual key={visual.visual} step={{ ...visual, title: step.title }} />
            ))}
            {result && <p className={styles.stepResult}>{t(result)}</p>}
          </li>
        );
      })}
    </ol>
  );
}

function SwapScenarios() {
  const t = useGuideTranslation();
  return (
    <nav className={styles.swapScenarios} aria-labelledby="swap-scenarios-heading">
      <h1 id="swap-scenarios-heading" tabIndex={-1} data-guide-heading>{t("Choose a swap scenario")}</h1>
      {shiftSwapScenarios.map(scenario => (
        <a key={scenario.title} href={`#guide-${scenario.id}`}>
          <span><strong>{t(scenario.title)}</strong><small>{t(scenario.choice)}</small></span>
          <ChevronRightIcon size={17} />
        </a>
      ))}
    </nav>
  );
}

function Installation() {
  const t = useGuideTranslation();
  const [platform, setPlatform] = useState<"ios" | "android">("ios");
  return (
    <section className={styles.section}>
      <h1 tabIndex={-1} data-guide-heading>{t("Install on your phone")}</h1>
      <div className={styles.segments} role="group" aria-label={t("Installation platform")}>
        <button type="button" aria-pressed={platform === "ios"} onClick={() => setPlatform("ios")}>{t("iPhone / iOS")}</button>
        <button type="button" aria-pressed={platform === "android"} onClick={() => setPlatform("android")}>{t("Android")}</button>
      </div>
      <GuideSteps steps={platformSteps[platform]} />
      <div className={styles.references} aria-label={t("Browser instructions")}>
        <a href="https://support.apple.com/guide/iphone/iphea86e5236/ios" target="_blank" rel="noreferrer">Apple</a>
        <a href="https://support.google.com/chrome/answer/9658361?co=GENIE.Platform%3DAndroid" target="_blank" rel="noreferrer">Google Chrome</a>
      </div>
    </section>
  );
}

function CorrectionExamples() {
  const t = useGuideTranslation();
  return <section className={styles.section}>
    <h2>{t("Examples of shift changes")}</h2>
    <table className={styles.comparison}>
      <thead><tr><th scope="col">{t("Original shift")}</th><th scope="col">{t("Requested shift")}</th></tr></thead>
      <tbody>
        <tr><td>09:00–15:00</td><td>08:30–16:00</td></tr>
        <tr><td>09:00–15:00</td><td>–</td></tr>
        <tr><td>{t("Day off")}</td><td>09:00–15:00</td></tr>
      </tbody>
    </table>
  </section>;
}

export function EmployeeInstallPage() {
  const applicationLanguage = useLanguageRevision();
  const [preferredLanguage] = useState(() => readGuideLanguage(applicationLanguage));
  const [language, setGuideLanguage] = useState<Language | null>(null);
  const [choosingLanguage, setChoosingLanguage] = useState(true);

  function confirmLanguage(next: Language) {
    rememberGuideLanguage(next);
    setGuideLanguage(next);
    setChoosingLanguage(false);
  }

  return <>
    {language && (
      <GuideLanguageContext.Provider value={language}>
        <GuideContent language={language} onChangeLanguage={() => setChoosingLanguage(true)} />
      </GuideLanguageContext.Provider>
    )}
    {choosingLanguage && (
      <GuideLanguageDialog displayLanguage={language ?? applicationLanguage}
        initialLanguage={language ?? preferredLanguage} onConfirm={confirmLanguage}
        onDismiss={language ? () => setChoosingLanguage(false) : undefined} />
    )}
  </>;
}

function GuideContent({ language, onChangeLanguage }: { language: Language; onChangeLanguage: () => void }) {
  const t = useGuideTranslation();
  const { search } = useLocation();
  const params = new URLSearchParams(search);
  const requestedTopic = params.get("topic");
  const requestedSection = params.get("section");
  const activeIndex = guideTopics.findIndex(topic => topic.id === requestedTopic);
  const activeTopic = guideTopics[activeIndex];
  const nextTopic = activeTopic ? guideTopics[activeIndex + 1] : undefined;
  const pageRef = useRef<HTMLElement>(null);
  const fromHistory = useRef(false);

  useEffect(() => {
    const rememberTraversal = () => { fromHistory.current = true; };
    window.addEventListener("popstate", rememberTraversal);
    return () => window.removeEventListener("popstate", rememberTraversal);
  }, []);

  useLayoutEffect(() => {
    const page = pageRef.current;
    const target = requestedSection === "password" ? page?.querySelector("#guide-password") : null;
    if (!fromHistory.current) (target ?? page)?.scrollIntoView({ block: "start", inline: "nearest", behavior: "instant" });
    (target?.querySelector<HTMLElement>("h2") ?? page?.querySelector<HTMLElement>("[data-guide-heading]"))?.focus({ preventScroll: true });
    fromHistory.current = false;
  }, [activeTopic?.id, requestedSection]);

  return (
    <article ref={pageRef} className={styles.page} lang={language} aria-label={t(activeTopic?.title ?? "GF App Guide")}>
      <div className={styles.guideToolbar}>
        {activeTopic && (
          <NavLink className={styles.allTopics} to="/install">
            <ArrowRightIcon size={17} />
            {t("All topics")}
          </NavLink>
        )}
        <button type="button" className={styles.guideLanguage} onClick={onChangeLanguage}
          aria-label={t("Change guide language ({0})", languageNames[language])} aria-haspopup="dialog">
          <AccountSettingsIcon name="globe" size={17} />
          {language.toUpperCase()}
        </button>
      </div>
      {activeTopic ? <>
        <div className={styles.sections} key={activeTopic.id}>
          {activeTopic.id === "getting-started" && <Installation />}
          {activeTopic.id === "shift-swaps" && <SwapScenarios />}
          {activeTopic.groups.map((group, index) => {
            const isSwap = activeTopic.id === "shift-swaps";
            const Heading = index === 0 && activeTopic.id !== "getting-started" && !isSwap ? "h1" : "h2";
            return <section className={styles.section} id={group.id ? `guide-${group.id}` : undefined} key={group.title}>
              <Heading className={isSwap ? styles.swapSectionHeading : undefined} tabIndex={-1} data-guide-heading={Heading === "h1" || undefined}>{t(group.title)}</Heading>
              <GuideSteps steps={group.steps} />
            </section>;
          })}
          {activeTopic.id === "shift-corrections" && <CorrectionExamples />}
        </div>
        <footer className={styles.topicFooter}>
          <NavLink className={styles.next} to={nextTopic ? topicUrl(nextTopic) : "/install"}>
            <span>
              {nextTopic ? <><small>{t("Next topic")}</small><strong>{t(nextTopic.title)}</strong></>
                : <strong>{t("Back to guide topics")}</strong>}
            </span>
            <ChevronRightIcon size={18} />
          </NavLink>
        </footer>
      </> : <>
        <section className={styles.welcome}>
          <h1 tabIndex={-1} data-guide-heading>{t("Welcome to GF")}</h1>
          <span className={styles.topicCount}>{t("{0} topics", guideTopics.length)}</span>
        </section>
        <nav aria-label={t("Guide topics")} className={styles.topics}>
          {guideTopics.map(topic => (
            <NavLink key={topic.id} to={topicUrl(topic)} className={styles.topic}>
              <span className={styles.topicIcon} aria-hidden="true">{topicIcons[topic.id]}</span>
              <span className={styles.topicCopy}>
                <strong>{t(topic.title)}</strong>
                <span>{t(topic.subtitle)}</span>
              </span>
              <ChevronRightIcon className={styles.chevron} size={18} />
            </NavLink>
          ))}
        </nav>
      </>}
    </article>
  );
}
