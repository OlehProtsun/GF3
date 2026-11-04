type AccountSettingsIconName = "globe" | "at" | "user" | "team" | "mail" | "phone" | "lock" | "key" | "bell" | "document" | "download" | "edit";

const paths: Record<AccountSettingsIconName, string> = {
  globe: "M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0ZM3 12h18M12 3c-5 5-5 13 0 18 5-5 5-13 0-18Z",
  at: "M16 8v7c0 3 5 1 5-3a9 9 0 1 0-4 7M16 12a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z",
  user: "M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0ZM4 21v-2a8 6 0 0 1 16 0v2Z",
  team: "M15 7a3 3 0 1 1-6 0 3 3 0 0 1 6 0ZM6 20v-2a6 5 0 0 1 12 0v2ZM3 9a3 3 0 0 1 3-3M18 6a3 3 0 0 1 3 3M2 19v-3a4 4 0 0 1 3-4M19 12a4 4 0 0 1 3 4v3",
  mail: "M3 5h18v14H3ZM3 5l9 8 9-8",
  phone: "M7 3 3 5c0 8 8 16 16 16l2-4-5-3-2 2c-3-1-5-3-6-6l2-2Z",
  lock: "M5 10h14v11H5ZM8 10V6a4 4 0 0 1 8 0v4M12 14v3",
  key: "M14 3a7 7 0 0 0-6 10L2 19v3h4v-3h3v-3l2-2a7 7 0 1 0 3-11ZM16 7h.01",
  bell: "M4 18h16l-2-3V9a6 6 0 0 0-12 0v6ZM10 21h4M12 2v1",
  document: "M5 2h10l4 4v16H5ZM14 2v5h5M8 11h8M8 15h8M8 19h4",
  download: "M12 3v12m-5-5 5 5 5-5M4 16v5h16v-5",
  edit: "m4 16 12-12 4 4L8 20l-5 1ZM14 6l4 4",
};

export function AccountSettingsIcon({ name, size = 20, className }: { name: AccountSettingsIconName; size?: number; className?: string }) {
  return <svg width={size} height={size} className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false"><path d={paths[name]} /></svg>;
}
