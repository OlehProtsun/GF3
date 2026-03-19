import { useCallback, useEffect, useMemo, useState } from "react";

type RecordId = string | number;

function readPinnedIds(storageKey: string) {
  if (typeof window === "undefined") {
    return [] as string[];
  }

  try {
    const rawValue = window.localStorage.getItem(storageKey);
    if (!rawValue) {
      return [] as string[];
    }

    const parsed = JSON.parse(rawValue);
    return Array.isArray(parsed) ? parsed.filter(value => typeof value === "string") : [];
  } catch {
    return [] as string[];
  }
}

export function usePinnedRecords<T extends { id: RecordId }>(storageKey: string, items: T[]) {
  const [pinnedIds, setPinnedIds] = useState<string[]>(() => readPinnedIds(storageKey));

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    window.localStorage.setItem(storageKey, JSON.stringify(pinnedIds));
  }, [pinnedIds, storageKey]);

  const pinnedIdSet = useMemo(() => new Set(pinnedIds), [pinnedIds]);

  const sortedItems = useMemo(() => {
    const pinnedItems: T[] = [];
    const regularItems: T[] = [];

    items.forEach(item => {
      if (pinnedIdSet.has(String(item.id))) {
        pinnedItems.push(item);
        return;
      }

      regularItems.push(item);
    });

    return [...pinnedItems, ...regularItems];
  }, [items, pinnedIdSet]);

  const togglePin = useCallback((id: RecordId) => {
    const stringId = String(id);

    setPinnedIds(currentIds => {
      if (currentIds.includes(stringId)) {
        return currentIds.filter(currentId => currentId !== stringId);
      }

      return [stringId, ...currentIds];
    });
  }, []);

  return {
    sortedItems,
    pinnedIdSet,
    togglePin,
  };
}
