import { useEffect, useState } from "react";
import { Table, Button, Modal, Form, Input, Select, InputNumber, Space, Card, Progress, Tabs, message } from "antd";
import { TrophyOutlined, PlusOutlined, BarChartOutlined, TagOutlined } from "@ant-design/icons";
import { api, KpiTargetItem, SalesKpiReportItem, StoreItem } from "../api";

export default function Kpi() {
  const [targets, setTargets] = useState<KpiTargetItem[]>([]);
  const [report, setReport] = useState<SalesKpiReportItem[]>([]);
  const [stores, setStores] = useState<StoreItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedMonth, setSelectedMonth] = useState<number>(new Date().getMonth() + 1);
  const [selectedYear, setSelectedYear] = useState<number>(new Date().getFullYear());
  const [form] = Form.useForm();
  const [messageApi, contextHolder] = message.useMessage();

  const fetchData = async () => {
    setLoading(true);
    try {
      const [resT, resR, resS] = await Promise.all([
        api.get("/api/v1/admin/kpi/targets", { params: { month: selectedMonth, year: selectedYear } }),
        api.get("/api/v1/admin/kpi/sales-report", { params: { month: selectedMonth, year: selectedYear } }),
        api.get("/api/v1/admin/stores"),
      ]);
      setTargets(resT.data.data || []);
      setReport(resR.data.data || []);
      setStores(resS.data.data || []);
    } catch {
      messageApi.error("Không thể tải dữ liệu KPI.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchData(); }, [selectedMonth, selectedYear]);

  const handleSave = async (v: any) => {
    try {
      const st = stores.find((s) => s.id === v.storeId);
      await api.post("/api/v1/admin/kpi/targets", {
        staffId: "00000000-0000-0000-0000-000000000000",
        staffName: v.staffName,
        storeId: v.storeId || null,
        storeName: st?.name || null,
        month: selectedMonth,
        year: selectedYear,
        targetRevenue: v.targetRevenue,
        targetOrders: v.targetOrders,
      });
      messageApi.success("Thiết lập KPI thành công!");
      setIsModalOpen(false);
      form.resetFields();
      fetchData();
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message || "Lỗi khi lưu KPI.");
    }
  };

  const reportColumns = [
    { title: "Nhân Viên", dataIndex: "staffName", key: "staffName" },
    { title: "Cửa Hàng", dataIndex: "storeName", key: "storeName", render: (t?: string) => t || "Chung" },
    { title: "Doanh Thu Chỉ Tiêu", dataIndex: "targetRevenue", render: (v: number) => `${v.toLocaleString()}đ` },
    { title: "Đạt Được", dataIndex: "actualRevenue", render: (v: number) => <strong>{v.toLocaleString()}đ</strong> },
    { title: "% Đạt", dataIndex: "revenueCompletionRate", render: (r: number) => <Progress percent={r} size="small" /> },
    { title: "Đơn Thực Tế", dataIndex: "actualOrders" },
  ];

  const targetColumns = [
    { title: "Nhân Viên", dataIndex: "staffName" },
    { title: "Cửa Hàng", dataIndex: "storeName", render: (t?: string) => t || "Chung" },
    { title: "Thời Gian", render: (_: any, r: KpiTargetItem) => `T${r.month}/${r.year}` },
    { title: "Chỉ Tiêu Doanh Thu", dataIndex: "targetRevenue", render: (v: number) => `${v.toLocaleString()}đ` },
    { title: "Chỉ Tiêu Đơn", dataIndex: "targetOrders" },
  ];

  return (
    <div>
      {contextHolder}
      <Card
        title={<span><TrophyOutlined style={{ marginRight: 8 }} /> Quản Lý KPI Sales</span>}
        extra={
          <Space>
            <Select value={selectedMonth} onChange={setSelectedMonth} style={{ width: 100 }}>
              {Array.from({ length: 12 }, (_, i) => (<Select.Option key={i + 1} value={i + 1}>Tháng {i + 1}</Select.Option>))}
            </Select>
            <Select value={selectedYear} onChange={setSelectedYear} style={{ width: 90 }}>
              {[2025, 2026, 2027].map((y) => (<Select.Option key={y} value={y}>{y}</Select.Option>))}
            </Select>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => { form.resetFields(); setIsModalOpen(true); }}>
              Giao KPI
            </Button>
          </Space>
        }
      >
        <Tabs
          items={[
            { key: "report", label: <span><BarChartOutlined /> Báo Cáo KPI</span>, children: <Table columns={reportColumns} dataSource={report} rowKey={(r) => r.staffName} loading={loading} /> },
            { key: "targets", label: <span><TagOutlined /> Chỉ Tiêu Đã Giao</span>, children: <Table columns={targetColumns} dataSource={targets} rowKey="id" loading={loading} /> },
          ]}
        />
      </Card>

      <Modal title="Giao Chỉ Tiêu KPI" open={isModalOpen} onCancel={() => setIsModalOpen(false)} onOk={() => form.submit()}>
        <Form form={form} layout="vertical" onFinish={handleSave}>
          <Form.Item label="Nhân Viên Sales" name="staffName" rules={[{ required: true }]}>
            <Input placeholder="Tên hoặc Username (VD: admin)" />
          </Form.Item>
          <Form.Item label="Cửa Hàng" name="storeId">
            <Select placeholder="Chọn cửa hàng" allowClear>
              {stores.map((s) => (<Select.Option key={s.id} value={s.id}>{s.name}</Select.Option>))}
            </Select>
          </Form.Item>
          <Form.Item label="Chỉ Tiêu Doanh Thu (VNĐ)" name="targetRevenue" rules={[{ required: true }]}>
            <InputNumber style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item label="Chỉ Tiêu Số Đơn" name="targetOrders" rules={[{ required: true }]}>
            <InputNumber style={{ width: "100%" }} min={0} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
