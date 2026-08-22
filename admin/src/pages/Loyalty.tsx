import { useState } from "react";
import { Card, Input, Button, Descriptions, Table, Tag, Space, message } from "antd";
import { SearchOutlined, TrophyOutlined } from "@ant-design/icons";
import { api } from "../api";

interface LoyaltyInfo {
  customerId: string;
  points: number;
  tier: string;
  lifetimeSpend: number;
}

interface Reward {
  id: number;
  name: string;
  description?: string | null;
  pointsCost: number;
  value: number;
}

interface Transaction {
  id: string;
  pointsDelta: number;
  type: string;
  reference: string;
  note?: string | null;
  createdAt: string;
}

export default function Loyalty() {
  const [customerId, setCustomerId] = useState("");
  const [loyalty, setLoyalty] = useState<LoyaltyInfo | null>(null);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [rewards, setRewards] = useState<Reward[]>([]);
  const [loading, setLoading] = useState(false);
  const [messageApi, contextHolder] = message.useMessage();

  const search = async () => {
    if (!customerId.trim()) { messageApi.error("Nhập Customer ID."); return; }
    setLoading(true);
    setLoyalty(null);
    setTransactions([]);
    try {
      const [resLoyalty, resTxns, resRewards] = await Promise.all([
        api.get(`/api/v1/loyalty/${customerId.trim()}`),
        api.get(`/api/v1/loyalty/${customerId.trim()}/transactions`),
        api.get("/api/v1/loyalty/rewards"),
      ]);
      setLoyalty(resLoyalty.data.data);
      setTransactions(resTxns.data.data || []);
      setRewards(resRewards.data.data || []);
    } catch {
      messageApi.error("Không tìm thấy thông tin tích điểm cho khách hàng này.");
    } finally {
      setLoading(false);
    }
  };

  const txnColumns = [
    { title: "Ngày", dataIndex: "createdAt", key: "createdAt", render: (v: string) => new Date(v).toLocaleDateString("vi-VN") },
    {
      title: "Điểm", dataIndex: "pointsDelta", key: "pointsDelta", render: (v: number) => (
        <span style={{ color: v > 0 ? "green" : "red", fontWeight: 600 }}>
          {v > 0 ? `+${v}` : v}
        </span>
      ),
    },
    { title: "Loại", dataIndex: "type", key: "type" },
    { title: "Tham Chiếu", dataIndex: "reference", key: "reference", render: (v?: string) => v || "-" },
  ];

  const rewardColumns = [
    { title: "ID", dataIndex: "id", key: "id", width: 60 },
    { title: "Quà", dataIndex: "name", key: "name" },
    { title: "Mô Tả", dataIndex: "description", key: "description", render: (v?: string) => v || "-" },
    { title: "Điểm Cần", dataIndex: "pointsCost", key: "pointsCost", render: (v: number) => <Tag color="gold">{v} điểm</Tag> },
    { title: "Giá Trị", dataIndex: "value", key: "value", render: (v: number) => v.toLocaleString("vi-VN") + "₫" },
  ];

  return (
    <div>
      {contextHolder}
      <Space direction="vertical" size="middle" style={{ width: "100%" }}>
        <Card title={<span><TrophyOutlined style={{ marginRight: 8 }} /> Tra cứu Tích Điểm</span>}>
          <Space>
            <Input
              placeholder="Nhập Customer ID (GUID)"
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              onPressEnter={search}
              style={{ width: 360 }}
            />
            <Button type="primary" icon={<SearchOutlined />} onClick={search} loading={loading}>
              Tra cứu
            </Button>
          </Space>
        </Card>

        {loyalty && (
          <>
            <Card title="Thông tin Tích Điểm" size="small">
              <Descriptions column={2} size="small">
                <Descriptions.Item label="Customer ID">{loyalty.customerId}</Descriptions.Item>
                <Descriptions.Item label="Hạng">{loyalty.tier}</Descriptions.Item>
                <Descriptions.Item label="Điểm Hiện Tại">
                  <Tag color="gold" style={{ fontSize: 16, fontWeight: 700 }}>{loyalty.points}</Tag>
                </Descriptions.Item>
                <Descriptions.Item label="Tổng Chi Tiêu">{loyalty.lifetimeSpend.toLocaleString("vi-VN")}₫</Descriptions.Item>
              </Descriptions>
            </Card>

            <Card title="Kho Quà" size="small">
              <Table columns={rewardColumns} dataSource={rewards} rowKey="id" pagination={false} size="small" />
            </Card>

            <Card title="Lịch Sử Giao Dịch" size="small">
              <Table columns={txnColumns} dataSource={transactions} rowKey="id" pagination={false} size="small" />
            </Card>
          </>
        )}
      </Space>
    </div>
  );
}