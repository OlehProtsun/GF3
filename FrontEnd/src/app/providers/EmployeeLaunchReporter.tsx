import { useEffect } from "react";
import { employeeLaunchesApi } from "@entities/employee-launches/api/employeeLaunchesApi";
import { getPwaLaunchMode } from "@shared/lib/pwaDisplayMode";
import { useAuth } from "./AuthProvider";

const reportedEmployeeIds = new Set<number>();

export function EmployeeLaunchReporter() {
  const { status, session } = useAuth();
  const employeeId = status === "authenticated" && session?.role === "employee" ? session.employeeId : null;

  useEffect(() => {
    if (!employeeId || employeeId <= 0 || reportedEmployeeIds.has(employeeId)) return;
    reportedEmployeeIds.add(employeeId);
    void employeeLaunchesApi.record(getPwaLaunchMode()).catch(error => {
      console.debug("Could not record the employee launch.", error);
    });
  }, [employeeId]);

  return null;
}
