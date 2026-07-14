import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { api } from '../api/client';
import { clearToken, getToken, setToken } from '../api/authToken';
import type { AuthResponse, LoginRequest, User } from '../api/types';

const USER_STORAGE_KEY = 'golfclub-admin-user';

function loadStoredUser(): User | null {
  const raw = localStorage.getItem(USER_STORAGE_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as User;
  } catch {
    return null;
  }
}

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => (getToken() ? loadStoredUser() : null));

  const login = async (request: LoginRequest) => {
    const response = await api.post<AuthResponse>('/api/auth/login', request);
    if (!response.user.isAdmin) {
      throw new Error('This account does not have admin access.');
    }
    setToken(response.token);
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(response.user));
    setUser(response.user);
  };

  const logout = () => {
    clearToken();
    localStorage.removeItem(USER_STORAGE_KEY);
    setUser(null);
  };

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, login, logout }),
    [user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within an AuthProvider');
  return context;
}
