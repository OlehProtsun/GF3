import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RegulationAcceptanceGate } from "./RegulationAcceptanceGate";

const mocks = vi.hoisted(() => ({
  document: { id: 1, title: "Rules", version: "1.0", message: "Read the PDF", pdfFileName: "rules.pdf" },
  mutate: vi.fn(), download: vi.fn(), pending: false,
}));
vi.mock("@app/providers/AuthProvider", () => ({ useAuth: () => ({ session: { role: "employee", employeeId: 1 } }) }));
vi.mock("@entities/regulations", () => ({
  usePendingRegulationsQuery: () => ({ data: [mocks.document] }),
  useAcceptRegulationMutation: () => ({ mutate: mocks.mutate, isPending: mocks.pending }),
  regulationsApi: { downloadPdf: mocks.download },
}));

describe("RegulationAcceptanceGate", () => {
  beforeEach(() => {
    mocks.pending = false;
    mocks.document.id = 1;
    mocks.mutate.mockReset();
    mocks.download.mockReset();
  });

  it("requires an unchecked personal acknowledgement and exposes public notices", () => {
    render(<RegulationAcceptanceGate />);
    expect(screen.getByRole("checkbox")).not.toBeChecked();
    expect(screen.getByRole("button", { name: "Accept and continue" })).toBeDisabled();
    expect(screen.getByRole("link", { name: "Legal documents" })).toHaveAttribute("href", "/legal/index.html");
    expect(screen.getByText(/not on behalf of your employer/)).toBeVisible();
    expect(screen.getByRole("button", { name: "Download PDF · rules.pdf" })).toBeEnabled();
    fireEvent.click(screen.getByRole("checkbox"));
    fireEvent.click(screen.getByRole("button", { name: "Accept and continue" }));
    expect(mocks.mutate).toHaveBeenCalledWith(1, expect.objectContaining({ onError: expect.any(Function) }));
  });

  it("disables repeat acceptance while the mutation is pending", () => {
    const view = render(<RegulationAcceptanceGate />);
    fireEvent.click(screen.getByRole("checkbox"));
    mocks.pending = true;
    view.rerender(<RegulationAcceptanceGate />);
    const saving = screen.getByRole("button", { name: "Saving acceptance..." });
    expect(saving).toBeDisabled();
    fireEvent.click(saving);
    expect(mocks.mutate).not.toHaveBeenCalled();
  });

  it("resets acknowledgement for a different version", () => {
    const view = render(<RegulationAcceptanceGate />);
    fireEvent.click(screen.getByRole("checkbox"));
    mocks.document.id = 2;
    view.rerender(<RegulationAcceptanceGate />);
    expect(screen.getByRole("checkbox")).not.toBeChecked();
  });

  it("reports a failed download and permits retry", async () => {
    mocks.download.mockRejectedValue(new Error("Download failed"));
    render(<RegulationAcceptanceGate />);
    fireEvent.click(screen.getByRole("button", { name: "Download PDF · rules.pdf" }));
    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Download failed"));
    expect(mocks.download).toHaveBeenCalledWith(1);
    expect(screen.getByRole("button", { name: "Download PDF · rules.pdf" })).toBeEnabled();
  });

  it("disables repeated downloads until the request completes", async () => {
    let reject!: (error: Error) => void;
    mocks.download.mockReturnValue(new Promise((_, rejectPromise) => { reject = rejectPromise; }));
    render(<RegulationAcceptanceGate />);
    fireEvent.click(screen.getByRole("button", { name: "Download PDF · rules.pdf" }));
    const downloading = screen.getByRole("button", { name: "Downloading..." });
    expect(downloading).toBeDisabled();
    fireEvent.click(downloading);
    expect(mocks.download).toHaveBeenCalledTimes(1);
    reject(new Error("Retry"));
    await screen.findByRole("alert");
  });

  it("reports a failed acceptance without accepting automatically", () => {
    mocks.mutate.mockImplementation((_id, options: { onError: (error: Error) => void }) => options.onError(new Error("Save failed")));
    render(<RegulationAcceptanceGate />);
    fireEvent.click(screen.getByRole("checkbox"));
    fireEvent.click(screen.getByRole("button", { name: "Accept and continue" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Save failed");
    expect(screen.getByRole("button", { name: "Accept and continue" })).toBeEnabled();
  });
});
