import { useEffect, useState } from "react";
import { Button, Input, Modal, Select, Space, Table, Typography, message } from "antd";
import { PlusOutlined, SearchOutlined } from "@ant-design/icons";
import { api } from "../api";

interface Product {
  id: number;
  name: string;
  sku: string;
  categoryName: string | null;
  brandName: string | null;
  price: number;
  salePrice: number | null;
  displayPrice: number;
  isActive: boolean;
}

interface Category {
  id: number;
  name: string;
}

const vnd = (v: number) => new Intl.NumberFormat("vi-VN").format(v) + "đ";

export default function Products() {
  const [products, setProducts] = useState<Product[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(false);
  const [categories, setCategories] = useState<Category[]>([]);
  const [createOpen, setCreateOpen] = useState(false);
  const [messageApi, contextHolder] = message.useMessage();
  const [form, setForm] = useState({ name: "", categoryId: 0, brandId: 1, price: 0, salePrice: null as number | null });

  async function load(p = page, term = search) {
    setLoading(true);
    try {
      const res = await api.get("/api/v1/products", {
        params: { page: p, pageSize: 10, searchTerm: term || undefined },
      });
      setProducts(res.data?.data?.items ?? []);
      setTotal(res.data?.data?.totalCount ?? 0);
    } catch {
      messageApi.error("Không tải được danh sách sản phẩm — backend đã chạy chưa?");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load(1);
    api
      .get("/api/v1/categories")
      .then((r) => setCategories(r.data?.data ?? []))
      .catch(() => setCategories([]));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleCreate() {
    if (!form.name || !form.categoryId || !form.price) {
      messageApi.warning("Nhập đủ tên, danh mục và giá.");
      return;
    }
    try {
      const res = await api.post("/api/v1/products", {
        name: form.name,
        categoryId: form.categoryId,
        brandId: form.brandId,
        price: form.price,
        salePrice: form.salePrice,
        warrantyMonths: 36,
      });
      if (res.data?.success) {
        messageApi.success("Đã tạo sản phẩm.");
        setCreateOpen(false);
        setForm({ name: "", categoryId: 0, brandId: 1, price: 0, salePrice: null });
        load(1);
      } else {
        messageApi.error(res.data?.message ?? "Tạo thất bại.");
      }
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Tạo thất bại.");
    }
  }

  const columns = [
    { title: "ID", dataIndex: "id", width: 60 },
    { title: "Tên sản phẩm", dataIndex: "name" },
    { title: "SKU", dataIndex: "sku", width: 170 },
    { title: "Danh mục", dataIndex: "categoryName", width: 130 },
    { title: "Thương hiệu", dataIndex: "brandName", width: 120 },
    {
      title: "Giá bán",
      dataIndex: "displayPrice",
      width: 130,
      render: (v: number, record: Product) => (
        <Space direction="vertical" size={0}>
          <span>{vnd(v)}</span>
          {record.salePrice != null && (
            <span style={{ textDecoration: "line-through", color: "#999", fontSize: 12 }}>
              {vnd(record.price)}
            </span>
          )}
        </Space>
      ),
    },
    {
      title: "Trạng thái",
      dataIndex: "isActive",
      width: 110,
      render: (v: boolean) => (v ? "Đang bán" : "Ẩn"),
    },
  ];

  return (
    <div>
      {contextHolder}
      <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Sản phẩm ({total})
        </Typography.Title>
        <Space>
          <Input
            prefix={<SearchOutlined />}
            placeholder="Tìm theo tên/SKU"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onPressEnter={() => {
              setPage(1);
              load(1, search);
            }}
            style={{ width: 240 }}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>
            Thêm sản phẩm
          </Button>
        </Space>
      </div>

      <Table
        rowKey="id"
        columns={columns}
        dataSource={products}
        loading={loading}
        pagination={{
          current: page,
          total,
          pageSize: 10,
          onChange: (p) => {
            setPage(p);
            load(p);
          },
        }}
      />

      <Modal
        title="Thêm sản phẩm mới"
        open={createOpen}
        onOk={handleCreate}
        onCancel={() => setCreateOpen(false)}
        okText="Tạo"
        cancelText="Hủy"
      >
        <Space direction="vertical" style={{ width: "100%" }} size="middle">
          <Input
            placeholder="Tên sản phẩm *"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
          />
          <Select
            placeholder="Danh mục *"
            style={{ width: "100%" }}
            value={form.categoryId || undefined}
            onChange={(v) => setForm({ ...form, categoryId: v })}
            options={categories.map((c) => ({ value: c.id, label: c.name }))}
          />
          <Input
            type="number"
            placeholder="Giá bán (VND) *"
            value={form.price || ""}
            onChange={(e) => setForm({ ...form, price: Number(e.target.value) })}
          />
          <Input
            type="number"
            placeholder="Giá khuyến mãi (bỏ trống nếu không có)"
            value={form.salePrice ?? ""}
            onChange={(e) =>
              setForm({ ...form, salePrice: e.target.value ? Number(e.target.value) : null })
            }
          />
        </Space>
      </Modal>
    </div>
  );
}
