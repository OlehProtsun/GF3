export function finishPwaSplash(): void {
  if (typeof document === "undefined") return;
  const splash = document.getElementById("gf3-startup-splash");
  if (!splash || splash.classList.contains("gf3-splash-exiting")) return;
  document.getElementById("gf3-startup-fallback")?.remove();

  const style = window.getComputedStyle(splash);
  const canFade = document.documentElement.classList.contains("gf3-pwa-standalone") &&
    style.display !== "none" && style.opacity === "1" &&
    !window.matchMedia?.("(prefers-reduced-motion: reduce)")?.matches &&
    style.transitionProperty.split(",").some(property => ["all", "opacity"].includes(property.trim())) &&
    style.transitionDuration.split(",").some(duration => Number.parseFloat(duration) > 0);
  if (!canFade) {
    splash.remove();
    return;
  }

  const finish = (event: TransitionEvent) => {
    if (event.target === splash && event.propertyName === "opacity") splash.remove();
  };
  splash.addEventListener("transitionend", finish);
  splash.addEventListener("transitioncancel", finish);
  splash.style.pointerEvents = "none";
  splash.classList.add("gf3-splash-exiting");
  if (splash.getAnimations && splash.getAnimations().length === 0) splash.remove();
}
