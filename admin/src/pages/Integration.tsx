import { useEffect, useState } from "react";
import { Button, Card, Col, Row, Space, Statistic, Table, Tag, Typography, message } from "antd";
import { CloudSyncOutlined, ReloadOutlined } from "@ant-design/icons";
import { api } from "../api";

interface OutboxStatus {
  total: number;
  pending: number;
  processed: number;
  failed: number;
}

interface OutboxMessage {
  id: string;
  eventType: string;
  occurredAt: string;
  processedAt: string | null;
  error: string | null;
  retryCount: number;
  status: string;
}

interface SyncLog {
  id: string;
  targetSystem: string;
  direction: string;
  eventType: string;
  success: boolean;
  error: string | null;
  retryCount: number;
  createdAt: string;
}

interface ErpOrder {
  id: string;
  erpOrderNo: string;
  orderNumber: string;
  customerPhone: string;
  totalAmount: number;
  paymentMethod: string;
  deliveryMethod: string;
  status: string;
  syncedAt: string | null;
}

interface ErpSummary {
  totalOrders: number;
  syncedEvents: number;
  failedEvents: number;
  pendingEvents: number;
}

const STATUS_COLORS: Record<string, string> = {
  Processed: "green",
  Pending: "orange",
  Failed: "red",
};

