import { useState } from "react";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import type { CommunicationDraftBlock } from "@entities/communications";
import { setLanguage } from "@shared/i18n";
import { CommunicationBlockEditor } from "./CommunicationBlockEditor";

vi.mock("@entities/communications", async original => ({ ...await original<typeof import("@entities/communications")>(),
  CommunicationGif: () => null, CommunicationPhoto: () => null,
}));
beforeEach(() => setLanguage("en"));

function Editor({ initial, onError }: { initial: CommunicationDraftBlock[]; onError: (message: string) => void }) {
  const [blocks, setBlocks] = useState(initial);
  return <CommunicationBlockEditor blocks={blocks} onChange={setBlocks} onError={onError} disabled={false} />;
}

const rows = () => within(screen.getByRole("list", { name: "Content order" })).getAllByRole("listitem");
const file = (name: string, size = 16, type = "image/png") => new File([new Uint8Array(size)], name, { type });

test("adds, replaces, reorders and removes content while retaining text", async () => {
  const user = userEvent.setup();
  render(<Editor initial={[]} onError={vi.fn()} />);
  await user.click(screen.getByRole("button", { name: "Add text" }));
  await user.type(screen.getByRole("textbox", { name: "Text" }), "Draft");
  await user.upload(screen.getByLabelText("Add photo"), file("first.png"));
  await user.upload(screen.getByLabelText("Add GIF"), file("demo.gif", 16, "image/gif"));
  await user.click(within(rows()[2]).getByRole("button", { name: "Move up" }));
  expect(rows()[1]).toHaveTextContent("demo.gif");
  await user.click(within(rows()[1]).getByRole("button", { name: "Move down" }));
  expect(rows()[2]).toHaveTextContent("demo.gif");
  await user.upload(within(rows()[1]).getByLabelText("Replace"), file("replacement.png"));
  expect(rows()[1]).toHaveTextContent("replacement.png");
  expect(rows()[1]).not.toHaveTextContent("first.png");
  expect(within(rows()[1]).getByRole("button", { name: "Reorder block 2" })).toHaveAttribute("aria-describedby");
  await user.click(within(rows()[2]).getByRole("button", { name: "Remove" }));
  expect(rows()).toHaveLength(2);
  expect(screen.getByRole("textbox", { name: "Text" })).toHaveValue("Draft");
});

test("rejects oversized and unsupported files and caps the block count", () => {
  const onError = vi.fn();
  const initial: CommunicationDraftBlock[] = [
    { id: "one", type: "photo", file: file("one.png", 3 * 1024 * 1024) },
    { id: "two", type: "photo", file: file("two.png", 3 * 1024 * 1024) },
  ];
  render(<Editor initial={initial} onError={onError} />);
  const picker = screen.getByLabelText("Add photo");
  fireEvent.change(picker, { target: { files: [file("large.png", 3 * 1024 * 1024 + 1)] } });
  fireEvent.change(picker, { target: { files: [file("third.png", 3 * 1024 * 1024)] } });
  fireEvent.change(picker, { target: { files: [file("invalid.txt", 10, "text/plain")] } });
  expect(onError.mock.calls.map(([message]) => message)).toEqual([
    "File too large", "New attachments cannot exceed 8 MB per save.", "Unsupported file format",
  ]);
  expect(rows()).toHaveLength(2);
  const add = screen.getByRole("button", { name: "Add text" });
  for (let i = 0; i < 18; i++) fireEvent.click(add);
  expect(rows()).toHaveLength(20);
  expect(add).toBeDisabled();
  expect(picker).toBeDisabled();
});
