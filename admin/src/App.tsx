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
} from "@ant-design/icons";
import { Layout, Menu } from "antd";
import Dashboard from "./pages/Dashboard";
import Products from "./pages/Products";
import Orders from "./pages/Orders";
import Reviews from "./pages/Reviews";
import Banners from "./pages/Banners";
import Promotions from "./pages/Promotions";
import Stocks from "./pages/Stocks";
import Integration from "./pages/Integration";
import Login from "./pages/Login";

const { Header, Sider, Content } = Layout;

// Auth stub — Phase 2 thay bằng JWT từ API + refresh token
function useAuth() {
  const [token, setToken] = useState<string | null>(localStorage.getItem("harness-admin-token"));
  const login = (t: string) => {
    localStorage.setItem("harness-admin-token", t);
    setToken(t);
  };
  const logout = () => {
    localStorage.removeItem("harness-admin-token");
    setToken(null);
  };
  return { token, login, logout };
}

export default function App() {
  const { token, login, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  if (!token) return <Login onSuccess={login} />;

  const selectedKey = location.pathname.replace("/", "") || "dashboard";

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Sider theme="dark">
        <div style={{ color: "#fff", padding: 16, fontWeight: 700, fontSize: 18 }}>Harness Admin</div>
        <Menu
          theme="dark"
          selectedKeys={[selectedKey]}
          onClick={({ key }) => navigate(`/${key}`)}
          items={[
            { key: "dashboard", icon: <HomeOutlined />, label: "Tổng quan" },
            { key: "products", icon: <AppstoreOutlined />, label: "Sản phẩm" },
            { key: "orders", icon: <ShoppingCartOutlined />, label: "Đơn hàng" },
            { key: "stocks", icon: <DatabaseOutlined />, label: "Tồn kho" },
            { key: "promotions", icon: <GiftOutlined />, label: "Khuyến mãi" },
            { key: "reviews", icon: <StarOutlined />, label: "Kiểm duyệt" },
            { key: "banners", icon: <PictureOutlined />, label: "Banner" },
            { key: "integration", icon: <ApiOutlined />, label: "Tích hợp" },
          ]}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            background: "#fff",
            display: "flex",
            justifyContent: "flex-end",
            alignItems: "center",
          }}
        >
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
            <Route path="/integration" element={<Integration />} />
          </Routes>
        </Content>
      </Layout>
    </Layout>
  );
}
