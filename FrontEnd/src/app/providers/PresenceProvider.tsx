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
const managerDataChangedMethodName = "ManagerDataChanged";
const shiftSwapsChangedMethodName = "ShiftSwapsChanged";
const workflowLogCreatedMethodName = "WorkflowLogCreated";
const scheduleEditLockChangedMethodName = "ScheduleEditLockChanged";
const managerEditLockChangedMethodName = "ManagerEditLockChanged";
const setManagerEditLocksMethodName = "SetManagerEditLocks";
const managerEditLockCheckTimeoutMs = 8000;

type ScheduleChangedUpdate = {
  containerId: number;
  graphId: number;
  reason: string;
  changedAtUtc: string;
};

type ManagerDataChangedUpdate = {
  resourceType: string;
  resourceId?: string | null;
  containerId?: number | null;
  graphId?: number | null;
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
  lockedByManagerId?: number | null;
  changedAtUtc: string;
};

type ScheduleEditLockTarget = {
  containerId: number;
  graphId: number;
};

export type ManagerEditLockTarget = {
  resourceType: string;
  resourceId: string;
  containerId?: number | null;
  graphId?: number | null;
};

export type ManagerEditLockState = ManagerEditLockTarget & {
  isLocked: boolean;
  lockedBy?: string | null;
  lockedByManagerId?: number | null;
  changedAtUtc: string;
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
  setScheduleEditLocks: (locks: ScheduleEditLockTarget[]) => Promise<ManagerEditLockState[]>;
  setManagerEditLocks: (locks: ManagerEditLockTarget[]) => Promise<ManagerEditLockState[]>;
  managerEditLocks: Record<string, ManagerEditLockState>;
  notifications: EmployeeRealtimeNotification[];
  clearNotifications: () => void;
};

const RealtimeContext = createContext<RealtimeContextValue>({
  setScheduleEditLocks: async () => [],
  setManagerEditLocks: async () => [],
  managerEditLocks: {},
  notifications: [],
  clearNotifications: () => undefined,
});

export function useRealtime() {
  return useContext(RealtimeContext);
}

export const managerEditResourceTypes = {
  schedule: "schedule",
  availabilityGroup: "availability-group",
  employee: "employee",
  shop: "shop",
  container: "container",
  availabilityBind: "availability-bind",
  managerProfile: "manager-profile",
} as const;

export function buildManagerEditLockKey(target: Pick<ManagerEditLockTarget, "resourceType" | "resourceId">) {
  return `${target.resourceType.trim().toLowerCase()}:${target.resourceId.trim()}`;
}

export function buildManagerEditLockMessage(state: ManagerEditLockState | null | undefined, fallbackName: string) {
  const lockedBy = state?.lockedBy?.trim() || "another manager";
  return `${fallbackName} is currently being edited by ${lockedBy}. You cannot edit it right now.`;
}

function scheduleToManagerEditLockTarget(target: ScheduleEditLockTarget): ManagerEditLockTarget {
  return {
    resourceType: managerEditResourceTypes.schedule,
    resourceId: `${target.containerId}:${target.graphId}`,
    containerId: target.containerId,
    graphId: target.graphId,
  };
}

function managerEditLockStatesEqual(left: ManagerEditLockState | undefined, right: ManagerEditLockState) {
  if (!left) {
    return false;
  }

  return (
    left.resourceType === right.resourceType &&
    left.resourceId === right.resourceId &&
    left.containerId === right.containerId &&
    left.graphId === right.graphId &&
    left.isLocked === right.isLocked &&
    left.lockedBy === right.lockedBy &&
    left.lockedByManagerId === right.lockedByManagerId &&
    left.changedAtUtc === right.changedAtUtc
  );
}

function managerEditLockStatesCoverTargets(targets: ManagerEditLockTarget[], states: ManagerEditLockState[]) {
  const stateKeys = new Set(states.map(buildManagerEditLockKey));
  return targets.every(target => stateKeys.has(buildManagerEditLockKey(target)));
}

