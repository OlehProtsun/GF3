/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext } from "react";
import type { PropsWithChildren } from "react";
import { usePublicHolidaySettingsQuery } from "@entities/public-holidays/api/queries";

const PublicHolidayVisibilityContext = createContext<boolean>(true);

export function usePublicHolidayVisibility(): boolean {
  return useContext(PublicHolidayVisibilityContext);
}

export function PublicHolidayVisibilityProvider({ children, enabled }: PropsWithChildren<{ enabled: boolean }>) {
  const query = usePublicHolidaySettingsQuery(enabled);
  return (
    <PublicHolidayVisibilityContext.Provider value={query.data?.showPublicHolidays ?? true}>
      {children}
    </PublicHolidayVisibilityContext.Provider>
  );
}
