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
