import { beforeEach as beforeEnglishTest } from "vitest";
import { setLanguage as setTestLanguage } from "@shared/i18n";
import { describe, expect, test } from "vitest";
import { createEmployeeFormState, validateEmployeeForm } from "./form";

beforeEnglishTest(() => setTestLanguage("en"));

describe("employee form model", () => {
  test("creates empty and populated form state", () => {
    expect(createEmployeeFormState()).toEqual({
      contractType: "zlecenie",
      firstName: "",
      lastName: "",
      email: "",
      phone: "",
      username: "",
      password: "",
    });
    expect(createEmployeeFormState({
      contractType: "zlecenie",
      firstName: "Amy",
      lastName: "Jones",
      email: null,
      phone: "555",
      username: "amy",
      hasLoginAccount: true,
    })).toEqual({
      contractType: "zlecenie",
      firstName: "Amy",
      lastName: "Jones",
      email: "",
      phone: "555",
      username: "amy",
      password: "",
    });
  });

  test("validates identity and email fields", () => {
    expect(validateEmployeeForm({
      contractType: "zlecenie",
      firstName: "",
      lastName: " ",
      email: "invalid",
      phone: "",
      username: "",
      password: "",
    }, { isCreate: true, hasLoginAccount: false })).toEqual({
      firstName: "First name is required",
      lastName: "Last name is required",
      email: "Invalid email format",
    });
  });

  test("validates login account creation and updates", () => {
    expect(validateEmployeeForm({
      contractType: "zlecenie",
      firstName: "Amy",
      lastName: "Jones",
      email: "amy@example.com",
      phone: "",
      username: "am",
      password: "",
    }, { isCreate: true, hasLoginAccount: false })).toEqual({
      username: "Username must be at least 3 characters long",
      password: "Password is required when creating a login",
    });

    expect(validateEmployeeForm({
      contractType: "zlecenie",
      firstName: "Amy",
      lastName: "Jones",
      email: "",
      phone: "",
      username: "bad user",
      password: "123",
    }, { isCreate: false, hasLoginAccount: false })).toEqual({
      username: "Username may use letters, numbers, dots, underscores, and dashes",
      password: "Password must contain exactly 6 digits.",
    });

    expect(validateEmployeeForm({
      contractType: "zlecenie",
      firstName: "Amy",
      lastName: "Jones",
      email: "",
      phone: "",
      username: "amy.jones",
      password: "123abc",
    }, { isCreate: false, hasLoginAccount: true })).toEqual({
      password: "Password must contain exactly 6 digits.",
    });

    expect(validateEmployeeForm({
      contractType: "zlecenie",
      firstName: "Amy",
      lastName: "Jones",
      email: "",
      phone: "",
      username: "",
      password: "123456",
    }, { isCreate: false, hasLoginAccount: false })).toEqual({
      username: "Username is required when setting a password",
    });
  });

  test("requires username for existing login accounts but allows blank password", () => {
    expect(validateEmployeeForm({
      contractType: "zlecenie",
      firstName: "Amy",
      lastName: "Jones",
      email: "",
      phone: "",
      username: "",
      password: "",
    }, { isCreate: false, hasLoginAccount: true })).toEqual({
      username: "Username is required for employees with an existing login",
    });

    expect(validateEmployeeForm({
      contractType: "zlecenie",
      firstName: "Amy",
      lastName: "Jones",
      email: "",
      phone: "",
      username: "amy.jones",
      password: "",
    }, { isCreate: false, hasLoginAccount: true })).toEqual({});
  });
});
