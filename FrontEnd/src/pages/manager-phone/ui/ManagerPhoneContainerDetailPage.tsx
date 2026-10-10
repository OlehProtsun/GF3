import { parsePhoneId } from "./parsePhoneId";
import { useState } from "react";
import { NavLink, useLocation, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useContainerByIdQuery, useContainerGraphsQuery, containersApi, buildContainerGraphSummaries, filterGraphSummaries, buildContainerStatistics, type ContainerGraphRecords } from "@entities/containers";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import { useShopsListQuery } from "@entities/shops/api/queries";
import { queryKeys } from "@shared/api/queryKeys";
import { t } from "@shared/i18n";
import { ManagerPhonePage } from "./ManagerPhonePage";
import { StatisticsIcon } from "@shared/ui/icons";
import styles from "./ManagerPhonePage.module.css";

export function ManagerPhoneContainerDetailPage() {
  const location = useLocation();
  const id = parsePhoneId(useParams<{ containerId: string }>().containerId);
  const [search, setSearch] = useState("");
  const container = useContainerByIdQuery(id);
  const graphsQuery = useContainerGraphsQuery(id);
  const employees = useEmployeesListQuery({ refreshKey: location.key });
  const shops = useShopsListQuery({ refreshKey: location.key });
  const graphs = graphsQuery.data ?? [];
  const graphIdsKey = graphs.map(item => item.id).sort((a, b) => a - b).join(",");
  const records = useQuery({ queryKey: queryKeys.containers.graphRecords(id ?? 0, graphIdsKey), enabled: id !== null && graphs.length > 0,
    cancelOnUnmount: true, staleTime: 30_000,
    queryFn: async ({ signal }) => {
      const results = await Promise.all(graphs.map(async graph => {
        const [employees, slots] = await Promise.all([containersApi.listGraphEmployees(id!, graph.id, signal), containersApi.listGraphSlots(id!, graph.id, signal)]);
        return [graph.id, { employees, slots }] as const;
      }));
      return Object.fromEntries(results) as Record<number, ContainerGraphRecords>;
    } });
  const shopsById = new Map((shops.data ?? []).map(item => [item.id, item]));
  const summaries = filterGraphSummaries(buildContainerGraphSummaries(graphs, records.data ?? {}, shopsById), search);
  const statistics = buildContainerStatistics({ graphs, graphRecordsById: records.data ?? {}, shopsById, employeesById: new Map((employees.data ?? []).map(item => [item.id, item])) });
  return <ManagerPhonePage backTo="/container" valid={id !== null} missing={!container.data} query={search} onQueryChange={setSearch}
    queries={[container, graphsQuery, employees, shops, records]} queryKeys={[queryKeys.containers.all, queryKeys.employees.all, queryKeys.shops.all]}>
    <section className={styles.card}><h2>{container.data?.name}</h2><p>{container.data?.note}</p><p>{t("Total Employees: {0}", statistics.totalEmployees)} · {t("Total Hours: {0}", statistics.totalHoursText)}</p></section>
    <div className={styles.tiles}>{summaries.map(item => <NavLink className={styles.tile} key={item.graph.id} to={`/container/${id}/graphs/${item.graph.id}`}>
      <strong>{item.graph.name}</strong><span>{item.monthYearLabel} · {item.shopName} · {t(item.graph.publicationStatus === "public" ? "Public" : "Private")}</span>
      <span>{t("Employees: {0}", item.employeeCount)} · {t("Hours: {0}", item.assignedHoursText)}</span>
    </NavLink>)}</div>
    {summaries.length === 0 && <p>{t("No results")}</p>}
    {statistics.pivotRows.length > 0 && <section className={styles.card}>
      <h2 className={styles.statisticsHeading}><StatisticsIcon size={18} />{t("Statistics")}</h2>
      <div className={styles.metrics}>{statistics.pivotRows.map((row, index) => <article className={`${styles.metricCard} ${row.isTotal ? styles.metricTotal : ""}`} key={index}>
        <h3>{row.employee}</h3>
        <dl className={styles.metricValues}>
          <div><dt>{t("Work Days")}</dt><dd>{row.workDays}</dd></div>
          <div><dt>{t("Free Days")}</dt><dd>{row.freeDays}</dd></div>
          <div><dt>{t("Hours")}</dt><dd>{row.hoursSum}</dd></div>
        </dl>
        {statistics.shopHeaders.length > 0 && <details><summary>{t("Shops")}</summary>
          <dl className={styles.shopValues}>{statistics.shopHeaders.map(shop => <div key={shop.key}><dt>{shop.name}</dt><dd>{row.hoursByShop[shop.key]}</dd></div>)}</dl>
        </details>}
      </article>)}</div>
    </section>}
  </ManagerPhonePage>;
}
