import { QueryClient, QueryClientProvider } from "../../src/shared/lib/tanstack/react-query";
import { AuthProvider, useAuth } from "../../src/app/providers/AuthProvider";
import { AvailabilityRelatedHintDialog } from "../../src/entities/availability-groups/ui/AvailabilityRelatedHintDialog";
import { AvailabilityTransferDialog } from "../../src/entities/availability-groups/ui/AvailabilityTransferDialog";
import { ContainerGraphVersionsDialog } from "../../src/entities/containers/ui/ContainerGraphVersionsDialog";
import { ShiftSwapHistoryDialog } from "../../src/entities/containers/ui/ShiftSwapHistoryDialog";
import { ContainerGraphManualColumnsCard } from "../../src/entities/containers/ui/ContainerGraphManualColumnsCard";
import { EmployeeCommunicationDialog } from "../../src/entities/communications/ui/EmployeeCommunicationDialog";
import { RegulationAcceptanceGate } from "../../src/entities/regulations/ui/RegulationAcceptanceGate";
import { EmployeeShiftCorrectionDialog } from "../../src/pages/employee-schedule/ui/EmployeeShiftCorrectionDialog";
import { SavingOverlay } from "../../src/shared/ui/SavingOverlay/SavingOverlay";
import { longName, source, schedule, swap } from "./viewport-data";

const noop = () => {};
const common = { open: true, onCancel: noop };
const client = new QueryClient();
// eslint-disable-next-line react-refresh/only-export-components -- Each fixture loads on a fresh page; it does not use HMR.
function PendingRegulation() {
  const { status } = useAuth();
  return status === "authenticated" ? <RegulationAcceptanceGate /> : null;
}
export const stressCases = {
  transferFull: <AvailabilityTransferDialog {...common} employeeId={1} employeeName="Aleksandra Nowak" year={2026} month={9} sources={[source]} isLoading={false} isPending={false} onConfirm={noop} />,
  availabilityRelated: <AvailabilityRelatedHintDialog {...common} employeeId={1} employeeName="Aleksandra Nowak" year={2026} month={9} source={source} highlightedDayOfMonths={[1, 2, 3]} />,
  versions: <QueryClientProvider client={client}><ContainerGraphVersionsDialog {...common} containerId={2} graphId={9} hasUnsavedChanges onCheckoutComplete={noop} /></QueryClientProvider>,
  history: <ShiftSwapHistoryDialog {...common} swap={swap} />,
  correction: <EmployeeShiftCorrectionDialog {...common} schedule={schedule} employeeId={1} existingRequests={[]} isSending={false} onSubmit={noop} />,
  communication: <QueryClientProvider client={client}><EmployeeCommunicationDialog employeeId={1} /></QueryClientProvider>,
  regulation: <AuthProvider><QueryClientProvider client={client}><PendingRegulation /></QueryClientProvider></AuthProvider>,
  saving: <SavingOverlay active delayMs={0} title="Saving changes" message={longName.repeat(5)} />,
  manual: <ContainerGraphManualColumnsCard columns={Array.from({ length: 12 }, (_, i) => ({ columnId: i + 1, label: `Extra team ${i + 1}`, cells: Object.fromEntries(Array.from({ length: 30 }, (_, d) => [d + 1, "08:00 - 16:00"])) }))} allowSwap year={2026} month={9} employees={[]} onAddColumn={noop} onDeleteColumn={noop} onPublishShift={noop} />,
};
