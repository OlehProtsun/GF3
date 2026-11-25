import { useEffect } from "react";
import { useAuth } from "@app/providers/AuthProvider";
import { finishPwaSplash } from "@shared/lib/pwaSplash";

export function AppReadySignal({ redirected }: { redirected: boolean }) {
  const { status } = useAuth();
  useEffect(() => {
    if (status !== "loading" && !redirected) finishPwaSplash();
  }, [status, redirected]);
  return null;
}
