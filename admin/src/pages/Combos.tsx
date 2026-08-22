import { useEffect, useState } from "react";
import { Table, Button, Modal, Form, Input, InputNumber, Select, Card, Space, Tag, message } from "antd";
import { PlusOutlined, AppstoreOutlined } from "@ant-design/icons";
import { api } from "../api";

interface ComboItem {
  id: number;
  name: string;
  slug: string;
  roomType: string;
  roomTypeLabel: string;
  description?: string | null;
  isActive: boolean;
  discountedPrice?: number | null;
  regularTotal: number;
  saleTotal: number;
  savings: number;
}

const ROOM_TYPES = [
  { value: "LivingRoom", label: "Phòng khách" },
  { value: "BedRoom", label: "Phòng ngủ" },
  { value: "DiningRoom", label: "Phòng bếp/ăn" },
  { value: "HomeOffice", label: "Văn phòng tại nhà" },
];

export default function Combos() {
  const [combos, setCombos] = useState<ComboItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [form] = Form.useForm();
  const [messageApi, contextHolder] = message.useMessage();

  const fetchCombos = async () => {
    setLoading(true);
    try {
      const res = await api.get("/api/v1/combos", { params: { onlyActive: false } });
      setCombos(res.data.data || []);
    } catch {
      messageApi.error("Không thể tải danh sách combo.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchCombos(); }, []);

  const handleCreate = async (values: any) => {
    try {
      await api.post("/api/v1/combos", {
        name: values.name,
        roomType: values.roomType,
        description: values.description || null,
        discountedPrice: values.discountedPrice || null,
        items: values.items?.filter((i: any) => i.productId) || [],
      });
      messageApi.success("Tạo combo thành công!");
      setIsModalOpen(false);
      form.resetFields();
      fetchCombos();
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message || "Đã có lỗi xảy ra.");
    }
  };

  const columns = [
    { title: "ID", dataIndex: "id", key: "id", width: 60 },
    { title: "Tên Combo", dataIndex: "name", key: "name", render: (text: string) => <strong>{text}</strong> },
    { title: "Phòng", dataIndex: "roomTypeLabel", key: "roomTypeLabel" },
    {
      title: "Giá Gốc", dataIndex: "regularTotal", key: "regularTotal", align: "right" as const,
      render: (v: number) => v.toLocaleString("vi-VN") + "₫",
    },
    {
      title: "Giá Bán", dataIndex: "saleTotal", key: "saleTotal", align: "right" as const,
      render: (v: number) => <strong>{v.toLocaleString("vi-VN")}₫</strong>,
    },
    {
      title: "Tiết Kiệm", dataIndex: "savings", key: "savings", align: "right" as const,
      render: (v: number) => v > 0 ? <Tag color="green">{v.toLocaleString("vi-VN")}₫</Tag> : "-",
    },
    { title: "Trạng Thái", dataIndex: "isActive", key: "isActive", width: 100,
      render: (active: boolean) => active
        ? <Tag color="green">Hoạt động</Tag>
        : <Tag color="red">Ẩn</Tag>,
    },
  ];

  return (
    <div>
      {contextHolder}
      <Card
        title={<span><AppstoreOutlined style={{ marginRight: 8 }} /> Combo Phòng</span>}
        extra={<Button type="primary" icon={<PlusOutlined />} onClick={() => { form.resetFields(); setIsModalOpen(true); }}>Tạo Combo</Button>}
      >
        <Table columns={columns} dataSource={combos} rowKey="id" loading={loading} />

        <Modal
          title="Tạo Combo Mới"
          open={isModalOpen}
          onCancel={() => setIsModalOpen(false)}
          onOk={() => form.submit()}
          width={600}
        >
          <Form form={form} layout="vertical" onFinish={handleCreate}>
            <Form.Item label="Tên Combo" name="name" rules={[{ required: true, message: "Nhập tên combo" }]}>
              <Input placeholder="VD: Bộ Sofa + Bàn Trà Phòng Khách" />
            </Form.Item>
            <Form.Item label="Loại Phòng" name="roomType" rules={[{ required: true, message: "Chọn loại phòng" }]}>
              <Select options={ROOM_TYPES} placeholder="Chọn loại phòng" />
            </Form.Item>
            <Form.Item label="Mô Tả" name="description">
              <Input.TextArea rows={2} placeholder="Mô tả combo (không bắt buộc)" />
            </Form.Item>
            <Form.Item label="Giá KM (nếu có)" name="discountedPrice">
              <InputNumber style={{ width: "100%" }} min={0} placeholder="Để trống nếu không giảm" />
            </Form.Item>

            {/* Items sub-form */}
            <Form.List name="items">
              {(fields, { add, remove }) => (
                <>
                  {fields.map(({ key, name, ...rest }) => (
                    <Space key={key} align="baseline" style={{ display: "flex", marginBottom: 8 }}>
                      <Form.Item {...rest} name={[name, "productId"]} rules={[{ required: true, message: "Nhập ID sản phẩm" }]}>
                        <InputNumber min={1} placeholder="ID SP" style={{ width: 100 }} />
                      </Form.Item>
                      <Form.Item {...rest} name={[name, "quantity"]} rules={[{ required: true, message: "SL" }]}>
                        <InputNumber min={1} max={20} placeholder="SL" style={{ width: 70 }} />
                      </Form.Item>
                      <Button danger onClick={() => remove(name)}>✕</Button>
                    </Space>
                  ))}
                  <Button type="dashed" onClick={() => add()}>+ Thêm sản phẩm</Button>
                </>
              )}
            </Form.List>
          </Form>
        </Modal>
      </Card>
    </div>
  );
}