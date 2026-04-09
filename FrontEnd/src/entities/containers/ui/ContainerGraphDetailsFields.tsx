import { useMemo, type ReactNode } from "react";
import type { Employee } from "@entities/employees/model/types";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
import type {
  ContainerGraphFormErrors,
  ContainerGraphFormState,
} from "@entities/containers/model/graphForm";
import type { Shop } from "@entities/shops/model/types";
import { NumberStepperInput } from "@shared/ui/components/NumberStepperInput";
import { SearchableSelect, type SearchableSelectOption } from "@shared/ui/components/SearchableSelect";
import { LabeledField, TextInput } from "@shared/ui/forms/Field";
import styles from "./ContainerGraphEditor.module.css";

const MONTH_OPTIONS = [
  { value: "1", label: "January" },
  { value: "2", label: "February" },
  { value: "3", label: "March" },
  { value: "4", label: "April" },
  { value: "5", label: "May" },
  { value: "6", label: "June" },
  { value: "7", label: "July" },
  { value: "8", label: "August" },
  { value: "9", label: "September" },
  { value: "10", label: "October" },
  { value: "11", label: "November" },
  { value: "12", label: "December" },
] as const;

type ScheduleDetailsEmployeeRow = {
  employeeId: number;
  minHoursMonth: string;
};

type ContainerGraphDetailsFieldsProps = {
  idPrefix?: string;
  form: ContainerGraphFormState;
  formErrors: ContainerGraphFormErrors;
  shops: Shop[];
  availabilityGroups: AvailabilityGroup[];
  employees: Employee[];
  graphEmployeeRows: ScheduleDetailsEmployeeRow[];
  topSlot?: ReactNode;
  showNameField?: boolean;
  showAvailabilityField?: boolean;
  showEmployeeMinHoursField?: boolean;
  selectDropdownPlacement?: "down" | "up";
  shopSelectShadow?: "default" | "soft";
  onFieldChange: (field: keyof ContainerGraphFormState) => (value: string) => void;
  onEmployeeMinHoursChange: (employeeId: number, value: string) => void;
};

function joinClassNames(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
}

function getStepperValue(rawValue: string, fallbackValue: number) {
  const parsedValue = Number.parseInt(rawValue, 10);
  return Number.isFinite(parsedValue) ? parsedValue : fallbackValue;
}

function getMonthOptionLabel(month: number) {
  return MONTH_OPTIONS.find(option => Number(option.value) === month)?.label ?? `Month ${month}`;
}

