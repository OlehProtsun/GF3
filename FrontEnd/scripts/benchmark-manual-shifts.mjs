import { createServer } from "vite";
import { execFileSync } from "node:child_process";
import { writeFileSync, unlinkSync } from "node:fs";

const baselinePath = "src/entities/containers/model/.generatedManualShifts.benchmark.ts";
const compare = process.argv.includes("--compare-baseline");
if (compare) writeFileSync(baselinePath, execFileSync("git", ["show", "HEAD:FrontEnd/src/entities/containers/model/generatedManualShifts.ts"]), { flag: "wx" });

let server;
try {
  server = await createServer({ server: { middlewareMode: true }, appType: "custom" });
  const { deriveGeneratedVacancies, reconcileGeneratedManualColumns } = await server.ssrLoadModule("/src/entities/containers/model/generatedManualShifts.ts");
  const baseline = compare ? await server.ssrLoadModule(`/${baselinePath}`) : null;
  const { enumerateManualShiftOptions } = await server.ssrLoadModule("/src/entities/containers/model/manualShiftIntervals.ts");
  const measure = (name, run) => {
    const expected = JSON.stringify(run());
    const times = Array.from({ length: 5 }, () => {
      const start = performance.now();
      const actual = run();
      const duration = performance.now() - start;
      if (JSON.stringify(actual) !== expected) throw new Error(`Non-deterministic ${name}`);
      return duration;
    }).sort((a, b) => a - b);
    console.log(JSON.stringify({ name, medianMs: +times[2].toFixed(3), maxMs: +times[4].toFixed(3), repeats: 5, warmup: 1 }));
  };
  for (const employees of [10, 50, 100]) for (const peoplePerShift of [1, 2, 5]) for (const staffed of [0, 25, 100]) {
    const slots = Array.from({ length: 31 }, (_, day) => Array.from({ length: employees }, (_, index) => ({
      dayOfMonth: day + 1, employeeId: index < employees * staffed / 100 ? index + 1 : null,
      fromTime: index % 2 ? "15:00" : "09:00", toTime: index % 2 ? "21:00" : "15:00",
      status: index < employees * staffed / 100 ? 1 : 0,
    }))).flat();
    const input = { slots, year: 2026, month: 12, peoplePerShift, shift1Time: "09:00-15:00", shift2Time: "15:00-21:00" };
    if (baseline) measure(`baseline/vacancies/${employees}/${peoplePerShift}/${staffed}`, () => baseline.deriveGeneratedVacancies(input));
    measure(`vacancies/${employees}/${peoplePerShift}/${staffed}`, () => deriveGeneratedVacancies(input));
  }
  for (const count of [1, 5, 20]) for (const offers of [0, 100, 500]) {
    const columns = Array.from({ length: count }, (_, i) => ({ columnId: i + 1, source: "generated-unfilled", label: "Auto",
      cells: Object.fromEntries(Array.from({ length: 31 }, (_, day) => [day + 1, "09:00-15:00,09:00-15:00,15:00-21:00"])) }));
    const reservations = Array.from({ length: offers }, (_, i) => ({ manualColumnId: i % count + 1, dayOfMonth: i % 31 + 1, fromTime: "09:00", toTime: "15:00" }));
    const input = { vacancies: [], columns, reservations, retainedGeneratedColumnIds: new Set(), columnOrder: [1, ...columns.map(c => -c.columnId)] };
    if (baseline) measure(`baseline/reconcile/${count}/${offers}`, () => baseline.reconcileGeneratedManualColumns(input));
    measure(`reconcile/${count}/${offers}`, () => reconcileGeneratedManualColumns(input));
    measure(`options/${count}/${offers}`, () => columns.flatMap(column => Object.keys(column.cells).map(day => enumerateManualShiftOptions(
      column.cells[day], reservations.filter(r => r.manualColumnId === column.columnId && r.dayOfMonth === Number(day))))));
  }
} finally {
  await server?.close();
  if (compare) unlinkSync(baselinePath);
}
