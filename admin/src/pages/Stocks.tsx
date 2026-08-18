import { useEffect, useState } from "react";
import { Button, Card, Col, Input, InputNumber, Row, Select, Space, Table, Typography, message } from "antd";
import { SearchOutlined } from "@ant-design/icons";
import { api } from "../api";

interface StockRow {
  warehouseId: number;
  variantSku: string;
  quantityAvailable: number;
  quantityReserved: number;
}

interface Warehouse {
  id: number;
  code: string;
  name: string;
  address: string;
  isShowroom: boolean;
}

export default function Stocks() {
  const [messageApi, contextHolder] = message.useMessage();
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [rows, setRows] = useState<StockRow[]>([]);
  const [sku, setSku] = useState("");
  const [loading, setLoading] = useState(false);

  // Adjust form
  const [adjust, setAdjust] = useState({ warehouseId: 0, delta: 0, reference: "" });
  // Set form
  const [setForm, setSetForm] = useState({ warehouseId: 0, quantity: 0, reference: "" });
  // Transfer form
  const [transfer, setTransfer] = useState({ fromWarehouseId: 0, toWarehouseId: 0, quantity: 0, reference: "" });

  useEffect(() => {
    api
      .get("/api/v1/warehouses")
      .then((r) => setWarehouses(r.data?.data ?? []))
      .catch(() => setWarehouses([]));
  }, []);

  async function lookup(value: string) {
    if (!value.trim()) return;
    setLoading(true);
    try {
      const res = await api.get(`/api/v1/stocks/${encodeURIComponent(value.trim())}`);
      setRows(res.data?.data ?? []);
      if (!res.data?.success) messageApi.warning(res.data?.message ?? "Không tìm thấy tồn kho.");
    } catch (err: any) {
      setRows([]);
      messageApi.error(err?.response?.data?.message ?? "Lỗi tra cứu.");
    } finally {
      setLoading(false);
    }
  }

  async function adjustStock() {
    if (!sku.trim() || !adjust.warehouseId || !adjust.delta || !adjust.reference) {
      messageApi.warning("Nhập đủ SKU, kho, số lượng (+/-) và lý do.");
      return;
    }
    try {
      const res = await api.post("/api/v1/stocks/adjust", {
        warehouseId: adjust.warehouseId,
        variantSku: sku.trim(),
        delta: adjust.delta,
        reference: adjust.reference,
      });
      if (res.data?.success) {
        messageApi.success("Đã điều chỉnh tồn kho.");
        setAdjust({ warehouseId: 0, delta: 0, reference: "" });
        lookup(sku);
      } else {
        messageApi.error(res.data?.message ?? "Thất bại.");
      }
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Thất bại.");
    }
  }

  async function setStock() {
    if (!sku.trim() || !setForm.warehouseId || !setForm.reference) {
      messageApi.warning("Nhập đủ SKU, kho, số lượng và lý do.");
      return;
    }
    try {
      const res = await api.post("/api/v1/stocks/set", {
        warehouseId: setForm.warehouseId,
        variantSku: sku.trim(),
        quantity: setForm.quantity,
        reference: setForm.reference,
      });
      if (res.data?.success) {
        messageApi.success("Đã khai báo tồn kho.");
        setSetForm({ warehouseId: 0, quantity: 0, reference: "" });
        lookup(sku);
      } else {
        messageApi.error(res.data?.message ?? "Thất bại.");
      }
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Thất bại.");
    }
  }

  async function transferStock() {
    if (!sku.trim() || !transfer.fromWarehouseId || !transfer.toWarehouseId || !transfer.quantity || !transfer.reference) {
      messageApi.warning("Nhập đủ SKU, kho nguồn, kho đích, số lượng và lý do.");
      return;
    }
    try {
      const res = await api.post("/api/v1/stocks/transfer", {
        fromWarehouseId: transfer.fromWarehouseId,
        toWarehouseId: transfer.toWarehouseId,
        variantSku: sku.trim(),
        quantity: transfer.quantity,
        reference: transfer.reference,
      });
      if (res.data?.success) {
        messageApi.success("Đã chuyển kho.");
        setTransfer({ fromWarehouseId: 0, toWarehouseId: 0, quantity: 0, reference: "" });
        lookup(sku);
      } else {
        messageApi.error(res.data?.message ?? "Thất bại.");
      }
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Thất bại.");
    }
  }

  const warehouseOptions = warehouses.map((w) => ({
    value: w.id,
    label: `${w.name}${w.isShowroom ? " (showroom)" : ""}`,
  }));

  return (
    <div>
      {contextHolder}
      <Typography.Title level={3}>Tồn kho theo showroom</Typography.Title>
      <Input.Search
        prefix={<SearchOutlined />}
        placeholder="Nhập SKU sản phẩm (VD: SKU-1)"
        onSearch={lookup}
        enterButton="Tra cứu"
        style={{ maxWidth: 400, marginBottom: 16 }}
        loading={loading}
        value={sku}
        onChange={(e) => setSku(e.target.value)}
      />

      <Table
        rowKey={(r) => `${r.warehouseId}-${r.variantSku}`}
        columns={[
          { title: "Kho", dataIndex: "warehouseId", render: (v: number) => warehouses.find((w) => w.id === v)?.name ?? `Kho #${v}` },
          { title: "SKU", dataIndex: "variantSku" },
          { title: "Sẵn sàng", dataIndex: "quantityAvailable" },
          { title: "Đang giữ", dataIndex: "quantityReserved" },
        ]}
        dataSource={rows}
        pagination={false}
        style={{ marginBottom: 16 }}
      />

      <Row gutter={16}>
        <Col span={8}>
          <Card title="Điều chỉnh" size="small">
            <Space direction="vertical" style={{ width: "100%" }} size="middle">
              <Select
                placeholder="Kho"
                style={{ width: "100%" }}
                value={adjust.warehouseId || undefined}
                onChange={(v) => setAdjust({ ...adjust, warehouseId: v })}
                options={warehouseOptions}
              />
              <InputNumber
                placeholder="Số lượng (+/-) *"
                style={{ width: "100%" }}
                value={adjust.delta || undefined}
                onChange={(v) => setAdjust({ ...adjust, delta: v ?? 0 })}
              />
              <Input
                placeholder="Lý do (VD: Kiểm kê) *"
                value={adjust.reference}
                onChange={(e) => setAdjust({ ...adjust, reference: e.target.value })}
              />
              <Button type="primary" onClick={adjustStock}>Điều chỉnh</Button>
            </Space>
          </Card>
        </Col>
        <Col span={8}>
          <Card title="Khai báo tồn kho" size="small">
            <Space direction="vertical" style={{ width: "100%" }} size="middle">
              <Select
                placeholder="Kho"
                style={{ width: "100%" }}
                value={setForm.warehouseId || undefined}
                onChange={(v) => setSetForm({ ...setForm, warehouseId: v })}
                options={warehouseOptions}
              />
              <InputNumber
                placeholder="Số lượng *"
                style={{ width: "100%" }}
                value={setForm.quantity || undefined}
                onChange={(v) => setSetForm({ ...setForm, quantity: v ?? 0 })}
              />
              <Input
                placeholder="Lý do (VD: Nhập hàng) *"
                value={setForm.reference}
                onChange={(e) => setSetForm({ ...setForm, reference: e.target.value })}
              />
              <Button type="primary" onClick={setStock}>Khai báo</Button>
            </Space>
          </Card>
        </Col>
        <Col span={8}>
          <Card title="Chuyển kho" size="small">
            <Space direction="vertical" style={{ width: "100%" }} size="middle">
              <Select
                placeholder="Kho nguồn"
                style={{ width: "100%" }}
                value={transfer.fromWarehouseId || undefined}
                onChange={(v) => setTransfer({ ...transfer, fromWarehouseId: v })}
                options={warehouseOptions}
              />
              <Select
                placeholder="Kho đích"
                style={{ width: "100%" }}
                value={transfer.toWarehouseId || undefined}
                onChange={(v) => setTransfer({ ...transfer, toWarehouseId: v })}
                options={warehouseOptions}
              />
              <InputNumber
                placeholder="Số lượng *"
                style={{ width: "100%" }}
                value={transfer.quantity || undefined}
                onChange={(v) => setTransfer({ ...transfer, quantity: v ?? 0 })}
              />
              <Input
                placeholder="Lý do (VD: Bổ sung showroom Q.2) *"
                value={transfer.reference}
                onChange={(e) => setTransfer({ ...transfer, reference: e.target.value })}
              />
              <Button type="primary" onClick={transferStock}>Chuyển</Button>
            </Space>
          </Card>
        </Col>
      </Row>
    </div>
  );
}
