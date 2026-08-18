import { useState, useCallback } from "react";
import {
  Alert,
  Button,
  Card,
  Col,
  Row,
  Space,
  Spin,
  Statistic,
  Table,
  Tag,
  Tooltip,
  Typography,
  message,
} from "antd";
import {
  DollarOutlined,
  ReloadOutlined,
  ShoppingOutlined,
  UserOutlined,
  WarningOutlined,
} from "@ant-design/icons";
import type { ColumnsType } from "antd/es/table";
import { api } from "../api";

// ===== Types (trùng với DashboardQueries.cs — JSON trả về PascalCase) =====

interface DashboardKpis {
  TotalOrders?: number;
  TotalRevenue?: number;
  RevenueThisMonth?: number;
  AvgOrderValue?: number;
  TotalCustomers?: number;
  PaidOrders?: number;
  PendingOutbox?: number;
  FailedOutbox?: number;
  ErpFailed?: number;
}

interface RevenueByDayItem {
  Date?: string;
  OrderCount?: number;
  Revenue?: number;
}

interface TopProductItem {
  ProductId?: number;
  ProductName?: string;
  VariantSku?: string;
  TotalQty?: number;
  TotalRevenue?: number;
}

interface OrderStatusItem {
  Status?: number;
  Count?: number;
}

interface LowStockItem {
  VariantSku?: string;
  ProductName?: string;
  WarehouseName?: string;
  QuantityAvailable?: number;
}

// Map trạng thái đơn hàng (cùng enum OrderStatus ở backend)
const ORDER_STATUS_LABELS: Record<number, string> = {
  1: "Chờ xác nhận",
  2: "Đang xử lý",
  3: "Đang giao",
  4: "Đã giao",
  5: "Hoàn thành",
  6: "Đã hủy",
  7: "Đã hoàn tiền",
};

const STATUS_COLOR: Record<number, string> = {
  1: "blue",
  2: "processing",
  3: "orange",
  4: "cyan",
  5: "green",
  6: "red",
  7: "purple",
};

const vnd = (v: number | undefined) =>
  `${new Intl.NumberFormat("vi-VN").format(Math.round(Number(v) || 0))}\u00a0\u20ab`;

const num = (v: number | undefined) => Number(v) || 0;

// ===== Hook: tải toàn bộ dữ liệu dashboard, retry tự động khi mất kết nối =====

interface DashboardData {
  kpis: DashboardKpis | null;
  revenue: RevenueByDayItem[];
  topProducts: TopProductItem[];
  orderStatus: OrderStatusItem[];
  lowStock: LowStockItem[];
}

