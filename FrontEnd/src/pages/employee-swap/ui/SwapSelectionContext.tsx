import { t } from "@shared/i18n";

export function SwapSelectionContext({ message, employeeName, own = false }: {
  message: "Select a schedule for {0}." | "Select a shift from {0}'s schedule to swap with yours." | "Choose a shift from {0}'s schedule."; employeeName: string; own?: boolean;
}) {
  const [before, after] = t(message).split("{0}");
  return <>{before}<strong style={{ color: own ? "#246bff" : undefined, fontWeight: 600, overflowWrap: "anywhere" }}>{employeeName}</strong>{after}</>;
}
