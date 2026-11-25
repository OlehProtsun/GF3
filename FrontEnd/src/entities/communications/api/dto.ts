export type CommunicationContentBlock = {
  id: string;
  type: "text" | "photo" | "gif";
  text?: string | null;
  media?: CommunicationGifAttachment | null;
};

export type CommunicationDraftBlock = CommunicationContentBlock & { file?: File };
export const maxCommunicationBlocks = 20;
export const maxCommunicationUploadBytes = 8 * 1024 * 1024;

export type CommunicationGifAttachment = { key: string; fileName: string; size: number };

export type CommunicationMessageDto = {
  id: number;
  title: string;
  body: string;
  visibleFromUtc: string;
  deadlineAtUtc: string;
  createdAtUtc: string;
  createdByManagerId?: number | null;
  createdByManagerName: string;
  isActive: boolean;
  blocks?: CommunicationContentBlock[] | null;
  photo?: CommunicationGifAttachment | null;
  gif?: CommunicationGifAttachment | null;
};

export type CreateCommunicationMessageDto = {
  title: string;
  body: string;
  visibleFromUtc: string;
  deadlineAtUtc: string;
  blocks?: CommunicationDraftBlock[];
  photoFile?: File | null;
  gifFile?: File | null;
};

export type UpdateCommunicationMessageDto = CreateCommunicationMessageDto & {
  id: number;
  removePhoto?: boolean;
  removeGif?: boolean;
};
