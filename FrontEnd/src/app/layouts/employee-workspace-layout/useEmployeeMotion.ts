import { useLayoutEffect, useRef, type RefObject } from "react";
import { useGSAP } from "@gsap/react";
import { gsap } from "gsap";

gsap.registerPlugin(useGSAP);

export function useEmployeeMotion(scope: RefObject<HTMLDivElement | null>, pathname: string) {
  const positionedIndicators = useRef(new WeakSet<HTMLElement>());

  useLayoutEffect(() => {
    const root = scope.current;
    if (!root) return;
    const navs = Array.from(root.querySelectorAll<HTMLElement>("[data-employee-nav]"));
    const positionIndicators = (animate: boolean) => {
      for (const nav of navs) {
        const active = nav.querySelector<HTMLElement>('[aria-current="page"]');
        const indicator = nav.querySelector<HTMLElement>("[data-employee-nav-indicator]");
        if (!active || !indicator || nav.clientWidth === 0) continue;
        const position = {
          x: active.offsetLeft,
          y: active.offsetTop,
          width: active.offsetWidth,
          height: active.offsetHeight,
          visibility: "visible",
        };
        gsap.killTweensOf(indicator);
        if (animate && !window.matchMedia?.("(prefers-reduced-motion: reduce)").matches && positionedIndicators.current.has(indicator)) {
          gsap.to(indicator, { ...position, duration: 0.56, ease: "power2.inOut" });
        } else {
          gsap.set(indicator, position);
        }
        positionedIndicators.current.add(indicator);
      }
    };
    positionIndicators(true);
    // The initial observer delivery must not interrupt the navigation tween.
    let initialResize = true;
    const resizeObserver = new ResizeObserver(() => {
      if (initialResize) { initialResize = false; return; }
      positionIndicators(false);
    });
    for (const nav of navs) {
      resizeObserver.observe(nav);
      nav.querySelectorAll("a").forEach(tab => resizeObserver.observe(tab));
    }
    return () => {
      resizeObserver.disconnect();
      for (const nav of navs) gsap.killTweensOf(nav.querySelector("[data-employee-nav-indicator]"));
    };
  }, [scope, pathname]);

  useGSAP((_context, contextSafe) => {
    const root = scope.current;
    if (!root || !contextSafe || window.matchMedia?.("(prefers-reduced-motion: reduce)").matches) return;
    const seen = new WeakMap<HTMLElement, string | null>();
    const presets: Record<string, { opacity: number; y: number; scale: number; duration: number }> = {
      "schedule-panel": { opacity: 0.35, y: 18, scale: 1, duration: 0.52 },
      "schedule-content": { opacity: 0.45, y: 12, scale: 0.99, duration: 0.42 },
      "schedule-value": { opacity: 0.6, y: 0, scale: 0.96, duration: 0.3 },
    };
    const reveal = contextSafe((elements: HTMLElement[]) => {
      elements.forEach((element, index) => {
        // The hero uses a compositor animation so throttled JS frames cannot step its movement.
        if (element.dataset.employeeMotion === "from-top") return;
        gsap.killTweensOf(element);
        const preset = presets[element.dataset.employeeMotion ?? ""];
        gsap.fromTo(element, {
          opacity: preset?.opacity ?? 0,
          y: preset?.y ?? 26,
          scale: preset?.scale ?? 0.975,
        }, {
          opacity: 1, y: 0, scale: 1, duration: preset?.duration ?? 0.68,
          delay: Math.min(index, 4) * (preset ? 0.045 : 0.06),
          ease: "power2.out", clearProps: "opacity,transform",
        });
      });
    });
    const collect = (node: Element, pending: Set<HTMLElement>) => {
      const elements = node.matches("[data-employee-motion]") ? [node] : [];
      elements.push(...node.querySelectorAll("[data-employee-motion]"));
      for (const element of elements) {
        if (!(element instanceof HTMLElement) || !root.contains(element)) continue;
        const key = element.getAttribute("data-motion-key");
        if (seen.has(element) && seen.get(element) === key) continue;
        seen.set(element, key);
        pending.add(element);
      }
    };
    const initial = new Set<HTMLElement>();
    collect(root, initial);
    reveal([...initial]);
    // Lazy routes and query results arrive after the layout mounts.
    const observer = new MutationObserver(records => {
      const pending = new Set<HTMLElement>();
      for (const record of records) {
        if (record.type === "attributes" && record.target instanceof Element) collect(record.target, pending);
        for (const node of record.addedNodes) {
          if (node instanceof Element) collect(node, pending);
        }
        for (const node of record.removedNodes) {
          if (node instanceof Element) {
            gsap.killTweensOf([node, ...node.querySelectorAll("[data-employee-motion]")]);
          }
        }
      }
      reveal([...pending]);
    });
    observer.observe(root, { childList: true, subtree: true, attributes: true, attributeFilter: ["data-motion-key"] });
    return () => observer.disconnect();
  }, { scope, dependencies: [pathname], revertOnUpdate: true });
}
