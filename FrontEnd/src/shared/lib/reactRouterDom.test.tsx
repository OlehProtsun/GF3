import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, test, vi } from "vitest";
import {
  BrowserRouter,
  Navigate,
  NavLink,
  matchPath,
  renderMatched,
  useLocation,
  useNavigate,
  useNavigationBlocker,
  useParams,
} from "./react-router-dom";

function LocationPanel() {
  const location = useLocation();
  const navigate = useNavigate();

  return (
    <section>
      <output aria-label="pathname">{location.pathname}</output>
      <output aria-label="search">{location.search}</output>
      <output aria-label="hash">{location.hash}</output>
      <button type="button" onClick={() => navigate("/shop/42/edit?tab=details#top")}>
        open shop
      </button>
      <NavLink to="/home" className={({ isActive }) => (isActive ? "active" : "idle")}>
        Home
      </NavLink>
    </section>
  );
}

function ParamsPanel() {
  const params = useParams<{ containerId?: string; graphId?: string; shopId?: string }>();

  return (
    <section>
      <output aria-label="container">{params.containerId ?? ""}</output>
      <output aria-label="graph">{params.graphId ?? ""}</output>
      <output aria-label="shop">{params.shopId ?? ""}</output>
    </section>
  );
}

function BlockerPanel({ onBlocked }: { onBlocked: () => void }) {
  const location = useLocation();
  const { proceedBlockedNavigation, cancelBlockedNavigation } = useNavigationBlocker(
    true,
    nextLocation => nextLocation.pathname === "/blocked",
    onBlocked,
  );

  return (
    <section>
      <output aria-label="blocked-path">{location.pathname}</output>
      <NavLink to="/blocked">Blocked</NavLink>
      <NavLink to="/allowed">Allowed</NavLink>
      <button type="button" onClick={proceedBlockedNavigation}>
        proceed
      </button>
      <button type="button" onClick={cancelBlockedNavigation}>
        cancel
      </button>
    </section>
  );
}

function RedirectPanel({ to }: { to: string }) {
  const location = useLocation();

  return (
    <section>
      <Navigate to={to} />
      <output aria-label="redirect-path">{location.pathname}</output>
    </section>
  );
}

describe("react-router-dom shim", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/");
  });

  test("BrowserRouter, useNavigate, useLocation, and NavLink update browser state", async () => {
    const user = userEvent.setup();
    render(
      <BrowserRouter>
        <LocationPanel />
      </BrowserRouter>,
    );

    expect(screen.getByLabelText("pathname")).toHaveTextContent("/");
    expect(screen.getByRole("link", { name: "Home" })).toHaveClass("idle");

    await user.click(screen.getByRole("button", { name: "open shop" }));

    expect(screen.getByLabelText("pathname")).toHaveTextContent("/shop/42/edit");
    expect(screen.getByLabelText("search")).toHaveTextContent("?tab=details");
    expect(screen.getByLabelText("hash")).toHaveTextContent("#top");
    expect(window.location.pathname).toBe("/shop/42/edit");

    await user.click(screen.getByRole("link", { name: "Home" }));

    expect(screen.getByLabelText("pathname")).toHaveTextContent("/home");
    expect(screen.getByRole("link", { name: "Home" })).toHaveClass("active");
  });

  test("useParams decodes supported parameterized routes", async () => {
    window.history.replaceState({}, "", "/container/12/graphs/encoded%20graph/edit");

    render(
      <BrowserRouter>
        <ParamsPanel />
      </BrowserRouter>,
    );

    expect(screen.getByLabelText("container")).toHaveTextContent("12");
    expect(screen.getByLabelText("graph")).toHaveTextContent("encoded graph");
    expect(screen.getByLabelText("shop")).toHaveTextContent("");
  });

  test("navigation blocker can cancel or proceed blocked push navigations", async () => {
    const user = userEvent.setup();
    const onBlocked = vi.fn();

    render(
      <BrowserRouter>
        <BlockerPanel onBlocked={onBlocked} />
      </BrowserRouter>,
    );

    await user.click(screen.getByRole("link", { name: "Blocked" }));

    expect(onBlocked).toHaveBeenCalledTimes(1);
    expect(screen.getByLabelText("blocked-path")).toHaveTextContent("/");
    expect(window.location.pathname).toBe("/");

    await user.click(screen.getByRole("button", { name: "cancel" }));
    await user.click(screen.getByRole("link", { name: "Allowed" }));

    expect(screen.getByLabelText("blocked-path")).toHaveTextContent("/allowed");

    await user.click(screen.getByRole("link", { name: "Blocked" }));
    await user.click(screen.getByRole("button", { name: "proceed" }));

    expect(onBlocked).toHaveBeenCalledTimes(2);
    expect(screen.getByLabelText("blocked-path")).toHaveTextContent("/blocked");
  });

  test("Navigate redirects after render", async () => {
    render(
      <BrowserRouter>
        <RedirectPanel to="/employee/77" />
      </BrowserRouter>,
    );

    await waitFor(() => {
      expect(screen.getByLabelText("redirect-path")).toHaveTextContent("/employee/77");
    });
  });

  test("matchPath and renderMatched handle static, parameterized, and missing routes", () => {
    expect(matchPath("/shop/12", "/shop/:shopId")).toBe(true);
    expect(matchPath("/shop/12/edit", "/shop/:shopId")).toBe(false);
    expect(matchPath("/shop/12", "/employee/:employeeId")).toBe(false);

    expect(renderMatched("/shop/12", [
      { path: "/employee/:employeeId", element: <span>Employee</span> },
      { path: "/shop/:shopId", element: <span>Shop</span> },
    ])).toEqual(<span>Shop</span>);
    expect(renderMatched("/missing", [
      { path: "/shop/:shopId", element: <span>Shop</span> },
    ])).toBeNull();
  });
});
