import type { SVGProps, ReactNode } from "react";

export type IconProps = Omit<SVGProps<SVGSVGElement>, "children"> & {
  size?: number;
  title?: string;
};

export type CreateIconOptions = {
  viewBox?: string;
  fillRule?: "evenodd" | "nonzero";
};

export type IconPathDef = {
  d: string;
  transform?: string;
};

function getPathRules(fillRule?: CreateIconOptions["fillRule"]) {
  return fillRule ? { fillRule, clipRule: fillRule } : null;
}

function SvgWrapper({
  size,
  title,
  viewBox,
  children,
  ...props
}: IconProps & { viewBox: string; children: ReactNode }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox={viewBox}
      xmlns="http://www.w3.org/2000/svg"
      fill="currentColor"
      aria-hidden={title ? undefined : true}
      aria-label={title}
      role={title ? "img" : "presentation"}
      focusable="false"
      {...props}
    >
      {title ? <title>{title}</title> : null}
      {children}
    </svg>
  );
}

export function createIcon(d: string, options: CreateIconOptions = {}) {
  const viewBox = options.viewBox ?? "0 0 24 24";
  const rules = getPathRules(options.fillRule);

  return function Icon({ size = 22, title, ...props }: IconProps) {
    return (
      <SvgWrapper size={size} title={title} viewBox={viewBox} {...props}>
        <path d={d} {...(rules ?? {})} />
      </SvgWrapper>
    );
  };
}

export function createIconPaths(
  paths: IconPathDef[],
  options: CreateIconOptions = {}
) {
  const viewBox = options.viewBox ?? "0 0 24 24";
  const rules = getPathRules(options.fillRule);

  return function Icon({ size = 22, title, ...props }: IconProps) {
    return (
      <SvgWrapper size={size} title={title} viewBox={viewBox} {...props}>
        {paths.map((p, idx) => (
          <path
            key={idx}
            d={p.d}
            transform={p.transform}
            {...(rules ?? {})}
          />
        ))}
      </SvgWrapper>
    );
  };
}
