import { t } from "@shared/i18n";
import { createContext, startTransition, useCallback, useContext, useEffect, useRef, useState, type PropsWithChildren } from "react";
import { ApiError, getAuthAccessToken, getErrorMessage, setAuthAccessToken, subscribeUnauthorized } from "@shared/api/httpClient";
import { authApi } from "@entities/auth";
import type { AuthLoginResult, AuthSession, LoginInput } from "@entities/auth";

type AuthStatus = "loading" | "authenticated" | "unauthenticated";

type AuthContextValue = {
  status: AuthStatus;
  session: AuthSession | null;
  bootstrapError: string | null;
  passwordChanged: boolean;
  completePasswordChange: () => void;
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

  try {
    const storedValue = window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
    return storedValue && storedValue.trim().length > 0 ? storedValue.trim() : null;
  } catch {
    return null;
  }
}

function persistAccessToken(token: string | null) {
  if (typeof window === "undefined") {
    return;
  }

  try {
    if (token && token.trim().length > 0) {
      window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token.trim());
    } else {
      window.localStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
    }
  } catch {
    // The session remains usable in memory when storage is unavailable.
  }
}

export function AuthProvider({ children }: PropsWithChildren) {
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [session, setSession] = useState<AuthSession | null>(null);
  const [bootstrapError, setBootstrapError] = useState<string | null>(null);
  const [passwordChanged, setPasswordChanged] = useState(false);

  const generation = useRef(0);

  const applyUnauthenticatedState = useCallback((nextBootstrapError: string | null = null) => {
    startTransition(() => {
      setSession(null);
      setStatus("unauthenticated");
      setBootstrapError(nextBootstrapError);
    });
  }, []);

  const storeAccessToken = useCallback((token: string | null) => {
    const normalizedToken = token && token.trim().length > 0 ? token.trim() : null;
    setAuthAccessToken(normalizedToken);
    persistAccessToken(normalizedToken);
    return normalizedToken;
  }, []);

  const applyAuthenticatedState = useCallback((nextSession: AuthSession) => {
    startTransition(() => {
      setPasswordChanged(false);
      setSession(nextSession);
      setStatus("authenticated");
      setBootstrapError(null);
    });
  }, []);

  const refreshSession = useCallback(async () => {
    const requestGeneration = ++generation.current;
    const activeToken = getAuthAccessToken() ?? readStoredAccessToken();
    if (!activeToken) {
      storeAccessToken(null);
      applyUnauthenticatedState(null);
      return;
    }

    storeAccessToken(activeToken);

    try {
      const nextSession = await authApi.session();
      if (requestGeneration !== generation.current) return;
      applyAuthenticatedState(nextSession);
    } catch (error) {
      if (requestGeneration !== generation.current) return;
      if (error instanceof ApiError && error.status === 401) {
        storeAccessToken(null);
        applyUnauthenticatedState(null);
        return;
      }

      applyUnauthenticatedState(getErrorMessage(error, t("Could not restore the current session.")));
    }
  }, [applyAuthenticatedState, applyUnauthenticatedState, storeAccessToken]);

  useEffect(() => subscribeUnauthorized(() => {
    generation.current++;
    storeAccessToken(null);
    applyUnauthenticatedState();
  }), [applyUnauthenticatedState, storeAccessToken]);

  useEffect(() => {
    void refreshSession();
    return () => { generation.current++; };
  }, [refreshSession]);

  useEffect(() => {
    const handleStorage = (event: StorageEvent) => {
      if (event.key !== AUTH_TOKEN_STORAGE_KEY && event.key !== null) return;
      generation.current++;
      setAuthAccessToken(null);
      applyUnauthenticatedState();
    };
    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, [applyUnauthenticatedState]);

  const login = async (input: LoginInput) => {
    const requestGeneration = ++generation.current;
    const result = await authApi.login(input);
    if (requestGeneration !== generation.current) throw new Error(t("Request was canceled."));
    storeAccessToken(result.accessToken);
    applyAuthenticatedState(result.session);
    return result.session;
  };

  const replaceLoginResult = (result: AuthLoginResult) => {
    generation.current++;
    storeAccessToken(result.accessToken);
    applyAuthenticatedState(result.session);
  };

  const logout = async () => {
    generation.current++;
    storeAccessToken(null);
    applyUnauthenticatedState(null);
    try {
      await authApi.logout();
    } catch {
      // Local logout must finish even when the anonymous logout endpoint is unavailable.
    }
  };

  const completePasswordChange = () => {
    // The password reset has already revoked this token on the server.
    generation.current++;
    storeAccessToken(null);
    startTransition(() => {
      setPasswordChanged(true);
      applyUnauthenticatedState(null);
    });
  };

  return (
    <AuthContext.Provider
      value={{
        status,
        session,
        bootstrapError,
        passwordChanged,
        completePasswordChange,
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
