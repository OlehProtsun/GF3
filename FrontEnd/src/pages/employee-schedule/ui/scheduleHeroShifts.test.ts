import { describe, expect, it } from "vitest";
import type { EmployeeSchedule } from "@entities/employee-schedule";
import { getHeroShifts } from "./scheduleHeroShifts";
const schedule = (year: number, month: number, day: number, from = "08:00", to = "16:00", employeeId: number | null = 12): EmployeeSchedule => ({
  id: day, name: "Main schedule", year, month, containerId: 1, containerName: "Main", shopId: 1, shopName: "Shop", publicationStatus: "public",
  slots: [{id: day, dayOfMonth: day, slotNo: 1, employeeId, fromTime: from, toTime: to, status: "ASSIGNED"}],
});
describe("schedule hero shifts", () => {
  it("uses today's and tomorrow's assignments across all schedules, never coworkers or unassigned slots", () => {
    const result = getHeroShifts([schedule(2026,9,19), schedule(2026,9,20), schedule(2026,9,19,"10:00","12:00",99), schedule(2026,9,19,"10:00","12:00",null)],12,new Date(2026,8,19,12));
    expect(result.now).toHaveLength(1); expect(result.tomorrow).toHaveLength(1);
    expect(result.now[0]!.end).toBe(new Date(2026,8,19,16).getTime());
  });
  it("handles month/year boundaries and overnight shifts", () => {
    const result = getHeroShifts([schedule(2026,12,30,"22:00","06:00"),schedule(2026,12,31,"22:00","06:00"),schedule(2027,1,1)],12,new Date(2026,11,31,2));
    expect(result.now).toHaveLength(2); expect(result.tomorrow).toHaveLength(1);
    expect(result.now[0]!.end).toBe(new Date(2026,11,31,6).getTime());
    expect(result.now[1]!.end).toBe(new Date(2027,0,1,6).getTime());
  });
  it("removes the previous night's completed shift and ignores invalid/off-day entries", () => {
    const invalid=schedule(2026,9,19,"25:00");
    const off=schedule(2026,9,19); off.slots[0]!.status="OFF";
    expect(getHeroShifts([invalid,off,schedule(2026,9,18,"22:00","06:00")],12,new Date(2026,8,19,12))).toEqual({now:[],tomorrow:[]});
    expect(getHeroShifts([schedule(2026,9,19)],null,new Date(2026,8,19))).toEqual({now:[],tomorrow:[]});
  });
});
