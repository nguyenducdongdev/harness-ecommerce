import { useEffect, useState } from "react";
import { Button, Popconfirm, Rate, Space, Table, Tag, Typography, message } from "antd";
import { CheckOutlined, CloseOutlined } from "@ant-design/icons";
import { api } from "../api";

interface ReviewQueueItem {
  id: string;
  productId: number;
  customerName: string;
  rating: number;
  content: string;
  verifiedPurchase: boolean;
  createdAt: string;
}

export default function Reviews() {
  const [items, setItems] = useState<ReviewQueueItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [messageApi, contextHolder] = message.useMessage();

  async function load(p = page) {
    setLoading(true);
    try {
      const res = await api.get("/api/v1/reviews/pending", { params: { page: p, pageSize: 20 } });
      setItems(res.data?.data?.items ?? []);
      setTotal(res.data?.data?.totalCount ?? 0);
    } catch {
      messageApi.error("Không tải được hàng chờ duyệt — backend đã chạy chưa?");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load(1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function moderate(id: string, approve: boolean) {
    try {
      await api.put(`/api/v1/reviews/${id}/${approve ? "approve" : "reject"}`);
      messageApi.success(approve ? "Đã duyệt đánh giá." : "Đã từ chối đánh giá.");
      load(page);
    } catch (err: any) {
      messageApi.error(err?.response?.data?.message ?? "Thao tác thất bại.");
    }
  }

  const columns = [
    { title: "SP#", dataIndex: "productId", width: 70 },
    { title: "Khách hàng", dataIndex: "customerName", width: 160 },
    {
      title: "Sao",
      dataIndex: "rating",
      width: 130,
      render: (v: number) => <Rate disabled value={v} style={{ fontSize: 14 }} />,
    },
    { title: "Nội dung", dataIndex: "content", ellipsis: true },
    {
      title: "Mua hàng thật",
      dataIndex: "verifiedPurchase",
      width: 110,
      render: (v: boolean) =>
        v ? <Tag color="green">Đã mua</Tag> : <Tag>Không xác thực</Tag>,
    },
    {
      title: "Gửi lúc",
      dataIndex: "createdAt",
      width: 130,
      render: (v: string) => new Date(v).toLocaleString("vi-VN"),
    },
    {
      title: "Hành động",
      width: 170,
      render: (_: unknown, record: ReviewQueueItem) => (
        <Space>
          <Popconfirm title="Duyệt đánh giá này?" onConfirm={() => moderate(record.id, true)}>
            <Button size="small" type="primary" icon={<CheckOutlined />}>
              Duyệt
            </Button>
          </Popconfirm>
          <Popconfirm title="Từ chối đánh giá này?" onConfirm={() => moderate(record.id, false)}>
            <Button size="small" danger icon={<CloseOutlined />}>
              Từ chối
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      {contextHolder}
      <Typography.Title level={3}>Kiểm duyệt đánh giá ({total})</Typography.Title>
      <Typography.Paragraph type="secondary">
        Đánh giá khách gửi chờ duyệt. Chỉ đánh giá được Duyệt mới hiển thị trên trang sản phẩm.
      </Typography.Paragraph>
      <Table
        rowKey="id"
        columns={columns}
        dataSource={items}
        loading={loading}
        pagination={{
          current: page,
          total,
          pageSize: 20,
          onChange: (p) => {
            setPage(p);
            load(p);
          },
        }}
      />
    </div>
  );
}
