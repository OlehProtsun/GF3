import { t } from "@shared/i18n";
import { createContext, startTransition, useContext, useEffect, useState, type PropsWithChildren } from "react";
import { ApiError, getAuthAccessToken, getErrorMessage, setAuthAccessToken } from "@shared/api/httpClient";
import { authApi } from "@entities/auth";
import type { AuthLoginResult, AuthSession, LoginInput } from "@entities/auth";

type AuthStatus = "loading" | "authenticated" | "unauthenticated";

type AuthContextValue = {
  status: AuthStatus;
  session: AuthSession | null;
  bootstrapError: string | null;
  login: (input: LoginInput) => Promise<AuthSession>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<void>;
  replaceLoginResult: (result: AuthLoginResult) => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);
const AUTH_TOKEN_STORAGE_KEY = "gf3.auth.access-token";

function readStoredAccessToken() {
  if (typeof window === "undefined") {
    return null;
  }

  const storedValue = window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
  return storedValue && storedValue.trim().length > 0 ? storedValue.trim() : null;
}

function persistAccessToken(token: string | null) {
  if (typeof window === "undefined") {
    return;
  }

  if (token && token.trim().length > 0) {
    window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token.trim());
    return;
  }

  window.localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
}

export function AuthProvider({ children }: PropsWithChildren) {
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [session, setSession] = useState<AuthSession | null>(null);
  const [bootstrapError, setBootstrapError] = useState<string | null>(null);

  const applyUnauthenticatedState = (nextBootstrapError: string | null = null) => {
    startTransition(() => {
      setSession(null);
      setStatus("unauthenticated");
      setBootstrapError(nextBootstrapError);
    });
  };

  const storeAccessToken = (token: string | null) => {
    const normalizedToken = token && token.trim().length > 0 ? token.trim() : null;
    setAuthAccessToken(normalizedToken);
    persistAccessToken(normalizedToken);
    return normalizedToken;
  };

  const applyAuthenticatedState = (nextSession: AuthSession) => {
    startTransition(() => {
      setSession(nextSession);
      setStatus("authenticated");
      setBootstrapError(null);
    });
  };

  const refreshSession = async () => {
    const activeToken = getAuthAccessToken() ?? readStoredAccessToken();
    if (!activeToken) {
      storeAccessToken(null);
      applyUnauthenticatedState(null);
      return;
    }

    storeAccessToken(activeToken);

    try {
      const nextSession = await authApi.session();
      applyAuthenticatedState(nextSession);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        storeAccessToken(null);
        applyUnauthenticatedState(null);
        return;
      }

      applyUnauthenticatedState(getErrorMessage(error, t("Could not restore the current session.")));
    }
  };

  useEffect(() => {
    void refreshSession();
  }, []);

  const login = async (input: LoginInput) => {
    const result = await authApi.login(input);
    storeAccessToken(result.accessToken);
    applyAuthenticatedState(result.session);
    return result.session;
  };

  const replaceLoginResult = (result: AuthLoginResult) => {
    storeAccessToken(result.accessToken);
    applyAuthenticatedState(result.session);
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } finally {
      storeAccessToken(null);
      applyUnauthenticatedState(null);
    }
  };

  return (
    <AuthContext.Provider
      value={{
        status,
        session,
        bootstrapError,
        login,
        logout,
        refreshSession,
        replaceLoginResult,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("AuthProvider is missing");
  }

  return context;
}
