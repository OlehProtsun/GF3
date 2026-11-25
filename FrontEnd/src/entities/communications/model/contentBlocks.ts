import type { CommunicationContentBlock, CommunicationMessageDto } from "../api/dto";

export function getCommunicationBlocks(message: CommunicationMessageDto): CommunicationContentBlock[] {
  if (message.blocks) return message.blocks;
  const blocks: CommunicationContentBlock[] = [{ id: "legacy-text", type: "text", text: message.body }];
  if (message.photo) blocks.push({ id: "legacy-photo", type: "photo", media: message.photo });
  if (message.gif) blocks.push({ id: "legacy-gif", type: "gif", media: message.gif });
  return blocks;
}
