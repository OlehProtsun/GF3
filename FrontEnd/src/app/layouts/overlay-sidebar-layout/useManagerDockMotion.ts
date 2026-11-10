import { useCallback, useLayoutEffect, useRef, type PointerEvent, type RefObject } from "react";
import { gsap } from "gsap";

export function useManagerDockMotion(navRef: RefObject<HTMLElement | null>) {
  const move = useRef<(x: number, y: number) => void>(() => {});
  const resetRef = useRef<() => void>(() => {});
  useLayoutEffect(() => {
    const root = navRef.current;
    if (!root) return;
    const reduced = window.matchMedia?.("(prefers-reduced-motion: reduce)");
    const hover = window.matchMedia?.("(hover: hover) and (pointer: fine)");
    let frame = 0;
    let pointer: { x: number; y: number } | null = null;
    let scaleRange = 0.2;
    let push = 4;
    let faceOffset = 2;
    let nodes: { anchor: HTMLElement; face: HTMLElement; scaleX: ReturnType<typeof gsap.quickTo>; scaleY: ReturnType<typeof gsap.quickTo>; x: ReturnType<typeof gsap.quickTo>; y: ReturnType<typeof gsap.quickTo> }[] = [];
    const reset = () => {
      cancelAnimationFrame(frame); frame = 0;
      pointer = null;
      nodes.forEach(({ face, scaleX, scaleY, x, y }) => {
        scaleX.tween.pause(); scaleY.tween.pause(); x.tween.pause(); y.tween.pause();
        gsap.set(face, { scale: 1, x: 0, y: 0 });
        scaleX.tween.invalidate(); scaleY.tween.invalidate(); x.tween.invalidate(); y.tween.invalidate();
      });
      const active = root.querySelector<HTMLElement>('[aria-current="page"]')?.getBoundingClientRect();
      root.style.setProperty("--dock-glow-y", active ? `${active.top + active.height / 2 - root.getBoundingClientRect().top}px` : "50%");
    };
    const measure = () => {
      const previousPointer = pointer;
      reset();
      nodes.forEach(({ face }) => gsap.killTweensOf(face));
      const style = getComputedStyle(root);
      scaleRange = parseFloat(style.getPropertyValue("--dock-scale-range")) || 0.2;
      push = parseFloat(style.getPropertyValue("--dock-neighbor-push")) || 4;
      faceOffset = parseFloat(style.getPropertyValue("--dock-face-offset")) || 2;
      nodes = Array.from(root.querySelectorAll<HTMLElement>("[data-manager-dock-anchor]")).map(anchor => {
        const face = anchor.querySelector<HTMLElement>("[data-manager-dock-face]")!;
        const options = { duration: 0.32, ease: "power3.out" };
        // quickTo/resetTo requires individual properties; CSSPlugin's scale alias expands into two.
        return { anchor, face, scaleX: gsap.quickTo(face, "scaleX", options), scaleY: gsap.quickTo(face, "scaleY", options), x: gsap.quickTo(face, "x", options), y: gsap.quickTo(face, "y", options) };
      });
      if (previousPointer) move.current(previousPointer.x, previousPointer.y);
    };
    move.current = (x, y) => {
      if (!Number.isFinite(x) || !Number.isFinite(y) || reduced?.matches || !hover?.matches) return;
      const box = root.getBoundingClientRect();
      if (x < box.left || x > box.right || y < box.top || y > box.bottom) return;
      pointer = { x, y };
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(() => {
        frame = 0;
        root.style.setProperty("--dock-glow-y", `${Math.max(0, Math.min(box.height, y - box.top))}px`);
        nodes.forEach(node => {
          const anchorBox = node.anchor.getBoundingClientRect();
          const delta = anchorBox.top + anchorBox.height / 2 - y;
          const d = Math.abs(delta);
          const w = d > 240 ? 0 : Math.exp(-0.5 * (d / 52) ** 2);
          node.scaleX(1 + scaleRange * w);
          node.scaleY(1 + scaleRange * w);
          node.x(faceOffset * w);
          node.y(d > 240 ? 0 : Math.sign(delta) * push * Math.exp(-0.5 * ((d - 86) / 78) ** 2) * Math.min(1, d / 48));
        });
      });
    };
    resetRef.current = reset;
    measure();
    const observer = new MutationObserver(measure);
    observer.observe(root, { childList: true, subtree: true, attributes: true, attributeFilter: ["aria-current", "data-collapsed"] });
    const resizeObserver = new ResizeObserver(measure);
    resizeObserver.observe(root);
    root.querySelectorAll<HTMLElement>("[data-manager-dock-anchor]").forEach(anchor => resizeObserver.observe(anchor));
    const onTransitionEnd = (event: TransitionEvent) => {
      if (event.target === root && event.propertyName === "transform") measure();
    };
    root.addEventListener("transitionend", onTransitionEnd);
    root.addEventListener("scroll", measure, true);
    window.addEventListener("resize", measure);
    window.addEventListener("blur", reset);
    reduced?.addEventListener("change", reset);
    hover?.addEventListener("change", reset);
    return () => {
      reset(); nodes.forEach(({ face }) => gsap.killTweensOf(face));
      observer.disconnect(); resizeObserver.disconnect();
      root.removeEventListener("transitionend", onTransitionEnd);
      root.removeEventListener("scroll", measure, true);
      window.removeEventListener("resize", measure);
      window.removeEventListener("blur", reset);
      reduced?.removeEventListener("change", reset);
      hover?.removeEventListener("change", reset);
      move.current = () => {}; resetRef.current = () => {};
    };
  }, [navRef]);
  const reset = useCallback(() => resetRef.current(), []);
  const onPointerMove = useCallback((event: PointerEvent<HTMLElement>) => {
    if (event.pointerType === "mouse" || event.pointerType === "pen") move.current(event.clientX, event.clientY);
  }, []);
  return { onPointerMove, onPointerLeave: reset, reset };
}
