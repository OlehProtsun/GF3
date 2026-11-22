import { StrictMode } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, expect, test, vi } from "vitest";
import { EmployeeLaunchReporter } from "./EmployeeLaunchReporter";

const mocks = vi.hoisted(() => ({ auth: vi.fn(), record: vi.fn(), getMode: vi.fn() }));
vi.mock("./AuthProvider", () => ({ useAuth: mocks.auth }));
vi.mock("@entities/employee-launches/api/employeeLaunchesApi", () => ({ employeeLaunchesApi: { record: mocks.record } }));
vi.mock("@shared/lib/pwaDisplayMode", () => ({ getPwaLaunchMode: mocks.getMode }));

beforeEach(() => {
  mocks.record.mockReset().mockResolvedValue(undefined);
  mocks.getMode.mockReturnValue("browser");
});

test("reports after auth only once during StrictMode, route renders and in-document remounts", () => {
  mocks.auth.mockReturnValue({ status: "loading" });
  const view = render(<StrictMode><EmployeeLaunchReporter /></StrictMode>);
  expect(mocks.record).not.toHaveBeenCalled();
  mocks.auth.mockReturnValue({ status: "authenticated", session: { role: "manager", employeeId: 25 } });
  view.rerender(<StrictMode><EmployeeLaunchReporter /></StrictMode>);
  expect(mocks.record).not.toHaveBeenCalled();
  mocks.auth.mockReturnValue({ status: "authenticated", session: { role: "employee", employeeId: 25 } });
  view.rerender(<StrictMode><EmployeeLaunchReporter /><div>Home</div></StrictMode>);
  view.rerender(<StrictMode><EmployeeLaunchReporter /><div>Swap</div></StrictMode>);
  view.unmount();
  render(<StrictMode><EmployeeLaunchReporter /></StrictMode>);
  expect(mocks.record).toHaveBeenCalledExactlyOnceWith("browser");
});

test("a fresh module reports a new document and failures leave the app visible", async () => {
  mocks.auth.mockReturnValue({ status: "authenticated", session: { role: "employee", employeeId: 25 } });
  mocks.getMode.mockReturnValue("pwa");
  mocks.record.mockRejectedValue(new Error("offline"));
  const debug = vi.spyOn(console, "debug").mockImplementation(() => {});
  try {
    vi.resetModules();
    const { EmployeeLaunchReporter: FreshReporter } = await import("./EmployeeLaunchReporter");
    render(<StrictMode><FreshReporter /><p>Employee workspace</p></StrictMode>);
    expect(screen.getByText("Employee workspace")).toBeVisible();
    expect(mocks.record).toHaveBeenCalledExactlyOnceWith("pwa");
    await waitFor(() => expect(debug).toHaveBeenCalledOnce());
  } finally { debug.mockRestore(); }
});
