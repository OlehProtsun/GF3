import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { ProblemScene } from "./scenes/ProblemScene";
import { ReframeScene } from "./scenes/ReframeScene";
import { ManagerScene } from "./scenes/ManagerScene";
import { AvailabilityScene } from "./scenes/AvailabilityScene";
import { ScheduleScene } from "./scenes/ScheduleScene";
import { MoreScene } from "./scenes/MoreScene";
import { SwapScene } from "./scenes/SwapScene";
import { ConnectedScene } from "./scenes/ConnectedScene";
import { OutroScene } from "./scenes/OutroScene";
import { PROMO_DURATION_SECONDS, PROMO_FPS, PROMO_HEIGHT, PROMO_WIDTH, SCENES, MOTION_CUE_SHEET } from "./storyboard";
import { createPromoTimeline, type PromoTimelineController } from "./timeline";
import styles from "./PromoApp.module.css";
import "./promoTheme.css";
import { ManagerCapture } from "./real-ui/RealFeatureShot";

export function PromoApp() {
  return new URLSearchParams(window.location.search).get("productShot") === "manager" ? <ManagerCapture /> : <FilmPreview />;
}

function FilmPreview() {
  const stage = useRef<HTMLDivElement>(null);
  const [capture] = useState(() => new URLSearchParams(window.location.search).get("capture") === "1");
  const [size, setSize] = useState(() => ({ width: window.innerWidth, height: window.innerHeight }));
  const [reduced, setReduced] = useState(() => window.matchMedia("(prefers-reduced-motion: reduce)").matches);
  const [ready, setReady] = useState(false);
  const [time, setTime] = useState(0);
  const [playing, setPlaying] = useState(false);
  const controller = useRef<PromoTimelineController | null>(null);

  useEffect(() => {
    const resize = () => setSize({ width: window.innerWidth, height: window.innerHeight });
    const media = window.matchMedia("(prefers-reduced-motion: reduce)");
    const change = () => setReduced(media.matches);
    window.addEventListener("resize", resize);
    media.addEventListener("change", change);
    return () => { window.removeEventListener("resize", resize); media.removeEventListener("change", change); };
  }, []);

  useLayoutEffect(() => {
    if (!stage.current) return;
    const film = createPromoTimeline(stage.current);
    controller.current = film;
    let active = true;
    const initialize = async () => {
      await document.fonts.ready;
      await Promise.all(Array.from(stage.current!.querySelectorAll("img")).map(image => image.decode()));
      if (!active) return;
      if (capture) {
        window.__GF3_PROMO__ = Object.freeze({
          duration: PROMO_DURATION_SECONDS, fps: PROMO_FPS, width: PROMO_WIDTH, height: PROMO_HEIGHT,
          ready: true, seek: film.seek, getTime: film.getTime,
        });
      } else {
        let lastTick = -1;
        film.onUpdate(seconds => {
          const tick = Math.floor(seconds * 10);
          if (tick !== lastTick) { lastTick = tick; setTime(seconds); }
          if (seconds >= PROMO_DURATION_SECONDS) setPlaying(false);
        });
        if (reduced) film.seek(SCENES[0].end - 1.5);
      }
      setPlaying(false);
      setReady(true);
    };
    void initialize().catch(error => { if (active) console.error("Film assets failed to decode", error); });
    return () => {
      active = false;
      delete window.__GF3_PROMO__;
      film.destroy();
      controller.current = null;
    };
  }, [capture, reduced]);

  const seek = (seconds: number) => {
    const target = Math.max(0, Math.min(PROMO_DURATION_SECONDS, seconds));
    const scene = SCENES.find(scene => target < scene.end) ?? SCENES[8];
    controller.current?.seek(reduced && !capture ? scene.end - 1.5 : target);
    setTime(target);
    setPlaying(false);
  };
  const toggle = () => {
    if (!controller.current) return;
    if (playing) { controller.current.pause(); setPlaying(false); }
    else if (reduced) {
      const index = SCENES.findIndex(scene => time < scene.end);
      seek(SCENES[(index + 1) % SCENES.length].start);
    } else {
      if (time >= PROMO_DURATION_SECONDS) controller.current.seek(0);
      controller.current.play(); setPlaying(true);
    }
  };

  useEffect(() => {
    if (capture) return;
    const keydown = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement;
      const formControl = target.matches("input, textarea, select, button") || target.isContentEditable;
      if (event.code === "Space" && !formControl) { event.preventDefault(); toggle(); }
      else if (target.closest("[data-promo-controls]") && ["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) {
        event.preventDefault();
        seek(event.key === "Home" ? 0 : event.key === "End" ? PROMO_DURATION_SECONDS : time + (event.key === "ArrowRight" ? 1 : -1));
      }
    };
    window.addEventListener("keydown", keydown);
    return () => window.removeEventListener("keydown", keydown);
  });

  const scale = Math.min(size.width / PROMO_WIDTH, size.height / PROMO_HEIGHT);
  const current = SCENES.find(scene => time < scene.end) ?? SCENES[8];
  return <main className={styles.page} data-promo-mode={!ready ? "loading" : capture ? "capture" : "preview"}>
    <div className={styles.viewport}><div className={styles.frame} style={{ width: PROMO_WIDTH * scale, height: PROMO_HEIGHT * scale }}>
      <div ref={stage} className={styles.stage} data-promo-stage data-motion-cue-sheet={JSON.stringify(MOTION_CUE_SHEET)} data-promo-capture={capture} style={{ transform: `scale(${scale})` }} aria-label="GF3 — prezentacja produktu">
        <ProblemScene /><ReframeScene /><ManagerScene /><AvailabilityScene /><ScheduleScene /><SwapScene /><MoreScene /><ConnectedScene /><OutroScene />
      </div>
    </div></div>
    {!capture && <>
      {reduced && <div className={styles.summary} role="status">Podgląd bez ruchu · {current.headline}</div>}
      <div className={styles.toolbar} data-promo-controls aria-label="Sterowanie prezentacją">
        <button type="button" onClick={toggle} disabled={!ready} aria-pressed={playing}>{reduced ? "Następna scena" : playing ? "Pauza" : time >= PROMO_DURATION_SECONDS ? "Odtwórz ponownie" : "Odtwórz"}</button>
        <label className={styles.srOnly} htmlFor="promo-progress">Pozycja prezentacji</label>
        <input id="promo-progress" type="range" min="0" max={PROMO_DURATION_SECONDS} step="0.1" value={time} disabled={!ready} onChange={event => seek(Number(event.target.value))} aria-valuetext={`${time.toFixed(1)} z 75 sekund`} />
        <output htmlFor="promo-progress" aria-live="off">{time.toFixed(1)} / 75 s</output>
      </div>
      <p className={styles.srOnly}>Dziewięć scen: {SCENES.map(scene => scene.headline).join(" ")} Zobacz, jak działa.</p>
    </>}
  </main>;
}
