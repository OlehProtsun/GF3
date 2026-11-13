import { useImperativeHandle, useLayoutEffect, useRef, useState, type HTMLAttributes, type Ref } from "react";
import styles from "./SwapScrollRegion.module.css";

type Props = HTMLAttributes<HTMLDivElement> & { scrollRef?: Ref<HTMLDivElement> };

export function SwapScrollRegion({ children, className = "", scrollRef, ...attributes }: Props) {
  const innerRef = useRef<HTMLDivElement | null>(null);
  const [edges, setEdges] = useState({ top: false, bottom: false });
  useImperativeHandle(scrollRef, () => innerRef.current!, []);
  useLayoutEffect(() => {
    const element = innerRef.current;
    if (!element) return;
    const update = () => {
      const top = element.scrollTop > 2;
      const bottom = element.scrollHeight - element.clientHeight - element.scrollTop > 2;
      setEdges(previous => previous.top === top && previous.bottom === bottom ? previous : { top, bottom });
    };
    update();
    element.addEventListener("scroll", update, { passive: true });
    const observer = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(update);
    observer?.observe(element);
    for (const child of element.children) observer?.observe(child);
    return () => { element.removeEventListener("scroll", update); observer?.disconnect(); };
  }, [children]);
  return <div className={styles.frame} data-scroll-top={edges.top} data-scroll-bottom={edges.bottom}>
    <div {...attributes} className={`${styles.scroller} ${className}`} ref={innerRef}>{children}</div>
  </div>;
}
