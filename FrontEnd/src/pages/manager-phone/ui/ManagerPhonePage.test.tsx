import { fireEvent, render, screen } from "@testing-library/react";
import { expect, test, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ComponentProps } from "react";
import { ApiError } from "@shared/api/httpClient";
import { ManagerPhonePage } from "./ManagerPhonePage";
function mount(props: ComponentProps<typeof ManagerPhonePage> = {}) {
  const client = new QueryClient();
  const invalidate = vi.spyOn(client, "invalidateQueries");
  const view = render(<QueryClientProvider client={client}><BrowserRouter><ManagerPhonePage {...props}>Schedule content</ManagerPhonePage></BrowserRouter></QueryClientProvider>);
  return { ...view, invalidate };
}
test("optional title and parent Back link, including title without Back", () => {
  const view = mount({ backTo: "/container/1", title: "Schedule Profile" });
  expect(screen.getByRole("link", { name: "Back" })).toHaveAttribute("href", "/container/1");
  expect(screen.getByText("Schedule Profile").tagName).toBe("SPAN");
  view.unmount();
  mount({ title: "Schedule Profile" });
  expect(screen.getByText("Schedule Profile")).toBeInTheDocument();
  expect(screen.queryByRole("link")).not.toBeInTheDocument();
});
test("normal callers retain optional search without title", () => {
  const onQueryChange = vi.fn();
  mount({ backTo: "/container", query: "Anna", onQueryChange });
  expect(screen.queryByText("Schedule Profile")).not.toBeInTheDocument();
  fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Jan" } });
  expect(onQueryChange).toHaveBeenCalledWith("Jan");
  fireEvent.click(screen.getByRole("button", { name: "Clear Search" }));
  expect(onQueryChange).toHaveBeenCalledWith("");
});
test.each([
  [{ valid: false }, "Invalid record ID."],
  [{ queries: [{ isLoading: true, isError: false }] }, "Loading..."],
  [{ missing: true }, "Record not found."],
  [{ missing: true, queries: [{ isLoading: false, isError: true, error: new ApiError({ message: "missing", status: 404 }) }] }, "Record not found."],
] as const)("toolbar persists through query state %j", (state, message) => {
  mount({ ...state, queries: "queries" in state ? [...state.queries] : [], backTo: "/container/1", title: "Schedule Profile" });
  expect(screen.getByText(message)).toBeInTheDocument();
  expect(screen.getByText("Schedule Profile")).toBeInTheDocument();
  expect(screen.queryByText("Schedule content")).not.toBeInTheDocument();
});
test("supporting-query failure preserves retry invalidation", () => {
  const { invalidate } = mount({ title: "Schedule Profile", queries: [{ isLoading: false, isError: true }], queryKeys: [["containers"], ["employees"]] });
  expect(screen.getByText("Could not load records. Please try again.")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Retry" }));
  expect(invalidate).toHaveBeenCalledWith({ queryKey: ["containers"] });
  expect(invalidate).toHaveBeenCalledWith({ queryKey: ["employees"] });
});

