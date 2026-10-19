import { request } from "@shared/api/httpClient";

export type PushConfig = { enabled: boolean; publicKey: string | null };
export type BrowserPushSubscription = { endpoint: string; keys: { p256dh: string; auth: string } };
export const employeePushApi = {
  getConfig: (signal?: AbortSignal) => request<PushConfig>("employee-push/config", { signal }),
  upsertSubscription: (body: BrowserPushSubscription, signal?: AbortSignal) => request<void>("employee-push/subscriptions", { method: "PUT", body, signal }),
  deleteSubscription: (endpoint: string, signal?: AbortSignal) => request<void>("employee-push/subscriptions", { method: "DELETE", body: { endpoint }, signal }),
};
