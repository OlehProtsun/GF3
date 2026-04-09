import { useCallback, useState } from "react";

type DraftUpdater<TValue> = TValue | ((currentValue: TValue) => TValue);

type SyncedDraftState<TValue> = {
  sourceKey: string;
  value: TValue;
};

function resolveDraftUpdater<TValue>(updater: DraftUpdater<TValue>, currentValue: TValue) {
  return typeof updater === "function"
    ? (updater as (value: TValue) => TValue)(currentValue)
    : updater;
}

export function useSyncedDraft<TValue>(sourceKey: string, sourceValue: TValue) {
  const [draftState, setDraftState] = useState<SyncedDraftState<TValue>>(() => ({
    sourceKey,
    value: sourceValue,
  }));

  const value = draftState.sourceKey === sourceKey ? draftState.value : sourceValue;

  const setValue = useCallback(
    (updater: DraftUpdater<TValue>) => {
      setDraftState((currentDraftState) => {
        const currentValue =
          currentDraftState.sourceKey === sourceKey
            ? currentDraftState.value
            : sourceValue;

        return {
          sourceKey,
          value: resolveDraftUpdater(updater, currentValue),
        };
      });
    },
    [sourceKey, sourceValue],
  );

  const reset = useCallback(
    (nextValue: TValue = sourceValue) => {
      setDraftState({
        sourceKey,
        value: nextValue,
      });
    },
    [sourceKey, sourceValue],
  );

  return { value, setValue, reset };
}
