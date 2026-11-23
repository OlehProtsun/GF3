import { expect, test } from "vitest";
import type { CommunicationMessageDto } from "../api/dto";
import { getCommunicationBlocks } from "./contentBlocks";

test("uses legacy text, photo and GIF only when explicit blocks are absent", () => {
  const media = { key: "photo", fileName: "photo.webp", size: 8, contentType: "image/webp" };
  const gif = { ...media, key: "gif", fileName: "motion.gif", contentType: "image/gif" };
  const message = Object.freeze({ body: "Fixture", photo: media, gif }) as CommunicationMessageDto;
  expect(getCommunicationBlocks(message)).toEqual([
    { id: "legacy-text", type: "text", text: "Fixture" },
    { id: "legacy-photo", type: "photo", media },
    { id: "legacy-gif", type: "gif", media: gif },
  ]);
  const blocks = [{ id: "first", type: "gif" as const, media: gif }, { id: "second", type: "text" as const, text: "Ordered" }];
  Object.freeze(blocks);
  expect(getCommunicationBlocks({ ...message, blocks })).toBe(blocks);
  expect(getCommunicationBlocks({ ...message, blocks: [] })).toEqual([]);
});
