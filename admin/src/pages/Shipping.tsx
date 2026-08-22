import { useState } from "react";
import { Card, Input, Button, Descriptions, Tag, Space, Select, message, Form, Modal } from "antd";
import { SearchOutlined, CarOutlined, SendOutlined } from "@ant-design/icons";
import { api } from "../api";

interface ShipmentInfo {
  id: string;
  orderId: string;
  status: string;
  carrier?: string | null;
  trackingCode?: string | null;
  estimatedDelivery?: string | null;
  createdAt: string;
}

const STATUS_OPTIONS = [
  { value: "Pending", label: "Chờ xử lý" },
  { value: "Confirmed", label: "Đã xác nhận" },
  { value: "PickedUp", label: "Đã lấy hàng" },
  { value: "InTransit", label: "Đang giao" },
  { value: "Delivered", label: "Đã giao" },
  { value: "Failed", label: "Thất bại" },
  { value: "Returned", label: "Hoàn hàng" },
];

export default function Shipping() {
  const [orderId, setOrderId] = useState("");
  const [shipment, setShipment] = useState<ShipmentInfo | null>(null);
  const [loading, setLoading] = useState(false);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [createForm] = Form.useForm();
  const [messageApi, contextHolder] = message.useMessage();

  const search = async () => {
    if (!orderId.trim()) { messageApi.error("Nhập mã đơn hàng (GUID)."); return; }
    setLoading(true);
    setShipment(null);
    try {
      const res = await api.get(`/api/v1/shipments/by-order/${orderId.trim()}`);
      setShipment(res.data.data);
    } catch {
      messageApi.error("Không tìm thấy lô hàng cho đơn này.");
    } finally {
      setLoading(false);
    }
  };

  const updateStatus = async (newStatus: string) => {
    if (!shipment) return;
    try {
      await api.put(`/api/v1/shipments/${shipment.id}/status`, { status: newStatus });
      messageApi.success("Cập nhật trạng thái thành công!");
      search();
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message || "Lỗi cập nhật.");
    }
  };

  const handleCreate = async (values: any) => {
    try {
      await api.post("/api/v1/shipments", values);
      messageApi.success("Tạo lô hàng thành công!");
      setCreateModalOpen(false);
      createForm.resetFields();
      setOrderId(values.orderId);
      search();
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message || "Lỗi tạo lô hàng.");
    }
  };

  const statusColor: Record<string, string> = {
    Pending: "default", Confirmed: "processing", PickedUp: "blue",
    InTransit: "processing", Delivered: "success", Failed: "error", Returned: "warning",
  };

  return (
    <div>
      {contextHolder}
      <Card
        title={<span><CarOutlined style={{ marginRight: 8 }} /> Quản Lý Vận Chuyển</span>}
        extra={<Button icon={<SendOutlined />} onClick={() => { createForm.resetFields(); setCreateModalOpen(true); }}>Tạo Lô Hàng</Button>}
      >
        <Space direction="vertical" size="middle" style={{ width: "100%" }}>
          <Space>
            <Input
              placeholder="Nhập Order ID (GUID)"
              value={orderId}
              onChange={(e) => setOrderId(e.target.value)}
              onPressEnter={search}
              style={{ width: 360 }}
            />
            <Button type="primary" icon={<SearchOutlined />} onClick={search} loading={loading}>
              Tra cứu
            </Button>
          </Space>

          {shipment && (
            <>
              <Descriptions bordered column={1} size="small">
                <Descriptions.Item label="Mã Lô Hàng">{shipment.id}</Descriptions.Item>
                <Descriptions.Item label="Mã Đơn Hàng">{shipment.orderId}</Descriptions.Item>
                <Descriptions.Item label="Trạng Thái">
                  <Tag color={statusColor[shipment.status] || "default"}>{shipment.status}</Tag>
                </Descriptions.Item>
                <Descriptions.Item label="Đơn Vị VC">{shipment.carrier ?? "-"}</Descriptions.Item>
                <Descriptions.Item label="Mã Tracking">{shipment.trackingCode ?? "-"}</Descriptions.Item>
                <Descriptions.Item label="Dự Kiến Giao">{shipment.estimatedDelivery ?? "-"}</Descriptions.Item>
              </Descriptions>

              <Space style={{ marginTop: 12 }}>
                <Select
                  placeholder="Cập nhật trạng thái..."
                  options={STATUS_OPTIONS}
                  onChange={updateStatus}
                  style={{ width: 200 }}
                />
              </Space>
            </>
          )}
        </Space>
      </Card>

      <Modal
        title="Tạo Lô Hàng Mới"
        open={createModalOpen}
        onCancel={() => setCreateModalOpen(false)}
        onOk={() => createForm.submit()}
      >
        <Form form={createForm} layout="vertical" onFinish={handleCreate}>
          <Form.Item label="Order ID" name="orderId" rules={[{ required: true, message: "Nhập Order ID" }]}>
            <Input placeholder="GUID của đơn hàng" />
          </Form.Item>
          <Form.Item label="Đơn Vị Vận Chuyển" name="carrier">
            <Select
              placeholder="Chọn hãng VC"
              options={[
                { value: "GHN", label: "GHN" },
                { value: "GHTK", label: "GHTK" },
                { value: "Internal", label: "Nội bộ" },
              ]}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}