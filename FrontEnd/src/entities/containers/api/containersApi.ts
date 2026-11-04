import { request } from "@shared/api/httpClient";
import type {
  ContainerDto,
  GenerateGraphRequestDto,
  GenerateGraphPreviewRequestDto,
  GenerateGraphResponseDto,
  GraphCellStyleDto,
  ManagerGraphFillColorBindDto,
  ManagerGraphTextColorBindDto,
  GraphDto,
  GraphEmployeeDto,
  GraphSlotDto,
  GraphVersionTreeDto,
  SaveContainerDto,
  SaveGraphDto,
  SaveGraphEmployeeDto,
  ReplaceGraphSlotsDto,
  SaveSchedulePresetDto,
  SaveManagerGraphFillColorBindDto,
  SaveManagerGraphTextColorBindDto,
  SaveGraphSlotDto,
  SchedulePresetDto,
  UpsertGraphCellStyleDto,
  UpdateGraphsPublicationDto,
} from "./dto";

const endpoint = "containers";

export const containersApi = {
  list: (signal?: AbortSignal) => request<ContainerDto[]>(endpoint, { signal }),
  byId: (id: number, signal?: AbortSignal) => request<ContainerDto>(`${endpoint}/${id}`, { signal }),
  create: (payload: SaveContainerDto) => request<ContainerDto>(endpoint, { method: "POST", body: payload }),
  update: (id: number, payload: SaveContainerDto) => request<void>(`${endpoint}/${id}`, { method: "PUT", body: payload }),
  remove: (id: number) => request<void>(`${endpoint}/${id}`, { method: "DELETE" }),

  listGraphs: (containerId: number, signal?: AbortSignal) => request<GraphDto[]>(`${endpoint}/${containerId}/graphs`, { signal }),
  graphById: (containerId: number, graphId: number, signal?: AbortSignal) => request<GraphDto>(`${endpoint}/${containerId}/graphs/${graphId}`, { signal }),
  createGraph: (containerId: number, payload: SaveGraphDto) => request<GraphDto>(`${endpoint}/${containerId}/graphs`, { method: "POST", body: payload }),
  updateGraph: (containerId: number, graphId: number, payload: SaveGraphDto) =>
    request<void>(`${endpoint}/${containerId}/graphs/${graphId}`, { method: "PUT", body: payload }),
  updateGraphsPublication: (containerId: number, payload: UpdateGraphsPublicationDto) =>
    request<void>(`${endpoint}/${containerId}/graphs/publication`, { method: "PUT", body: payload }),
  removeGraph: (containerId: number, graphId: number) => request<void>(`${endpoint}/${containerId}/graphs/${graphId}`, { method: "DELETE" }),
  listGraphVersions: (containerId: number, graphId: number, signal?: AbortSignal) =>
    request<GraphVersionTreeDto>(`${endpoint}/${containerId}/graphs/${graphId}/versions`, { signal }),
  commitGraphVersion: (containerId: number, graphId: number) =>
    request<GraphVersionTreeDto>(`${endpoint}/${containerId}/graphs/${graphId}/versions`, { method: "POST" }),
  checkoutGraphVersion: (containerId: number, graphId: number, versionId: number) =>
    request<GraphVersionTreeDto>(`${endpoint}/${containerId}/graphs/${graphId}/versions/${versionId}/checkout`, { method: "POST" }),
  removeGraphVersion: (containerId: number, graphId: number, versionId: number) =>
    request<void>(`${endpoint}/${containerId}/graphs/${graphId}/versions/${versionId}`, { method: "DELETE" }),
  listSchedulePresets: (containerId: number, signal?: AbortSignal) =>
    request<SchedulePresetDto[]>(`${endpoint}/${containerId}/schedule-presets`, { signal }),
  createSchedulePreset: (containerId: number, payload: SaveSchedulePresetDto) =>
    request<SchedulePresetDto>(`${endpoint}/${containerId}/schedule-presets`, { method: "POST", body: payload }),
  generateGraphPreview: (containerId: number, payload: GenerateGraphPreviewRequestDto) =>
    request<GenerateGraphResponseDto>(`${endpoint}/${containerId}/graphs/generate-preview`, { method: "POST", body: payload }),
  generateGraph: (containerId: number, graphId: number, payload: GenerateGraphRequestDto) =>
    request<GenerateGraphResponseDto>(`${endpoint}/${containerId}/graphs/${graphId}/generate`, { method: "POST", body: payload }),

  listGraphSlots: (containerId: number, graphId: number, signal?: AbortSignal) =>
    request<GraphSlotDto[]>(`${endpoint}/${containerId}/graphs/${graphId}/slots`, { signal }),
  replaceGraphSlots: (containerId: number, graphId: number, payload: ReplaceGraphSlotsDto) =>
    request<void>(`${endpoint}/${containerId}/graphs/${graphId}/slots`, { method: "PUT", body: payload }),
  createGraphSlot: (containerId: number, graphId: number, payload: SaveGraphSlotDto) =>
    request<GraphSlotDto>(`${endpoint}/${containerId}/graphs/${graphId}/slots`, { method: "POST", body: payload }),
  updateGraphSlot: (containerId: number, graphId: number, slotId: number, payload: SaveGraphSlotDto) =>
    request<void>(`${endpoint}/${containerId}/graphs/${graphId}/slots/${slotId}`, { method: "PUT", body: payload }),
  removeGraphSlot: (containerId: number, graphId: number, slotId: number) =>
    request<void>(`${endpoint}/${containerId}/graphs/${graphId}/slots/${slotId}`, { method: "DELETE" }),

  listGraphEmployees: (containerId: number, graphId: number, signal?: AbortSignal) =>
    request<GraphEmployeeDto[]>(`${endpoint}/${containerId}/graphs/${graphId}/employees`, { signal }),
  createGraphEmployee: (containerId: number, graphId: number, payload: SaveGraphEmployeeDto) =>
    request<GraphEmployeeDto>(`${endpoint}/${containerId}/graphs/${graphId}/employees`, { method: "POST", body: payload }),
  updateGraphEmployee: (containerId: number, graphId: number, graphEmployeeId: number, payload: SaveGraphEmployeeDto) =>
    request<void>(`${endpoint}/${containerId}/graphs/${graphId}/employees/${graphEmployeeId}`, { method: "PUT", body: payload }),
  removeGraphEmployee: (containerId: number, graphId: number, graphEmployeeId: number) =>
    request<void>(`${endpoint}/${containerId}/graphs/${graphId}/employees/${graphEmployeeId}`, { method: "DELETE" }),

  listGraphCellStyles: (containerId: number, graphId: number, signal?: AbortSignal) =>
    request<GraphCellStyleDto[]>(`${endpoint}/${containerId}/graphs/${graphId}/cell-styles`, { signal }),
  upsertGraphCellStyle: (containerId: number, graphId: number, payload: UpsertGraphCellStyleDto) =>
    request<GraphCellStyleDto>(`${endpoint}/${containerId}/graphs/${graphId}/cell-styles`, { method: "PUT", body: payload }),
  removeGraphCellStyle: (containerId: number, graphId: number, styleId: number) =>
    request<void>(`${endpoint}/${containerId}/graphs/${graphId}/cell-styles/${styleId}`, { method: "DELETE" }),
};

