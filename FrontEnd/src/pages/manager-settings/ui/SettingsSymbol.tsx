export function SettingsSymbol({ kind, size = 26 }: { kind: "calendar" | "globe"; size?: number }) {
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">
    {kind === "calendar" ? <><rect x="3" y="5" width="18" height="16" rx="3" /><path d="M7 3v5M17 3v5M3 10h18" /></>
      : <><circle cx="12" cy="12" r="9" /><ellipse cx="12" cy="12" rx="4" ry="9" /><path d="M3 12h18M5 6.5a18 18 0 0 0 14 0M5 17.5a18 18 0 0 1 14 0" /></>}
  </svg>;
}
