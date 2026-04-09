import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./App";
import { QueryProvider } from "@app/providers/QueryProvider";
import { ErrorAlertsViewport } from "@shared/ui/feedback/error-alerts/ErrorAlertsViewport";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryProvider>
      <App />
      <ErrorAlertsViewport />
    </QueryProvider>
  </StrictMode>,
);
