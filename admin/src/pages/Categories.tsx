import { useEffect, useState } from "react";
import { Table, Card, Tag, Switch, message } from "antd";
import { AppstoreOutlined } from "@ant-design/icons";
import { api } from "../api";

interface CategoryItem {
  id: number;
  name: string;
  slug: string;
  description?: string | null;
  isActive: boolean;
  sortOrder: number;
  parentId?: number | null;
}

export default function Categories() {
  const [categories, setCategories] = useState<CategoryItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [messageApi, contextHolder] = message.useMessage();

  const fetch = async () => {
    setLoading(true);
    try {
      const res = await api.get("/api/v1/categories", { params: { onlyActive: false } });
      setCategories(res.data.data || []);
    } catch {
      messageApi.error("Không thể tải danh mục.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetch(); }, []);

  const columns = [
    { title: "ID", dataIndex: "id", key: "id", width: 60 },
    { title: "Tên Danh Mục", dataIndex: "name", key: "name", render: (text: string) => <strong>{text}</strong> },
    { title: "Slug", dataIndex: "slug", key: "slug" },
    { title: "Mô Tả", dataIndex: "description", key: "description", render: (v?: string) => v || "-" },
    { title: "Thứ Tự", dataIndex: "sortOrder", key: "sortOrder", width: 80 },
    {
      title: "Trạng Thái", dataIndex: "isActive", key: "isActive", width: 120,
      render: (active: boolean) => active
        ? <Tag color="green">Hoạt động</Tag>
        : <Tag color="red">Ẩn</Tag>,
    },
  ];

  return (
    <div>
      {contextHolder}
      <Card title={<span><AppstoreOutlined style={{ marginRight: 8 }} /> Danh Mục Sản Phẩm</span>}>
        <Table columns={columns} dataSource={categories} rowKey="id" loading={loading} />
      </Card>
    </div>
  );
}