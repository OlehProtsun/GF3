import { useLayoutEffect, useState, type CSSProperties, type RefObject } from "react";

export function useDropdownPosition(
  open: boolean,
  triggerRef: RefObject<HTMLElement | null>,
  dropdownRef: RefObject<HTMLElement | null>,
  placement: "up" | "down" = "down",
  preferredWidth?: number,
) {
  const [style, setStyle] = useState<CSSProperties>({});

  useLayoutEffect(() => {
    if (!open) return;
    const viewport = window.visualViewport;
    const update = () => {
      const trigger = triggerRef.current?.getBoundingClientRect();
      const dropdown = dropdownRef.current;
      if (!trigger || !dropdown) return;

      const padding = 12;
      const gap = 10;
      const leftEdge = (viewport?.offsetLeft ?? 0) + padding;
      const topEdge = (viewport?.offsetTop ?? 0) + padding;
      const rightEdge = leftEdge + (viewport?.width ?? window.innerWidth) - padding * 2;
      const bottomEdge = topEdge + (viewport?.height ?? window.innerHeight) - padding * 2;
      const width = Math.min(preferredWidth ?? trigger.width, rightEdge - leftEdge);
      const below = Math.max(0, bottomEdge - trigger.bottom - gap);
      const above = Math.max(0, trigger.top - topEdge - gap);
      const desiredHeight = dropdown.firstElementChild?.scrollHeight ?? dropdown.scrollHeight;
      const up = placement === "up"
        ? above >= desiredHeight || above >= below
        : below < desiredHeight && above > below;
      const maxHeight = Math.min(up ? above : below, bottomEdge - topEdge);
      const height = Math.min(dropdown.getBoundingClientRect().height, maxHeight);
      setStyle({
        width,
        maxHeight,
        left: Math.max(leftEdge, Math.min(trigger.left, rightEdge - width)),
        top: Math.max(topEdge, Math.min(up ? trigger.top - gap - height : trigger.bottom + gap, bottomEdge - height)),
      });
    };

    update();
    const observer = new ResizeObserver(update);
    if (triggerRef.current) observer.observe(triggerRef.current);
    if (dropdownRef.current) observer.observe(dropdownRef.current);
    window.addEventListener("resize", update);
    window.addEventListener("scroll", update, true);
    viewport?.addEventListener("resize", update);
    viewport?.addEventListener("scroll", update);
    return () => {
      observer.disconnect();
      window.removeEventListener("resize", update);
      window.removeEventListener("scroll", update, true);
      viewport?.removeEventListener("resize", update);
      viewport?.removeEventListener("scroll", update);
    };
  }, [open, triggerRef, dropdownRef, placement, preferredWidth]);

  return style;
}
