import { useEffect, useState } from "react";
import {
  Button,
  Card,
  Col,
  DatePicker,
  Divider,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Table,
  Typography,
  message,
} from "antd";
import { PlusOutlined, ThunderboltOutlined } from "@ant-design/icons";
import dayjs, { type Dayjs } from "dayjs";
import { api } from "../api";

interface FlashSaleItem {
  id: number;
  productId: number;
  productName: string;
  salePrice: number;
  quantityLimit: number;
  quantitySold: number;
  isSoldOut: boolean;
}

interface FlashSale {
  id: number;
  name: string;
  startAt: string;
  endAt: string;
  items: FlashSaleItem[];
}

interface Product {
  id: number;
  name: string;
}

const vnd = (v: number) => new Intl.NumberFormat("vi-VN").format(v) + "đ";

export default function Promotions() {
  const [messageApi, contextHolder] = message.useMessage();
  const [flashSales, setFlashSales] = useState<FlashSale[]>([]);
  const [products, setProducts] = useState<Product[]>([]);

  // Voucher form
  const [voucher, setVoucher] = useState({
    code: "",
    type: "Percent",
    value: 0,
    startAt: null as Dayjs | null,
    endAt: null as Dayjs | null,
    minOrderAmount: 0,
    maxDiscountAmount: null as number | null,
  });

  // Flash sale form
  const [flash, setFlash] = useState({ name: "", startAt: null as Dayjs | null, endAt: null as Dayjs | null });

  // Add item form
  const [item, setItem] = useState({
    flashSaleId: 0,
    productId: 0,
    salePrice: 0,
    quantityLimit: 0,
  });

  async function loadAll() {
    try {
      const [fs, pr] = await Promise.all([
        api.get("/api/v1/flash-sales/active"),
        api.get("/api/v1/products", { params: { page: 1, pageSize: 100 } }),
      ]);
      setFlashSales(fs.data?.data ?? []);
      setProducts(pr.data?.data?.items ?? []);
      if (item.flashSaleId === 0 && (fs.data?.data?.length ?? 0) > 0) {
        setItem((i) => ({ ...i, flashSaleId: fs.data.data[0].id }));
      }
    } catch {
      messageApi.error("Không tải được dữ liệu khuyến mãi — backend đã chạy chưa?");
    }
  }

  useEffect(() => {
    loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function createVoucher() {
    if (!voucher.code || !voucher.value || !voucher.startAt || !voucher.endAt) {
      messageApi.warning("Nhập đủ mã, giá trị và thời gian hiệu lực.");
      return;
    }
    try {
      const res = await api.post("/api/v1/vouchers", {
        code: voucher.code,
        type: voucher.type,
        value: voucher.value,
        startAt: voucher.startAt.toISOString(),
        endAt: voucher.endAt.toISOString(),
        minOrderAmount: voucher.minOrderAmount,
        maxDiscountAmount: voucher.maxDiscountAmount,
      });
      if (res.data?.success) {
        messageApi.success("Đã tạo voucher.");
        setVoucher({ code: "", type: "Percent", value: 0, startAt: null, endAt: null, minOrderAmount: 0, maxDiscountAmount: null });
      } else {
        messageApi.error(res.data?.message ?? "Tạo thất bại.");
      }
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Tạo thất bại.");
    }
  }

  async function createFlashSale() {
    if (!flash.name || !flash.startAt || !flash.endAt) {
      messageApi.warning("Nhập đủ tên và thời gian diễn ra.");
      return;
    }
    try {
      const res = await api.post("/api/v1/flash-sales", {
        name: flash.name,
        startAt: flash.startAt.toISOString(),
        endAt: flash.endAt.toISOString(),
      });
      if (res.data?.success) {
        messageApi.success("Đã tạo flash sale.");
        setFlash({ name: "", startAt: null, endAt: null });
        loadAll();
      } else {
        messageApi.error(res.data?.message ?? "Tạo thất bại.");
      }
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Tạo thất bại.");
    }
  }

  async function addItem() {
    if (!item.flashSaleId || !item.productId || !item.salePrice || !item.quantityLimit) {
      messageApi.warning("Chọn flash sale, sản phẩm, giá KM và số lượng.");
      return;
    }
    try {
      const res = await api.post(`/api/v1/flash-sales/${item.flashSaleId}/items`, {
        productId: item.productId,
        salePrice: item.salePrice,
        quantityLimit: item.quantityLimit,
      });
      if (res.data?.success) {
        messageApi.success("Đã thêm sản phẩm vào flash sale.");
        setItem({ flashSaleId: item.flashSaleId, productId: 0, salePrice: 0, quantityLimit: 0 });
        loadAll();
      } else {
        messageApi.error(res.data?.message ?? "Thêm thất bại.");
      }
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Thêm thất bại.");
    }
  }

  return (
    <div>
      {contextHolder}
      <Typography.Title level={3}>Khuyến mãi</Typography.Title>

      <Row gutter={16}>
        <Col span={12}>
          <Card title="Voucher" size="small">
            <Space direction="vertical" style={{ width: "100%" }} size="middle">
              <Space>
                <Input
                  placeholder="Mã (VD: SALE20) *"
                  value={voucher.code}
                  onChange={(e) => setVoucher({ ...voucher, code: e.target.value })}
                  style={{ width: 180 }}
                />
                <Select
                  style={{ width: 140 }}
                  value={voucher.type}
                  onChange={(v) => setVoucher({ ...voucher, type: v })}
                  options={[
                    { value: "Percent", label: "Phần trăm (%)" },
                    { value: "FixedAmount", label: "Số tiền cố định (đ)" },
                  ]}
                />
                <InputNumber
                  min={0}
                  placeholder="Giá trị"
                  value={voucher.value || undefined}
                  onChange={(v) => setVoucher({ ...voucher, value: v ?? 0 })}
                  style={{ width: 120 }}
                />
              </Space>
              <Space>
                <DatePicker
                  showTime
                  placeholder="Bắt đầu"
                  value={voucher.startAt}
                  onChange={(v) => setVoucher({ ...voucher, startAt: v })}
                />
                <DatePicker
                  showTime
                  placeholder="Kết thúc"
                  value={voucher.endAt}
                  onChange={(v) => setVoucher({ ...voucher, endAt: v })}
                />
              </Space>
              <Space>
                <InputNumber
                  min={0}
                  placeholder="Đơn tối thiểu"
                  value={voucher.minOrderAmount || undefined}
                  onChange={(v) => setVoucher({ ...voucher, minOrderAmount: v ?? 0 })}
                  style={{ width: 150 }}
                />
                <InputNumber
                  min={0}
                  placeholder="Giảm tối đa (bỏ trống)"
                  value={voucher.maxDiscountAmount ?? undefined}
                  onChange={(v) => setVoucher({ ...voucher, maxDiscountAmount: v ?? null })}
                  style={{ width: 180 }}
                />
              </Space>
              <Button type="primary" icon={<PlusOutlined />} onClick={createVoucher}>
                Tạo voucher
              </Button>
            </Space>
          </Card>
        </Col>

        <Col span={12}>
          <Card title={<span><ThunderboltOutlined /> Flash sale</span>} size="small">
            <Space direction="vertical" style={{ width: "100%" }} size="middle">
              <Input
                placeholder="Tên chương trình (VD: Săn sale nội thất 9.9) *"
                value={flash.name}
                onChange={(e) => setFlash({ ...flash, name: e.target.value })}
              />
              <Space>
                <DatePicker
                  showTime
                  placeholder="Bắt đầu"
                  value={flash.startAt}
                  onChange={(v) => setFlash({ ...flash, startAt: v })}
                />
                <DatePicker
                  showTime
                  placeholder="Kết thúc"
                  value={flash.endAt}
                  onChange={(v) => setFlash({ ...flash, endAt: v })}
                />
              </Space>
              <Button type="primary" icon={<PlusOutlined />} onClick={createFlashSale}>
                Tạo flash sale
              </Button>

              <Divider style={{ margin: "8px 0" }} />
              <Space.Compact style={{ width: "100%" }}>
                <Select
                  placeholder="Flash sale"
                  style={{ width: "35%" }}
                  value={item.flashSaleId || undefined}
                  onChange={(v) => setItem({ ...item, flashSaleId: v })}
                  options={flashSales.map((f) => ({ value: f.id, label: f.name }))}
                />
                <Select
                  showSearch
                  placeholder="Sản phẩm"
                  style={{ width: "35%" }}
                  value={item.productId || undefined}
                  onChange={(v) => setItem({ ...item, productId: v })}
                  options={products.map((p) => ({ value: p.id, label: p.name }))}
                  optionFilterProp="label"
                />
                <InputNumber
                  min={1000}
                  placeholder="Giá KM"
                  value={item.salePrice || undefined}
                  onChange={(v) => setItem({ ...item, salePrice: v ?? 0 })}
                  style={{ width: "15%" }}
                />
                <InputNumber
                  min={1}
                  placeholder="SL"
                  value={item.quantityLimit || undefined}
                  onChange={(v) => setItem({ ...item, quantityLimit: v ?? 0 })}
                  style={{ width: "15%" }}
                />
              </Space.Compact>
              <Button onClick={addItem}>Thêm sản phẩm vào flash sale</Button>
            </Space>
          </Card>
        </Col>
      </Row>

      <Card title="Flash sale đang chạy" size="small" style={{ marginTop: 16 }}>
        <Table
          rowKey="id"
          pagination={false}
          dataSource={flashSales}
          columns={[
            { title: "Tên chương trình", dataIndex: "name" },
            {
              title: "Thời gian",
              render: (_, r: FlashSale) =>
                `${dayjs(r.startAt).format("DD/MM HH:mm")} → ${dayjs(r.endAt).format("DD/MM HH:mm")}`,
            },
            { title: "Sản phẩm", render: (_: unknown, r: FlashSale) => r.items.length },
            {
              title: "Doanh số",
              render: (_: unknown, r: FlashSale) => r.items.reduce((s, i) => s + i.quantitySold, 0),
            },
            {
              title: "Chi tiết",
              render: (_: unknown, r: FlashSale) => (
                <Space direction="vertical" size={0}>
                  {r.items.map((i) => (
                    <span key={i.id} style={{ fontSize: 12 }}>
                      {i.productName} — {vnd(i.salePrice)} ({i.quantitySold}/{i.quantityLimit})
                      {i.isSoldOut ? " ❌" : ""}
                    </span>
                  ))}
                  {r.items.length === 0 && <Typography.Text type="secondary">Chưa có sản phẩm</Typography.Text>}
                </Space>
              ),
            },
          ]}
        />
      </Card>
    </div>
  );
}
