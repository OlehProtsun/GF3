import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useEffectEvent,
  useMemo,
  useRef,
  useState,
  type PropsWithChildren,
} from "react";
import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from "@microsoft/signalr";
import { useQueryClient, type QueryClient } from "@tanstack/react-query";
import { useAuth } from "@app/providers/AuthProvider";
import type {
  Employee,
  EmployeePresenceUpdate,
} from "@entities/employees/model/types";
import type { WorkflowLog } from "@entities/workflow-logs";
import {
  buildApiUrl,
  getAuthAccessToken,
} from "@shared/api/httpClient";
import { queryKeys } from "@shared/api/queryKeys";
import { getEmployeeOpenShiftNotificationId } from "@shared/lib/employeeNotificationReadState";
import { isDev } from "@shared/lib/isDev";

const presenceHubMethodName = "PresenceChanged";
const scheduleChangedMethodName = "ScheduleChanged";
const shiftSwapsChangedMethodName = "ShiftSwapsChanged";
const workflowLogCreatedMethodName = "WorkflowLogCreated";
const scheduleEditLockChangedMethodName = "ScheduleEditLockChanged";
const setScheduleEditLocksMethodName = "SetScheduleEditLocks";

type ScheduleChangedUpdate = {
  containerId: number;
  graphId: number;
  reason: string;
  changedAtUtc: string;
};

type ShiftSwapsChangedUpdate = {
  containerId?: number | null;
  graphId?: number | null;
  scheduleId?: number | null;
  shiftSwapId?: number | null;
  reason: string;
  changedAtUtc: string;
};

type ScheduleEditLockChangedUpdate = {
  containerId: number;
  graphId: number;
  isLocked: boolean;
  lockedBy?: string | null;
  changedAtUtc: string;
};

type ScheduleEditLockTarget = {
  containerId: number;
  graphId: number;
};

export type EmployeeRealtimeNotification = {
  id: string;
  kind: "schedule" | "shiftSwap";
  reason: string;
  occurredAtUtc: string;
  containerId?: number | null;
  graphId?: number | null;
  scheduleId?: number | null;
  shiftSwapId?: number | null;
};

type RealtimeContextValue = {
  setScheduleEditLocks: (locks: ScheduleEditLockTarget[]) => void;
  notifications: EmployeeRealtimeNotification[];
  clearNotifications: () => void;
};

const RealtimeContext = createContext<RealtimeContextValue>({
  setScheduleEditLocks: () => undefined,
  notifications: [],
  clearNotifications: () => undefined,
});

export function useRealtime() {
  return useContext(RealtimeContext);
}

function invalidateRealtimeQuery(queryClient: QueryClient, queryKey: readonly unknown[]) {
  void queryClient.invalidateQueries({
    queryKey,
  });
}

function invalidateRealtimeBaselineQueries(queryClient: QueryClient) {
  invalidateRealtimeQuery(queryClient, queryKeys.home.dashboard());
  invalidateRealtimeQuery(queryClient, queryKeys.containers.all);
  invalidateRealtimeQuery(queryClient, queryKeys.employeeSchedules.all);
  invalidateRealtimeQuery(queryClient, queryKeys.shiftSwaps.all);
  invalidateRealtimeQuery(queryClient, queryKeys.workflowLogs.all);
}

function invalidateGraphRealtimeQueries(queryClient: QueryClient, containerId: number, graphId: number) {
  invalidateRealtimeBaselineQueries(queryClient);
  invalidateRealtimeQuery(queryClient, queryKeys.containers.byId(containerId));
  invalidateRealtimeQuery(queryClient, queryKeys.containers.graphs(containerId));
  invalidateRealtimeQuery(queryClient, queryKeys.containers.graphById(containerId, graphId));
  invalidateRealtimeQuery(queryClient, queryKeys.containers.graphSlots(containerId, graphId));
  invalidateRealtimeQuery(queryClient, queryKeys.containers.graphSlotsBatches(containerId));
  invalidateRealtimeQuery(queryClient, queryKeys.containers.graphEmployees(containerId, graphId));
  invalidateRealtimeQuery(queryClient, queryKeys.containers.graphCellStyles(containerId, graphId));
  invalidateRealtimeQuery(queryClient, queryKeys.containers.graphRecordsPrefix(containerId));
}

