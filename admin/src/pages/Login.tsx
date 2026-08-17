import { useState } from "react";
import { Button, Card, Form, Input, Typography } from "antd";
import { LockOutlined, UserOutlined } from "@ant-design/icons";

interface Props {
  onSuccess: (token: string) => void;
}

// Login stub — Phase 2: gọi POST /api/v1/auth/login nhận JWT thật
export default function Login({ onSuccess }: Props) {
  const [loading, setLoading] = useState(false);

  async function handleLogin(values: { username: string; password: string }) {
    setLoading(true);
    try {
      // Stub: chấp nhận mọi tài khoản trong Phase 1
      await new Promise((r) => setTimeout(r, 500));
      onSuccess(`stub-token-${values.username}`);
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
          Phase 1: đăng nhập mô phỏng. JWT + phân quyền RBAC sẽ hoàn thiện ở Phase 2.
        </Typography.Text>
      </Card>
    </div>
  );
}
