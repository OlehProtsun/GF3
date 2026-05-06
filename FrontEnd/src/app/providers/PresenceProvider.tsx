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
import { useQueryClient } from "@tanstack/react-query";
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

type RealtimeContextValue = {
  setScheduleEditLocks: (locks: ScheduleEditLockTarget[]) => void;
};

const RealtimeContext = createContext<RealtimeContextValue>({
  setScheduleEditLocks: () => undefined,
});

export function useRealtime() {
  return useContext(RealtimeContext);
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

  const applyPresenceUpdate = useEffectEvent((update: EmployeePresenceUpdate) => {
    const employeeQueryKey = queryKeys.employees.byId(update.employeeId);
    const employeeState = queryClient.getQueryState<Employee>(employeeQueryKey);
    if (employeeState.data) {
      queryClient.setQueryData(employeeQueryKey, patchEmployeePresence(employeeState.data, update));
    }

    void queryClient.invalidateQueries({
      queryKey: queryKeys.employees.all,
    });
  });

  const applyScheduleChanged = useEffectEvent((update: ScheduleChangedUpdate) => {
    void queryClient.invalidateQueries({
      queryKey: queryKeys.containers.all,
    });
    void queryClient.invalidateQueries({
      queryKey: queryKeys.containers.graphs(update.containerId),
    });
    void queryClient.invalidateQueries({
      queryKey: queryKeys.employeeSchedules.all,
    });
    void queryClient.invalidateQueries({
      queryKey: queryKeys.shiftSwaps.all,
    });
  });

  const applyShiftSwapsChanged = useEffectEvent((update: ShiftSwapsChangedUpdate) => {
    void queryClient.invalidateQueries({
      queryKey: queryKeys.shiftSwaps.all,
    });
    void queryClient.invalidateQueries({
      queryKey: queryKeys.employeeSchedules.all,
    });

    if (update.containerId && update.graphId) {
      void queryClient.invalidateQueries({
        queryKey: queryKeys.shiftSwaps.graphLog(update.containerId, update.graphId),
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

    void queryClient.invalidateQueries({
      queryKey: queryKeys.workflowLogs.all,
    });
  });

  const applyScheduleEditLockChanged = useEffectEvent((_update: ScheduleEditLockChangedUpdate) => {
    void queryClient.invalidateQueries({
      queryKey: queryKeys.shiftSwaps.employee(),
    });
    void queryClient.invalidateQueries({
      queryKey: queryKeys.employeeSchedules.all,
    });
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

  const realtimeContextValue = useMemo<RealtimeContextValue>(() => ({
    setScheduleEditLocks,
  }), [setScheduleEditLocks]);

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
        void queryClient.invalidateQueries({
          queryKey: queryKeys.employees.all,
        });
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
          void queryClient.invalidateQueries({
            queryKey: queryKeys.employees.all,
          });
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
    applyPresenceUpdate,
    applyScheduleChanged,
    applyShiftSwapsChanged,
    applyWorkflowLogCreated,
    applyScheduleEditLockChanged,
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