export default function Integration() {
  const [messageApi, contextHolder] = message.useMessage();
  const [status, setStatus] = useState<OutboxStatus>({ total: 0, pending: 0, processed: 0, failed: 0 });
  const [outbox, setOutbox] = useState<OutboxMessage[]>([]);
  const [outboxTotal, setOutboxTotal] = useState(0);
  const [outboxPage, setOutboxPage] = useState(1);
  const [logs, setLogs] = useState<SyncLog[]>([]);
  const [logsTotal, setLogsTotal] = useState(0);
  const [logsPage, setLogsPage] = useState(1);
  const [erpOrders, setErpOrders] = useState<ErpOrder[]>([]);
  const [erpTotal, setErpTotal] = useState(0);
  const [erpPage, setErpPage] = useState(1);
  const [erpSummary, setErpSummary] = useState<ErpSummary>({ totalOrders: 0, syncedEvents: 0, failedEvents: 0, pendingEvents: 0 });
  const [loading, setLoading] = useState(false);

  async function load(outboxPageNo = outboxPage, logsPageNo = logsPage, erpPageNo = erpPage) {
    setLoading(true);
    try {
      const [s, ob, sl, es, eo] = await Promise.all([
        api.get("/api/v1/integrations/outbox/status"),
        api.get("/api/v1/integrations/outbox", { params: { page: outboxPageNo, pageSize: 20 } }),
        api.get("/api/v1/integrations/sync-logs", { params: { page: logsPageNo, pageSize: 20 } }),
        api.get("/api/v1/integrations/erp/summary"),
        api.get("/api/v1/integrations/erp/orders", { params: { page: erpPageNo, pageSize: 20 } }),
      ]);
      setStatus(s.data?.data ?? { total: 0, pending: 0, processed: 0, failed: 0 });
      setOutbox(ob.data?.data?.items ?? []);
      setOutboxTotal(ob.data?.data?.totalCount ?? 0);
      setLogs(sl.data?.data?.items ?? []);
      setLogsTotal(sl.data?.data?.totalCount ?? 0);
      setErpSummary(es.data?.data ?? { totalOrders: 0, syncedEvents: 0, failedEvents: 0, pendingEvents: 0 });
      setErpOrders(eo.data?.data?.items ?? []);
      setErpTotal(eo.data?.data?.totalCount ?? 0);
    } catch {
      messageApi.error("Không tải được dữ liệu Integration — backend đã chạy chưa?");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load(1, 1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function retryFailed() {
    try {
      const res = await api.post("/api/v1/integrations/outbox/retry");
      messageApi.success(res.data?.message ?? "Đã retry.");
      load(outboxPage, logsPage, erpPage);
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Retry thất bại.");
    }
  }

  async function retryErp() {
    try {
      const res = await api.post("/api/v1/integrations/erp/retry");
      messageApi.success(res.data?.message ?? "Đã retry ERP.");
      load(outboxPage, logsPage, erpPage);
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Retry ERP thất bại.");
    }
  }

  return (
    <div>
      {contextHolder}
      <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Giám sát tích hợp (Outbox → RabbitMQ)
        </Typography.Title>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={() => load(outboxPage, logsPage)} loading={loading}>
            Làm mới
          </Button>
          <Button type="primary" icon={<CloudSyncOutlined />} onClick={retryFailed}>
            Retry các event lỗi
          </Button>
        </Space>
      </div>

      <Row gutter={16}>
        <Col span={6}>
          <Card>
            <Statistic title="Tổng event" value={status.total} />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic title="Đã publish" value={status.processed} valueStyle={{ color: "#3f8600" }} />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic title="Đang chờ" value={status.pending} valueStyle={{ color: "#cf6600" }} />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic title="Lỗi (max retry)" value={status.failed} valueStyle={{ color: "#cf1322" }} />
          </Card>
        </Col>
      </Row>

      <Card title="Event outbox" size="small" style={{ marginTop: 16 }}>
        <Table
          rowKey="id"
          size="small"
          loading={loading}
          dataSource={outbox}
          pagination={{
            current: outboxPage,
            total: outboxTotal,
            pageSize: 20,
            onChange: (p) => {
              setOutboxPage(p);
              load(p, logsPage);
            },
          }}
          columns={[
            { title: "Event", dataIndex: "eventType", width: 260 },
            {
              title: "Xảy ra",
              dataIndex: "occurredAt",
              width: 150,
              render: (v: string) => new Date(v).toLocaleString("vi-VN"),
            },
            {
              title: "Trạng thái",
              dataIndex: "status",
              width: 110,
              render: (v: string) => <Tag color={STATUS_COLORS[v] ?? "default"}>{v}</Tag>,
            },
            { title: "Retry", dataIndex: "retryCount", width: 70 },
            { title: "Lỗi", dataIndex: "error", ellipsis: true, render: (v: string | null) => v ?? "—" },
          ]}
        />
      </Card>

      <Card title="Nhật ký đồng bộ hệ thống ngoài (ERP/DMS/sàn TMĐT)" size="small" style={{ marginTop: 16 }}>
        <Table
          rowKey="id"
          size="small"
          loading={loading}
          dataSource={logs}
          pagination={{
            current: logsPage,
            total: logsTotal,
            pageSize: 20,
            onChange: (p) => {
              setLogsPage(p);
              load(outboxPage, p);
            },
          }}
          columns={[
            { title: "Hệ thống", dataIndex: "targetSystem", width: 100 },
            {
              title: "Chiều",
              dataIndex: "direction",
              width: 70,
              render: (v: string) => <Tag>{v === "out" ? "Gửi đi" : "Nhận về"}</Tag>,
            },
            { title: "Event", dataIndex: "eventType", width: 240 },
            {
              title: "Kết quả",
              dataIndex: "success",
              width: 90,
              render: (v: boolean) => (v ? <Tag color="green">OK</Tag> : <Tag color="red">Lỗi</Tag>),
            },
            { title: "Retry", dataIndex: "retryCount", width: 70 },
            { title: "Lỗi", dataIndex: "error", ellipsis: true, render: (v: string | null) => v ?? "—" },
            {
              title: "Lúc",
              dataIndex: "createdAt",
              width: 150,
              render: (v: string) => new Date(v).toLocaleString("vi-VN"),
            },
          ]}
        />
      </Card>

      <Card title="Phiếu bán đã đồng bộ ERP (RabbitMQ → integration.erp_sales_orders)" size="small" style={{ marginTop: 16 }}>
        <Row gutter={16} style={{ marginBottom: 12 }}>
          <Col span={6}>
            <Statistic title="Tổng phiếu ERP" value={erpSummary.totalOrders} />
          </Col>
          <Col span={6}>
            <Statistic title="Event đã đồng bộ" value={erpSummary.syncedEvents} valueStyle={{ color: "#3f8600" }} />
          </Col>
          <Col span={6}>
            <Statistic title="Event chờ" value={erpSummary.pendingEvents} valueStyle={{ color: "#cf6600" }} />
          </Col>
          <Col span={6}>
            <Statistic title="Event lỗi" value={erpSummary.failedEvents} valueStyle={{ color: "#cf1322" }} />
          </Col>
        </Row>
        <Space style={{ marginBottom: 12 }}>
          <Button type="primary" icon={<CloudSyncOutlined />} onClick={retryErp} loading={loading}>
            Retry bản ghi ERP lỗi
          </Button>
        </Space>
        <Table
          rowKey="id"
          size="small"
          loading={loading}
          dataSource={erpOrders}
          pagination={{
            current: erpPage,
            total: erpTotal,
            pageSize: 20,
            onChange: (p) => {
              setErpPage(p);
              load(outboxPage, logsPage, p);
            },
          }}
          columns={[
            { title: "Mã phiếu ERP", dataIndex: "erpOrderNo", width: 180 },
            { title: "Mã đơn TMĐT", dataIndex: "orderNumber", width: 180 },
            { title: "SĐT khách", dataIndex: "customerPhone", width: 130 },
            {
              title: "Tổng tiền",
              dataIndex: "totalAmount",
              width: 120,
              render: (v: number) => v.toLocaleString("vi-VN") + "₫",
            },
            { title: "Thanh toán", dataIndex: "paymentMethod", width: 110 },
            { title: "Giao hàng", dataIndex: "deliveryMethod", width: 110 },
            {
              title: "Trạng thái",
              dataIndex: "status",
              width: 110,
              render: (v: string) => <Tag color={v === "Created" ? "blue" : v === "Paid" ? "green" : "default"}>{v}</Tag>,
            },
            {
              title: "Đồng bộ lúc",
              dataIndex: "syncedAt",
              width: 150,
              render: (v: string | null) => (v ? new Date(v).toLocaleString("vi-VN") : "—"),
            },
          ]}
        />
      </Card>
    </div>
  );
}
