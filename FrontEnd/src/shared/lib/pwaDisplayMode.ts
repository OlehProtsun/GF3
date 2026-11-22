export type PwaLaunchMode = "browser" | "pwa";

const standaloneQuery = "(display-mode: standalone)";

export function getPwaLaunchMode(): PwaLaunchMode {
  if (typeof window === "undefined") return "browser";
  const navigator = window.navigator as (Navigator & { standalone?: boolean }) | undefined;
  return window.matchMedia?.(standaloneQuery)?.matches || navigator?.standalone === true ? "pwa" : "browser";
}

export function subscribePwaDisplayMode(callback: () => void): () => void {
  const media = typeof window === "undefined" ? undefined : window.matchMedia?.(standaloneQuery);
  media?.addEventListener?.("change", callback);
  return () => media?.removeEventListener?.("change", callback);
}
