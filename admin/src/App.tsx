import { useState } from "react";
import { Navigate, Route, Routes, useLocation, useNavigate } from "react-router-dom";
import {
  ApiOutlined,
  AppstoreOutlined,
  DatabaseOutlined,
  GiftOutlined,
  HomeOutlined,
  PictureOutlined,
  ShoppingCartOutlined,
  StarOutlined,
  ShopOutlined,
  CalendarOutlined,
  TrophyOutlined,
} from "@ant-design/icons";
import { Layout, Menu, Typography } from "antd";
import Dashboard from "./pages/Dashboard";
import Products from "./pages/Products";
import Orders from "./pages/Orders";
import Reviews from "./pages/Reviews";
import Banners from "./pages/Banners";
import Promotions from "./pages/Promotions";
import Stocks from "./pages/Stocks";
import Integration from "./pages/Integration";
import Stores from "./pages/Stores";
import Attendance from "./pages/Attendance";
import Kpi from "./pages/Kpi";
import Login from "./pages/Login";

const { Header, Sider, Content } = Layout;

// Auth: JWT + roles từ login thật (POST /api/v1/auth/admin/login)
interface AdminProfile {
  username: string;
  displayName: string;
  roles: string[];
}

function readProfile(): AdminProfile | null {
  try {
    const raw = localStorage.getItem("harness-admin-profile");
    return raw ? (JSON.parse(raw) as AdminProfile) : null;
  } catch {
    return null;
  }
}

function useAuth() {
  const [token, setToken] = useState<string | null>(localStorage.getItem("harness-admin-token"));
  const [profile, setProfile] = useState<AdminProfile | null>(readProfile());
  const login = (t: string) => {
    localStorage.setItem("harness-admin-token", t);
    setToken(t);
    setProfile(readProfile());
  };
  const logout = () => {
    localStorage.removeItem("harness-admin-token");
    localStorage.removeItem("harness-admin-profile");
    setToken(null);
    setProfile(null);
  };
  return { token, profile, login, logout };
}

const hasRole = (profile: AdminProfile | null, ...roles: string[]) =>
  !!profile && profile.roles.some((r) => roles.includes(r));

export default function App() {
  const { token, profile, login, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  if (!token) return <Login onSuccess={login} />;

  const selectedKey = location.pathname.replace("/", "") || "dashboard";

  const menuItems = [
    { key: "dashboard", icon: <HomeOutlined />, label: "Tổng quan" },
    { key: "products", icon: <AppstoreOutlined />, label: "Sản phẩm" },
    { key: "orders", icon: <ShoppingCartOutlined />, label: "Đơn hàng" },
    ...(hasRole(profile, "Admin", "SuperAdmin", "Warehouse")
      ? [{ key: "stocks", icon: <DatabaseOutlined />, label: "Tồn kho" }]
      : []),
    ...(hasRole(profile, "Admin", "SuperAdmin")
      ? [{ key: "promotions", icon: <GiftOutlined />, label: "Khuyến mãi" }]
      : []),
    ...(hasRole(profile, "Admin", "SuperAdmin", "Reviewer")
      ? [{ key: "reviews", icon: <StarOutlined />, label: "Kiểm duyệt" }]
      : []),
    ...(hasRole(profile, "Admin", "SuperAdmin", "Content")
      ? [{ key: "banners", icon: <PictureOutlined />, label: "Banner" }]
      : []),
    ...(hasRole(profile, "Admin", "SuperAdmin", "Operations")
      ? [
          { key: "stores", icon: <ShopOutlined />, label: "Cửa hàng" },
          { key: "attendance", icon: <CalendarOutlined />, label: "Chấm công" },
          { key: "kpi", icon: <TrophyOutlined />, label: "KPI Sales" },
          { key: "integration", icon: <ApiOutlined />, label: "Tích hợp" },
        ]
      : []),
  ];

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Sider theme="dark">
        <div style={{ color: "#fff", padding: 16, fontWeight: 700, fontSize: 18 }}>Harness Admin</div>
        <Menu
          theme="dark"
          selectedKeys={[selectedKey]}
          onClick={({ key }) => navigate(`/${key}`)}
          items={menuItems}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            background: "#fff",
            display: "flex",
            justifyContent: "flex-end",
            alignItems: "center",
            gap: 16,
          }}
        >
          <span>
            {profile?.displayName ?? profile?.username ?? "Admin"}{" "}
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {profile?.roles?.join(", ")}
            </Typography.Text>
          </span>
          <a onClick={logout} style={{ cursor: "pointer" }}>
            Đăng xuất
          </a>
        </Header>
        <Content style={{ margin: 16 }}>
          <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/products" element={<Products />} />
            <Route path="/orders" element={<Orders />} />
            <Route path="/stocks" element={<Stocks />} />
            <Route path="/promotions" element={<Promotions />} />
            <Route path="/reviews" element={<Reviews />} />
            <Route path="/banners" element={<Banners />} />
            <Route path="/stores" element={<Stores />} />
            <Route path="/attendance" element={<Attendance />} />
            <Route path="/kpi" element={<Kpi />} />
            <Route path="/integration" element={<Integration />} />
          </Routes>
        </Content>
      </Layout>
    </Layout>
  );
}
