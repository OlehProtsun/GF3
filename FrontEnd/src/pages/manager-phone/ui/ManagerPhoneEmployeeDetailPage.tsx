import { parsePhoneId } from "./parsePhoneId";
import { useParams } from "react-router-dom";
import { useEmployeeByIdQuery } from "@entities/employees/api/queries";
import { EmployeeProfileCard } from "@entities/employees/ui/EmployeeProfileCard";
import { useEmployeeRegulationHistoryQuery } from "@entities/regulations";
import { RegulationHistoryCard } from "@entities/regulations/ui/RegulationHistoryCard";
import { queryKeys } from "@shared/api/queryKeys";
import { ManagerPhonePage } from "./ManagerPhonePage";
import styles from "./ManagerPhonePage.module.css";

export function ManagerPhoneEmployeeDetailPage() {
  const id = parsePhoneId(useParams<{ employeeId: string }>().employeeId);
  const employee = useEmployeeByIdQuery(id);
  const history = useEmployeeRegulationHistoryQuery(id);
  return <ManagerPhonePage backTo="/employee" valid={id !== null} missing={!employee.data} queries={[employee, history]} queryKeys={[queryKeys.employees.all, queryKeys.regulations.all]}>
    <div className={styles.employeeProfile}><EmployeeProfileCard employee={employee.data} showManagementActions={false} isLoading={false} hasLoadError={false} isDeleting={false} isKicking={false} onEditEmployee={() => {}} onDeleteEmployee={() => {}} onKickEmployee={() => {}} /></div>
    <RegulationHistoryCard acceptances={history.data ?? []} isLoading={false} />
  </ManagerPhonePage>;
}
