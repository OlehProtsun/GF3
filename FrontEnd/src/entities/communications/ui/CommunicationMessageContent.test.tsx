import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import { CommunicationMessageContent, CommunicationMessageFooter } from "./CommunicationMessageContent";

vi.mock("./CommunicationGif", () => ({
  CommunicationGif: () => <div>Animated fixture</div>,
  CommunicationPhoto: () => <div>Photo fixture</div>,
}));

test("renders the ordered blocks and forwards the dismissal action", async () => {
  setLanguage("en");
  const close = vi.fn();
  const change = vi.fn();
  render(<section aria-label="Fixture preview"><CommunicationMessageContent onClose={close}
    message={{ title: "Fixture", deadlineAtUtc: "2026-11-23T11:00:00Z", createdByManagerName: "Fixture manager" }}
    blocks={[{ id: "photo", type: "photo" }, { id: "body", type: "text", text: " Fixture text " }, { id: "gif", type: "gif" }]} />
    <CommunicationMessageFooter doNotShowAgain={false} onChange={change} onClose={close} /></section>);
  const preview = within(screen.getByRole("region", { name: "Fixture preview" }));
  expect(preview.getAllByText(/^(Photo fixture|Fixture text|Animated fixture)$/).map(node => node.textContent))
    .toEqual(["Photo fixture", "Fixture text", "Animated fixture"]);
  await userEvent.click(preview.getByRole("checkbox", { name: "Don't show this again" }));
  expect(change).toHaveBeenCalledWith(true);
  await userEvent.click(preview.getByRole("button", { name: "Close", exact: true }));
  expect(close).toHaveBeenCalledOnce();
});
