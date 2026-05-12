import { describe, expect, test } from "vitest";
import { createShopFormState, validateShopForm } from "./form";

describe("shop form model", () => {
  test("creates empty and populated form state", () => {
    expect(createShopFormState()).toEqual({
      name: "",
      address: "",
      description: "",
    });
    expect(createShopFormState({
      name: "Main",
      address: "1 Market",
      description: null,
    })).toEqual({
      name: "Main",
      address: "1 Market",
      description: "",
    });
  });

  test("requires name and address", () => {
    expect(validateShopForm({ name: " ", address: "", description: "Optional" })).toEqual({
      name: "Name is required",
      address: "Address is required",
    });
    expect(validateShopForm({ name: "Main", address: "1 Market", description: "" })).toEqual({});
  });
});
