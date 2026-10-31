import { gsap } from "gsap";
import { PROMO_DURATION_SECONDS, SCENES, FEATURE_CUES } from "./storyboard";

export type PromoTimelineController = {
  duration: () => number;
  play: () => void;
  pause: () => void;
  seek: (seconds: number) => void;
  getTime: () => number;
  onUpdate: (listener: (seconds: number) => void) => void;
  destroy: () => void;
};

export function createPromoTimeline(root: HTMLElement): PromoTimelineController {
  let timeline!: gsap.core.Timeline;
  const context = gsap.context(() => {
    const select = (selector: string) => Array.from(root.querySelectorAll<HTMLElement | SVGElement>(selector));
    const sceneTarget = (id: string, target = "") => select(`[data-promo-scene="${id}"]${target ? ` ${target}` : ""}`);
    timeline = gsap.timeline({ paused: true });
    gsap.set(select("[data-promo-scene]"), { autoAlpha: 0, x: 0, y: 0, scale: 1 });
    gsap.set(sceneTarget("pain"), { autoAlpha: 1 });

    const enter = (id: string, target: string, at: number, duration: number, from: gsap.TweenVars = {}) => {
      gsap.set(sceneTarget(id, target), { autoAlpha: 0, y: 24, ...from });
      timeline.fromTo(sceneTarget(id, target), { autoAlpha: 0, y: 24, ...from }, {
        autoAlpha: 1, x: 0, y: 0, scale: 1, rotation: 0, duration, ease: "power3.out", immediateRender: false,
      }, at);
    };
    const showScene = (id: string, previous: string, at: number) => {
      timeline.fromTo(sceneTarget(id), { autoAlpha: 0, y: 22 }, {
        autoAlpha: 1, y: 0, duration: .5, ease: "power2.inOut", immediateRender: false,
      }, at);
      timeline.set(sceneTarget(previous), { autoAlpha: 0 }, at + .5);
    };

    for (const [index, scene] of SCENES.entries()) {
      timeline.addLabel(scene.id, scene.start);
      if (index > 0) showScene(scene.id, SCENES[index - 1].id, scene.start);
      // An already legible first frame also makes seeking to exactly zero useful.
      gsap.set(sceneTarget(scene.id, "[data-promo-headline]"), { autoAlpha: .2, y: 20 });
      timeline.fromTo(sceneTarget(scene.id, "[data-promo-headline]"), { autoAlpha: .2, y: 20 }, {
        autoAlpha: 1, y: 0, duration: [1.2, 1.5, 1.6, 1.6, 1.6, 1.6, 1.4, 1.3, 2][index],
        ease: "power3.out", immediateRender: false,
      }, scene.start);
      timeline.fromTo(sceneTarget(scene.id, "[data-promo-headline]"), { clipPath: "inset(0 0 80% 0)" }, {
        clipPath: "inset(-8px)", duration: 1.1, ease: "power4.out", immediateRender: false,
      }, scene.start + .05);
      if (!["reveal", "outro"].includes(scene.id)) enter(scene.id, "[data-promo-supporting]", scene.start + .3, .9);
    }

    gsap.set(sceneTarget("pain", "[data-promo-chaos]"), { autoAlpha: 0 });
    enter("pain", '[data-promo-chaos="grid"]', 1.1, .9, { x: -120, rotation: -6 });
    enter("pain", '[data-promo-chaos="messages"]', 1.8, 1, { x: 100, rotation: 5 });
    enter("pain", '[data-promo-chaos="availability"]', 2.7, 1, { x: 90, rotation: -4 });
    timeline.to(sceneTarget("pain", '[data-promo-chaos="grid"]'), { x: 220, scale: .9, duration: .7, ease: "power2.inOut" }, 5.3);
    timeline.to(sceneTarget("pain", '[data-promo-chaos="availability"]'), { x: -200, scale: .9, duration: .7, ease: "power2.inOut" }, 5.3);
    enter("reveal", "[data-promo-workspace]", 7.6, 1.1, { scale: .94 });
    enter("reveal", "[data-promo-reveal-brand]", 8.2, .8);
    enter("reveal", "[data-promo-supporting]", 8.6, .8);

    // All photographs are pre-rendered, decoded production pixels, with fixed state layers.
    gsap.set(select('[data-promo-shot-state="beginning"]'), { autoAlpha: 1 });
    gsap.set(select('[data-promo-shot-state="interaction"], [data-promo-shot-state="outcome"]'), { autoAlpha: 0 });
    gsap.set(select("[data-promo-fragment], [data-promo-cursor], [data-promo-touch]"), { autoAlpha: 0 });
    for (const feature of FEATURE_CUES) {
      const shot = sceneTarget(feature.id, "[data-promo-real-shot]");
      const fragments = sceneTarget(feature.id, "[data-promo-fragment]");
      gsap.set(sceneTarget(feature.id, '[data-promo-shot-state="beginning"]'), { autoAlpha: 0 });
      gsap.set(shot, { borderColor: "transparent", boxShadow: "none", backgroundColor: "transparent" });
      timeline.fromTo(shot, { rotationY: -9, rotationX: 3 }, { rotationY: 0, rotationX: 0, duration: 1.4, ease: "power3.inOut", immediateRender: false }, feature.assemble);
      fragments.forEach((fragment, index) => {
        const x = Number(fragment.dataset.homeX), y = Number(fragment.dataset.homeY);
        const width = Number(fragment.dataset.homeWidth), height = Number(fragment.dataset.homeHeight);
        const scale = Math.min(4, 1000 / width, 430 / height);
        const from = index === 0 ? { x: 760 - x - width / 2, y: 310 - y - height / 2, scale } : { x: index % 2 ? -180 : 180, y: index * 35, scale: .94 };
        timeline.fromTo(fragment, { ...from, autoAlpha: 0 }, { ...from, autoAlpha: 1, duration: .55, ease: "power3.out", immediateRender: false }, feature.start + .5 + index * .55);
        timeline.to(fragment, { x: 0, y: 0, scale: 1, boxShadow: "0 0 0 rgba(15,23,42,0)", duration: 1.2, ease: "power3.inOut" }, feature.assemble + index * .12);
        timeline.to(fragment, { autoAlpha: 0, duration: .15 }, feature.assemble + 1.4 + index * .12);
      });
      timeline.to(sceneTarget(feature.id, '[data-promo-shot-state="beginning"]'), { autoAlpha: 1, duration: .4 }, feature.assemble + .8);
      timeline.to(shot, { borderColor: "#dbe2ee", boxShadow: "0 28px 80px rgba(15,23,42,.09)", duration: .5 }, feature.assemble + .8);
      for (const [state, at] of [["interaction", feature.interaction], ["outcome", feature.outcome]] as const) {
        timeline.to(sceneTarget(feature.id, `[data-promo-shot-state="${state}"]`), { autoAlpha: 1, duration: .45, ease: "power2.inOut" }, at);
      }
      const gesture = feature.outcome - .4;
      if (["manager", "availability", "swap"].includes(feature.id)) {
        timeline.fromTo(sceneTarget(feature.id, "[data-promo-cursor]"), { autoAlpha: 0, x: 80, y: 50 }, { autoAlpha: 1, x: 0, y: 0, duration: .7, ease: "power3.out", immediateRender: false }, gesture - .8);
        timeline.to(sceneTarget(feature.id, "[data-promo-cursor]"), { scale: .88, duration: .12, repeat: 1, yoyo: true }, gesture);
        timeline.fromTo(sceneTarget(feature.id, "[data-promo-touch]"), { autoAlpha: .8, scale: .5 }, { autoAlpha: 0, scale: 2.2, duration: .5, immediateRender: false }, gesture);
        timeline.to(sceneTarget(feature.id, "[data-promo-cursor]"), { autoAlpha: 0, duration: .3 }, feature.outcome + .7);
      }
      // A second independent crop spotlights real summary/date pixels, then returns.
      if (feature.id === "more") {
        const focus = fragments[1];
        timeline.set(focus, { boxShadow: "0 18px 45px rgba(15,23,42,.18)" }, feature.outcome + .5);
        timeline.fromTo(focus, { autoAlpha: 0, scale: 1 }, { autoAlpha: 1, scale: 3.2, x: -130, duration: .65, ease: "power3.inOut", immediateRender: false }, feature.outcome + .5);
        timeline.to(focus, { scale: 1, x: 0, duration: .5, ease: "power3.inOut" }, feature.end - 1.3);
        timeline.to(focus, { autoAlpha: 0, duration: .15 }, feature.end - .8);
      }
      const bridge = fragments.at(-1)!;
      timeline.set(bridge, { autoAlpha: 1, boxShadow: "0 12px 45px rgba(37,99,235,.2)" }, feature.end - .65);
      timeline.to(bridge, { x: 60, y: -40, rotationY: -5, duration: .65, ease: "power3.inOut" }, feature.end - .65);
    }
    // Reveal uses the same native shift rectangle as the following manager chapter.
    const revealPieces = sceneTarget("reveal", "[data-promo-fragment]");
    timeline.fromTo(revealPieces, { autoAlpha: 0, x: -70, y: 35, scale: .7 }, { autoAlpha: 1, x: 0, y: 0, scale: 1, stagger: .18, duration: 1, ease: "power3.out", immediateRender: false }, 6.3);
    gsap.set(sceneTarget("reveal", '[data-promo-shot-state="beginning"]'), { autoAlpha: 0 });
    timeline.to(sceneTarget("reveal", '[data-promo-shot-state="beginning"]'), { autoAlpha: 1, duration: .5 }, 8.3);
    timeline.to(revealPieces, { autoAlpha: 0, duration: .25 }, 8.5);
    timeline.set(sceneTarget("connected", '[data-promo-shot-state="outcome"]'), { autoAlpha: 1 }, 63);
    timeline.set(sceneTarget("connected", '[data-promo-real-shot="swap"] [data-promo-shot-state="outcome"]'), { autoAlpha: 0 }, 63);
    timeline.set(sceneTarget("connected", '[data-promo-real-shot="swap"] [data-promo-shot-state="interaction"]'), { autoAlpha: 1 }, 63);
    enter("connected", "[data-promo-demo]", 64.1, 1, { scale: 1.05 });
    enter("connected", '[data-promo-window="manager"]', 64.1, 1, { x: 55 });
    enter("connected", '[data-promo-window="employee"]', 64.3, 1);
    enter("connected", '[data-promo-window="availability"]', 64.4, 1);
    enter("connected", '[data-promo-window="swap"]', 64.5, 1, { x: -55 });
    for (const [index, piece] of sceneTarget("connected", "[data-promo-fragment]").entries()) {
      timeline.fromTo(piece, { autoAlpha: 0, x: index % 2 ? 40 : -40, y: 30 }, { autoAlpha: 1, x: 0, y: 0, duration: .65, ease: "power3.out", immediateRender: false }, 64 + index * .08);
      timeline.to(piece, { autoAlpha: 0, duration: .15 }, 65.5);
    }
    gsap.set(sceneTarget("connected", "[data-promo-overview]"), { autoAlpha: 0 });
    gsap.set(sceneTarget("connected", "[data-promo-connections]"), { strokeDashoffset: 1000 });
    timeline.fromTo(sceneTarget("connected", "[data-promo-connections]"), { strokeDashoffset: 1000 }, { strokeDashoffset: 0, duration: 1.6, ease: "power2.inOut", immediateRender: false }, 66);
    timeline.to(sceneTarget("connected", "[data-promo-demo]"), { scale: .98, duration: 1, ease: "power2.inOut" }, 68);
    gsap.set(sceneTarget("outro", "[data-promo-motif]"), { strokeDashoffset: 5000 });
    timeline.fromTo(sceneTarget("outro", "[data-promo-motif]"), { strokeDashoffset: 5000 }, { strokeDashoffset: 0, duration: 2, ease: "power2.out", immediateRender: false }, 69);
    enter("outro", "[data-promo-supporting]", 71, .8);
    enter("outro", "[data-promo-cta]", 71.5, .5);
    timeline.to({ hold: 0 }, { hold: 1, duration: 3, ease: "none" }, 72);
    timeline.pause(0);
  }, root);

  return {
    duration: () => PROMO_DURATION_SECONDS,
    play: () => { timeline.play(); },
    pause: () => { timeline.pause(); },
    seek: (seconds: number) => {
      if (!Number.isFinite(seconds)) throw new TypeError("Promo seek requires a finite number of seconds.");
      timeline.pause();
      // Render from the same baseline so late sets/tweens cannot depend on seek history.
      timeline.totalTime(0, true);
      timeline.totalTime(Math.max(0, Math.min(PROMO_DURATION_SECONDS, seconds)), true);
    },
    getTime: () => timeline.time(),
    onUpdate: listener => { timeline.eventCallback("onUpdate", () => listener(timeline.time())); },
    destroy: () => { timeline.eventCallback("onUpdate", null); timeline.kill(); context.revert(); },
  };
}
