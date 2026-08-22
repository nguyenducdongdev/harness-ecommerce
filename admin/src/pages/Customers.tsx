import { useEffect, useState } from "react";
import { Table, Input, Button, Card, Tag, Space, message } from "antd";
import { SearchOutlined, UserOutlined } from "@ant-design/icons";
import { api } from "../api";

interface CustomerInfo {
  id: string;
  fullName: string;
  phone: string;
  email?: string | null;
}

export default function Customers() {
  const [phone, setPhone] = useState("");
  const [customer, setCustomer] = useState<CustomerInfo | null>(null);
  const [loading, setLoading] = useState(false);
  const [messageApi, contextHolder] = message.useMessage();

  const search = async () => {
    if (!/^0\d{9,10}$/.test(phone)) {
      messageApi.error("Số điện thoại không hợp lệ (VD: 0912345678).");
      return;
    }
    setLoading(true);
    setCustomer(null);
    try {
      const res = await api.get("/api/v1/customers/by-phone", { params: { phone } });
      setCustomer(res.data.data);
    } catch {
      messageApi.error("Không tìm thấy khách hàng với số điện thoại này.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      {contextHolder}
      <Card
        title={<span><UserOutlined style={{ marginRight: 8 }} /> Tra cứu Khách Hàng</span>}
      >
        <Space direction="vertical" size="middle" style={{ width: "100%" }}>
          <Space>
            <Input
              placeholder="Nhập SĐT (VD: 0912345678)"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              onPressEnter={search}
              style={{ width: 260 }}
              maxLength={11}
            />
            <Button type="primary" icon={<SearchOutlined />} onClick={search} loading={loading}>
              Tra cứu
            </Button>
          </Space>

          {customer && (
            <Table
              dataSource={[customer]}
              rowKey="id"
              columns={[
                { title: "Họ Tên", dataIndex: "fullName", key: "fullName" },
                { title: "SĐT", dataIndex: "phone", key: "phone" },
                { title: "Email", dataIndex: "email", key: "email", render: (v?: string) => v || "-" },
              ]}
              pagination={false}
            />
          )}
        </Space>
      </Card>
    </div>
  );
}