import { expect, test } from "vitest";
import { createEmployeeFormState, validateEmployeeForm } from "./form";
import { stableSerialize } from "@shared/lib/stableSerialize";
import { setLanguage } from "@shared/i18n";
test("default and persisted contract participate in the unsaved-change snapshot", () => {
  const defaultForm = createEmployeeFormState(); expect(defaultForm.contractType).toBe("zlecenie");
  const form = createEmployeeFormState({ firstName: "Ada", lastName: "Worker", hasLoginAccount: false, contractType: "o_prace" });
  expect(form.contractType).toBe("o_prace"); expect(stableSerialize(form)).not.toBe(stableSerialize({ ...form, contractType: "zlecenie" }));
});
test("rejects unknown contract values with localized field validation", () => {
  setLanguage("pl"); const form = { ...createEmployeeFormState(), firstName: "Ada", lastName: "Worker", contractType: "unknown" as never };
  expect(validateEmployeeForm(form, {isCreate:true,hasLoginAccount:false}).contractType).toBe("Wybierz prawidłowy rodzaj umowy.");
});