export function useManagerEditLocks(targets: ManagerEditLockTarget[]) {
  const { setManagerEditLocks, managerEditLocks } = useRealtime();
  const { session } = useAuth();
  const setManagerEditLocksRef = useRef(setManagerEditLocks);
  const targetKey = useMemo(
    () => targets.map(buildManagerEditLockKey).sort().join("|"),
    [targets],
  );
  const stableTargets = useMemo(() => targets, [targetKey]);
  const [resolvedTargetKey, setResolvedTargetKey] = useState("");
  const [timedOutTargetKey, setTimedOutTargetKey] = useState("");

  useEffect(() => {
    setManagerEditLocksRef.current = setManagerEditLocks;
  }, [setManagerEditLocks]);

  useEffect(() => {
    let isDisposed = false;
    const timeoutId = targetKey.length > 0
      ? window.setTimeout(() => {
        if (!isDisposed) {
          setTimedOutTargetKey(targetKey);
        }
      }, managerEditLockCheckTimeoutMs)
      : undefined;

    setResolvedTargetKey(targetKey.length === 0 ? targetKey : "");
    setTimedOutTargetKey("");

    void setManagerEditLocksRef.current(stableTargets).then((states) => {
      if (
        !isDisposed &&
        (targetKey.length === 0 || managerEditLockStatesCoverTargets(stableTargets, states))
      ) {
        setResolvedTargetKey(targetKey);
      }
    });

    return () => {
      isDisposed = true;
      if (timeoutId !== undefined) {
        window.clearTimeout(timeoutId);
      }
      void setManagerEditLocksRef.current([]);
    };
  }, [stableTargets, targetKey]);

  const states = useMemo(
    () => stableTargets
      .map(target => managerEditLocks[buildManagerEditLockKey(target)])
      .filter((state): state is ManagerEditLockState => Boolean(state)),
    [managerEditLocks, stableTargets],
  );
  const hasLockStateForEveryTarget = targetKey.length > 0 && stableTargets.every(target =>
    Boolean(managerEditLocks[buildManagerEditLockKey(target)]),
  );
  const isWaitingForLockResponse = (
    targetKey.length > 0 &&
    resolvedTargetKey !== targetKey &&
    !hasLockStateForEveryTarget &&
    timedOutTargetKey !== targetKey
  );
  const lockedByOtherState = states.find(state =>
    state.isLocked &&
    (
      state.lockedByManagerId == null ||
      session?.managerId == null ||
      state.lockedByManagerId !== session.managerId
    ),
  ) ?? null;

  return {
    states,
    lockedByOtherState,
    isLockedByOther: lockedByOtherState !== null,
    isCheckingLocks: lockedByOtherState === null && isWaitingForLockResponse,
  };
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
  const [desiredManagerEditLocks, setDesiredManagerEditLocks] = useState<ManagerEditLockTarget[]>([]);
  const desiredManagerEditLocksRef = useRef<ManagerEditLockTarget[]>([]);
  const [managerEditLocks, setManagerEditLocksState] = useState<Record<string, ManagerEditLockState>>({});
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

  const applyManagerDataChanged = useEffectEvent((update: ManagerDataChangedUpdate) => {
    invalidateRealtimeBaselineQueries(queryClient);

    const parsedResourceId = update.resourceId ? Number(update.resourceId) : null;
    const resourceId = Number.isFinite(parsedResourceId) ? parsedResourceId : null;

    switch (update.resourceType) {
      case managerEditResourceTypes.schedule:
        if (update.containerId != null && update.graphId != null) {
          invalidateGraphRealtimeQueries(queryClient, update.containerId, update.graphId);
        }
        break;

      case managerEditResourceTypes.container:
        invalidateRealtimeQuery(queryClient, queryKeys.containers.all);
        if (resourceId !== null) {
          invalidateRealtimeQuery(queryClient, queryKeys.containers.byId(resourceId));
          invalidateRealtimeQuery(queryClient, queryKeys.containers.graphs(resourceId));
          invalidateRealtimeQuery(queryClient, queryKeys.containers.schedulePresets(resourceId));
          invalidateRealtimeQuery(queryClient, queryKeys.containers.graphRecordsPrefix(resourceId));
        }
        break;

      case managerEditResourceTypes.availabilityGroup:
        invalidateRealtimeQuery(queryClient, queryKeys.availabilityGroups.all);
        invalidateRealtimeQuery(queryClient, queryKeys.containers.all);
        if (resourceId !== null) {
          invalidateRealtimeQuery(queryClient, queryKeys.availabilityGroups.byId(resourceId));
          invalidateRealtimeQuery(queryClient, queryKeys.availabilityGroups.items(resourceId));
          invalidateRealtimeQuery(queryClient, queryKeys.availabilityGroups.members(resourceId));
          invalidateRealtimeQuery(queryClient, queryKeys.availabilityGroups.slots(resourceId));
        }
        break;

      case managerEditResourceTypes.employee:
        invalidateRealtimeQuery(queryClient, queryKeys.employees.all);
        invalidateRealtimeQuery(queryClient, queryKeys.availabilityGroups.all);
        invalidateRealtimeQuery(queryClient, queryKeys.containers.all);
        invalidateRealtimeQuery(queryClient, queryKeys.employeeSchedules.all);
        invalidateRealtimeQuery(queryClient, queryKeys.shiftSwaps.all);
        if (resourceId !== null) {
          invalidateRealtimeQuery(queryClient, queryKeys.employees.byId(resourceId));
        }
        break;

      case managerEditResourceTypes.shop:
        invalidateRealtimeQuery(queryClient, queryKeys.shops.all);
        invalidateRealtimeQuery(queryClient, queryKeys.containers.all);
        if (resourceId !== null) {
          invalidateRealtimeQuery(queryClient, queryKeys.shops.byId(resourceId));
        }
        break;

      case managerEditResourceTypes.availabilityBind:
        invalidateRealtimeQuery(queryClient, queryKeys.availabilityBinds.all);
        if (resourceId !== null) {
          invalidateRealtimeQuery(queryClient, queryKeys.availabilityBinds.byId(resourceId));
        }
        break;

      case managerEditResourceTypes.managerProfile:
        invalidateRealtimeQuery(queryClient, queryKeys.managerProfile.me());
        invalidateRealtimeQuery(queryClient, queryKeys.managerProfile.list());
        break;

      default:
        break;
    }
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

  const applyManagerEditLockStates = useEffectEvent((states: ManagerEditLockState[]) => {
    setManagerEditLocksState(current => {
      const next = { ...current };
      let hasChanges = false;
      states.forEach(state => {
        const lockKey = buildManagerEditLockKey(state);
        if (!managerEditLockStatesEqual(current[lockKey], state)) {
          next[lockKey] = state;
          hasChanges = true;
        }
      });
      return hasChanges ? next : current;
    });
  });

  const applyManagerEditLockChanged = useEffectEvent((update: ManagerEditLockState) => {
    applyManagerEditLockStates([update]);

    if (update.resourceType === managerEditResourceTypes.schedule) {
      invalidateRealtimeQuery(queryClient, queryKeys.shiftSwaps.employee());
      invalidateRealtimeQuery(queryClient, queryKeys.employeeSchedules.all);
    }

    if (!update.isLocked) {
      const unlockedKey = buildManagerEditLockKey(update);
      const shouldTryAcquire = desiredManagerEditLocksRef.current.some(target =>
        buildManagerEditLockKey(target) === unlockedKey,
      );
      if (shouldTryAcquire) {
        void publishManagerEditLocks(desiredManagerEditLocksRef.current);
      }
    }
  });

  const publishManagerEditLocks = useCallback(async (locks: ManagerEditLockTarget[]) => {
    const connection = connectionRef.current;
    if (!connection || connection.state !== HubConnectionState.Connected || session?.role !== "manager") {
      return [];
    }

    try {
      const states = await connection.invoke<ManagerEditLockState[]>(setManagerEditLocksMethodName, locks);
      applyManagerEditLockStates(states);
      return states;
    } catch (error) {
      if (isDev) {
        console.warn("[Realtime] Could not publish manager edit locks.", error);
      }
      return [];
    }
  }, [applyManagerEditLockStates, session?.role]);

  const setManagerEditLocks = useCallback((locks: ManagerEditLockTarget[]) => {
    desiredManagerEditLocksRef.current = locks;
    setDesiredManagerEditLocks(locks);
    if (locks.length > 0) {
      setManagerEditLocksState(current => {
        const next = { ...current };
        let hasChanges = false;
        locks.forEach(lock => {
          const lockKey = buildManagerEditLockKey(lock);
          if (lockKey in next) {
            delete next[lockKey];
            hasChanges = true;
          }
        });
        return hasChanges ? next : current;
      });
    }
    return publishManagerEditLocks(locks);
  }, [publishManagerEditLocks]);

  const setScheduleEditLocks = useCallback((locks: ScheduleEditLockTarget[]) => {
    return setManagerEditLocks(locks.map(scheduleToManagerEditLockTarget));
  }, [setManagerEditLocks]);

  const clearNotifications = useCallback(() => {
    setNotifications([]);
  }, []);

  const realtimeContextValue = useMemo<RealtimeContextValue>(() => ({
    setScheduleEditLocks,
    setManagerEditLocks,
    managerEditLocks,
    notifications,
    clearNotifications,
  }), [clearNotifications, managerEditLocks, notifications, setManagerEditLocks, setScheduleEditLocks]);

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

    connection.on(managerDataChangedMethodName, (update: ManagerDataChangedUpdate) => {
      applyManagerDataChanged(update);
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

    connection.on(managerEditLockChangedMethodName, (update: ManagerEditLockState) => {
      applyManagerEditLockChanged(update);
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
      connection.off(managerDataChangedMethodName);
      connection.off(shiftSwapsChangedMethodName);
      connection.off(workflowLogCreatedMethodName);
      connection.off(scheduleEditLockChangedMethodName);
      connection.off(managerEditLockChangedMethodName);
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
    desiredManagerEditLocksRef.current = desiredManagerEditLocks;
    void publishManagerEditLocks(desiredManagerEditLocks);
  }, [connectionTick, desiredManagerEditLocks, publishManagerEditLocks]);

  return (
    <RealtimeContext.Provider value={realtimeContextValue}>
      {children}
    </RealtimeContext.Provider>
  );
}
