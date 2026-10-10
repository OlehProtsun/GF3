import { parsePhoneId } from "./parsePhoneId";
import { useLocation, useParams } from "react-router-dom";
import { useAvailabilityGroupsListQuery } from "@entities/availability-groups";
import { ContainerGraphProfileWorkspace, useContainerByIdQuery, useContainerGraphsQuery, useGraphByIdQuery, useGraphCellStylesQuery, useGraphEmployeesQuery, useGraphSlotsBatchQuery, useGraphSlotsQuery } from "@entities/containers";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import { useShopsListQuery } from "@entities/shops/api/queries";
import { queryKeys } from "@shared/api/queryKeys";
import { ManagerPhonePage } from "./ManagerPhonePage";
import { t } from "@shared/i18n";

export function ManagerPhoneGraphPage() {
  const location = useLocation();
  const params = useParams<{ containerId: string; graphId: string }>();
  const containerId = parsePhoneId(params.containerId);
  const graphId = parsePhoneId(params.graphId);
  const valid = containerId !== null && graphId !== null;
  const container = useContainerByIdQuery(containerId);
  const graphs = useContainerGraphsQuery(containerId, valid);
  const graph = useGraphByIdQuery(containerId, graphId, valid);
  const graphEmployees = useGraphEmployeesQuery(containerId, graphId, valid);
  const slots = useGraphSlotsQuery(containerId, graphId, valid);
  const cellStyles = useGraphCellStylesQuery(containerId, graphId, valid);
  const employees = useEmployeesListQuery({ refreshKey: location.key });
  const shops = useShopsListQuery({ refreshKey: location.key });
  const groups = useAvailabilityGroupsListQuery(location.key);
  const relatedGraphs = (graphs.data ?? []).filter(item => item.id !== graphId && item.year === graph.data?.year && item.month === graph.data?.month);
  const relatedSlots = useGraphSlotsBatchQuery(containerId, relatedGraphs.map(item => item.id), valid);
  return <ManagerPhonePage title={t("Schedule Profile")} backTo={containerId ? `/container/${containerId}` : "/container"} valid={valid} missing={!graph.data || !container.data}
    queries={[container, graphs, graph, graphEmployees, slots, cellStyles, employees, shops, groups, relatedSlots]}
    queryKeys={[queryKeys.containers.all, queryKeys.employees.all, queryKeys.shops.all, queryKeys.availabilityGroups.all]}>
    <ContainerGraphProfileWorkspace container={container.data} graph={graph.data} shop={(shops.data ?? []).find(item => item.id === graph.data?.shopId)}
      availabilityGroup={(groups.data ?? []).find(item => item.id === graph.data?.availabilityGroupId)} graphEmployees={graphEmployees.data ?? []}
      slots={slots.data ?? []} cellStyles={cellStyles.data ?? []} relatedGraphs={relatedGraphs} relatedGraphSlotsById={relatedSlots.data ?? {}}
      employeesById={new Map((employees.data ?? []).map(item => [item.id, item]))} compactSize showManagementActions={false}
      isLoading={false} hasLoadError={false} isDeleting={false} onEdit={() => {}} onDelete={() => {}} />
  </ManagerPhonePage>;
}
