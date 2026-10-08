import { NavLink } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { loadHomeDashboard } from "@pages/home/ui/homeDashboard";
import { queryKeys } from "@shared/api/queryKeys";
import { t } from "@shared/i18n";
import { ManagerPhonePage } from "./ManagerPhonePage";
import styles from "./ManagerPhonePage.module.css";

export function ManagerPhoneHomePage() {
  const dashboard = useQuery({ queryKey: queryKeys.home.dashboard(), queryFn: ({ signal }) => loadHomeDashboard(signal) });
  return <ManagerPhonePage queries={[dashboard]} queryKeys={[queryKeys.home.dashboard()]}>
    <div className={styles.counts}>
      {[["Containers", dashboard.data?.totalContainersCount], ["Schedules", dashboard.data?.monthSchedulesCount], ["Today assignments", dashboard.data?.todayAssignmentsCount], ["Shops", dashboard.data?.activeShopsCount]].map(([label, value]) => <div className={styles.card} key={label}><strong>{value ?? 0}</strong>{t(String(label))}</div>)}
    </div>
    <div className={styles.tiles}>{[["Containers", "/container"], ["Dispo", "/availability"], ["Employees", "/employee"], ["More", "/more"]].map(([label, to]) => <NavLink className={styles.tile} to={to} key={to}>{t(label)} →</NavLink>)}</div>
  </ManagerPhonePage>;
}
