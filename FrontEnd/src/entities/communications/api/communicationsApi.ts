import { request } from "@shared/api/httpClient";
import type {
  CommunicationMessageDto,
  CreateCommunicationMessageDto,
  UpdateCommunicationMessageDto,
} from "./dto";

const endpoint = "communications";

export const communicationsApi = {
  listForManager: (signal?: AbortSignal) =>
    request<CommunicationMessageDto[]>(endpoint, { signal }),
  create: (payload: CreateCommunicationMessageDto) =>
    request<CommunicationMessageDto>(endpoint, {
      method: "POST",
      body: {
        title: payload.title.trim(),
        body: payload.body.trim(),
        visibleFromUtc: payload.visibleFromUtc,
        deadlineAtUtc: payload.deadlineAtUtc,
      },
    }),
  update: ({ id, ...payload }: UpdateCommunicationMessageDto) =>
    request<CommunicationMessageDto>(`${endpoint}/${id}`, {
      method: "PUT",
      body: {
        title: payload.title.trim(),
        body: payload.body.trim(),
        visibleFromUtc: payload.visibleFromUtc,
        deadlineAtUtc: payload.deadlineAtUtc,
      },
    }),
  delete: (communicationId: number) =>
    request<void>(`${endpoint}/${communicationId}`, { method: "DELETE" }),
  pendingForEmployee: (signal?: AbortSignal) =>
    request<CommunicationMessageDto[]>(`${endpoint}/pending`, { signal }),
  dismissForEmployee: (communicationId: number) =>
    request<void>(`${endpoint}/${communicationId}/dismiss`, {
      method: "POST",
    }),
};
