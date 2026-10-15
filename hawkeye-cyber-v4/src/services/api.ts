import axios, { AxiosError } from 'axios';
import {
  AuthResponse,
  CreateZoneRequest,
  DashboardDocument,
  DashboardRequest,
  LiveFlightsResponse,
  MapBounds,
  NotificationHistoryItem,
  PriorityZone,
  SystemHealth,
  User,
  UserRole,
} from '../types';

// ==========================================
// 1. הגדרות תצורה לבקאנד (Backend Configuration)
// ==========================================
// Relative URLs: in Docker, nginx (port 3000) proxies them to the backends; with
// "npm run dev" the vite proxy does the same. So the app works from localhost and
// from any LAN IP without changing anything.

export interface BackendConfig {
  authApiUrl: string;       // FlightTracking.Api: users, auth, zones, alerts, live flights
  dashboardApiUrl: string;  // FlightDashboardApi: one-time risk dashboards
  signalRHubUrl: string;    // SignalR hub on FlightTracking.Api
}

export const defaultConfig: BackendConfig = {
  authApiUrl: '/api',
  dashboardApiUrl: '/dashboard/api',
  signalRHubUrl: '/notificationHub',
};

const CONFIG_KEY = 'hawkeye_backend_config_v2';
export const TOKEN_KEY = 'hawkeye_token';
export const USER_KEY = 'hawkeye_user';
export const UNAUTHORIZED_EVENT = 'hawkeye:unauthorized';

// שליפת ההגדרות מ-localStorage או שימוש ב-defaultConfig
export const getStoredConfig = (): BackendConfig => {
  try {
    const stored = localStorage.getItem(CONFIG_KEY);
    return stored ? { ...defaultConfig, ...JSON.parse(stored) } : defaultConfig;
  } catch {
    return defaultConfig;
  }
};

export const saveStoredConfig = (config: BackendConfig): void => {
  try {
    localStorage.setItem(CONFIG_KEY, JSON.stringify(config));
  } catch {
    // storage unavailable - the defaults keep working
  }
};

export const getToken = (): string | null => {
  try {
    return localStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
};

const http = axios.create({ timeout: 20000 });

// JWT on every request
http.interceptors.request.use((request) => {
  const token = getToken();
  if (token) {
    request.headers.Authorization = `Bearer ${token}`;
  }
  return request;
});

// Expired / invalid token -> AuthContext logs the user out
http.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401 && !error.config?.url?.includes('/auth/')) {
      window.dispatchEvent(new Event(UNAUTHORIZED_EVENT));
    }
    return Promise.reject(error);
  }
);

export const getErrorMessage = (error: unknown): string => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string; errors?: Record<string, string[]> } | undefined;
    if (data?.message) return data.message;
    if (data?.errors) return Object.values(data.errors).flat().join(' ');
    if (!error.response) return 'השרת אינו זמין (Network error)';
    return `${error.response.status} ${error.response.statusText}`;
  }
  return error instanceof Error ? error.message : String(error);
};

// ==========================================
// 2. שירותי הזדהות ומשתמשים (Auth API)
// ==========================================

export const loginApi = async (email: string, password: string): Promise<AuthResponse> => {
  const { authApiUrl } = getStoredConfig();
  const response = await http.post<AuthResponse>(`${authApiUrl}/auth/login`, { email, password });
  return response.data;
};

export const registerApi = async (
  userName: string,
  email: string,
  password: string,
  fone?: string
): Promise<AuthResponse> => {
  const { authApiUrl } = getStoredConfig();
  const response = await http.post<AuthResponse>(`${authApiUrl}/auth/register`, {
    userName,
    email,
    password,
    fone: fone || null,
  });
  return response.data;
};

// Stores an FCM registration token for this user (used by the NotificationWorker)
export const updatePushTokenApi = async (userId: number, pushToken: string | null): Promise<void> => {
  const { authApiUrl } = getStoredConfig();
  await http.put(`${authApiUrl}/users/${userId}/push-token`, { pushToken });
};

