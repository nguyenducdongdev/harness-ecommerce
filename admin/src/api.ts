import axios from "axios";

// axios instance — qua proxy /api của Vite trong dev
export const api = axios.create({
  baseURL: "",
  timeout: 15000,
});

// Phase 2: gắn JWT vào header khi có token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("harness-admin-token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
