import { useEffect, useState } from "react";
import { Table, Button, Modal, Form, Input, Switch, Tag, Space, Card, message } from "antd";
import { PlusOutlined, EditOutlined, DeleteOutlined, ShopOutlined } from "@ant-design/icons";
import { api, StoreItem } from "../api";

export default function Stores() {
  const [stores, setStores] = useState<StoreItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingStore, setEditingStore] = useState<StoreItem | null>(null);
  const [form] = Form.useForm();
  const [messageApi, contextHolder] = message.useMessage();

  const fetchStores = async () => {
    setLoading(true);
    try {
      const res = await api.get("/api/v1/admin/stores");
      setStores(res.data.data || []);
    } catch {
      messageApi.error("Không thể tải danh sách cửa hàng.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchStores();
  }, []);

  const handleSave = async (values: any) => {
    try {
      if (editingStore) {
        await api.put(`/api/v1/admin/stores/${editingStore.id}`, {
          name: values.name,
          address: values.address,
          phone: values.phone || "",
          managerName: values.managerName || "",
          isActive: values.isActive ?? true,
          latitude: values.latitude ?? null,
          longitude: values.longitude ?? null,
        });
        messageApi.success("Cập nhật cửa hàng thành công!");
      } else {
        await api.post("/api/v1/admin/stores", {
          code: values.code,
          name: values.name,
          address: values.address,
          phone: values.phone || "",
          managerName: values.managerName || "",
          latitude: values.latitude ?? null,
          longitude: values.longitude ?? null,
        });
        messageApi.success("Thêm mới cửa hàng thành công!");
      }
      setIsModalOpen(false);
      form.resetFields();
      setEditingStore(null);
      fetchStores();
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message || "Đã có lỗi xảy ra.");
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/api/v1/admin/stores/${id}`);
      messageApi.success("Đã xóa cửa hàng.");
      fetchStores();
    } catch {
      messageApi.error("Không thể xóa cửa hàng.");
    }
  };

  const openEditModal = (store: StoreItem) => {
    setEditingStore(store);
    form.setFieldsValue({
      code: store.code,
      name: store.name,
      address: store.address,
      phone: store.phone,
      managerName: store.managerName,
      isActive: store.isActive,
      latitude: store.latitude ?? null,
      longitude: store.longitude ?? null,
    });
    setIsModalOpen(true);
  };

  const columns = [
    { title: "Mã Cửa Hàng", dataIndex: "code", key: "code", render: (text: string) => <strong>{text}</strong> },
    { title: "Tên Cửa Hàng", dataIndex: "name", key: "name" },
    { title: "Vĩ Độ", dataIndex: "latitude", key: "latitude", render: (v?: number) => v?.toFixed(4) ?? "-" },
    { title: "Kinh Độ", dataIndex: "longitude", key: "longitude", render: (v?: number) => v?.toFixed(4) ?? "-" },
    { title: "Địa Chỉ", dataIndex: "address", key: "address" },
    { title: "Số Điện Thoại", dataIndex: "phone", key: "phone" },
    { title: "Quản Lý", dataIndex: "managerName", key: "managerName", render: (text?: string) => text || "-" },
    {
      title: "Trạng Thái",
      dataIndex: "isActive",
      key: "isActive",
      render: (active: boolean) => (active ? <Tag color="green">Đang hoạt động</Tag> : <Tag color="red">Tạm dừng</Tag>),
    },
    {
      title: "Thao Tác",
      key: "action",
      render: (_: any, record: StoreItem) => (
        <Space>
          <Button icon={<EditOutlined />} onClick={() => openEditModal(record)} />
          <Button icon={<DeleteOutlined />} danger onClick={() => handleDelete(record.id)} />
        </Space>
      ),
    },
  ];

  return (
    <div>
      {contextHolder}
      <Card
        title={
          <span>
            <ShopOutlined style={{ marginRight: 8 }} /> Quản lý Chuỗi Cửa Hàng / Showroom
          </span>
        }
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setEditingStore(null);
              form.resetFields();
              setIsModalOpen(true);
            }}
          >
            Thêm Cửa Hàng
          </Button>
        }
      >
        <Table columns={columns} dataSource={stores} rowKey="id" loading={loading} />
      </Card>

      <Modal
        title={editingStore ? "Cập Nhật Cửa Hàng" : "Thêm Mới Cửa Hàng"}
        open={isModalOpen}
        onCancel={() => {
          setIsModalOpen(false);
          setEditingStore(null);
        }}
        onOk={() => form.submit()}
      >
        <Form form={form} layout="vertical" onFinish={handleSave}>
          {!editingStore && (
            <Form.Item label="Mã Cửa Hàng" name="code" rules={[{ required: true, message: "Vui lòng nhập mã" }]}>
              <Input placeholder="VD: CH-Q1" />
            </Form.Item>
          )}
          <Form.Item label="Tên Cửa Hàng" name="name" rules={[{ required: true, message: "Vui lòng nhập tên" }]}>
            <Input placeholder="VD: Showroom Quận 1" />
          </Form.Item>
          <Form.Item label="Địa Chỉ" name="address" rules={[{ required: true, message: "Vui lòng nhập địa chỉ" }]}>
            <Input placeholder="123 Nguyễn Huệ, Q1, TP.HCM" />
          </Form.Item>
          <Form.Item label="Số Điện Thoại" name="phone">
            <Input placeholder="02812345678" />
          </Form.Item>
          <Form.Item label="Tên Quản Lý" name="managerName">
            <Input placeholder="Nguyễn Văn A" />
          </Form.Item>
          {editingStore && (
            <Form.Item label="Trạng Thái Hoạt Động" name="isActive" valuePropName="checked">
              <Switch />
            </Form.Item>
          )}
          <Form.Item label="Vĩ Độ (Latitude)" name="latitude">
            <Input type="number" step="0.0001" placeholder="VD: 10.7723" />
          </Form.Item>
          <Form.Item label="Kinh Độ (Longitude)" name="longitude">
            <Input type="number" step="0.0001" placeholder="VD: 106.7043" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
