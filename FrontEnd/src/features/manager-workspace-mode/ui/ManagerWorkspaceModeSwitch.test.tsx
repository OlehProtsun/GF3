import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, test, vi } from "vitest";
import { useState } from "react";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider, useMutation } from "@tanstack/react-query";
import { ManagerWorkspaceModeSwitch } from "./ManagerWorkspaceModeSwitch";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
const auth = vi.hoisted(() => ({ setManagerWorkspaceMode: vi.fn() }));
vi.mock("@app/providers/AuthProvider", () => ({ useAuth: () => auth }));
afterEach(() => { cleanup(); vi.clearAllMocks(); });
function DirtyEditor() {
 const [dirty] = useState(true); const prompt = useUnsavedChangesPrompt({ when: dirty });
 return <>{prompt.dialog}<ManagerWorkspaceModeSwitch targetMode="phone" /></>;
}
test("unsaved changes require existing navigation prompt before token exchange; cancel keeps editor", async () => {
 window.history.replaceState({}, "", "/employee/1/edit"); auth.setManagerWorkspaceMode.mockResolvedValue(undefined);
 render(<QueryClientProvider client={new QueryClient()}><BrowserRouter><DirtyEditor /></BrowserRouter></QueryClientProvider>);
 fireEvent.click(screen.getByRole("button", { name: /Switch to Phone/ })); await screen.findByRole("dialog"); expect(auth.setManagerWorkspaceMode).not.toHaveBeenCalled();
 fireEvent.click(screen.getByRole("button", { name: "Stay here" })); expect(window.location.pathname).toBe("/employee/1/edit");
 fireEvent.click(screen.getByRole("button", { name: /Switch to Phone/ })); fireEvent.click(await screen.findByRole("button", { name: "Leave page" }));
 await waitFor(() => expect(auth.setManagerWorkspaceMode).toHaveBeenCalledTimes(1)); await waitFor(() => expect(window.location.pathname).toBe("/"));
});
test("pending saves block switching through shared mutation count", async () => {
 let finish!: () => void;
 function Saving() { const mutation = useMutation({ mutationFn: () => new Promise<void>(resolve => { finish = resolve; }) }); return <><button onClick={() => mutation.mutate(undefined)}>save</button><ManagerWorkspaceModeSwitch targetMode="phone" /></>; }
 render(<QueryClientProvider client={new QueryClient()}><BrowserRouter><Saving /></BrowserRouter></QueryClientProvider>);
 fireEvent.click(screen.getByText("save")); await waitFor(() => expect(finish).toBeDefined()); expect(screen.getByRole("button", { name: /Switch to Phone/ })).toBeDisabled();
 await act(async () => finish()); expect(screen.getByRole("button", { name: /Switch to Phone/ })).toBeEnabled(); expect(auth.setManagerWorkspaceMode).not.toHaveBeenCalled();
});


test.each(["phone", "pc"] as const)("compact %s switch retains pending state and mode exchange with an external label", async targetMode => {
 let finish!: () => void;
 auth.setManagerWorkspaceMode.mockImplementation(() => new Promise<void>(resolve => { finish = resolve; }));
 window.history.replaceState({}, "", "/container");
 render(<QueryClientProvider client={new QueryClient()}><BrowserRouter><ManagerWorkspaceModeSwitch targetMode={targetMode} compact itemClassName="navItem" buttonClassName="navButton" labelClassName="navLabel" /></BrowserRouter></QueryClientProvider>);
 const label = targetMode === "phone" ? "Switch to Phone" : "Switch to Desktop";
 const button = screen.getByRole("button", { name: label, exact: true });
 expect(button).toHaveClass("navButton"); expect(button.textContent).toBe("");
 expect(screen.getByText(label).previousElementSibling).toBe(button);
 fireEvent.click(button); await waitFor(() => expect(auth.setManagerWorkspaceMode).toHaveBeenCalledWith(targetMode));
 expect(button).toBeDisabled(); expect(button).toHaveAttribute("aria-busy", "true");
 await act(async () => finish()); await waitFor(() => expect(window.location.pathname).toBe("/")); expect(button).toBeEnabled();
});
