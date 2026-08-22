import axios from "axios";

// axios instance — qua proxy /api của Vite trong dev
export const api = axios.create({
  baseURL: "",
  timeout: 15000,
});

// Gắn JWT vào header khi có token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("harness-admin-token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Token hết hạn / bị từ chối → xoá phiên và về trang đăng nhập
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401 && localStorage.getItem("harness-admin-token")) {
      localStorage.removeItem("harness-admin-token");
      localStorage.removeItem("harness-admin-profile");
      if (!window.location.pathname.endsWith("/login")) {
        window.location.assign("/");
      }
    }
    return Promise.reject(error);
  },
);

export interface StoreItem {
  id: string;
  code: string;
  name: string;
  address: string;
  phone: string;
  managerName?: string;
  isActive: boolean;
  createdAt: string;
  latitude?: number | null;
  longitude?: number | null;
}

export interface AttendanceItem {
  id: string;
  staffId: string;
  staffName: string;
  storeId: string;
  storeName: string;
  workDate: string;
  checkInTime?: string;
  checkOutTime?: string;
  status: number;
  statusText: string;
  notes?: string;
}

export interface KpiTargetItem {
  id: string;
  staffId: string;
  staffName: string;
  storeId?: string;
  storeName?: string;
  month: number;
  year: number;
  targetRevenue: number;
  targetOrders: number;
  notes?: string;
}

export interface SalesKpiReportItem {
  targetId?: string;
  staffId: string;
  staffName: string;
  storeId?: string;
  storeName?: string;
  month: number;
  year: number;
  targetRevenue: number;
  targetOrders: number;
  actualRevenue: number;
  actualOrders: number;
  revenueCompletionRate: number;
  orderCompletionRate: number;
}

