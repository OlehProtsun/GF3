import { useEffect, useState } from "react";
import { OverlaySidebarLayout } from "@app/layouts/overlay-sidebar-layout";
import { HomeTestPage } from "@pages/hometest";

function getPathname() {
  return window.location.pathname;
}

export function AppRouter() {
  const [pathname, setPathname] = useState(getPathname);

  useEffect(() => {
    const onPopState = () => setPathname(getPathname());
    window.addEventListener("popstate", onPopState);

    return () => window.removeEventListener("popstate", onPopState);
  }, []);

  if (pathname !== "/") {
    return null;
  }

  return (
    <OverlaySidebarLayout>
      <HomeTestPage />
    </OverlaySidebarLayout>
  );
}
