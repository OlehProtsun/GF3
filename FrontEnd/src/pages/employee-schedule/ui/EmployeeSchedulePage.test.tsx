import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { BrowserRouter } from "react-router-dom";
import { beforeEach, describe, expect, test, vi } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { buildSchedulePdfHtml, EmployeeSchedulePage } from "./EmployeeSchedulePage";
import styles from "./EmployeeSchedulePage.module.css";

const mocks = vi.hoisted(() => ({
  scheduleQuery: vi.fn(),
  matrix: vi.fn(),
  uiStateQuery: {
    data: { scheduleColumnOrders: {}, readNotificationIds: [] },
    refetch: vi.fn(),
  },
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

vi.mock("@entities/employee-ui-state", () => ({
  employeeUiStateApi: {
    saveScheduleColumnOrder: vi.fn().mockResolvedValue(undefined),
  },
  useEmployeeUiStateQuery: () => mocks.uiStateQuery,
}));
vi.mock("@entities/containers/ui/ContainerGraphMatrix", () => ({
  ContainerGraphMatrix: (props: {
    title: string;
    icon?: ReactNode;
    graph: EmployeeSchedule;
    columns: Array<{ employeeId: number; label: string }>;
    cellMap: Record<string, string>;
    onColumnHeaderClick?: (column: { employeeId: number; label: string }) => void;
  }) => {
    mocks.matrix(props);

    return (
      <section data-testid="schedule-matrix">
        {props.icon}
        <h2>{props.title}</h2>
        <span>{props.graph.name}</span>
        <span>{props.columns.map(column => column.label).join(", ")}</span>
        {props.onColumnHeaderClick
          ? props.columns.map(column => (
            <button
              key={column.employeeId}
              type="button"
              aria-label={"Customize " + column.label}
              onClick={() => props.onColumnHeaderClick?.(column)}
            >
              {column.label}
            </button>
          ))
          : null}
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
    lastUpdatedAtUtc: "2026-06-28T12:45:00Z",
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
    relatedScheduleAssignments: [
      { employeeId: 7, dayOfMonth: 13, scheduleId: 2, scheduleName: "Second May Schedule" },
      { employeeId: 12, dayOfMonth: 10, scheduleId: 2, scheduleName: "Second May Schedule" },
      { employeeId: 12, dayOfMonth: 10, scheduleId: 3, scheduleName: "Late May Schedule" },
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
  window.localStorage.clear();
  mocks.scheduleQuery.mockReset();
  mocks.matrix.mockClear();
});

describe("EmployeeSchedulePage", () => {
  test("uses intrinsic PDF column sizing with minimal cell padding", () => {
    const html = buildSchedulePdfHtml(
      schedules[0],
      [
        { employeeId: 12, kind: "employee", manualColumnId: null, graphEmployeeId: 10, label: "Zoe Young", minHoursMonth: 80, totalMinutes: 0, totalText: "" },
        { employeeId: 7, kind: "employee", manualColumnId: null, graphEmployeeId: 11, label: "Adam Blue", minHoursMonth: 60, totalMinutes: 0, totalText: "" },
      ],
      {
        "12:10": "08:00 - 12:00, Late May Schedule, Second May Schedule",
        "7:12": "09:00 - 13:00",
      },
    );
    const document = new DOMParser().parseFromString(html, "text/html");
    const employeeColumns = document.querySelectorAll<HTMLTableColElement>("col.employee-column");
    const styleText = document.querySelector("style")?.textContent ?? "";

    expect(employeeColumns).toHaveLength(25);
    expect([...employeeColumns].every(column => column.style.width === "")).toBe(true);
    expect(styleText).toContain("table-layout: auto");
    expect(styleText).toContain("padding: 0 1px");
  });

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
    expect(screen.getAllByText("Last Update")).toHaveLength(2);
    expect(screen.getByText(/28 Jun 2026/)).toBeInTheDocument();

    expect(within(screen.getByRole("table", { name: "Schedule hours summary" }))
      .getByText("May Schedule, Second May Schedule")).toBeInTheDocument();
    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent("May Schedule");
    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent("Adam Blue, Zoe Young");
    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent('"12:10":"08:00 - 12:00, Late May Schedule, Second May Schedule"');
    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent('"12:11":"22:00 - 02:00"');
    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent('"7:12":"09:00 - 13:00"');
    expect(screen.getByTestId("schedule-matrix")).toHaveTextContent('"7:13":"Second May Schedule"');

    expect(mocks.matrix).toHaveBeenCalledWith(expect.objectContaining({
      readOnly: true,
      showColumnTotals: false,
      graph: expect.objectContaining({ id: 1 }),
      cellMap: expect.objectContaining({
        "7:13": "Second May Schedule",
        "12:10": "08:00 - 12:00, Late May Schedule, Second May Schedule",
      }),
    }));
    const matrixProps = mocks.matrix.mock.calls.at(-1)?.[0];
    expect(matrixProps).not.toHaveProperty("onVisualHintClick");
    expect(matrixProps).not.toHaveProperty("onVisualHintCellClick");
  });

  test("switches to a daily view, marks work days, and orders early, all-day, and late workers", async () => {
    const user = userEvent.setup();
    const dailySchedule: EmployeeSchedule = {
      ...schedules[0],
      relatedScheduleAssignments: [
        ...(schedules[0].relatedScheduleAssignments ?? []),
        { employeeId: 12, dayOfMonth: 14, scheduleId: 35, scheduleName: "F35" },
      ],
      employees: [
        ...(schedules[0].employees ?? []),
        { id: 12, employeeId: 5, firstName: "Alex", lastName: "All Day", displayName: "Alex All Day", displayOrder: 3 },
        { id: 13, employeeId: 9, firstName: "Liam", lastName: "Late", displayName: "Liam Late", displayOrder: 4 },
      ],
      slots: [
        ...schedules[0].slots,
        { id: 20, dayOfMonth: 10, slotNo: 1, employeeId: 5, fromTime: "00:00", toTime: "00:00", status: "ASSIGNED" },
        { id: 21, dayOfMonth: 10, slotNo: 1, employeeId: 9, fromTime: "18:00", toTime: "22:00", status: "ASSIGNED" },
      ],
    };
    mocks.scheduleQuery.mockReturnValue({ data: [dailySchedule], isLoading: false, error: null });

    renderPage();
    await user.click(screen.getByRole("button", { name: "Show daily schedule view" }));

    const workDay = screen.getByRole("tab", { name: "Sunday 10, working day" });
    const dayOff = screen.getByRole("tab", { name: "Saturday 9, day off" });
    expect(workDay).toHaveClass(styles.dailyScheduleDayWorking, styles.dailyScheduleDaySelected);
    expect(dayOff).toHaveClass(styles.dailyScheduleDayOff);
    expect(screen.getByRole("button", { name: "Show schedule matrix view" })).toHaveAttribute("aria-pressed", "true");

    const workerCards = within(screen.getByRole("tabpanel", { name: "Sunday 10" })).getAllByRole("article");
    expect(workerCards.map(card => card.textContent)).toEqual([
      expect.stringContaining("Zoe Young08:00 - 12:00, Late May Schedule, Second May Schedule"),
      expect.stringContaining("Alex All DayAll day"),
      expect.stringContaining("Liam Late18:00 - 22:00"),
    ]);

    await user.click(dayOff);
    expect(screen.getByText("No one is scheduled.")).toBeInTheDocument();

    const relatedOnlyDay = screen.getByRole("tab", { name: "Thursday 14, working day" });
    expect(relatedOnlyDay).toHaveClass(styles.dailyScheduleDayWorking);
    await user.click(relatedOnlyDay);
    expect(within(screen.getByRole("tabpanel", { name: "Thursday 14" })).getByRole("article"))
      .toHaveTextContent("Zoe YoungF35");

    await user.click(screen.getByRole("button", { name: "Show schedule matrix view" }));
    expect(screen.getByTestId("schedule-matrix")).toBeInTheDocument();
  });

  test("calculates estimated salary and restores the Work hours total", async () => {
    const user = userEvent.setup();
    mocks.scheduleQuery.mockReturnValue({
      data: schedules,
      isLoading: false,
      error: null,
    });

    renderPage();

    const calculator = screen.getByRole("region", { name: "Salary calculator" });
    const hoursInput = within(calculator).getByRole("textbox", { name: "Hours" });
    const rateInput = within(calculator).getByRole("textbox", { name: "Hourly rate" });
    const result = within(calculator).getByLabelText("Estimated pay");

    expect(hoursInput).toHaveValue("12");
    expect(result).toHaveTextContent("—");

    await user.clear(hoursInput);
    await user.type(hoursInput, "10");
    await user.type(rateInput, "31,4");
    await user.click(within(calculator).getByRole("button", { name: "Calculate salary" }));

    expect(result).toHaveTextContent("314.00");

    await user.click(within(calculator).getByRole("button", { name: "Reset" }));

    expect(hoursInput).toHaveValue("12");
    expect(rateInput).toHaveValue("31,4");
    expect(result).toHaveTextContent("—");
  });
  test("lets the employee customize and persist the schedule column order", async () => {
    const user = userEvent.setup();
    mocks.scheduleQuery.mockReturnValue({
      data: schedules,
      isLoading: false,
      error: null,
    });

    renderPage();

    await user.click(screen.getByRole("button", { name: "Customize Adam Blue" }));

    const dialog = screen.getByRole("dialog", { name: "Customize schedule" });
    expect(within(dialog).getByText("Adam Blue")).toBeInTheDocument();
    expect(within(dialog).getByText("Zoe Young")).toBeInTheDocument();

    await user.click(within(dialog).getByRole("button", { name: "Move Zoe Young left" }));
    await user.click(within(dialog).getByRole("button", { name: "Apply" }));

    const latestMatrixProps = mocks.matrix.mock.calls.at(-1)?.[0];
    expect(latestMatrixProps.columns.map((column: { label: string }) => column.label)).toEqual([
      "Zoe Young",
      "Adam Blue",
    ]);

    const storedValue = JSON.parse(
      window.localStorage.getItem("gf3:employee-schedule-column-order:v1:employee-12") ?? "null",
    );
    expect(storedValue).toEqual({
      version: 1,
      scheduleOrders: {
        "1": [12, 7],
      },
    });
  });
  test("lets the employee choose the month and year used by Summary", async () => {
    const user = userEvent.setup();
    const periodSchedules: EmployeeSchedule[] = [
      ...schedules,
      {
        id: 3,
        containerId: 6,
        containerName: "June Container",
        shopId: 8,
        shopName: "East Shop",
        name: "June Schedule",
        year: 2026,
        month: 6,
        publicationStatus: "public",
        slots: [
          { id: 5, dayOfMonth: 5, slotNo: 1, employeeId: 12, fromTime: "10:00", toTime: "12:00", status: "ASSIGNED" },
        ],
      },
      {
        id: 4,
        containerId: 7,
        containerName: "Past Container",
        shopId: 9,
        shopName: "West Shop",
        name: "May 2025 Schedule",
        year: 2025,
        month: 5,
        publicationStatus: "public",
        slots: [
          { id: 6, dayOfMonth: 7, slotNo: 1, employeeId: 12, fromTime: "09:00", toTime: "12:00", status: "ASSIGNED" },
        ],
      },
    ];

    mocks.scheduleQuery.mockReturnValue({
      data: periodSchedules,
      isLoading: false,
      error: null,
    });

    renderPage();

    const summaryTable = screen.getByRole("table", { name: "Schedule hours summary" });
    const monthSelect = screen.getByRole("button", { name: "Summary month" });
    const yearSelect = screen.getByRole("button", { name: "Summary year" });

    expect(monthSelect).toHaveTextContent("May");
    expect(yearSelect).toHaveTextContent("2026");

    await user.click(monthSelect);
    const monthOptions = screen.getByRole("listbox", { name: "Summary month" });
    await user.click(within(monthOptions).getByRole("option", { name: "June" }));

    expect(monthSelect).toHaveTextContent("June");
    expect(within(summaryTable).getByText("June Schedule")).toBeInTheDocument();
    expect(within(summaryTable).getAllByText("2h")).toHaveLength(2);

    await user.click(yearSelect);
    const yearOptions = screen.getByRole("listbox", { name: "Summary year" });
    await user.click(within(yearOptions).getByRole("option", { name: "2025" }));

    expect(yearSelect).toHaveTextContent("2025");
    expect(monthSelect).toHaveTextContent("May");
    expect(within(summaryTable).getByText("May 2025 Schedule")).toBeInTheDocument();
    expect(within(summaryTable).getAllByText("3h")).toHaveLength(2);
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
