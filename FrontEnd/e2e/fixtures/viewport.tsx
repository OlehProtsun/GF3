import { stressCases } from "./viewport-stress";
import { IosButton } from "../../src/shared/ui/components/IosButton";
import { RecordTile } from "../../src/shared/ui/components/RecordTile";
import { ProfileSummaryCard } from "../../src/shared/ui/components/ProfileSummaryCard";
import { AvailabilityDateTimeField } from "../../src/entities/availability-groups/ui/AvailabilityPublicationCard";
import { createRoot } from "react-dom/client";
import "../../src/index.css";
import { ConfirmDialog } from "../../src/shared/ui/ConfirmDialog/ConfirmDialog";
import { AvailabilityGroupFormDialog } from "../../src/entities/availability-groups/ui/AvailabilityGroupFormDialog";
import { AvailabilityTransferDialog } from "../../src/entities/availability-groups/ui/AvailabilityTransferDialog";
import { ContainerGraphColorDialog } from "../../src/entities/containers/ui/ContainerGraphColorDialog";
import { ContainerGraphPresetDialog } from "../../src/entities/containers/ui/ContainerGraphPresetDialog";
import { ContainerGraphRelatedHintDialog } from "../../src/entities/containers/ui/ContainerGraphRelatedHintDialog";
import { ContainerGraphHighlightColorDialog } from "../../src/entities/containers/ui/ContainerGraphHighlightColorDialog";
import { EmployeeScheduleColumnOrderDialog } from "../../src/pages/employee-schedule/ui/EmployeeScheduleColumnOrderDialog";
import { ContainerGraphPresetSelect } from "../../src/entities/containers/ui/ContainerGraphPresetSelect";
import { SearchableSelect } from "../../src/shared/ui/components/SearchableSelect/SearchableSelect";
import { createInitialGraphForm } from "../../src/entities/containers/model/graphForm";

const noop = () => {};
const save = async () => {};
const common = { open: true, onCancel: noop, onSave: save };
const columns = Array.from({ length: 25 }, (_, i) => ({ employeeId: i + 1, label: `Employee ${i + 1}`, kind: "employee" as const, manualColumnId: null, graphEmployeeId: null, minHoursMonth: null, totalMinutes: 0, totalText: "" }));
const cases = {
  longText: <div style={{ width: "100%", maxWidth: 400, boxSizing: "border-box", padding: 16, display: "grid", gap: 16 }}>
    <IosButton label={"LongActionLabel".repeat(20)} icon={<span>+</span>} />
    <RecordTile title={"LongEmployeeName".repeat(20)} description={"LongDescription".repeat(20)} metaItems={[{ label: "Email", value: "longemail".repeat(20) + "@example.com" }]} />
    <ProfileSummaryCard sectionTitle="Profile" name={"LongEmployeeName".repeat(20)} subtitle={"longusername".repeat(20)} details={[{ label: "Email", value: "longemail".repeat(20) + "@example.com" }]} />
  </div>,
  ...stressCases,
  confirm: <ConfirmDialog {...common} title="Confirm changes" message={"A detailed explanation of the changes and their consequences. ".repeat(35)} onConfirm={noop} />,
  group: <AvailabilityGroupFormDialog {...common} mode="create" isSubmitting={false} submitError={"Please review the availability group settings. ".repeat(15)} />,
  transfer: <AvailabilityTransferDialog {...common} employeeId={1} employeeName="Test Employee" year={2026} month={9} sources={[]} isLoading={false} isPending={false} onConfirm={noop} />,
  colors: <ContainerGraphColorDialog {...common} mode="fill" value="#dbeafe" fillColorBinds={[]} textColorBinds={[]} reservedValueBindKeys={new Set()} isFillColorBindBusy={false} isTextColorBindBusy={false} onBindFillColor={save} onBindTextColor={save} onDeleteFillColorBind={save} onDeleteTextColorBind={save} />,
  preset: <ContainerGraphPresetDialog {...common} initialForm={createInitialGraphForm()} shops={[]} />,
  related: <ContainerGraphRelatedHintDialog {...common} graphName="September schedule" employeeName="Test Employee" year={2026} month={9} dayLabel="Monday" currentCellMap={{}} detail={{ employeeId: 1, dayOfMonth: 1, visualHint: "Related shift", relatedGraphs: [{ graphId: 2, graphName: "Related schedule", intervals: [], intervalsText: "", dayValues: [] }] }} />,
  highlight: <ContainerGraphHighlightColorDialog {...common} value="#DBEAFE" eyebrow="Schedule" title="Highlight color" inputLabel="Color" isSaving={false} />,
  columns: <EmployeeScheduleColumnOrderDialog {...common} columns={columns} defaultColumns={columns} activeEmployeeId={1} />,
  select: <div style={{ position: "fixed", bottom: 20, right: 16, width: 300 }}><SearchableSelect value="" options={Array.from({ length: 30 }, (_, i) => ({ value: String(i), label: `Option ${i + 1}` }))} placeholder="Choose option" dropdownTitle="Options" onChange={noop} /></div>,
  date: <div style={{ position: "fixed", bottom: 20, right: 16, width: 200, overflow: "hidden" }}><AvailabilityDateTimeField id="date" label="Date" value="2026-09-27" defaultTime="09:00" dateOnly onChange={noop} /></div>,
  presets: <div style={{ position: "fixed", bottom: 20, right: 16, width: 300 }}><ContainerGraphPresetSelect presets={[]} selectedPresetId={null} shops={[]} isLoading={false} onSelect={noop} onAddPreset={noop} /></div>,
};
const name = new URLSearchParams(location.search).get("case") as keyof typeof cases;
createRoot(document.getElementById("root")!).render(cases[name] ?? cases.colors);
