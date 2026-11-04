import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./App";
import { LanguageProvider } from "@app/providers/LanguageProvider";
import { AuthProvider } from "@app/providers/AuthProvider";
import { PresenceProvider } from "@app/providers/PresenceProvider";
import { QueryProvider } from "@app/providers/QueryProvider";
import { ErrorAlertsViewport } from "@shared/ui/feedback/error-alerts/ErrorAlertsViewport";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <AuthProvider>
      <QueryProvider>
        <LanguageProvider><PresenceProvider>
          <App />
          <ErrorAlertsViewport />
        </PresenceProvider></LanguageProvider>
      </QueryProvider>
    </AuthProvider>
  </StrictMode>,
);
