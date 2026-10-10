import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, test, vi } from "vitest";
import { ContainerGraphColorDialog } from "./ContainerGraphColorDialog";

function renderDialog(
  onBindFillColor: (key: string, fillColor: string) => Promise<void>,
  reservedValueBindKeys: ReadonlySet<string> = new Set(),
) {
  render(
    <ContainerGraphColorDialog
      open
      mode="fill"
      value="#dbeafe"
      fillColorBinds={[]}
      textColorBinds={[]}
      reservedValueBindKeys={reservedValueBindKeys}
      isFillColorBindBusy={false}
      isTextColorBindBusy={false}
      onCancel={() => undefined}
      onSave={() => undefined}
      onBindFillColor={onBindFillColor}
      onDeleteFillColorBind={async () => undefined}
      onBindTextColor={async () => undefined}
      onDeleteTextColorBind={async () => undefined}
    />,
  );
}

describe("ContainerGraphColorDialog fill color binds", () => {
  test("captures the next key for the selected fill color", async () => {
    const onBindFillColor = vi.fn().mockResolvedValue(undefined);
    renderDialog(onBindFillColor);

    fireEvent.click(screen.getByRole("button", { name: "Bind Key" }));
    fireEvent.keyDown(window, { key: "F4" });

    await waitFor(() => expect(onBindFillColor).toHaveBeenCalledWith("F4", "#DBEAFE"));
  });

  test("rejects a key already used by a value bind", () => {
    const onBindFillColor = vi.fn().mockResolvedValue(undefined);
    renderDialog(onBindFillColor, new Set(["F4"]));

    fireEvent.click(screen.getByRole("button", { name: "Bind Key" }));
    fireEvent.keyDown(window, { key: "F4" });

    expect(screen.getByText("Key 'F4' is already used by a value bind.")).toBeInTheDocument();
    expect(onBindFillColor).not.toHaveBeenCalled();
  });

  test("captures and removes a manager text-color binding", async () => {
    const onBindTextColor = vi.fn().mockResolvedValue(undefined);
    const onDeleteTextColorBind = vi.fn().mockResolvedValue(undefined);
    const { rerender } = render(
      <ContainerGraphColorDialog
        open
        mode="text"
        value="#0f172a"
        fillColorBinds={[]}
        textColorBinds={[]}
        reservedValueBindKeys={new Set()}
        isFillColorBindBusy={false}
        isTextColorBindBusy={false}
        onCancel={() => undefined}
        onSave={() => undefined}
        onBindFillColor={async () => undefined}
        onDeleteFillColorBind={async () => undefined}
        onBindTextColor={onBindTextColor}
        onDeleteTextColorBind={onDeleteTextColorBind}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Bind Key" }));
    fireEvent.keyDown(window, { key: "F5" });
    await waitFor(() => expect(onBindTextColor).toHaveBeenCalledWith("F5", "#0F172A"));

    rerender(
      <ContainerGraphColorDialog
        open
        mode="text"
        value="#0f172a"
        fillColorBinds={[]}
        textColorBinds={[{ id: 12, key: "F5", textColor: "#0F172A" }]}
        reservedValueBindKeys={new Set()}
        isFillColorBindBusy={false}
        isTextColorBindBusy={false}
        onCancel={() => undefined}
        onSave={() => undefined}
        onBindFillColor={async () => undefined}
        onDeleteFillColorBind={async () => undefined}
        onBindTextColor={onBindTextColor}
        onDeleteTextColorBind={onDeleteTextColorBind}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Unbind" }));
    await waitFor(() => expect(onDeleteTextColorBind).toHaveBeenCalledWith(12));
  });

  test("rejects a text-color key already used by a fill-color bind", () => {
    const onBindTextColor = vi.fn().mockResolvedValue(undefined);
    render(
      <ContainerGraphColorDialog
        open
        mode="text"
        value="#0f172a"
        fillColorBinds={[{ id: 1, key: "F4", fillColor: "#DBEAFE" }]}
        textColorBinds={[]}
        reservedValueBindKeys={new Set()}
        isFillColorBindBusy={false}
        isTextColorBindBusy={false}
        onCancel={() => undefined}
        onSave={() => undefined}
        onBindFillColor={async () => undefined}
        onDeleteFillColorBind={async () => undefined}
        onBindTextColor={onBindTextColor}
        onDeleteTextColorBind={async () => undefined}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Bind Key" }));
    fireEvent.keyDown(window, { key: "F4" });

    expect(screen.getByText("Key 'F4' is already used by a fill color bind.")).toBeInTheDocument();
    expect(onBindTextColor).not.toHaveBeenCalled();
  });
});
