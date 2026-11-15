import { useEffect, type RefObject } from "react";

const editableInputTypes = ["text", "search", "time", "number", "email", "tel", "url", "password"];

function revealFocusedField(dialog: HTMLDialogElement) {
  const field = document.activeElement;
  if (!(field instanceof HTMLTextAreaElement ||
    (field instanceof HTMLInputElement && editableInputTypes.includes(field.type))) ||
    field.disabled || field.readOnly || !dialog.contains(field)) return;

  for (let scroller = field.parentElement; scroller && scroller !== dialog; scroller = scroller.parentElement) {
    if (scroller.scrollHeight <= scroller.clientHeight) continue;
    const style = getComputedStyle(scroller);
    if (style.overflowY !== "auto" && style.overflowY !== "scroll") continue;

    const rect = field.getBoundingClientRect();
    const top = scroller.getBoundingClientRect().top + scroller.clientTop;
    const bottom = top + scroller.clientHeight;
    // Reduce the inset if the field nearly fills the available space.
    const maxPadding = Math.max(0, (scroller.clientHeight - rect.height) / 2);
    const visibleTop = top + Math.min(maxPadding, parseFloat(style.scrollPaddingTop) || 0);
    const visibleBottom = bottom - Math.min(maxPadding, parseFloat(style.scrollPaddingBottom) || 0);
    const delta = rect.top < visibleTop ? rect.top - visibleTop : Math.max(0, rect.bottom - visibleBottom);
    if (Math.abs(delta) > 1) scroller.scrollBy({ top: delta, behavior: "instant" });
    return;
  }
}

export function useSwapModalEnvironment(dialogRef: RefObject<HTMLDialogElement | null>, returnFocusRef?: RefObject<HTMLElement | null>, active = true): void {
  useEffect(() => {
    if (!active) return;
    const dialog = dialogRef.current;
    if (!dialog) return;
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
    let height: number | undefined;
    let top: number | undefined;
    let viewportFrame = 0;
    let focusFrame = 0;
    const queueFocusReveal = () => {
      if (focusFrame) return;
      focusFrame = requestAnimationFrame(() => {
        focusFrame = 0;
        // Leave panning to the user while pinch-zoomed.
        if (Math.abs((viewport?.scale ?? 1) - 1) < 0.01) revealFocusedField(dialog);
      });
    };
    const updateViewport = () => {
      const nextHeight = viewport?.height ?? window.innerHeight;
      const nextTop = viewport?.offsetTop ?? 0;
      if (nextHeight !== height) {
        height = nextHeight;
        dialog.style.setProperty("--swap-visual-height", `${height}px`);
        const compact = String(height < 600);
        if (dialog.dataset.compactViewport !== compact) dialog.dataset.compactViewport = compact;
      }
      if (nextTop !== top) {
        top = nextTop;
        dialog.style.setProperty("--swap-visual-top", `${top}px`);
      }
    };
    const queueViewportUpdate = () => {
      if (viewportFrame) return;
      viewportFrame = requestAnimationFrame(() => {
        viewportFrame = 0;
        updateViewport();
        queueFocusReveal();
      });
    };
    updateViewport();
    viewport?.addEventListener("resize", queueViewportUpdate);
    viewport?.addEventListener("scroll", queueViewportUpdate);
    window.addEventListener("resize", queueViewportUpdate);
    dialog.addEventListener("focusin", queueFocusReveal);
    dialog.addEventListener("input", queueFocusReveal);
    dialog.showModal();
    return () => {
      viewport?.removeEventListener("resize", queueViewportUpdate);
      viewport?.removeEventListener("scroll", queueViewportUpdate);
      window.removeEventListener("resize", queueViewportUpdate);
      dialog.removeEventListener("focusin", queueFocusReveal);
      dialog.removeEventListener("input", queueFocusReveal);
      cancelAnimationFrame(viewportFrame);
      cancelAnimationFrame(focusFrame);
      dialog.close();
      for (const restore of restoreStyles.reverse()) restore();
      for (const { element, left, top } of backgroundScrollers) { element.scrollLeft = left; element.scrollTop = top; }
      window.scrollTo({ ...pagePosition, behavior: "instant" });
      if (previousFocus?.isConnected) previousFocus.focus({ preventScroll: true });
    };
  }, [dialogRef, returnFocusRef, active]);
}
