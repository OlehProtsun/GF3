import { t } from "@shared/i18n";
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState, type CSSProperties } from "react";
import { createPortal } from "react-dom";
import type { SchedulePreset } from "@entities/containers/model/types";
import type { Shop } from "@entities/shops/model/types";
import { IosButton } from "@shared/ui/components/IosButton";
import { ArrowIcon, CheckIcon, PlusIcon, SearchIcon } from "@shared/ui/icons";
import styles from "./ContainerGraphPresetSelect.module.css";

const MONTH_LABELS = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
] as const;

type ContainerGraphPresetSelectProps = {
  presets: SchedulePreset[];
  selectedPresetId: number | null;
  shops: Shop[];
  isLoading: boolean;
  onSelect: (presetId: number) => void;
  onAddPreset: () => void;
};

function joinClassNames(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
}

function getMonthLabel(month: number) {
  return t(MONTH_LABELS[Math.min(Math.max(month - 1, 0), MONTH_LABELS.length - 1)]) ?? t("Month {0}", month);
}

function getPresetHint(preset: SchedulePreset, shopNameById: Map<number, string>) {
  const shopName = shopNameById.get(preset.shopId) ?? t("Shop {0}", preset.shopId);
  return `${preset.scheduleName} - ${shopName} - ${getMonthLabel(preset.month)} ${preset.year}`;
}

function getPresetMeta(preset: SchedulePreset) {
  const employeeOverrides = preset.employees.length;
  const employeeLabel = employeeOverrides === 1 ? t("1 employee override") : t("{0} employee overrides", employeeOverrides);
  return t("{0} / {1} - {2} per shift - {3}", preset.shift1Time, preset.shift2Time, preset.peoplePerShift, employeeLabel);
}

