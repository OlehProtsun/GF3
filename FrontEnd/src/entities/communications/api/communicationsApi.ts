import { request, requestFile } from "@shared/api/httpClient";
import type {
  CommunicationMessageDto,
  CreateCommunicationMessageDto,
  UpdateCommunicationMessageDto,
} from "./dto";

const endpoint = "communications";
export const maxCommunicationGifBytes = 3 * 1024 * 1024;

function messageText(payload: CreateCommunicationMessageDto) {
  return {
    title: payload.title.trim(),
    body: payload.body.trim(),
    visibleFromUtc: payload.visibleFromUtc,
    deadlineAtUtc: payload.deadlineAtUtc,
  };
}

function attachmentForm(payload: CreateCommunicationMessageDto & Pick<UpdateCommunicationMessageDto, "removePhoto" | "removeGif">) {
  const form = new FormData();
  for (const [key, value] of Object.entries(messageText(payload))) form.append(key, value);
  if (payload.blocks) {
    form.append("Blocks", JSON.stringify(payload.blocks.map(block => ({
      id: block.id,
      type: block.type,
      ...(block.type === "text" ? { text: block.text?.trim() } : { mediaKey: block.file ? null : block.media?.key }),
    }))));
    for (const block of payload.blocks) {
      if (block.file) form.append("Block-" + block.id, block.file);
    }
    return form;
  }
  if (payload.gifFile) form.append("File", payload.gifFile);
  if (payload.photoFile) form.append("Photo", payload.photoFile);
  if (payload.removeGif) form.append("removeGif", "true");
  if (payload.removePhoto) form.append("removePhoto", "true");
  return form;
}

export const communicationsApi = {
  listForManager: (signal?: AbortSignal) =>
    request<CommunicationMessageDto[]>(endpoint, { signal }),
  create: (payload: CreateCommunicationMessageDto) =>
    request<CommunicationMessageDto>((payload.blocks || payload.gifFile || payload.photoFile) ? `${endpoint}/with-gif` : endpoint, {
      method: "POST",
      body: (payload.blocks || payload.gifFile || payload.photoFile) ? attachmentForm(payload) : messageText(payload),
    }),
  update: ({ id, ...payload }: UpdateCommunicationMessageDto) =>
    request<CommunicationMessageDto>(`${endpoint}/${id}${(payload.blocks || payload.gifFile || payload.photoFile) ? "/with-gif" : ""}`, {
      method: "PUT",
      body: (payload.blocks || payload.gifFile || payload.photoFile) ? attachmentForm(payload) : { ...messageText(payload), removeGif: payload.removeGif, removePhoto: payload.removePhoto },
    }),
  gif: (id: number, key: string, signal: AbortSignal, photo = false) =>
    requestFile(`${endpoint}/${id}/${photo ? "photo" : "gif"}/${encodeURIComponent(key)}`, { signal }),
  delete: (communicationId: number) =>
    request<void>(`${endpoint}/${communicationId}`, { method: "DELETE" }),
  pendingForEmployee: (signal?: AbortSignal) =>
    request<CommunicationMessageDto[]>(`${endpoint}/pending`, { signal }),
  dismissForEmployee: (communicationId: number) =>
    request<void>(`${endpoint}/${communicationId}/dismiss`, {
      method: "POST",
    }),
};
