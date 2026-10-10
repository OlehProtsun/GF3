import { render, screen } from "@testing-library/react";
import { describe, expect, test } from "vitest";
import { AvailabilitySidebarSection } from "./AvailabilitySidebarSection";
import styles from "./AvailabilitySidebarSection.module.css";

describe("AvailabilitySidebarSection", () => {
  test("highlights a collapsed section that needs attention", () => {
    render(
      <AvailabilitySidebarSection label="Shift Corrections" collapsed attention
        onExpand={() => undefined} collapsedIcon={<span>!</span>}>
        <div>Correction content</div>
      </AvailabilitySidebarSection>,
    );

    expect(screen.getByRole("button", { name: "Expand Shift Corrections" }).parentElement)
      .toHaveClass(styles.sectionShellAttention);
  });

  test("can preserve the collapsed rail behavior on mobile", () => {
    render(
      <AvailabilitySidebarSection label="Schedule Details" collapsed preserveCollapsedOnMobile
        onExpand={() => undefined}>
        <div>Schedule details</div>
      </AvailabilitySidebarSection>,
    );

    expect(screen.getByRole("button", { name: "Expand Schedule Details" }).parentElement)
      .toHaveClass(styles.sectionShellPreserveCollapsed);
  });
});
