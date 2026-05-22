import { render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, test, vi } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { EmployeeSchedulePage } from "./EmployeeSchedulePage";

const mocks = vi.hoisted(() => ({
  scheduleQuery: vi.fn(),
  matrix: vi.fn(),
}));

vi.mock("@app/providers/AuthProvider", () => ({
  useAuth: () => ({
    session: {
      employeeId: 12,
      displayName: "Zoe Young",
      userName: "zoe",
    },
  }),
}));

vi.mock("@entities/employee-schedule", () => ({
  useEmployeeScheduleListQuery: () => mocks.scheduleQuery(),
}));

vi.mock("@entities/containers/ui/ContainerGraphMatrix", () => ({
  ContainerGraphMatrix: (props: {
    title: string;
    graph: EmployeeSchedule;
    columns: Array<{ employeeId: number; label: string }>;
    cellMap: Record<string, string>;
  }) => {
    mocks.matrix(props);

    return (
      <section data-testid="schedule-matrix">
        <h2>{props.title}</h2>
        <span>{props.graph.name}</span>
        <span>{props.columns.map(column => column.label).join(", ")}</span>
        <span>{JSON.stringify(props.cellMap)}</span>
      </section>
    );
  },
}));

const schedules: EmployeeSchedule[] = [
  {
    id: 1,
    containerId: 2,
    containerName: "Main Container",
    shopId: 4,
    shopName: "Central Shop",
    name: "May Schedule",
    year: 2026,
    month: 5,
    publicationStatus: "public",
    employees: [
      {
        id: 10,
        employeeId: 12,
        firstName: "Zoe",
        lastName: "Young",
        displayName: "Zoe Young",
        minHoursMonth: 80,
        displayOrder: 2,
      },
      {
        id: 11,
        employeeId: 7,
        firstName: "Adam",
        lastName: "Blue",
        displayName: "Adam Blue",
        minHoursMonth: 60,
        displayOrder: 1,
      },
    ],
    slots: [
      { id: 1, dayOfMonth: 10, slotNo: 1, employeeId: 12, fromTime: "08:00", toTime: "12:00", status: "ASSIGNED" },
      { id: 2, dayOfMonth: 11, slotNo: 1, employeeId: 12, fromTime: "22:00", toTime: "02:00", status: "ASSIGNED" },
      { id: 3, dayOfMonth: 12, slotNo: 1, employeeId: 7, fromTime: "09:00", toTime: "13:00", status: "ASSIGNED" },
    ],
  },
  {
    id: 2,
    containerId: 3,
    containerName: "Second Container",
    shopId: 5,
    shopName: "North Shop",
    name: "Second May Schedule",
    year: 2026,
    month: 5,
    publicationStatus: "public",
    employees: [
      {
        id: 12,
        employeeId: 12,
        firstName: "Zoe",
        lastName: "Young",
        displayName: "Zoe Young",
        minHoursMonth: null,
        displayOrder: 1,
      },
    ],
    slots: [
      { id: 4, dayOfMonth: 10, slotNo: 1, employeeId: 12, fromTime: "13:00", toTime: "17:00", status: "ASSIGNED" },
    ],
  },
];

function renderPage() {
  window.history.replaceState({}, "", "/schedule");

  return render(
    <BrowserRouter>
      <EmployeeSchedulePage />
    </BrowserRouter>,
  );
}

beforeEach(() => {
  mocks.scheduleQuery.mockReset();
  mocks.matrix.mockClear();
});

describe("EmployeeSchedulePage", () => {
  test("summarizes current employee work across public schedules and feeds selected schedule to the matrix", () => {
    mocks.scheduleQuery.mockReturnValue({
      data: schedules,
      isLoading: false,
      error: null,
    });

    renderPage();

    expect(screen.getAllByText("2 schedules")).not.toHaveLength(0);
    expect(screen.getByText("2 work days")).toBeInTheDocument();
    expect(screen.getByText("29 free days")).toBeInTheDocument();
    expect(screen.getByText("12h Total Hours")).toBeInTheDocument();
    expect(screen.getByText("Month: May")).toBeInTheDocument();
    expect(screen.getByText("Year: 2026")).toBeInTheDocument();

    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent("May Schedule");
    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent("Adam Blue, Zoe Young");
    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent('"12:10":"08:00 - 12:00"');
    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent('"12:11":"22:00 - 02:00"');
    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent('"7:12":"09:00 - 13:00"');

    expect(mocks.matrix).toHaveBeenCalledWith(expect.objectContaining({
      readOnly: true,
      showColumnTotals: false,
      graph: expect.objectContaining({ id: 1 }),
    }));
  });

  test("shows a loading and empty state when no public schedule is available", () => {
    mocks.scheduleQuery.mockReturnValue({
      data: [],
      isLoading: true,
      error: null,
    });

    const { rerender } = renderPage();

    expect(screen.getByText("Checking public schedules for your account.")).toBeInTheDocument();

    mocks.scheduleQuery.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    rerender(
      <BrowserRouter>
        <EmployeeSchedulePage />
      </BrowserRouter>,
    );

    expect(screen.getByText("Nothing is public for your account yet.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Go to Availability/i })).toHaveAttribute("href", "/availability");
  });
});
