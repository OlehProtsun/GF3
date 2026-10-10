import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test } from "vitest";
import { useSyncedDraft } from "./useSyncedDraft";
import { usePinnedRecords } from "./records/usePinnedRecords";

type TestRecord = {
  id: number;
  name: string;
};

function SyncedDraftHarness({ sourceKey, sourceValue }: { sourceKey: string; sourceValue: string }) {
  const draft = useSyncedDraft(sourceKey, sourceValue);

  return (
    <section>
      <output aria-label="draft">{draft.value}</output>
      <button type="button" onClick={() => draft.setValue(value => `${value}!`)}>
        append
      </button>
      <button type="button" onClick={() => draft.setValue("fixed")}>
        set fixed
      </button>
      <button type="button" onClick={() => draft.reset()}>
        reset source
      </button>
      <button type="button" onClick={() => draft.reset("custom")}>
        reset custom
      </button>
    </section>
  );
}

function PinnedRecordsHarness({ items }: { items: TestRecord[] }) {
  const { sortedItems, pinnedIdSet, togglePin } = usePinnedRecords("gf3:test:pins", items);

  return (
    <section>
      <ol>
        {sortedItems.map(item => (
          <li key={item.id}>
            {item.name}:{pinnedIdSet.has(String(item.id)) ? "pinned" : "regular"}
          </li>
        ))}
      </ol>
      <button type="button" onClick={() => togglePin(2)}>
        toggle two
      </button>
      <button type="button" onClick={() => togglePin(3)}>
        toggle three
      </button>
    </section>
  );
}

function readListItems() {
  return screen.getAllByRole("listitem").map(item => item.textContent);
}

describe("shared state hooks", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  test("useSyncedDraft keeps local edits for the same source key and switches to new source values", async () => {
    const user = userEvent.setup();
    const { rerender } = render(<SyncedDraftHarness sourceKey="one" sourceValue="server one" />);

    expect(screen.getByLabelText("draft")).toHaveTextContent("server one");

    await user.click(screen.getByRole("button", { name: "append" }));
    expect(screen.getByLabelText("draft")).toHaveTextContent("server one!");

    rerender(<SyncedDraftHarness sourceKey="one" sourceValue="server changed" />);
    expect(screen.getByLabelText("draft")).toHaveTextContent("server one!");

    rerender(<SyncedDraftHarness sourceKey="two" sourceValue="server two" />);
    expect(screen.getByLabelText("draft")).toHaveTextContent("server two");

    await user.click(screen.getByRole("button", { name: "set fixed" }));
    expect(screen.getByLabelText("draft")).toHaveTextContent("fixed");

    await user.click(screen.getByRole("button", { name: "reset source" }));
    expect(screen.getByLabelText("draft")).toHaveTextContent("server two");

    await user.click(screen.getByRole("button", { name: "reset custom" }));
    expect(screen.getByLabelText("draft")).toHaveTextContent("custom");
  });

  test("usePinnedRecords loads string ids, sorts pinned records first, and persists toggles", async () => {
    const user = userEvent.setup();
    const items: TestRecord[] = [
      { id: 1, name: "One" },
      { id: 2, name: "Two" },
      { id: 3, name: "Three" },
    ];
    window.localStorage.setItem("gf3:test:pins", JSON.stringify(["2", 3, "missing", "1"]));

    render(<PinnedRecordsHarness items={items} />);

    expect(readListItems()).toEqual(["One:pinned", "Two:pinned", "Three:regular"]);

    await user.click(screen.getByRole("button", { name: "toggle three" }));
    expect(readListItems()).toEqual(["One:pinned", "Two:pinned", "Three:pinned"]);
    await waitFor(() => {
      expect(JSON.parse(window.localStorage.getItem("gf3:test:pins") ?? "[]")).toEqual([
        "3",
        "2",
        "missing",
        "1",
      ]);
    });

    await user.click(screen.getByRole("button", { name: "toggle two" }));
    expect(readListItems()).toEqual(["One:pinned", "Three:pinned", "Two:regular"]);
    await waitFor(() => {
      expect(JSON.parse(window.localStorage.getItem("gf3:test:pins") ?? "[]")).toEqual([
        "3",
        "missing",
        "1",
      ]);
    });
  });

  test("usePinnedRecords recovers from malformed storage", async () => {
    const items: TestRecord[] = [
      { id: 1, name: "One" },
      { id: 2, name: "Two" },
    ];
    window.localStorage.setItem("gf3:test:pins", "{not-json");

    render(<PinnedRecordsHarness items={items} />);

    expect(readListItems()).toEqual(["One:regular", "Two:regular"]);
    await waitFor(() => {
      expect(window.localStorage.getItem("gf3:test:pins")).toBe("[]");
    });
  });
});
