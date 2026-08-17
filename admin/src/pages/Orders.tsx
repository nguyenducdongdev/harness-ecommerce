import { useState } from "react";
import { Input, Table, Typography, message } from "antd";
import { SearchOutlined } from "@ant-design/icons";
import { api } from "../api";

interface Order {
  id: string;
  orderNumber: string;
  status: string;
  customerName: string;
  customerPhone: string;
  totalAmount: number;
  items: { productName: string; quantity: number }[];
}

const STATUS_LABELS: Record<string, string> = {
  PendingConfirmation: "Chờ xác nhận",
  Processing: "Đang xử lý",
  Shipping: "Đang giao",
  Delivered: "Đã giao",
  Completed: "Hoàn thành",
  Cancelled: "Đã hủy",
  Refunded: "Đã hoàn tiền",
};

const vnd = (v: number) => new Intl.NumberFormat("vi-VN").format(v) + "đ";

export default function Orders() {
  const [order, setOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState(false);
  const [messageApi, contextHolder] = message.useMessage();

  async function lookup(value: string) {
    if (!value.trim()) return;
    setLoading(true);
    try {
      const res = await api.get(`/api/v1/orders/${encodeURIComponent(value.trim())}`);
      setOrder(res.data?.data ?? null);
      if (!res.data?.success) messageApi.warning(res.data?.message ?? "Không tìm thấy đơn.");
    } catch (err: any) {
      setOrder(null);
      messageApi.error(err?.response?.data?.message ?? "Lỗi tra cứu.");
    } finally {
      setLoading(false);
    }
  }

  const columns = [
    { title: "Sản phẩm", dataIndex: "productName" },
    { title: "SL", dataIndex: "quantity", width: 60 },
  ];

  return (
    <div>
      {contextHolder}
      <Typography.Title level={3}>Đơn hàng</Typography.Title>
      <Typography.Paragraph type="secondary">
        Tra cứu đơn theo mã. Danh sách đơn đầy đủ + duyệt/hủy/đổi trạng thái sẽ thêm ở Phase 2
        (cần phân quyền JWT).
      </Typography.Paragraph>

      <Input.Search
        prefix={<SearchOutlined />}
        placeholder="Nhập mã đơn (VD: HD260816-ABC123)"
        onSearch={lookup}
        enterButton="Tra cứu"
        style={{ maxWidth: 420, marginBottom: 16 }}
        loading={loading}
      />

      {order && (
        <>
          <Typography.Title level={5}>
            {order.orderNumber} — {STATUS_LABELS[order.status] ?? order.status}
          </Typography.Title>
          <Typography.Paragraph>
            Khách: {order.customerName} ({order.customerPhone}) — Tổng:{" "}
            <strong>{vnd(order.totalAmount)}</strong>
          </Typography.Paragraph>
          <Table rowKey={(r) => r.productName} columns={columns} dataSource={order.items} pagination={false} size="small" style={{ maxWidth: 600 }} />
        </>
      )}
    </div>
  );
}
