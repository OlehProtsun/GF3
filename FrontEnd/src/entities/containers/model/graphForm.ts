import type { Graph, GraphPublicationStatus } from "./types";
import { getGraphVisibleNote } from "./graphNote";

const SHIFT_TIME_PATTERN = /^(\d{1,2}):(\d{2})\s*-\s*(\d{1,2}):(\d{2})$/;

export type ContainerGraphFormState = {
  name: string;
  shopId: string;
  year: string;
  month: string;
  publicationStatus: GraphPublicationStatus;
  peoplePerShift: string;
  shift1Time: string;
  shift2Time: string;
  maxHoursPerEmpMonth: string;
  maxConsecutiveDays: string;
  maxConsecutiveFull: string;
  maxFullPerMonth: string;
  availabilityGroupId: string;
  note: string;
};

export type ContainerGraphFormErrors = Partial<Record<keyof ContainerGraphFormState, string>>;
export type ContainerGraphFormFieldElement = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

export function createInitialGraphForm(defaultShopId?: number | null): ContainerGraphFormState {
  const today = new Date();

  return {
    name: "",
    shopId: defaultShopId ? String(defaultShopId) : "",
    year: String(today.getFullYear()),
    month: String(today.getMonth() + 1),
    publicationStatus: "private",
    peoplePerShift: "1",
    shift1Time: "06:00 - 14:00",
    shift2Time: "14:00 - 22:00",
    maxHoursPerEmpMonth: "160",
    maxConsecutiveDays: "5",
    maxConsecutiveFull: "3",
    maxFullPerMonth: "10",
    availabilityGroupId: "",
    note: "",
  };
}

export function createGraphFormFromGraph(graph: Graph): ContainerGraphFormState {
  return {
    name: graph.name,
    shopId: String(graph.shopId),
    year: String(graph.year),
    month: String(graph.month),
    publicationStatus: graph.publicationStatus === "public" ? "public" : "private",
    peoplePerShift: String(graph.peoplePerShift),
    shift1Time: graph.shift1Time,
    shift2Time: graph.shift2Time,
    maxHoursPerEmpMonth: String(graph.maxHoursPerEmpMonth),
    maxConsecutiveDays: String(graph.maxConsecutiveDays),
    maxConsecutiveFull: String(graph.maxConsecutiveFull),
    maxFullPerMonth: String(graph.maxFullPerMonth),
    availabilityGroupId: graph.availabilityGroupId ? String(graph.availabilityGroupId) : "",
    note: getGraphVisibleNote(graph.note),
  };
}

export function parseIntegerField(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }

  const parsed = Number(trimmed);
  return Number.isInteger(parsed) ? parsed : null;
}

export function isValidGraphShiftTime(value: string) {
  const match = value.trim().match(SHIFT_TIME_PATTERN);
  if (!match) {
    return false;
  }

  const fromHour = Number(match[1]);
  const fromMinute = Number(match[2]);
  const toHour = Number(match[3]);
  const toMinute = Number(match[4]);

  if (
    Number.isNaN(fromHour) ||
    Number.isNaN(fromMinute) ||
    Number.isNaN(toHour) ||
    Number.isNaN(toMinute) ||
    fromHour > 23 ||
    toHour > 23 ||
    fromMinute > 59 ||
    toMinute > 59
  ) {
    return false;
  }

  const fromTotalMinutes = fromHour * 60 + fromMinute;
  const toTotalMinutes = toHour * 60 + toMinute;
  return toTotalMinutes > fromTotalMinutes;
}

export function buildGraphFormErrors(
  form: ContainerGraphFormState,
  availableShopIds: Set<number>,
  availableAvailabilityGroupIds: Set<number>,
): ContainerGraphFormErrors {
  const errors: ContainerGraphFormErrors = {};
  const shopId = parseIntegerField(form.shopId);
  const month = parseIntegerField(form.month);
  const year = parseIntegerField(form.year);
  const peoplePerShift = parseIntegerField(form.peoplePerShift);
  const maxHoursPerEmpMonth = parseIntegerField(form.maxHoursPerEmpMonth);
  const maxConsecutiveDays = parseIntegerField(form.maxConsecutiveDays);
  const maxConsecutiveFull = parseIntegerField(form.maxConsecutiveFull);
  const maxFullPerMonth = parseIntegerField(form.maxFullPerMonth);
  const availabilityGroupId = form.availabilityGroupId ? parseIntegerField(form.availabilityGroupId) : null;

  if (!form.name.trim()) {
    errors.name = "Name is required.";
  }

  if (availableShopIds.size === 0) {
    errors.shopId = "Create at least one shop before adding a schedule.";
  } else if (shopId === null || !availableShopIds.has(shopId)) {
    errors.shopId = "Select a valid shop.";
  }

  if (month === null || month < 1 || month > 12) {
    errors.month = "Month must be between 1 and 12.";
  }

  if (year === null || year < 2000 || year > 2100) {
    errors.year = "Enter a valid year.";
  }

  if (peoplePerShift === null || peoplePerShift < 1) {
    errors.peoplePerShift = "People per shift must be at least 1.";
  }

  if (!isValidGraphShiftTime(form.shift1Time)) {
    errors.shift1Time = "Use HH:mm - HH:mm format.";
  }

  if (!isValidGraphShiftTime(form.shift2Time)) {
    errors.shift2Time = "Use HH:mm - HH:mm format.";
  }

  if (maxHoursPerEmpMonth === null || maxHoursPerEmpMonth < 1) {
    errors.maxHoursPerEmpMonth = "Max hours must be at least 1.";
  }

  if (maxConsecutiveDays === null || maxConsecutiveDays < 0) {
    errors.maxConsecutiveDays = "Use 0 or more.";
  }

  if (maxConsecutiveFull === null || maxConsecutiveFull < 0) {
    errors.maxConsecutiveFull = "Use 0 or more.";
  }

  if (maxFullPerMonth === null || maxFullPerMonth < 0) {
    errors.maxFullPerMonth = "Use 0 or more.";
  }

  if (
    availabilityGroupId !== null &&
    (availabilityGroupId <= 0 || !availableAvailabilityGroupIds.has(availabilityGroupId))
  ) {
    errors.availabilityGroupId = "Select a valid availability group.";
  }

  return errors;
}

export function applyGraphApiErrors(validationErrors?: Record<string, string[]>) {
  if (!validationErrors) {
    return {};
  }

  const nextErrors: ContainerGraphFormErrors = {};
  const mapField = (field: keyof ContainerGraphFormErrors, ...keys: string[]) => {
    const message = keys
      .flatMap(key => [validationErrors[key], validationErrors[key.toLowerCase()]])
      .find(Boolean)?.[0];

    if (message) {
      nextErrors[field] = message;
    }
  };

  mapField("name", "Name");
  mapField("shopId", "ShopId");
  mapField("year", "Year");
  mapField("month", "Month");
  mapField("peoplePerShift", "PeoplePerShift");
  mapField("shift1Time", "Shift1Time");
  mapField("shift2Time", "Shift2Time");
  mapField("maxHoursPerEmpMonth", "MaxHoursPerEmpMonth");
  mapField("maxConsecutiveDays", "MaxConsecutiveDays");
  mapField("maxConsecutiveFull", "MaxConsecutiveFull");
  mapField("maxFullPerMonth", "MaxFullPerMonth");
  mapField("availabilityGroupId", "AvailabilityGroupId");
  mapField("note", "Note");

  return nextErrors;
}
