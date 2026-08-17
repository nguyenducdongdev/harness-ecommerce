import { useState } from "react";
import { Navigate, Route, Routes, useLocation, useNavigate } from "react-router-dom";
import { AppstoreOutlined, HomeOutlined, ShoppingCartOutlined } from "@ant-design/icons";
import { Layout, Menu } from "antd";
import Dashboard from "./pages/Dashboard";
import Products from "./pages/Products";
import Orders from "./pages/Orders";
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
          </Routes>
        </Content>
      </Layout>
    </Layout>
  );
}
