import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { EmployeeProfilePage } from "./EmployeeProfilePage";

const mocks = vi.hoisted(() => ({ mutate: vi.fn(), navigate: vi.fn(), pending: false }));

vi.mock("react-router-dom", () => ({
  useNavigate: () => mocks.navigate,
  useParams: () => ({ employeeId: "7" }),
}));
vi.mock("@entities/employees/api/queries", () => ({
  useEmployeeByIdQuery: () => ({ data: { id: 7 } }),
  useDeleteEmployeeMutation: () => ({ mutate: mocks.mutate, isPending: mocks.pending }),
  useKickEmployeeMutation: () => ({ mutate: vi.fn(), isPending: false }),
}));
vi.mock("@entities/employees/ui/EmployeeProfileCard", () => ({
  EmployeeProfileCard: ({ onDeleteEmployee }: { onDeleteEmployee: () => void }) => (
    <button onClick={onDeleteEmployee}>Delete employee</button>
  ),
}));
vi.mock("@shared/ui/PageHeader", () => ({ PageHeader: () => null }));
vi.mock("@entities/regulations", () => ({ useEmployeeRegulationHistoryQuery: () => ({ data: [] }) }));
vi.mock("@entities/regulations/ui/RegulationHistoryCard", () => ({ RegulationHistoryCard: () => null }));

beforeEach(() => { vi.clearAllMocks(); mocks.pending = false; });
afterEach(cleanup);

function openDialog() {
  fireEvent.click(screen.getByRole("button", { name: "Delete employee" }));
  return screen.getByRole("dialog", { name: "Usunąć pracownika?" });
}

test("requires explicit confirmation and cancellation sends no delete request", () => {
  render(<EmployeeProfilePage />);
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  const dialog = openDialog();
  expect(dialog).toHaveAccessibleDescription("Czy na pewno chcesz usunąć tego pracownika? Ta operacja jest nieodwracalna.");
  expect(mocks.mutate).not.toHaveBeenCalled();
  fireEvent.click(within(dialog).getByRole("button", { name: "Anuluj" }));
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  expect(mocks.mutate).not.toHaveBeenCalled();
});

test("confirms once, disables pending actions and preserves success navigation", () => {
  const view = render(<EmployeeProfilePage />);
  const dialog = openDialog();
  const confirm = within(dialog).getByRole("button", { name: "Usuń" });
  fireEvent.click(confirm);
  fireEvent.click(confirm);
  expect(mocks.mutate).toHaveBeenCalledExactlyOnceWith(7, expect.any(Object));
  mocks.pending = true;
  view.rerender(<EmployeeProfilePage />);
  expect(confirm).toBeDisabled();
  expect(within(dialog).getByRole("button", { name: "Anuluj" })).toBeDisabled();
  mocks.mutate.mock.calls[0][1].onSuccess();
  expect(mocks.navigate).toHaveBeenCalledWith("/employee");
});

test("allows retry after an error without closing the confirmation", () => {
  render(<EmployeeProfilePage />);
  const dialog = openDialog();
  const confirm = within(dialog).getByRole("button", { name: "Usuń" });
  fireEvent.click(confirm);
  mocks.mutate.mock.calls[0][1].onError(new Error("Delete failed"));
  expect(dialog).toBeInTheDocument();
  expect(mocks.navigate).not.toHaveBeenCalled();
  fireEvent.click(confirm);
  expect(mocks.mutate).toHaveBeenCalledTimes(2);
});
