export type ManagerNoteColor = "yellow" | "blue" | "green" | "rose" | "slate";

export type ManagerNote = {
  id: number;
  title: string;
  content: string;
  color: ManagerNoteColor;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type ManagerNotepadState = {
  isExpanded: boolean;
  isPinned: boolean;
  height: number;
};

export type ManagerNotepad = {
  notes: ManagerNote[];
  state: ManagerNotepadState;
};

export type SaveManagerNoteInput = Pick<ManagerNote, "title" | "content" | "color">;