const managerGraphFillColorBindsEndpoint = "manager-graph-fill-color-binds";

export const managerGraphFillColorBindsApi = {
  list: (signal?: AbortSignal) =>
    request<ManagerGraphFillColorBindDto[]>(managerGraphFillColorBindsEndpoint, { signal }),
  upsert: (payload: SaveManagerGraphFillColorBindDto) =>
    request<ManagerGraphFillColorBindDto>(managerGraphFillColorBindsEndpoint, { method: "PUT", body: payload }),
  remove: (id: number) =>
    request<void>(`${managerGraphFillColorBindsEndpoint}/${id}`, { method: "DELETE" }),
};

const managerGraphTextColorBindsEndpoint = "manager-graph-text-color-binds";

export const managerGraphTextColorBindsApi = {
  list: (signal?: AbortSignal) =>
    request<ManagerGraphTextColorBindDto[]>(managerGraphTextColorBindsEndpoint, { signal }),
  upsert: (payload: SaveManagerGraphTextColorBindDto) =>
    request<ManagerGraphTextColorBindDto>(managerGraphTextColorBindsEndpoint, { method: "PUT", body: payload }),
  remove: (id: number) =>
    request<void>(`${managerGraphTextColorBindsEndpoint}/${id}`, { method: "DELETE" }),
};
