import { useRef, type PropsWithChildren } from "react";
import { useGSAP } from "@gsap/react";
import { gsap } from "gsap";

gsap.registerPlugin(useGSAP);

export function PageTransition({ children, pathname }: PropsWithChildren<{ pathname: string }>) {
  const scope = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    // Opacity preserves the viewport containing block of fixed page controls.
    gsap.fromTo(Array.from(scope.current?.children ?? []),
      { opacity: 0.55 },
      { opacity: 1, duration: 0.22, ease: "power2.out", clearProps: "opacity" },
    );
  }, { scope, dependencies: [pathname], revertOnUpdate: true });

  return <div ref={scope} style={{ display: "contents" }}>{children}</div>;
}