function isOpenShiftPostedReason(reason: string) {
  return reason === "manager-manual-shift-offer-created";
}

function patchEmployeePresence<T extends Pick<Employee, "id" | "isOnline" | "lastLoginAtUtc">>(
  employee: T,
  update: EmployeePresenceUpdate,
): T {
  if (employee.id !== update.employeeId) {
    return employee;
  }

  return {
    ...employee,
    isOnline: update.isOnline,
    lastLoginAtUtc: update.lastLoginAtUtc ?? employee.lastLoginAtUtc,
  };
}

export function PresenceProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient();
  const { status, session } = useAuth();
  const connectionRef = useRef<HubConnection | null>(null);
  const [connectionTick, setConnectionTick] = useState(0);
  const [desiredScheduleEditLocks, setDesiredScheduleEditLocks] = useState<ScheduleEditLockTarget[]>([]);
  const [notifications, setNotifications] = useState<EmployeeRealtimeNotification[]>([]);

  useEffect(() => {
    setNotifications([]);
  }, [session?.role, session?.userName]);

  const pushEmployeeNotification = useEffectEvent((notification: EmployeeRealtimeNotification) => {
    if (session?.role !== "employee") {
      return;
    }

    setNotifications(current => [
      notification,
      ...current.filter(item => item.id !== notification.id),
    ].slice(0, 80));
  });

  const applyPresenceUpdate = useEffectEvent((update: EmployeePresenceUpdate) => {
    const employeeQueryKey = queryKeys.employees.byId(update.employeeId);
    const employeeState = queryClient.getQueryState<Employee>(employeeQueryKey);
    if (employeeState.data) {
      queryClient.setQueryData(employeeQueryKey, patchEmployeePresence(employeeState.data, update));
    }

    invalidateRealtimeQuery(queryClient, queryKeys.employees.all);
  });

  const applyScheduleChanged = useEffectEvent((update: ScheduleChangedUpdate) => {
    invalidateGraphRealtimeQueries(queryClient, update.containerId, update.graphId);
  });

  const applyShiftSwapsChanged = useEffectEvent((update: ShiftSwapsChangedUpdate) => {
    invalidateRealtimeQuery(queryClient, queryKeys.shiftSwaps.all);
    invalidateRealtimeQuery(queryClient, queryKeys.employeeSchedules.all);

    if (update.containerId != null && update.graphId != null) {
      invalidateRealtimeQuery(queryClient, queryKeys.shiftSwaps.graphLog(update.containerId, update.graphId));
    }

    if (isOpenShiftPostedReason(update.reason)) {
      const scheduleId = update.scheduleId ?? update.graphId ?? null;
      const notificationIdSource = update.shiftSwapId ?? scheduleId;
      pushEmployeeNotification({
        id: getEmployeeOpenShiftNotificationId(notificationIdSource),
        kind: "shiftSwap",
        reason: update.reason,
        occurredAtUtc: update.changedAtUtc,
        containerId: update.containerId,
        graphId: update.graphId,
        scheduleId,
        shiftSwapId: update.shiftSwapId,
      });
    }
  });

  const applyWorkflowLogCreated = useEffectEvent((entry: WorkflowLog) => {
    const logsQueryKey = queryKeys.workflowLogs.list();
    const current = queryClient.getQueryState<WorkflowLog[]>(logsQueryKey).data;

    if (current) {
      queryClient.setQueryData(
        logsQueryKey,
        [entry, ...current.filter(item => item.id !== entry.id)].slice(0, 200),
      );
    }

    invalidateRealtimeQuery(queryClient, queryKeys.workflowLogs.all);
  });

  const applyScheduleEditLockChanged = useEffectEvent((_update: ScheduleEditLockChangedUpdate) => {
    invalidateRealtimeQuery(queryClient, queryKeys.shiftSwaps.employee());
    invalidateRealtimeQuery(queryClient, queryKeys.employeeSchedules.all);
  });

  const publishScheduleEditLocks = useCallback(async (locks: ScheduleEditLockTarget[]) => {
    const connection = connectionRef.current;
    if (!connection || connection.state !== HubConnectionState.Connected || session?.role !== "manager") {
      return;
    }

    try {
      await connection.invoke(setScheduleEditLocksMethodName, locks);
    } catch (error) {
      if (isDev) {
        console.warn("[Realtime] Could not publish schedule edit locks.", error);
      }
    }
  }, [session?.role]);

  const setScheduleEditLocks = useCallback((locks: ScheduleEditLockTarget[]) => {
    setDesiredScheduleEditLocks(locks);
    void publishScheduleEditLocks(locks);
  }, [publishScheduleEditLocks]);

  const clearNotifications = useCallback(() => {
    setNotifications([]);
  }, []);

  const realtimeContextValue = useMemo<RealtimeContextValue>(() => ({
    setScheduleEditLocks,
    notifications,
    clearNotifications,
  }), [clearNotifications, notifications, setScheduleEditLocks]);

  useEffect(() => {
    if (status !== "authenticated" || !session || !getAuthAccessToken()) {
      return;
    }

    const sessionRole = session.role;

    const connection = new HubConnectionBuilder()
      .withUrl(buildApiUrl("realtime/presence"), {
        accessTokenFactory: () => getAuthAccessToken() ?? "",
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(isDev ? LogLevel.Warning : LogLevel.Error)
      .build();
    connectionRef.current = connection;

    if (sessionRole === "manager") {
      connection.on(presenceHubMethodName, (update: EmployeePresenceUpdate) => {
        applyPresenceUpdate(update);
      });

      connection.onreconnected(() => {
        invalidateRealtimeQuery(queryClient, queryKeys.employees.all);
        setConnectionTick(tick => tick + 1);
      });
    }

    connection.on(scheduleChangedMethodName, (update: ScheduleChangedUpdate) => {
      applyScheduleChanged(update);
    });

    connection.on(shiftSwapsChangedMethodName, (update: ShiftSwapsChangedUpdate) => {
      applyShiftSwapsChanged(update);
    });

    connection.on(workflowLogCreatedMethodName, (entry: WorkflowLog) => {
      applyWorkflowLogCreated(entry);
    });

    connection.on(scheduleEditLockChangedMethodName, (update: ScheduleEditLockChangedUpdate) => {
      applyScheduleEditLockChanged(update);
    });

    connection.onreconnected(() => {
      invalidateRealtimeBaselineQueries(queryClient);
      setConnectionTick(tick => tick + 1);
    });

    let isDisposed = false;

    async function startConnection() {
      try {
        await connection.start();
        if (!isDisposed) {
          setConnectionTick(tick => tick + 1);
        }
        if (!isDisposed && sessionRole === "manager") {
          invalidateRealtimeQuery(queryClient, queryKeys.employees.all);
        }
      } catch (error) {
        if (isDev && !isDisposed) {
          console.warn("[Presence] SignalR connection failed.", error);
        }
      }
    }

    void startConnection();

    return () => {
      isDisposed = true;
      connection.off(presenceHubMethodName);
      connection.off(scheduleChangedMethodName);
      connection.off(shiftSwapsChangedMethodName);
      connection.off(workflowLogCreatedMethodName);
      connection.off(scheduleEditLockChangedMethodName);
      if (connectionRef.current === connection) {
        connectionRef.current = null;
      }
      void connection.stop();
    };
  }, [
    queryClient,
    status,
    session,
  ]);

  useEffect(() => {
    void publishScheduleEditLocks(desiredScheduleEditLocks);
  }, [connectionTick, desiredScheduleEditLocks, publishScheduleEditLocks]);

  return (
    <RealtimeContext.Provider value={realtimeContextValue}>
      {children}
    </RealtimeContext.Provider>
  );
}
