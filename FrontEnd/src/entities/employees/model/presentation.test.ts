import { describe, expect, test } from "vitest";
import {
  formatEmployeeLastLogin,
  getEmployeeContactDetails,
  getEmployeeContactState,
  getEmployeeFullName,
  getEmployeeInitials,
  getEmployeePresenceTone,
} from "./presentation";
import type { Employee } from "./types";

const employee: Employee = {
  id: 1,
  firstName: "Ada",
  lastName: "Lovelace",
  email: "ada@example.com",
  phone: "+48123456789",
  username: "ada",
  hasLoginAccount: true,
  isOnline: false,
  lastLoginAtUtc: "2026-05-10T09:30:00",
};

describe("employee presentation model", () => {
  test("formats identity labels and initials with fallbacks", () => {
    expect(getEmployeeFullName(employee)).toBe("Ada Lovelace");
    expect(getEmployeeFullName({ firstName: " Ada ", lastName: " " })).toBe("Ada");
    expect(getEmployeeFullName(null, "Unknown employee")).toBe("Unknown employee");

    expect(getEmployeeInitials(employee)).toBe("AL");
    expect(getEmployeeInitials({ firstName: "", lastName: "" }, "??")).toBe("??");
  });

  test("derives presence tone and labels from login account state", () => {
    expect(getEmployeePresenceTone({ ...employee, isOnline: true })).toBe("online");
    expect(getEmployeeContactState({ ...employee, isOnline: true })).toBe("Online now");
    expect(getEmployeeContactState({ ...employee, isOnline: true }, "compact")).toBe("Online");

    expect(getEmployeePresenceTone(employee)).toBe("offline");
    expect(getEmployeeContactState(employee)).toBe("Offline");

    expect(getEmployeePresenceTone({ ...employee, hasLoginAccount: false })).toBe("inactive");
    expect(getEmployeeContactState({ ...employee, hasLoginAccount: false })).toBe("No login account");
    expect(getEmployeeContactState({ ...employee, hasLoginAccount: false }, "compact")).toBe("No login");
  });

  test("formats last login and contact details including mail and phone links", () => {
    expect(formatEmployeeLastLogin(null)).toBe("Never");
    expect(formatEmployeeLastLogin("not-a-date")).toBe("not-a-date");
    expect(formatEmployeeLastLogin("2026-05-10T09:30:00")).toBe("10 May 2026 09:30");

    expect(getEmployeeContactDetails(employee)).toEqual([
      { key: "last-login", label: "Last Login", value: "10 May 2026 09:30", href: undefined },
      { key: "username", label: "Username", value: "ada", href: undefined },
      { key: "email", label: "Recovery Email", value: "ada@example.com", href: "mailto:ada@example.com" },
      { key: "phone", label: "Phone", value: "+48123456789", href: "tel:+48123456789" },
    ]);

    expect(getEmployeeContactDetails({ ...employee, email: "", phone: "" })[2]).toMatchObject({
      value: "",
      href: undefined,
    });
  });
});
