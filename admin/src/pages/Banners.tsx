import { useEffect, useState } from "react";
import { Button, Input, InputNumber, Modal, Popconfirm, Select, Space, Table, Tag, Typography, message } from "antd";
import { PlusOutlined, StopOutlined } from "@ant-design/icons";
import { api } from "../api";

interface Banner {
  id: number;
  title: string;
  imageUrl: string;
  linkUrl: string | null;
  position: string;
  sortOrder: number;
  isActive: boolean;
}

const POSITIONS = [
  { value: "home-hero", label: "Home hero (đầu trang chủ)" },
  { value: "home-mid", label: "Home mid (giữa trang chủ)" },
  { value: "category-top", label: "Đầu danh mục" },
];

const EMPTY_FORM = { title: "", imageUrl: "", linkUrl: "", position: "home-hero", sortOrder: 1 };

export default function Banners() {
  const [banners, setBanners] = useState<Banner[]>([]);
  const [loading, setLoading] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [messageApi, contextHolder] = message.useMessage();
  const [form, setForm] = useState(EMPTY_FORM);

  async function load() {
    setLoading(true);
    try {
      const res = await api.get("/api/v1/banners/admin");
      setBanners(res.data?.data ?? []);
    } catch {
      messageApi.error("Không tải được danh sách banner — backend đã chạy chưa?");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleCreate() {
    if (!form.title || !form.imageUrl) {
      messageApi.warning("Nhập đủ tiêu đề và URL ảnh.");
      return;
    }
    try {
      const res = await api.post("/api/v1/banners", {
        title: form.title,
        imageUrl: form.imageUrl,
        linkUrl: form.linkUrl || null,
        position: form.position,
        sortOrder: form.sortOrder,
      });
      if (res.data?.success) {
        messageApi.success("Đã tạo banner.");
        setCreateOpen(false);
        setForm(EMPTY_FORM);
        load();
      } else {
        messageApi.error(res.data?.message ?? "Tạo thất bại.");
      }
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Tạo thất bại.");
    }
  }

  async function deactivate(id: number) {
    try {
      await api.put(`/api/v1/banners/${id}/deactivate`);
      messageApi.success("Đã ẩn banner.");
      load();
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Thao tác thất bại.");
    }
  }

  const columns = [
    { title: "ID", dataIndex: "id", width: 60 },
    { title: "Tiêu đề", dataIndex: "title" },
    {
      title: "Ảnh",
      dataIndex: "imageUrl",
      width: 120,
      render: (v: string) =>
        v ? <img src={v} alt="" style={{ width: 96, height: 48, objectFit: "cover", borderRadius: 4 }} /> : "—",
    },
    { title: "Vị trí", dataIndex: "position", width: 130 },
    { title: "Thứ tự", dataIndex: "sortOrder", width: 70 },
    {
      title: "Trạng thái",
      dataIndex: "isActive",
      width: 100,
      render: (v: boolean) => (v ? <Tag color="green">Đang chạy</Tag> : <Tag>Ẩn</Tag>),
    },
    {
      title: "Hành động",
      width: 110,
      render: (_: unknown, record: Banner) =>
        record.isActive ? (
          <Popconfirm title="Ẩn banner này?" onConfirm={() => deactivate(record.id)}>
            <Button size="small" danger icon={<StopOutlined />}>
              Ẩn
            </Button>
          </Popconfirm>
        ) : (
          "—"
        ),
    },
  ];

  return (
    <div>
      {contextHolder}
      <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Banner ({banners.length})
        </Typography.Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>
          Thêm banner
        </Button>
      </div>

      <Table rowKey="id" columns={columns} dataSource={banners} loading={loading} pagination={false} />

      <Modal
        title="Thêm banner mới"
        open={createOpen}
        onOk={handleCreate}
        onCancel={() => setCreateOpen(false)}
        okText="Tạo"
        cancelText="Hủy"
      >
        <Space direction="vertical" style={{ width: "100%" }} size="middle">
          <Input
            placeholder="Tiêu đề *"
            value={form.title}
            onChange={(e) => setForm({ ...form, title: e.target.value })}
          />
          <Input
            placeholder="URL ảnh * (VD: /images/banner-hero.jpg)"
            value={form.imageUrl}
            onChange={(e) => setForm({ ...form, imageUrl: e.target.value })}
          />
          <Input
            placeholder="Link khi click (bỏ trống nếu không có)"
            value={form.linkUrl}
            onChange={(e) => setForm({ ...form, linkUrl: e.target.value })}
          />
          <Select
            style={{ width: "100%" }}
            value={form.position}
            onChange={(v) => setForm({ ...form, position: v })}
            options={POSITIONS}
          />
          <InputNumber
            min={0}
            placeholder="Thứ tự hiển thị"
            value={form.sortOrder}
            onChange={(v) => setForm({ ...form, sortOrder: v ?? 1 })}
            style={{ width: "100%" }}
          />
        </Space>
      </Modal>
    </div>
  );
}
