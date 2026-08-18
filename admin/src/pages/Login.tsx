import { useState } from "react";
import { Alert, Button, Card, Form, Input, Typography } from "antd";
import { LockOutlined, UserOutlined } from "@ant-design/icons";
import { api } from "../api";

interface Props {
  onSuccess: (token: string) => void;
}

interface LoginData {
  accessToken: string;
  expiresAt: string;
  adminId: string;
  username: string;
  displayName: string;
  roles: string[];
}

// Đăng nhập admin thật: POST /api/v1/auth/admin/login → JWT (roles trong claims)
export default function Login({ onSuccess }: Props) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleLogin(values: { username: string; password: string }) {
    setLoading(true);
    setError(null);
    try {
      const res = await api.post("/api/v1/auth/admin/login", {
        username: values.username,
        password: values.password,
      });
      if (res.data?.success && res.data?.data?.accessToken) {
        const data: LoginData = res.data.data;
        localStorage.setItem("harness-admin-token", data.accessToken);
        localStorage.setItem("harness-admin-profile", JSON.stringify(data));
        onSuccess(data.accessToken);
      } else {
        setError(res.data?.message ?? "Đăng nhập thất bại.");
      }
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Không kết nối được backend.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "#f5f5f5",
      }}
    >
      <Card style={{ width: 380 }}>
        <Typography.Title level={3} style={{ textAlign: "center" }}>
          Harness Admin
        </Typography.Title>
        <Typography.Paragraph type="secondary" style={{ textAlign: "center" }}>
          Đăng nhập hệ thống quản trị
        </Typography.Paragraph>
        {error && <Alert type="error" message={error} showIcon style={{ marginBottom: 16 }} />}
        <Form onFinish={handleLogin} layout="vertical">
          <Form.Item name="username" rules={[{ required: true, message: "Nhập tài khoản" }]}>
            <Input prefix={<UserOutlined />} placeholder="Tài khoản" size="large" />
          </Form.Item>
          <Form.Item name="password" rules={[{ required: true, message: "Nhập mật khẩu" }]}>
            <Input.Password prefix={<LockOutlined />} placeholder="Mật khẩu" size="large" />
          </Form.Item>
          <Button type="primary" htmlType="submit" block size="large" loading={loading}>
            Đăng nhập
          </Button>
        </Form>
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          Tài khoản mặc định (dev): <strong>admin</strong> / <strong>Harness@123</strong> — đổi ngay trên
          production.
        </Typography.Text>
      </Card>
    </div>
  );
}