function useDashboardData() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchDashboard = useCallback(async (attempt = 1) => {
    setLoading(true);
    setError(null);
    try {
      const [kpiRes, revRes, topRes, statusRes, stockRes] = await Promise.all([
        api.get("/api/v1/dashboard/kpis"),
        api.get("/api/v1/dashboard/revenue?days=30"),
        api.get("/api/v1/dashboard/top-products?limit=5"),
        api.get("/api/v1/dashboard/order-status"),
        api.get("/api/v1/dashboard/low-stock?threshold=5"),
      ]);
      setData({
        kpis: kpiRes.data?.data ?? null,
        revenue: revRes.data?.data ?? [],
        topProducts: topRes.data?.data ?? [],
        orderStatus: statusRes.data?.data ?? [],
        lowStock: stockRes.data?.data ?? [],
      });
    } catch (err: any) {
      const msg = err?.response?.data?.message ?? err?.message ?? "Không thể tải dữ liệu dashboard.";
      // Tự động retry tối đa 3 lần khi mất kết nối / server lỗi
      if (attempt < 3) {
        const backoff = 1000 * attempt;
        setTimeout(() => fetchDashboard(attempt + 1), backoff);
      } else {
        setError(msg);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  return { data, loading, error, refetch: () => fetchDashboard(1) };
}

// ===== KPI Stat cards =====

function KpiCards({ kpis }: { kpis: DashboardKpis | null }) {
  return (
    <Row gutter={16}>
      <Col span={4}>
        <Card><Statistic title="Tổng đơn hàng" value={num(kpis?.TotalOrders)} prefix={<ShoppingOutlined />} /></Card>
      </Col>
      <Col span={4}>
        <Card><Statistic title="Doanh thu" value={num(kpis?.TotalRevenue)} precision={0} prefix={<DollarOutlined />} formatter={(v) => `${v ?? 0}\u00a0\u20ab`} /></Card>
      </Col>
      <Col span={4}>
        <Card><Statistic title="Doanh thu tháng này" value={num(kpis?.RevenueThisMonth)} precision={0} prefix={<DollarOutlined />} formatter={(v) => `${v ?? 0}\u00a0\u20ab`} /></Card>
      </Col>
      <Col span={4}>
        <Card><Statistic title="Giá trị đơn TB" value={num(kpis?.AvgOrderValue)} precision={0} prefix={<DollarOutlined />} formatter={(v) => `${v ?? 0}\u00a0\u20ab`} /></Card>
      </Col>
      <Col span={4}>
        <Card><Statistic title="Khách hàng" value={num(kpis?.TotalCustomers)} prefix={<UserOutlined />} /></Card>
      </Col>
      <Col span={4}>
        <Card><Statistic title="Đơn đã thanh toán" value={num(kpis?.PaidOrders)} prefix={<ShoppingOutlined />} /></Card>
      </Col>
      <Col span={4}>
        <Card><Statistic title="Outbox chờ xử lý" value={num(kpis?.PendingOutbox)} valueStyle={{ color: "#1890ff" }} /></Card>
      </Col>
      <Col span={4}>
        <Card><Statistic title="Outbox lỗi" value={num(kpis?.FailedOutbox)} valueStyle={{ color: "#f5222d" }} /></Card>
      </Col>
      <Col span={4}>
        <Card><Statistic title="ERP thất bại" value={num(kpis?.ErpFailed)} valueStyle={{ color: "#fa8c16" }} prefix={<WarningOutlined />} /></Card>
      </Col>
    </Row>
  );
}

// ===== Các table columns =====

const revenueColumns: ColumnsType<RevenueByDayItem> = [
  { title: "Ngày", dataIndex: "Date", key: "Date" },
  { title: "Số đơn", dataIndex: "OrderCount", key: "OrderCount", render: (v) => num(v) },
  { title: "Doanh thu", dataIndex: "Revenue", key: "Revenue", render: (v) => vnd(v) },
];

const topProductsColumns: ColumnsType<TopProductItem> = [
  { title: "STT", key: "index", render: (_, __, idx) => idx + 1 },
  { title: "Sản phẩm", dataIndex: "ProductName", key: "ProductName" },
  { title: "Mã biến thể", dataIndex: "VariantSku", key: "VariantSku" },
  { title: "Số lượng", dataIndex: "TotalQty", key: "TotalQty", render: (v) => num(v) },
  { title: "Doanh thu", dataIndex: "TotalRevenue", key: "TotalRevenue", render: (v) => vnd(v) },
];

const statusColumns: ColumnsType<OrderStatusItem> = [
  {
    title: "Trạng thái",
    dataIndex: "Status",
    key: "Status",
    render: (v) => <Tag color={STATUS_COLOR[v] ?? "default"}>{ORDER_STATUS_LABELS[v] ?? v ?? "—"}</Tag>,
  },
  { title: "Số đơn", dataIndex: "Count", key: "Count", render: (v) => num(v) },
];

const lowStockColumns: ColumnsType<LowStockItem> = [
  { title: "Mã biến thể", dataIndex: "VariantSku", key: "VariantSku" },
  { title: "Sản phẩm", dataIndex: "ProductName", key: "ProductName" },
  { title: "Kho", dataIndex: "WarehouseName", key: "WarehouseName" },
  {
    title: "Tồn kho",
    dataIndex: "QuantityAvailable",
    key: "QuantityAvailable",
        render: (v) => <Tag color={num(v) === 0 ? "red" : "orange"}>{num(v)}</Tag>,
  },
];

// ===== Main component =====

export default function Dashboard() {
  const { data, loading, error, refetch } = useDashboardData();
  const [messageApi, contextHolder] = message.useMessage();

  const handleRefresh = () => {
    refetch();
    messageApi.info("Đang tải lại dữ liệu dashboard...");
  };

  return (
    <div>
      {contextHolder}
      <Space style={{ width: "100%", justifyContent: "space-between", marginBottom: 12 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Tổng quan vận hành
        </Typography.Title>
        <Button icon={<ReloadOutlined />} onClick={handleRefresh} loading={loading} type="primary">
          Làm mới
        </Button>
      </Space>

      {error && (
        <Alert
          type="error"
          showIcon
          message={error}
          action={<Button size="small" danger onClick={handleRefresh}>Thử lại</Button>}
          style={{ marginBottom: 16 }}
        />
      )}

      {/* KPIs */}
      {data?.kpis ? (
        <KpiCards kpis={data.kpis} />
      ) : (
        !error && <Spin spinning={loading} />
      )}

      <Row gutter={16} style={{ marginTop: 16 }}>
        {/* Doanh thu theo ngày */}
        <Col span={12}>
          <Card title="Doanh thu 30 ngày qua" loading={loading}>
            <Table size="small" pagination={false} rowKey="Date" columns={revenueColumns} dataSource={data?.revenue ?? []} locale={{ emptyText: "Chưa có dữ liệu" }} />
          </Card>
        </Col>

        {/* Top sản phẩm bán chạy */}
        <Col span={12}>
          <Card title="Top 5 sản phẩm bán chạy" loading={loading}>
            <Table size="small" pagination={false} rowKey="VariantSku" columns={topProductsColumns} dataSource={data?.topProducts ?? []} locale={{ emptyText: "Chưa có dữ liệu" }} />
          </Card>
        </Col>

        {/* Phân bổ trạng thái đơn hàng */}
        <Col span={12} style={{ marginTop: 16 }}>
          <Card title="Phân bổ đơn hàng theo trạng thái" loading={loading}>
            <Table size="small" pagination={false} rowKey="Status" columns={statusColumns} dataSource={data?.orderStatus ?? []} locale={{ emptyText: "Chưa có dữ liệu" }} />
          </Card>
        </Col>

        {/* Tồn kho dưới ngưỡng */}
        <Col span={12} style={{ marginTop: 16 }}>
          <Card title="Sản phẩm tồn dưới ngưỡng (≤5)" loading={loading}
            extra={
              <Tooltip title="Cảnh báo nhập hàng — số lượng tồn ≤ ngưỡng">
                <WarningOutlined style={{ color: "#fa8c16" }} />
              </Tooltip>
            }>
            <Table size="small" pagination={false} rowKey="VariantSku" columns={lowStockColumns} dataSource={data?.lowStock ?? []} locale={{ emptyText: "Tồn kho đủ ngưỡng" }} />
          </Card>
        </Col>
            </Row>
    </div>
  );
}
