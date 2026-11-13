import { useRef, type PropsWithChildren } from "react";
import { useGSAP } from "@gsap/react";
import { gsap } from "gsap";

gsap.registerPlugin(useGSAP);

export function PageTransition({ children, pathname, employeeMotion = false }: PropsWithChildren<{ pathname: string; employeeMotion?: boolean }>) {
  const scope = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    // Schedule sections own their entrance; fading their parent compounds it.
    if (employeeMotion && pathname === "/schedule") return;
    // Opacity preserves the viewport containing block of fixed page controls.
    gsap.fromTo(Array.from(scope.current?.children ?? []),
      { opacity: employeeMotion ? 0.15 : 0.55 },
      { opacity: 1, duration: employeeMotion ? 0.62 : 0.22, ease: "power2.out", clearProps: "opacity" },
    );
  }, { scope, dependencies: [pathname, employeeMotion], revertOnUpdate: true });

  return <div ref={scope} style={{ display: "contents" }}>{children}</div>;
}