export function ContainerGraphDetailsFields({
  idPrefix = "graph",
  form,
  formErrors,
  shops,
  availabilityGroups,
  employees,
  graphEmployeeRows,
  topSlot,
  showNameField = true,
  showAvailabilityField = true,
  showEmployeeMinHoursField = true,
  selectDropdownPlacement = "down",
  shopSelectShadow = "default",
  onFieldChange,
  onEmployeeMinHoursChange,
}: ContainerGraphDetailsFieldsProps) {
  const hasScrollableMinHoursList = graphEmployeeRows.length > 5;
  const employeeById = useMemo(
    () => new Map(employees.map(employee => [employee.id, employee])),
    [employees],
  );
  const monthOptions = useMemo<SearchableSelectOption[]>(
    () =>
      MONTH_OPTIONS.map(option => ({
        value: option.value,
        label: option.label,
        keywords: `${option.value} ${option.label}`,
      })),
    [],
  );
  const shopOptions = useMemo<SearchableSelectOption[]>(
    () =>
      shops.map(shop => ({
        value: String(shop.id),
        label: shop.name,
        hint: shop.address,
        keywords: `${shop.id} ${shop.name} ${shop.address} ${shop.description ?? ""}`,
      })),
    [shops],
  );
  const availabilityOptions = useMemo<SearchableSelectOption[]>(
    () => [
      {
        value: "",
        label: "No availability group",
        hint: "Leave this schedule unlinked",
        keywords: "none no availability unlinked",
      },
      ...availabilityGroups.map(group => ({
        value: String(group.id),
        label: group.name,
        hint: `${getMonthOptionLabel(group.month)} ${group.year}`,
        keywords: `${group.id} ${group.name} ${group.month} ${group.year} ${getMonthOptionLabel(group.month)}`,
      })),
    ],
    [availabilityGroups],
  );

  return (
    <div className={joinClassNames(styles.formStack, styles.detailsForm)}>
      {topSlot ? <div className={styles.detailsTopSlot}>{topSlot}</div> : null}

      <div className={joinClassNames(styles.detailsRow, styles.detailsRowBalanced)}>
        {showNameField ? (
          <LabeledField id={`${idPrefix}-name`} label="Name*" error={formErrors.name} className={styles.detailsFieldCompact}>
            <TextInput
              id={`${idPrefix}-name`}
              className={styles.controlCompact}
              value={form.name}
              placeholder="Example: March 2026 Main Shop"
              aria-invalid={Boolean(formErrors.name)}
              aria-describedby={formErrors.name ? `${idPrefix}-name-error` : undefined}
              onChange={event => onFieldChange("name")(event.target.value)}
            />
          </LabeledField>
        ) : null}

        <LabeledField
          id={`${idPrefix}-people`}
          label="People / shift*"
          error={formErrors.peoplePerShift}
          className={joinClassNames(
            styles.detailsFieldCompact,
            !showNameField && styles.detailsFieldSpanFull,
          )}
        >
          <NumberStepperInput
            id={`${idPrefix}-people`}
            className={joinClassNames(
              styles.numberInput,
              styles.controlCompact,
              formErrors.peoplePerShift && styles.numberInputInvalid,
            )}
            value={getStepperValue(form.peoplePerShift, 1)}
            min={1}
            onChange={value => onFieldChange("peoplePerShift")(String(value))}
            ariaLabel="people per shift"
          />
        </LabeledField>
      </div>

      <div className={joinClassNames(styles.detailsRow, styles.detailsRowBalanced)}>
        <LabeledField id={`${idPrefix}-month`} label="Month*" error={formErrors.month} className={styles.detailsFieldCompact}>
          <SearchableSelect
            id={`${idPrefix}-month`}
            className={styles.controlMedium}
            size="field"
            value={form.month}
            options={monthOptions}
            placeholder="Select month..."
            dropdownTitle="Schedule months"
            fallbackHint=""
            searchEnabled={false}
            invalid={Boolean(formErrors.month)}
            dropdownPlacement={selectDropdownPlacement}
            ariaDescribedBy={formErrors.month ? `${idPrefix}-month-error` : undefined}
            ariaLabel="schedule months"
            onChange={value => onFieldChange("month")(value)}
          />
        </LabeledField>

        <LabeledField id={`${idPrefix}-year`} label="Year*" error={formErrors.year} className={styles.detailsFieldCompact}>
          <NumberStepperInput
            id={`${idPrefix}-year`}
            className={joinClassNames(
              styles.numberInput,
              styles.controlCompact,
              formErrors.year && styles.numberInputInvalid,
            )}
            value={getStepperValue(form.year, new Date().getFullYear())}
            min={2000}
            max={2100}
            onChange={value => onFieldChange("year")(String(value))}
            ariaLabel="schedule year"
          />
        </LabeledField>
      </div>

      <div className={joinClassNames(styles.detailsRow, styles.detailsRowBalanced)}>
        <LabeledField id={`${idPrefix}-shift1`} label="Shift 1*" error={formErrors.shift1Time} className={styles.detailsFieldCompact}>
          <TextInput
            id={`${idPrefix}-shift1`}
            className={styles.controlCompact}
            value={form.shift1Time}
            aria-invalid={Boolean(formErrors.shift1Time)}
            aria-describedby={formErrors.shift1Time ? `${idPrefix}-shift1-error` : undefined}
            onChange={event => onFieldChange("shift1Time")(event.target.value)}
          />
        </LabeledField>

        <LabeledField id={`${idPrefix}-shift2`} label="Shift 2*" error={formErrors.shift2Time} className={styles.detailsFieldCompact}>
          <TextInput
            id={`${idPrefix}-shift2`}
            className={styles.controlCompact}
            value={form.shift2Time}
            aria-invalid={Boolean(formErrors.shift2Time)}
            aria-describedby={formErrors.shift2Time ? `${idPrefix}-shift2-error` : undefined}
            onChange={event => onFieldChange("shift2Time")(event.target.value)}
          />
        </LabeledField>
      </div>

      <div className={joinClassNames(styles.detailsRow, styles.detailsRowBalanced)}>
        <LabeledField id={`${idPrefix}-max-hours`} label="Max hours / employee" error={formErrors.maxHoursPerEmpMonth} className={styles.detailsFieldCompact}>
          <NumberStepperInput
            id={`${idPrefix}-max-hours`}
            className={joinClassNames(
              styles.numberInput,
              styles.controlCompact,
              formErrors.maxHoursPerEmpMonth && styles.numberInputInvalid,
            )}
            value={getStepperValue(form.maxHoursPerEmpMonth, 160)}
            min={1}
            onChange={value => onFieldChange("maxHoursPerEmpMonth")(String(value))}
            ariaLabel="max hours per employee"
          />
        </LabeledField>

        <LabeledField id={`${idPrefix}-max-days`} label="Max consecutive days" error={formErrors.maxConsecutiveDays} className={styles.detailsFieldCompact}>
          <NumberStepperInput
            id={`${idPrefix}-max-days`}
            className={joinClassNames(
              styles.numberInput,
              styles.controlCompact,
              formErrors.maxConsecutiveDays && styles.numberInputInvalid,
            )}
            value={getStepperValue(form.maxConsecutiveDays, 0)}
            min={0}
            onChange={value => onFieldChange("maxConsecutiveDays")(String(value))}
            ariaLabel="max consecutive days"
          />
        </LabeledField>

        <LabeledField id={`${idPrefix}-max-full`} label="Max consecutive full" error={formErrors.maxConsecutiveFull} className={styles.detailsFieldCompact}>
          <NumberStepperInput
            id={`${idPrefix}-max-full`}
            className={joinClassNames(
              styles.numberInput,
              styles.controlCompact,
              formErrors.maxConsecutiveFull && styles.numberInputInvalid,
            )}
            value={getStepperValue(form.maxConsecutiveFull, 0)}
            min={0}
            onChange={value => onFieldChange("maxConsecutiveFull")(String(value))}
            ariaLabel="max consecutive full"
          />
        </LabeledField>

        <LabeledField id={`${idPrefix}-max-full-month`} label="Max full / month" error={formErrors.maxFullPerMonth} className={styles.detailsFieldCompact}>
          <NumberStepperInput
            id={`${idPrefix}-max-full-month`}
            className={joinClassNames(
              styles.numberInput,
              styles.controlCompact,
              formErrors.maxFullPerMonth && styles.numberInputInvalid,
            )}
            value={getStepperValue(form.maxFullPerMonth, 0)}
            min={0}
            onChange={value => onFieldChange("maxFullPerMonth")(String(value))}
            ariaLabel="max full per month"
          />
        </LabeledField>
      </div>

      <div className={joinClassNames(styles.detailsRow, styles.detailsRowBalanced)}>
        <LabeledField
          id={`${idPrefix}-shop`}
          label="Shop*"
          error={formErrors.shopId}
          className={joinClassNames(
            styles.detailsFieldFull,
            !showAvailabilityField && styles.detailsFieldSpanFull,
          )}
        >
          <SearchableSelect
            id={`${idPrefix}-shop`}
            className={styles.controlFull}
            value={form.shopId}
            options={shopOptions}
            placeholder={shops.length > 0 ? "Select shop..." : "No shops available"}
            dropdownTitle="Shop list"
            searchPlaceholder="Search shop..."
            emptyMessage="No shops match your search."
            invalid={Boolean(formErrors.shopId)}
            dropdownPlacement={selectDropdownPlacement}
            shadow={shopSelectShadow}
            ariaDescribedBy={formErrors.shopId ? `${idPrefix}-shop-error` : undefined}
            ariaLabel="shop list"
            onChange={value => onFieldChange("shopId")(value)}
          />
        </LabeledField>

        {showAvailabilityField ? (
          <LabeledField id={`${idPrefix}-availability`} label="Availability" error={formErrors.availabilityGroupId} className={styles.detailsFieldFull}>
            <SearchableSelect
              id={`${idPrefix}-availability`}
              className={styles.controlFull}
              value={form.availabilityGroupId}
              options={availabilityOptions}
              placeholder="Select availability..."
              dropdownTitle="Availability groups"
              searchPlaceholder="Search availability..."
              emptyMessage="No availability groups match your search."
              invalid={Boolean(formErrors.availabilityGroupId)}
              dropdownPlacement={selectDropdownPlacement}
              ariaDescribedBy={formErrors.availabilityGroupId ? `${idPrefix}-availability-error` : undefined}
              ariaLabel="availability groups"
              onChange={value => onFieldChange("availabilityGroupId")(value)}
            />
          </LabeledField>
        ) : null}
      </div>

      {showEmployeeMinHoursField ? (
        <div className={styles.detailsMinHoursBlock}>
          <div className={styles.detailsMinHoursHeader}>
            <span className={styles.detailsMinHoursTitle}>Min hours per employee</span>
            <span className={styles.detailsMinHoursMeta}>{`${graphEmployeeRows.length} assigned`}</span>
          </div>

          {graphEmployeeRows.length === 0 ? (
            <div className={styles.detailsMinHoursEmpty}>Add employees first to set monthly minimum hours.</div>
          ) : (
            <div
              className={joinClassNames(
                styles.detailsMinHoursList,
                hasScrollableMinHoursList && styles.detailsMinHoursListScrollable,
              )}
            >
              {graphEmployeeRows.map(row => {
                const employee = employeeById.get(row.employeeId);
                const employeeName = getEmployeeFullName(employee, `Employee ${row.employeeId}`);

                return (
                  <label key={`${idPrefix}-details-min-hours-${row.employeeId}`} className={styles.detailsMinHoursItem}>
                    <span className={styles.detailsMinHoursName}>{employeeName}</span>
                    <NumberStepperInput
                      className={joinClassNames(styles.numberInput, styles.detailsMinHoursInput)}
                      value={getStepperValue(row.minHoursMonth, 0)}
                      min={0}
                      onChange={value => onEmployeeMinHoursChange(row.employeeId, String(value))}
                      ariaLabel={`${employeeName} min hours per month`}
                    />
                  </label>
                );
              })}
            </div>
          )}
        </div>
      ) : null}
    </div>
  );
}
