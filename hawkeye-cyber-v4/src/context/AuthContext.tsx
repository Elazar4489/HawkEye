import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { AuthResponse, User } from '../types';
import { getErrorMessage, loginApi, registerApi, TOKEN_KEY, UNAUTHORIZED_EVENT, USER_KEY } from '../services/api';

interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  pushEnabled: boolean;
  pushSupported: boolean;
  // Each returns an error message, or null on success
  login: (email: string, pass: string) => Promise<string | null>;
  register: (email: string, name: string, pass: string, fone?: string) => Promise<string | null>;
  logout: () => void;
  requestPushPermission: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

// Browser notifications need a secure context (https or localhost).
const pushSupported = typeof window !== 'undefined' && 'Notification' in window && window.isSecureContext;

const loadSession = (): { user: User | null; token: string | null } => {
  try {
    const token = localStorage.getItem(TOKEN_KEY);
    const user = localStorage.getItem(USER_KEY);
    if (!token || !user) return { user: null, token: null };

    // Drop an expired JWT instead of failing on the first request
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    if (payload.exp && payload.exp * 1000 < Date.now()) return { user: null, token: null };

    return { user: JSON.parse(user) as User, token };
  } catch {
    return { user: null, token: null };
  }
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [session, setSession] = useState(loadSession);
  const [pushEnabled, setPushEnabled] = useState<boolean>(
    pushSupported && Notification.permission === 'granted'
  );

  const saveSession = (response: AuthResponse) => {
    try {
      localStorage.setItem(TOKEN_KEY, response.token);
      localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    } catch {
      // storage unavailable: the session still lives in memory
    }
    setSession({ user: response.user, token: response.token });
  };

  const logout = useCallback(() => {
    try {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
    } catch {
      // ignore
    }
    setSession({ user: null, token: null });
  }, []);

  // The API client raises this event on HTTP 401 (expired / invalid token)
  useEffect(() => {
    window.addEventListener(UNAUTHORIZED_EVENT, logout);
    return () => window.removeEventListener(UNAUTHORIZED_EVENT, logout);
  }, [logout]);

  const login = async (email: string, pass: string): Promise<string | null> => {
    try {
      saveSession(await loginApi(email, pass));
      return null;
    } catch (error) {
      return getErrorMessage(error);
    }
  };

  const register = async (email: string, name: string, pass: string, fone?: string): Promise<string | null> => {
    try {
      saveSession(await registerApi(name, email, pass, fone));
      return null;
    } catch (error) {
      return getErrorMessage(error);
    }
  };

  const requestPushPermission = async (): Promise<boolean> => {
    if (!pushSupported) return false;
    const result = await Notification.requestPermission();
    const granted = result === 'granted';
    setPushEnabled(granted);
    return granted;
  };

  return (
    <AuthContext.Provider
      value={{
        user: session.user,
        token: session.token,
        isAuthenticated: !!session.user && !!session.token,
        pushEnabled,
        pushSupported,
        login,
        register,
        logout,
        requestPushPermission
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
};