// ==========================================
// 3. טיסות בזמן אמת (latest OpenSky snapshot from MongoDB)
// ==========================================

export const getLiveFlightsApi = async (bounds?: MapBounds, limit = 3000): Promise<LiveFlightsResponse> => {
  const { authApiUrl } = getStoredConfig();
  const response = await http.get<LiveFlightsResponse>(`${authApiUrl}/flights/live`, {
    params: { ...bounds, limit },
  });
  return response.data;
};

// ==========================================
// 4. תחומי עניין (Priority zones - MySQL spatial)
// ==========================================

export const getZonesApi = async (userId: number): Promise<PriorityZone[]> => {
  const { authApiUrl } = getStoredConfig();
  const response = await http.get<PriorityZone[]>(`${authApiUrl}/users/${userId}/priorities`);
  return response.data;
};

export const createZoneApi = async (userId: number, zone: CreateZoneRequest): Promise<PriorityZone> => {
  const { authApiUrl } = getStoredConfig();
  const response = await http.post<PriorityZone>(`${authApiUrl}/users/${userId}/priorities`, {
    ...zone,
    userId,
  });
  return response.data;
};

export const deleteZoneApi = async (userId: number, zoneId: number): Promise<void> => {
  const { authApiUrl } = getStoredConfig();
  await http.delete(`${authApiUrl}/users/${userId}/priorities/${zoneId}`);
};

// ==========================================
// 5. היסטוריית התראות
// ==========================================

export const getAlertsHistoryApi = async (userId: number, take = 200): Promise<NotificationHistoryItem[]> => {
  const { authApiUrl } = getStoredConfig();
  const response = await http.get<NotificationHistoryItem[]>(`${authApiUrl}/users/${userId}/notifications`, {
    params: { take },
  });
  return response.data;
};

export const markAlertsReadApi = async (userId: number): Promise<void> => {
  const { authApiUrl } = getStoredConfig();
  await http.put(`${authApiUrl}/users/${userId}/notifications/read-all`);
};

// ==========================================
// 6. דשבורדים חד-פעמיים (FlightDashboardApi -> Elasticsearch)
// ==========================================

export const generateDashboardApi = async (request: DashboardRequest): Promise<DashboardDocument> => {
  const { dashboardApiUrl } = getStoredConfig();
  const response = await http.post<{ dashboardId: string; data: DashboardDocument }>(
    `${dashboardApiUrl}/dashboard/generate`,
    request,
    { timeout: 60000 }
  );
  return response.data.data;
};

export const getDashboardApi = async (id: string): Promise<DashboardDocument> => {
  const { dashboardApiUrl } = getStoredConfig();
  const response = await http.get<DashboardDocument>(`${dashboardApiUrl}/dashboard/${encodeURIComponent(id)}`);
  return response.data;
};

// ==========================================
// 7. פאנל אדמין וניטור תשתיות (Admin API)
// ==========================================

export const getUsersListApi = async (): Promise<User[]> => {
  const { authApiUrl } = getStoredConfig();
  const response = await http.get<User[]>(`${authApiUrl}/users`);
  return response.data;
};

export const updateUserRoleApi = async (userId: number, role: UserRole): Promise<void> => {
  const { authApiUrl } = getStoredConfig();
  await http.put(`${authApiUrl}/users/${userId}/role`, { role });
};

export const getSystemHealthApi = async (): Promise<SystemHealth> => {
  const { authApiUrl } = getStoredConfig();
  const response = await http.get<SystemHealth>(`${authApiUrl}/admin/health`);
  return response.data;
};

export const pingApi = async (config: BackendConfig): Promise<{ api: boolean; dashboard: boolean }> => {
  const check = async (url: string) => {
    try {
      await http.get(url, { timeout: 5000 });
      return true;
    } catch {
      return false;
    }
  };
  const [api, dashboard] = await Promise.all([
    check(`${config.authApiUrl}/health`),
    check(`${config.dashboardApiUrl}/health`),
  ]);
  return { api, dashboard };
};
