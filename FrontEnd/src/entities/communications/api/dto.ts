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
};

export type CreateCommunicationMessageDto = {
  title: string;
  body: string;
  visibleFromUtc: string;
  deadlineAtUtc: string;
};

export type UpdateCommunicationMessageDto = CreateCommunicationMessageDto & {
  id: number;
};
