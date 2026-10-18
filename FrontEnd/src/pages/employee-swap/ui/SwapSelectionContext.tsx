import { t } from "@shared/i18n";

export function SwapSelectionContext({ message, employeeName, own = false }: {
  message: string; employeeName: string; own?: boolean;
}) {
  const [before, after] = t(message).split("{0}");
  return <>{before}<strong style={{ color: own ? "#246bff" : undefined, fontWeight: 600, overflowWrap: "anywhere" }}>{employeeName}</strong>{after}</>;
}
