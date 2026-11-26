export type ManagerNoteColor = "yellow" | "blue" | "green" | "rose" | "slate";

export type ManagerNoteVisibility = "private" | "public" | "custom";

export type ManagerNote = {
  id: number;
  title: string;
  content: string;
  color: ManagerNoteColor;
  createdAtUtc: string;
  updatedAtUtc: string;
  ownerManagerId: number;
  visibility: ManagerNoteVisibility;
  isPinned: boolean;
  isLocked: boolean;
  folderId: number | null;
  tagIds: number[];
  sharedWithManagerIds: number[];
  revision: number;
  canEdit: boolean;
  canManage: boolean;
  activeEditor?: { displayName: string; expiresAtMs: number } | null;
};

export type ManagerNotepadState = {
  isExpanded: boolean;
  isPinned: boolean;
  height: number;
};

export type ManagerNotepad = {
  notes: ManagerNote[];
  state: ManagerNotepadState;
  folders: ManagerNoteFolder[];
  tags: ManagerNoteTag[];
};

type ManagerNoteFolder = { id: number; name: string; noteCount: number };
type ManagerNoteTag = { id: number; name: string; noteCount: number };
export type ManagerNoteShareTarget = { id: number; displayName: string };
export type ManagerNoteVersion = Pick<ManagerNote, "id" | "revision" | "title" | "content" | "color"> & {
  savedAtUtc: string;
  savedByManagerId: number;
};

type ManagerNoteMetadataInput = Partial<Pick<ManagerNote, "visibility" | "folderId" | "tagIds" | "sharedWithManagerIds">> & {
  clearFolder?: boolean;
};

export type SaveManagerNoteInput = Pick<ManagerNote, "title" | "content" | "color"> &
  ManagerNoteMetadataInput & { expectedRevision?: number };
export type UpdateManagerNoteMetadataInput = ManagerNoteMetadataInput &
  Partial<Pick<ManagerNote, "isPinned" | "isLocked">> & { expectedRevision: number };
