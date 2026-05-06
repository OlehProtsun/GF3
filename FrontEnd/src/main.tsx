import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./App";
import { AuthProvider } from "@app/providers/AuthProvider";
import { PresenceProvider } from "@app/providers/PresenceProvider";
import { QueryProvider } from "@app/providers/QueryProvider";
import { ErrorAlertsViewport } from "@shared/ui/feedback/error-alerts/ErrorAlertsViewport";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryProvider>
      <AuthProvider>
        <PresenceProvider>
          <App />
          <ErrorAlertsViewport />
        </PresenceProvider>
      </AuthProvider>
    </QueryProvider>
  </StrictMode>,
);
