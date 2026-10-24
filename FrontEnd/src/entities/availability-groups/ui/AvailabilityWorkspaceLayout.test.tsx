import { render, screen } from "@testing-library/react";
import { describe, expect, test } from "vitest";
import { AvailabilityWorkspaceLayout } from "./AvailabilityWorkspaceLayout";
import styles from "./AvailabilityWorkspaceLayout.module.css";

describe("AvailabilityWorkspaceLayout", () => {
  test("can preserve the side rail on mobile", () => {
    render(
      <AvailabilityWorkspaceLayout preserveSideLayoutOnMobile
        sidebar={<span>Sidebar</span>} main={<span>Main</span>} />,
    );

    expect(screen.getByText("Sidebar").parentElement?.parentElement?.parentElement)
      .toHaveClass(styles.layoutPreserveSideLayout);
  });
});
