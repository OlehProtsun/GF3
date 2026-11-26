import { createIcon } from "@shared/ui/icons/createIcon";

const icons = {
  note: createIcon("M12 21H6a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v2M8 7h7M8 11h3m2 9 1-4 5.5-5.5a1.4 1.4 0 0 1 2 2L16 18l-3 2Z"),
  pin: createIcon("m16 3 5 5-3 1-3 4v3l-1 1-7-7 1-1h3l4-3 1-3ZM10.5 13.5 4 20"),
  download: createIcon("M12 3v12m-4-4 4 4 4-4M5 17v3h14v-3"),
  back: createIcon("M20 12H4m7-7-7 7 7 7"),
  more: createIcon("M5 12h.01m6.99 0h.01m6.99 0h.01"),
  close: createIcon("m6 6 12 12M18 6 6 18"),
  lock: createIcon("M7 11V7a5 5 0 0 1 10 0v4M6 11h12a2 2 0 0 1 2 2v6a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-6a2 2 0 0 1 2-2Zm6 4v2"),
  users: createIcon("M15 20v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2m19 0v-2a4 4 0 0 0-3-3.9M16 4.2a4 4 0 0 1 0 7.6M13 8a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"),
  search: createIcon("m21 21-4.4-4.4M19 11a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z"),
  filter: createIcon("M4 6h16M7 12h10m-7 6h4"),
  layers: createIcon("m12 3 9 5-9 5-9-5 9-5Zm-9 9 9 5 9-5M3 16l9 5 9-5"),
  calendar: createIcon("M8 3v4m8-4v4M4 10h16M6 5h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2Z"),
  clock: createIcon("M12 7v5l3 2M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"),
  textSize: createIcon("M3 19 9 5l6 14M5 15h8m3 4 3-8 3 8m-5-2h4"),
  palette: createIcon("M12 3a9 9 0 1 0 0 18h1a2 2 0 0 0 2-2c0-1-.7-1.5-.7-2.3s.7-1.7 1.7-1.7h2a3 3 0 0 0 3-3 9 9 0 0 0-9-9ZM7 11h.01M10 7h.01M15 7h.01M18 11h.01"),
  clearFormat: createIcon("m4 19 5-14 5 14m-8-5h6m4 1 5 5m0-5-5 5"),
  lineSpacing: createIcon("M6 4v16M3 7l3-3 3 3m-6 10 3 3 3-3m3-12h9m-9 7h9m-9 7h9"),
  sort: createIcon("M8 4v16m-4-4 4 4 4-4M16 20V4m-4 4 4-4 4 4"),
  info: createIcon("M12 11v5m0-8h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"),
  alert: createIcon("M12 7v6m0 4h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"),
  undo: createIcon("M8 6 4 10l4 4m-4-4h10a5 5 0 0 1 0 10h-3"),
  chevronUp: createIcon("m6 15 6-6 6 6"),
  chevronRight: createIcon("m9 6 6 6-6 6"),
  chevron: createIcon("m6 9 6 6 6-6"),
  plus: createIcon("M12 5v14M5 12h14"),
};

export function NotepadIcon({ name, size = 16 }: { name: keyof typeof icons; size?: number }) {
  const Icon = icons[name];
  return <Icon size={size} fill="none" stroke="currentColor" strokeWidth={1.9} strokeLinecap="round" strokeLinejoin="round" />;
}
