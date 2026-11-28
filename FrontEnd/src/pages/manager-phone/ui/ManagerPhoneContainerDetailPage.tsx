import { parsePhoneId } from "./parsePhoneId";
import { useId, useState } from "react";
import { NavLink, useLocation, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useContainerByIdQuery, useContainerGraphsQuery, containersApi, buildContainerGraphSummaries, filterGraphSummaries, buildContainerStatistics, type ContainerGraphRecords } from "@entities/containers";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import { useShopsListQuery } from "@entities/shops/api/queries";
import { queryKeys } from "@shared/api/queryKeys";
import { t } from "@shared/i18n";
import { graphDateRange, useApprovedUrlopQuery } from "@entities/urlop";
import { ManagerPhonePage } from "./ManagerPhonePage";
import { AvailabilityIcon, ChevronRightIcon, ClockIcon, EmployeeIcon, ScheduleDetailsIcon, ScheduleIcon, ShopIcon, StatisticsIcon } from "@shared/ui/icons";
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
  const urlopRange = graphDateRange(graphs);
  const urlop = useApprovedUrlopQuery(urlopRange.from, urlopRange.to, undefined, graphs.length > 0);
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
  const statistics = buildContainerStatistics({ graphs, graphRecordsById: records.data ?? {}, shopsById, employeesById: new Map((employees.data ?? []).map(item => [item.id, item])), approvedUrlopDays: urlop.data });
  return <ManagerPhonePage title={container.data?.name || t("Container")} backTo="/container" valid={id !== null} missing={!container.data} query={search} onQueryChange={setSearch}
    queries={[container, graphsQuery, employees, shops, records, urlop]} queryKeys={[queryKeys.containers.all, queryKeys.employees.all, queryKeys.shops.all, queryKeys.urlop.all]}>
    <div className={styles.tiles}>
      {summaries.map(item => (
        <NavLink className={styles.tile} key={item.graph.id} to={`/container/${id}/graphs/${item.graph.id}`}>
          <span className={styles.tileIcon}><ScheduleDetailsIcon size={22} /></span>
          <span className={styles.tileContent}>
            <span className={styles.tileHeading}>
              <strong>{item.graph.name}</strong>
              <span className={styles.tilePublication}>{t(item.graph.publicationStatus === "public" ? "Public" : "Private")}</span>
            </span>
            <span className={styles.tilePeriod}>{item.monthYearLabel}</span>
            <span className={styles.tileMetadata}>
              <span><EmployeeIcon size={14} />{item.employeeCount === 1 ? t("1 employee") : t("{0} employees", item.employeeCount)}</span>
              <span><ClockIcon size={14} />{item.assignedHoursText}</span>
            </span>
          </span>
          <ChevronRightIcon className={styles.tileChevron} size={16} />
        </NavLink>
      ))}
    </div>
    {summaries.length === 0 && <p>{t("No results")}</p>}
    {statistics.pivotRows.length > 0 && <PhoneContainerStatistics key={id} statistics={statistics} />}
  </ManagerPhonePage>;
}

type Statistics = ReturnType<typeof buildContainerStatistics>;

const statisticMetrics = [
  { key: "workDays", label: "Work Days", Icon: ScheduleDetailsIcon },
  { key: "freeDays", label: "Free Days", Icon: AvailabilityIcon },
  { key: "urlopDays", label: "Urlop Days", Icon: ScheduleIcon },
  { key: "hoursSum", label: "Hours", Icon: ClockIcon },
] as const;

function PhoneContainerStatistics({ statistics }: { statistics: Statistics }) {
  const [expanded, setExpanded] = useState(false);
  const panelId = useId();
  const countId = useId();
  return <section className={styles.statisticsCard} data-phone-container-statistics>
    <h2 className={styles.statisticsHeading} aria-label={t("Statistics")}>
      <button type="button" className={styles.statisticsToggle} aria-expanded={expanded} aria-controls={panelId}
        aria-describedby={countId} onClick={() => setExpanded(current => !current)}>
        <span className={styles.statisticsIcon}><StatisticsIcon size={19} /></span>
        <span>{t("Statistics")}</span>
        <span className={styles.statisticsCount} id={countId} aria-hidden="true">
          {statistics.totalEmployees === 1 ? t("1 employee") : t("{0} employees", statistics.totalEmployees)}
        </span>
        <span className={styles.statisticsChevronCircle}><ChevronRightIcon className={styles.statisticsChevron} size={16} /></span>
      </button>
    </h2>
    <div id={panelId} className={styles.statisticsDisclosure} data-expanded={expanded} inert={!expanded} aria-hidden={!expanded}>
      <div className={styles.statisticsClip}>
        <div className={styles.metrics}>
          {statistics.pivotRows.map((row, index) => (
            <article className={[styles.metricCard, row.isTotal ? styles.metricTotal : ""].filter(Boolean).join(" ")} key={index}>
              <h3>
                <span className={styles.metricIcon}>{row.isTotal ? <StatisticsIcon size={18} /> : <EmployeeIcon size={18} />}</span>
                <span>{row.isTotal ? t("Total") : row.employee}</span>
              </h3>
              <dl className={styles.metricValues}>
                {statisticMetrics.map(({ key, label, Icon }) => (
                  <div key={key}><dt><Icon size={14} />{t(label)}</dt><dd>{row[key]}</dd></div>
                ))}
              </dl>
              {statistics.shopHeaders.length > 0 && <StatisticsShops shops={statistics.shopHeaders} hoursByShop={row.hoursByShop} />}
            </article>
          ))}
        </div>
      </div>
    </div>
  </section>;
}

function StatisticsShops({ shops, hoursByShop }: { shops: Statistics["shopHeaders"]; hoursByShop: Record<string, string> }) {
  const [expanded, setExpanded] = useState(false);
  const panelId = useId();
  return <div>
    <button type="button" className={styles.shopToggle} aria-expanded={expanded} aria-controls={panelId}
      onClick={() => setExpanded(current => !current)}>
      <ShopIcon size={16} />{t("Shops")}<ChevronRightIcon className={styles.statisticsChevron} size={14} />
    </button>
    <div id={panelId} className={styles.statisticsDisclosure} data-expanded={expanded} inert={!expanded} aria-hidden={!expanded}>
      <div className={styles.statisticsClip}>
        <dl className={styles.shopValues}>
          {shops.map(shop => <div key={shop.key}><dt>{shop.name}</dt><dd>{hoursByShop[shop.key]}</dd></div>)}
        </dl>
      </div>
    </div>
  </div>;
}
