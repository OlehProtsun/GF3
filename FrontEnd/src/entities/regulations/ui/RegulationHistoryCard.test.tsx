import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { setLanguage } from "@shared/i18n";
import type { RegulationAcceptance } from "@entities/regulations";
import { regulationsApi } from "@entities/regulations";
import { RegulationHistoryCard } from "./RegulationHistoryCard";
import styles from "./RegulationHistoryCard.module.css";

vi.mock("@entities/regulations", () => ({ regulationsApi: { downloadPdf: vi.fn() } }));
beforeEach(() => setLanguage("en"));
afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals(); vi.useRealTimers(); });
const acceptance: RegulationAcceptance = { id: 1, regulationDocumentId: 4, regulationTitle: "Employee rules", regulationVersion: "v2", pdfSha256: "0123456789abcdef", accountRole: "employee", accountId: 1, username: "worker", displayName: "Worker", acceptedAtUtc: "2026-10-01T12:00:00Z" };

test("profile shows zero count, empty and loading states", () => {
  const { rerender } = render(<RegulationHistoryCard appearance="profile" acceptances={[]} isLoading={false} />);
  expect(screen.getByRole("region")).toHaveClass(styles.profile);
  expect(screen.getByText("0")).toBeVisible();
  expect(screen.getByText("No regulation has been accepted yet.")).toBeVisible();
  rerender(<RegulationHistoryCard appearance="profile" acceptances={[]} isLoading />);
  expect(screen.getByText("Loading history...")).toBeVisible();
});

test.each(["default", "profile"] as const)("%s retains history metadata and PDF download lifecycle", async appearance => {
  const blob = new Blob(["pdf"]);
  vi.mocked(regulationsApi.downloadPdf).mockResolvedValue({ blob, fileName: "rules.pdf" });
  const create = vi.fn().mockReturnValue("blob:rules");
  const revoke = vi.fn();
  vi.stubGlobal("URL", { createObjectURL: create, revokeObjectURL: revoke });
  const open = vi.spyOn(window, "open").mockReturnValue(null);
  const timeout = vi.spyOn(window, "setTimeout");
  render(<RegulationHistoryCard appearance={appearance} acceptances={[acceptance]} isLoading={false} />);
  expect(screen.getByRole("region").classList.contains(styles.profile)).toBe(appearance === "profile");
  expect(screen.getByText("1")).toBeVisible();
  const button = screen.getByRole("button", { name: /Employee rules/ });
  expect(button).toHaveTextContent("v2");
  expect(button).toHaveTextContent("0123456789ab");
  expect(button.querySelector("time")).toHaveAttribute("dateTime", acceptance.acceptedAtUtc);
  fireEvent.click(button);
  await waitFor(() => expect(open).toHaveBeenCalledWith("blob:rules", "_blank", "noopener,noreferrer"));
  expect(regulationsApi.downloadPdf).toHaveBeenCalledWith(4);
  expect(create).toHaveBeenCalledWith(blob);
  const release = timeout.mock.calls.find(([, delay]) => delay === 60_000)?.[0];
  expect(release).toBeTypeOf("function");
  (release as () => void)();
  expect(revoke).toHaveBeenCalledWith("blob:rules");
});
