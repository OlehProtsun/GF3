import { request } from "@shared/api/httpClient";
import type { SaveShopInput } from "@entities/shops/model/types";
import type { SaveShopDto, ShopDto } from "./dto";

const endpoint = "shops";

function toSaveDto(input: SaveShopInput): SaveShopDto {
  return {
    name: input.name.trim(),
    address: input.address.trim(),
    description: input.description?.trim() || undefined,
  };
}

export const shopsApi = {
  list: (signal?: AbortSignal) => request<ShopDto[]>(endpoint, { signal }),
  byId: (id: number, signal?: AbortSignal) => request<ShopDto>(`${endpoint}/${id}`, { signal }),
  create: (payload: SaveShopInput) => request<ShopDto>(endpoint, { method: "POST", body: toSaveDto(payload) }),
  update: (id: number, payload: SaveShopInput) => request<void>(`${endpoint}/${id}`, { method: "PUT", body: toSaveDto(payload) }),
  remove: (id: number) => request<void>(`${endpoint}/${id}`, { method: "DELETE" }),
};
