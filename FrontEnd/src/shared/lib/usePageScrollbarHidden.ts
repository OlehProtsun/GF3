import { useEffect } from "react";

const HIDDEN_SCROLLBAR_CLASS = "page-scrollbar-hidden";

export function usePageScrollbarHidden(active = true) {
  useEffect(() => {
    if (!active || typeof document === "undefined") {
      return;
    }

    document.documentElement.classList.add(HIDDEN_SCROLLBAR_CLASS);
    document.body.classList.add(HIDDEN_SCROLLBAR_CLASS);

    return () => {
      document.documentElement.classList.remove(HIDDEN_SCROLLBAR_CLASS);
      document.body.classList.remove(HIDDEN_SCROLLBAR_CLASS);
    };
  }, [active]);
}
