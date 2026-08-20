import { useEffect, useState } from "react";
import { Table, Button, Modal, Form, Input, Select, Tag, Space, Card, DatePicker, message } from "antd";
import { CalendarOutlined, ClockCircleOutlined, PlusOutlined } from "@ant-design/icons";
import { api, AttendanceItem, StoreItem } from "../api";
import dayjs from "dayjs";

export default function Attendance() {
  const [records, setRecords] = useState<AttendanceItem[]>([]);
  const [stores, setStores] = useState<StoreItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [form] = Form.useForm();
  const [messageApi, contextHolder] = message.useMessage();

  const fetchData = async () => {
    setLoading(true);
    try {
      const [resAtt, resStore] = await Promise.all([
        api.get("/api/v1/admin/attendance"),
        api.get("/api/v1/admin/stores"),
      ]);
      setRecords(resAtt.data.data || []);
      setStores(resStore.data.data || []);
    } catch {
      messageApi.error("Không thể tải dữ liệu chấm công.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleSave = async (values: any) => {
    try {
      const selectedStore = stores.find((s) => s.id === values.storeId);
      await api.post("/api/v1/admin/attendance", {
        staffId: "00000000-0000-0000-0000-000000000000",
        staffName: values.staffName,
        storeId: values.storeId,
        storeName: selectedStore?.name || "",
        workDate: values.workDate.format("YYYY-MM-DD"),
        status: Number(values.status),
        notes: values.notes,
      });
      messageApi.success("Cập nhật chấm công thành công!");
      setIsModalOpen(false);
      form.resetFields();
      fetchData();
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message || "Lỗi khi lưu chấm công.");
    }
  };

  const handleCheckOut = async (id: string) => {
    try {
      await api.post("/api/v1/admin/attendance/check-out", { attendanceId: id });
      messageApi.success("Đã ghi nhận Check-out.");
      fetchData();
    } catch {
      messageApi.error("Không thể thực hiện Check-out.");
    }
  };

  const columns = [
    { title: "Nhân Viên", dataIndex: "staffName", key: "staffName", render: (text: string) => <strong>{text}</strong> },
    { title: "Cửa Hàng", dataIndex: "storeName", key: "storeName" },
    { title: "Ngày Làm Việc", dataIndex: "workDate", key: "workDate" },
    {
      title: "Check-in",
      dataIndex: "checkInTime",
      key: "checkInTime",
      render: (time?: string) => (time ? dayjs(time).format("HH:mm:ss DD/MM") : "-"),
    },
    {
      title: "Check-out",
      dataIndex: "checkOutTime",
      key: "checkOutTime",
      render: (time?: string) => (time ? dayjs(time).format("HH:mm:ss DD/MM") : "-"),
    },
    {
      title: "Trạng Thái",
      dataIndex: "status",
      key: "status",
      render: (status: number, record: AttendanceItem) => {
        let color = "green";
        if (status === 2) color = "orange";
        if (status === 3) color = "red";
        if (status === 4) color = "purple";
        return <Tag color={color}>{record.statusText || "Đúng giờ"}</Tag>;
      },
    },
    { title: "Ghi Chú", dataIndex: "notes", key: "notes", render: (text?: string) => text || "-" },
    {
      title: "Thao Tác",
      key: "action",
      render: (_: any, record: AttendanceItem) => (
        <Space>
          {!record.checkOutTime && (
            <Button size="small" icon={<ClockCircleOutlined />} onClick={() => handleCheckOut(record.id)}>
              Check-out
            </Button>
          )}
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
            <CalendarOutlined style={{ marginRight: 8 }} /> Quản Lý Chấm Công Nhân Viên
          </span>
        }
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              form.resetFields();
              form.setFieldsValue({ workDate: dayjs(), status: 1 });
              setIsModalOpen(true);
            }}
          >
            Nhập Chấm Công
          </Button>
        }
      >
        <Table columns={columns} dataSource={records} rowKey="id" loading={loading} />
      </Card>

      <Modal
        title="Nhập Bản Ghi Chấm Công"
        open={isModalOpen}
        onCancel={() => setIsModalOpen(false)}
        onOk={() => form.submit()}
      >
        <Form form={form} layout="vertical" onFinish={handleSave}>
          <Form.Item label="Tên Nhân Viên" name="staffName" rules={[{ required: true }]}>
            <Input placeholder="VD: Nguyễn Văn A" />
          </Form.Item>
          <Form.Item label="Cửa Hàng" name="storeId" rules={[{ required: true }]}>
            <Select placeholder="Chọn cửa hàng">
              {stores.map((s) => (
                <Select.Option key={s.id} value={s.id}>
                  {s.name} ({s.code})
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item label="Ngày Làm Việc" name="workDate" rules={[{ required: true }]}>
            <DatePicker style={{ width: "100%" }} format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item label="Trạng Thái Chấm Công" name="status" rules={[{ required: true }]}>
            <Select>
              <Select.Option value={1}>1. Đúng giờ (Present)</Select.Option>
              <Select.Option value={2}>2. Đi muộn (Late)</Select.Option>
              <Select.Option value={3}>3. Vắng mặt (Absent)</Select.Option>
              <Select.Option value={4}>4. Về sớm (Early Leave)</Select.Option>
            </Select>
          </Form.Item>
          <Form.Item label="Ghi Chú" name="notes">
            <Input.TextArea placeholder="Lý do..." />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
