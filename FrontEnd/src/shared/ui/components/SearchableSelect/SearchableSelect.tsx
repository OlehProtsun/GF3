import { t } from "@shared/i18n";
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState, type CSSProperties } from "react";
import { createPortal } from "react-dom";
import { ArrowIcon, CheckIcon, SearchIcon } from "@shared/ui/icons";
import styles from "./SearchableSelect.module.css";

export type SearchableSelectOption = {
  value: string;
  label: string;
  hint?: string;
  keywords?: string;
};

type SearchableSelectProps = {
  id?: string;
  value: string;
  options: SearchableSelectOption[];
  placeholder: string;
  dropdownTitle: string;
  size?: "default" | "compact" | "field" | "summary";
  searchPlaceholder?: string;
  emptyMessage?: string;
  fallbackHint?: string;
  showSelectedHint?: boolean;
  searchEnabled?: boolean;
  invalid?: boolean;
  disabled?: boolean;
  dropdownPlacement?: "down" | "up";
  shadow?: "default" | "soft";
  ariaDescribedBy?: string;
  ariaLabel?: string;
  className?: string;
  onChange: (value: string) => void;
};

function joinClassNames(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
}

export function SearchableSelect({
  id,
  value,
  options,
  placeholder,
  dropdownTitle,
  size = "default",
  searchPlaceholder = t("Search..."),
  emptyMessage = t("No matching options found."),
  fallbackHint,
  showSelectedHint = true,
  searchEnabled = true,
  invalid = false,
  disabled = false,
  dropdownPlacement = "down",
  shadow = "default",
  ariaDescribedBy,
  ariaLabel,
  className,
  onChange,
}: SearchableSelectProps) {
  const triggerRef = useRef<HTMLDivElement | null>(null);
  const dropdownRef = useRef<HTMLDivElement | null>(null);
  const searchInputRef = useRef<HTMLInputElement | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [searchText, setSearchText] = useState("");
  const [dropdownStyle, setDropdownStyle] = useState<CSSProperties>({});

  const selectedOption = options.find(option => option.value === value) ?? null;
  const filteredOptions = useMemo(() => {
    if (!searchEnabled) {
      return options;
    }

    const normalizedSearchText = searchText.trim().toLowerCase();
    if (!normalizedSearchText) {
      return options;
    }

    return options.filter(option => (
      [option.label, option.hint ?? "", option.keywords ?? "", option.value]
        .join(" ")
        .toLowerCase()
        .includes(normalizedSearchText)
    ));
  }, [options, searchEnabled, searchText]);
  const closeDropdown = useCallback(() => {
    setIsOpen(false);
    setSearchText("");
  }, []);
  const openDropdown = useCallback(() => {
    setSearchText("");
    setIsOpen(true);
  }, []);

  useLayoutEffect(() => {
    if (!isOpen || !triggerRef.current || !dropdownRef.current) {
      return;
    }

    const viewportPadding = 12;
    const dropdownOffset = 10;
    const updateDropdownPosition = () => {
      const triggerRect = triggerRef.current?.getBoundingClientRect();
      const dropdownRect = dropdownRef.current?.getBoundingClientRect();
      if (!triggerRect || !dropdownRect) {
        return;
      }

      const maxLeft = Math.max(viewportPadding, window.innerWidth - triggerRect.width - viewportPadding);
      const left = Math.min(Math.max(triggerRect.left, viewportPadding), maxLeft);
      const preferredTop =
        dropdownPlacement === "up"
          ? triggerRect.top - dropdownRect.height - dropdownOffset
          : triggerRect.bottom + dropdownOffset;
      const maxTop = Math.max(viewportPadding, window.innerHeight - dropdownRect.height - viewportPadding);

      setDropdownStyle({
        left,
        top: Math.min(Math.max(preferredTop, viewportPadding), maxTop),
        width: triggerRect.width,
      });
    };

    updateDropdownPosition();
    window.addEventListener("resize", updateDropdownPosition);
    window.addEventListener("scroll", updateDropdownPosition, true);
    return () => {
      window.removeEventListener("resize", updateDropdownPosition);
      window.removeEventListener("scroll", updateDropdownPosition, true);
    };
  }, [dropdownPlacement, isOpen]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (searchEnabled) {
      searchInputRef.current?.focus();
    }

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
  }, [closeDropdown, isOpen, searchEnabled]);

  const handleSelect = (nextValue: string) => {
    onChange(nextValue);
    closeDropdown();
  };

  const resolvedHint = showSelectedHint
    ? (selectedOption?.hint ?? fallbackHint ?? t("{0} options available", options.length))
    : "";
  const rootClassName = joinClassNames(styles.root, className);
  const dropdown = isOpen ? createPortal(
    <div ref={dropdownRef} className={styles.dropdownPortal} style={dropdownStyle}>
      <div className={styles.dropdown}>
        <div className={styles.dropdownHeader}>
          <span>{dropdownTitle}</span>
          <span>{filteredOptions.length}</span>
        </div>

        {searchEnabled ? (
          <div className={styles.searchField}>
            <SearchIcon className={styles.searchIcon} />
            <input
              ref={searchInputRef}
              className={styles.searchInput}
              value={searchText}
              onChange={event => setSearchText(event.target.value)}
              placeholder={searchPlaceholder}
              aria-label={searchPlaceholder}
            />
          </div>
        ) : null}

        <div className={styles.optionList} role="listbox" aria-label={ariaLabel ?? dropdownTitle}>
          {filteredOptions.length > 0 ? (
            filteredOptions.map(option => {
              const isSelected = option.value === value;

              return (
                <button
                  key={`${option.value}-${option.label}`}
                  type="button"
                  role="option"
                  aria-selected={isSelected}
                  className={joinClassNames(styles.option, isSelected && styles.optionSelected)}
                  onClick={() => handleSelect(option.value)}
                >
                  <div className={styles.optionText}>
                    <span className={styles.optionName}>{option.label}</span>
                    {option.hint ? <span className={styles.optionMeta}>{option.hint}</span> : null}
                  </div>

                  {isSelected ? <CheckIcon size={16} className={styles.optionCheck} /> : null}
                </button>
              );
            })
          ) : (
            <div className={styles.emptyDropdown}>{emptyMessage}</div>
          )}
        </div>
      </div>
    </div>,
    document.body,
  ) : null;

  return (
    <>
      <div ref={triggerRef} className={rootClassName}>
        <button
          id={id}
          type="button"
          className={joinClassNames(
            styles.selectButton,
            size === "compact" && styles.selectButtonCompact,
            size === "field" && styles.selectButtonField,
            size === "summary" && styles.selectButtonSummary,
            shadow === "soft" && styles.selectButtonShadowSoft,
            invalid && styles.selectButtonInvalid,
          )}
          onClick={() => {
            if (disabled) {
              return;
            }

            if (isOpen) {
              closeDropdown();
              return;
            }

            openDropdown();
          }}
          aria-haspopup="listbox"
          aria-expanded={isOpen}
          aria-invalid={invalid}
          aria-describedby={ariaDescribedBy}
          aria-label={ariaLabel ?? dropdownTitle}
          disabled={disabled}
        >
          <div className={styles.selectButtonText}>
            <span className={styles.selectButtonLabel}>{selectedOption?.label ?? placeholder}</span>
            {resolvedHint ? <span className={styles.selectButtonHint}>{resolvedHint}</span> : null}
          </div>

          <ArrowIcon size={16} className={joinClassNames(styles.selectChevron, isOpen && styles.selectChevronOpen)} />
        </button>
      </div>

      {dropdown}
    </>
  );
}