export function ContainerGraphPresetSelect({
  presets,
  selectedPresetId,
  shops,
  isLoading,
  onSelect,
  onAddPreset,
}: ContainerGraphPresetSelectProps) {
  const triggerRef = useRef<HTMLDivElement | null>(null);
  const dropdownRef = useRef<HTMLDivElement | null>(null);
  const searchInputRef = useRef<HTMLInputElement | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [searchText, setSearchText] = useState("");
  const [dropdownStyle, setDropdownStyle] = useState<CSSProperties>({});

  const shopNameById = useMemo(
    () => new Map(shops.map(shop => [shop.id, shop.name])),
    [shops],
  );
  const selectedPreset = useMemo(
    () => presets.find(preset => preset.id === selectedPresetId) ?? null,
    [presets, selectedPresetId],
  );
  const filteredPresets = useMemo(() => {
    const normalizedSearchText = searchText.trim().toLowerCase();
    if (!normalizedSearchText) {
      return presets;
    }

    return presets.filter(preset => [
      preset.name,
      preset.scheduleName,
      getPresetHint(preset, shopNameById),
      getPresetMeta(preset),
      String(preset.shopId),
    ]
      .join(" ")
      .toLowerCase()
      .includes(normalizedSearchText));
  }, [presets, searchText, shopNameById]);
  const closeDropdown = useCallback(() => {
    setIsOpen(false);
    setSearchText("");
  }, []);
  const openDropdown = useCallback(() => {
    setSearchText("");
    setIsOpen(true);
  }, []);

  useLayoutEffect(() => {
    if (!isOpen || !triggerRef.current) {
      return;
    }

    const updateDropdownPosition = () => {
      const rect = triggerRef.current?.getBoundingClientRect();
      if (!rect) {
        return;
      }

      setDropdownStyle({
        left: rect.left,
        top: rect.bottom + 10,
        width: rect.width,
      });
    };

    updateDropdownPosition();
    window.addEventListener("resize", updateDropdownPosition);
    window.addEventListener("scroll", updateDropdownPosition, true);
    return () => {
      window.removeEventListener("resize", updateDropdownPosition);
      window.removeEventListener("scroll", updateDropdownPosition, true);
    };
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    searchInputRef.current?.focus();

    const handlePointerDown = (event: MouseEvent) => {
      if (!(event.target instanceof Node)) {
        return;
      }

      const insideTrigger = triggerRef.current?.contains(event.target);
      const insideDropdown = dropdownRef.current?.contains(event.target);
      if (!insideTrigger && !insideDropdown) {
        closeDropdown();
      }
    };

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        closeDropdown();
      }
    };

    window.addEventListener("mousedown", handlePointerDown);
    window.addEventListener("keydown", handleEscape);
    return () => {
      window.removeEventListener("mousedown", handlePointerDown);
      window.removeEventListener("keydown", handleEscape);
    };
  }, [closeDropdown, isOpen]);

  const handleSelect = (presetId: number) => {
    onSelect(presetId);
    closeDropdown();
  };

  const triggerLabel = selectedPreset?.name ?? t("Apply preset");
  const triggerHint = selectedPreset
    ? getPresetHint(selectedPreset, shopNameById)
    : isLoading
      ? t("Loading saved presets...")
      : presets.length > 0
        ? t("{0} presets ready to reuse", presets.length)
        : t("Save your first Schedule Details preset");

  const dropdown = isOpen
    ? createPortal(
      <div ref={dropdownRef} className={styles.dropdownPortal} style={dropdownStyle}>
        <div className={styles.dropdown}>
          <div className={styles.dropdownHeader}>
            <div className={styles.dropdownTitleBlock}>
              <span className={styles.dropdownTitle}>{t("Schedule presets")}</span>
              <span className={styles.dropdownHint}>{t("Reuse saved detail setups or add a new one.")}</span>
            </div>
            <span className={styles.dropdownCount}>{isLoading ? "..." : filteredPresets.length}</span>
          </div>

          <div className={styles.searchField}>
            <SearchIcon className={styles.searchIcon} />
            <input
              ref={searchInputRef}
              className={styles.searchInput}
              value={searchText}
              onChange={event => setSearchText(event.target.value)}
              placeholder={t("Search presets...")}
              aria-label={t("Search presets")}
            />
          </div>

          <div className={styles.optionList} role="listbox" aria-label={t("Schedule presets")}>
            {isLoading ? (
              <div className={styles.emptyState}>{t("Loading presets...")}</div>
            ) : filteredPresets.length > 0 ? (
              filteredPresets.map(preset => {
                const isSelected = preset.id === selectedPresetId;

                return (
                  <button
                    key={preset.id}
                    type="button"
                    role="option"
                    aria-selected={isSelected}
                    className={joinClassNames(styles.option, isSelected && styles.optionSelected)}
                    onClick={() => handleSelect(preset.id)}
                  >
                    <div className={styles.optionBody}>
                      <span className={styles.optionName}>{preset.name}</span>
                      <span className={styles.optionHint}>{getPresetHint(preset, shopNameById)}</span>
                      <span className={styles.optionMeta}>{getPresetMeta(preset)}</span>
                    </div>

                    {isSelected ? <CheckIcon size={16} className={styles.optionCheck} /> : null}
                  </button>
                );
              })
            ) : (
              <div className={styles.emptyState}>
                {presets.length === 0
                  ? t("No presets yet. Save one from your current Schedule Details.")
                  : t("No presets match your search.")}
              </div>
            )}
          </div>

          <div className={styles.footer}>
            <IosButton
              label={t("Add preset")}
              icon={<PlusIcon size={16} />}
              onClick={() => {
                closeDropdown();
                onAddPreset();
              }}
            />
          </div>
        </div>
      </div>,
      document.body,
    )
    : null;

  return (
    <>
      <div ref={triggerRef} className={styles.root}>
        <button
          type="button"
          className={styles.trigger}
          onClick={() => {
            if (isOpen) {
              closeDropdown();
              return;
            }

            openDropdown();
          }}
          aria-haspopup="listbox"
          aria-expanded={isOpen}
          aria-label={t("Open schedule preset list")}
        >
          <div className={styles.triggerText}>
            <span className={styles.triggerLabel}>{triggerLabel}</span>
            <span className={styles.triggerHint}>{triggerHint}</span>
          </div>

          <ArrowIcon size={16} className={joinClassNames(styles.triggerChevron, isOpen && styles.triggerChevronOpen)} />
        </button>
      </div>

      {dropdown}
    </>
  );
}
