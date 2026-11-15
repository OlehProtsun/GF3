export type DemoEmployee = Readonly<{ id: string; name: string; initials: string }>;
export type DemoShift = Readonly<{ id: string; employeeId: string; dayId: string; start: string; end: string }>;
export type DemoSwapOffer = Readonly<{ id: string; shiftId: string; candidateId: string }>;
export type DemoAvailabilityEntry = Readonly<{ employeeId: string; dayId: string; available: boolean }>;

export const fixtureVersion = "gf3-promo-1";
export const workplace = "Sklep Centrum";
export const month = "Październik";
export const employees: readonly DemoEmployee[] = Object.freeze([
  Object.freeze({ id: "kasia", name: "Kasia", initials: "K" }),
  Object.freeze({ id: "marta", name: "Marta", initials: "M" }),
  Object.freeze({ id: "tomek", name: "Tomek", initials: "T" }),
]);
export const days = Object.freeze([
  Object.freeze({ id: "mon", label: "Pon", date: "12" }),
  Object.freeze({ id: "tue", label: "Wt", date: "13" }),
  Object.freeze({ id: "wed", label: "Śr", date: "14" }),
  Object.freeze({ id: "thu", label: "Czw", date: "15" }),
  Object.freeze({ id: "fri", label: "Pt", date: "16" }),
  Object.freeze({ id: "sat", label: "Sob", date: "17" }),
  Object.freeze({ id: "sun", label: "Ndz", date: "18" }),
]);
export const shifts: readonly DemoShift[] = Object.freeze([
  Object.freeze({ id: "kasia-mon", employeeId: "kasia", dayId: "mon", start: "09:00", end: "17:00" }),
  Object.freeze({ id: "kasia-wed", employeeId: "kasia", dayId: "wed", start: "12:00", end: "20:00" }),
  Object.freeze({ id: "marta-tue", employeeId: "marta", dayId: "tue", start: "09:00", end: "17:00" }),
  Object.freeze({ id: "marta-fri", employeeId: "marta", dayId: "fri", start: "12:00", end: "20:00" }),
  Object.freeze({ id: "tomek-mon", employeeId: "tomek", dayId: "mon", start: "12:00", end: "20:00" }),
  Object.freeze({ id: "tomek-thu", employeeId: "tomek", dayId: "thu", start: "09:00", end: "17:00" }),
]);
export const availability: readonly DemoAvailabilityEntry[] = Object.freeze(employees.flatMap(employee =>
  days.map(day => Object.freeze({ employeeId: employee.id, dayId: day.id,
    available: !((employee.id === "kasia" && day.id === "fri") ||
      (employee.id === "marta" && day.id === "wed") || day.id === "sun"),
  })),
));
export const swapOffer: DemoSwapOffer = Object.freeze({ id: "offer-kasia-mon", shiftId: "kasia-mon", candidateId: "marta" });
