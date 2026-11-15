import { useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, expect, test, vi } from "vitest";
import { createInitialGraphForm } from "../model/graphForm";
import { setLanguage, t } from "@shared/i18n";
import { ContainerGraphDetailsFields } from "./ContainerGraphDetailsFields";

beforeEach(() => setLanguage("en"));

test("independent circular priority checkboxes support keyboard and retain minimum hours", async () => {
  const changed = vi.fn();
  function Harness() {
    const [rows, setRows] = useState([{ employeeId: 1, minHoursMonth: "70", isPriority: false }, { employeeId: 2, minHoursMonth: "20", isPriority: false }]);
    return <ContainerGraphDetailsFields form={{ ...createInitialGraphForm(), availabilityGroupId: "1" }} formErrors={{}}
      employees={[{ id: 1, firstName: "Ada", lastName: "One", hasLoginAccount: false, isOnline: false }, { id: 2, firstName: "Grace", lastName: "Two", hasLoginAccount: false, isOnline: false }]}
      shops={[]} availabilityGroups={[]} graphEmployeeRows={rows} onFieldChange={() => () => undefined}
      onEmployeeMinHoursChange={changed} onEmployeePriorityChange={(id, checked) => setRows(current => current.map(row => row.employeeId === id ? { ...row, isPriority: checked } : row))} />;
  }
  render(<Harness />);
  const ada = screen.getByRole("checkbox", { name: "Priority: Ada One" });
  const grace = screen.getByRole("checkbox", { name: "Priority: Grace Two" });
  expect(ada).toHaveAccessibleDescription("Schedule this employee first using the priority shift duration and weekend settings.");
  ada.focus(); await userEvent.keyboard(" ");
  expect(ada).toBeChecked(); expect(ada).toHaveFocus();
  await userEvent.click(grace);
  expect(ada).toBeChecked(); expect(grace).toBeChecked();
  expect(screen.getByRole("textbox", { name: "Ada One min hours per month" })).toHaveValue("70");
  expect(changed).not.toHaveBeenCalled();
  await userEvent.click(screen.getByRole("button", { name: "Increase Ada One min hours per month" }));
  expect(changed).toHaveBeenCalledWith(1, "71");
  expect(ada.closest("label")!.querySelectorAll("input")).toHaveLength(1);
});

test.each(["en", "pl", "uk"] as const)("priority labels and descriptions are localized in %s", language => {
  setLanguage(language);
  render(<ContainerGraphDetailsFields form={{ ...createInitialGraphForm(), availabilityGroupId: "1" }} formErrors={{}}
    employees={[{ id: 1, firstName: "Ada", lastName: "Synthetic", hasLoginAccount: false, isOnline: false }]}
    shops={[]} availabilityGroups={[]} graphEmployeeRows={[{ employeeId: 1, minHoursMonth: "180", isPriority: true }]}
    onFieldChange={() => () => undefined} onEmployeeMinHoursChange={() => undefined} onEmployeePriorityChange={() => undefined} />);
  const checkbox = screen.getByRole("checkbox", { name: t("Priority: {0}", "Ada Synthetic") });
  expect(checkbox).toBeChecked();
  expect(checkbox).toHaveAccessibleDescription(t("Schedule this employee first using the priority shift duration and weekend settings."));
});
