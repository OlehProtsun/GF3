import { useEffect, type RefObject } from "react";

export function useSwapModalEnvironment(dialogRef: RefObject<HTMLDialogElement | null>, returnFocusRef?: RefObject<HTMLElement | null>, active = true): void {
  useEffect(() => {
    if (!active) return;
    const dialog = dialogRef.current;
    const previousFocus = returnFocusRef?.current ?? document.activeElement as HTMLElement | null;
    const pagePosition = { left: window.scrollX, top: window.scrollY };
    const restoreStyles: (() => void)[] = [];
    const setStyles = (element: HTMLElement, values: Record<string, string>) => {
      const previous = Object.keys(values).map(name => ({ name, value: element.style.getPropertyValue(name), priority: element.style.getPropertyPriority(name) }));
      for (const [name, value] of Object.entries(values)) element.style.setProperty(name, value);
      restoreStyles.push(() => { for (const { name, value, priority } of previous) {
        if (value) element.style.setProperty(name, value, priority);
        else element.style.removeProperty(name);
      } });
    };
    const root = document.getElementById("root");
    const backgroundScrollers = root ? [root, ...root.querySelectorAll<HTMLElement>("*")]
      .filter(element => /auto|scroll/.test(`${getComputedStyle(element).overflowX} ${getComputedStyle(element).overflowY}`))
      .map(element => ({ element, left: element.scrollLeft, top: element.scrollTop })) : [];
    for (const { element } of backgroundScrollers) setStyles(element, { "overflow-x": "hidden", "overflow-y": "hidden", "overscroll-behavior": "none", "scrollbar-gutter": "stable" });
    if (root) setStyles(root, { "touch-action": "none" });
    const scrollbarGap = window.innerWidth - document.documentElement.clientWidth;
    const bodyPadding = getComputedStyle(document.body).paddingRight;
    setStyles(document.documentElement, { "overflow-x": "hidden", "overflow-y": "hidden", "overscroll-behavior": "none" });
    // Fix the viewport on iOS as well as locking the app's independent scrollers.
    setStyles(document.body, { "overflow-x": "hidden", "overflow-y": "hidden", position: "fixed", top: `${-pagePosition.top}px`, left: `${-pagePosition.left}px`,
      width: "100%", "box-sizing": "border-box", "padding-right": `calc(${bodyPadding} + ${scrollbarGap}px)` });
    const viewport = window.visualViewport;
    const updateViewport = () => {
      const height = viewport?.height ?? window.innerHeight;
      dialog?.style.setProperty("--swap-visual-height", `${height}px`);
      dialog?.style.setProperty("--swap-visual-top", `${viewport?.offsetTop ?? 0}px`);
      if (dialog) dialog.dataset.compactViewport = String(height < 600);
    };
    updateViewport();
    viewport?.addEventListener("resize", updateViewport);
    viewport?.addEventListener("scroll", updateViewport);
    window.addEventListener("resize", updateViewport);
    dialog?.showModal();
    return () => {
      viewport?.removeEventListener("resize", updateViewport);
      viewport?.removeEventListener("scroll", updateViewport);
      window.removeEventListener("resize", updateViewport);
      dialog?.close();
      for (const restore of restoreStyles.reverse()) restore();
      for (const { element, left, top } of backgroundScrollers) { element.scrollLeft = left; element.scrollTop = top; }
      window.scrollTo({ ...pagePosition, behavior: "instant" });
      if (previousFocus?.isConnected) previousFocus.focus({ preventScroll: true });
    };
  }, [dialogRef, returnFocusRef, active]);
}
