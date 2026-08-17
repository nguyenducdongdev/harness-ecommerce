import { Card, Col, Row, Statistic, Typography } from "antd";
import {
  ShoppingOutlined,
  DollarOutlined,
  UserOutlined,
  AppstoreOutlined,
} from "@ant-design/icons";
import { useEffect, useState } from "react";
import { api } from "../api";

export default function Dashboard() {
  const [stats, setStats] = useState({ products: 0, categories: 0, warehouses: 0 });

  useEffect(() => {
    // Đếm nhanh từ các API công khai — Phase 3 thay bằng endpoint /api/v1/reports/summary
    Promise.all([
      api.get("/api/v1/products?page=1&pageSize=1").then((r) => r.data?.data?.totalCount ?? 0),
      api.get("/api/v1/categories").then((r) => r.data?.data?.length ?? 0),
      api.get("/api/v1/warehouses").then((r) => r.data?.data?.length ?? 0),
    ])
      .then(([products, categories, warehouses]) => setStats({ products, categories, warehouses }))
      .catch(() => setStats({ products: 0, categories: 0, warehouses: 0 }));
  }, []);

  return (
    <div>
      <Typography.Title level={3}>Tổng quan</Typography.Title>
      <Row gutter={16}>
        <Col span={6}>
          <Card>
            <Statistic title="Sản phẩm" value={stats.products} prefix={<AppstoreOutlined />} />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic title="Danh mục" value={stats.categories} prefix={<ShoppingOutlined />} />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic title="Kho / Showroom" value={stats.warehouses} prefix={<ShoppingOutlined />} />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="Doanh thu hôm nay"
              value={0}
              prefix={<DollarOutlined />}
              suffix="đ (Phase 3)"
            />
          </Card>
        </Col>
      </Row>
      <Card style={{ marginTop: 16 }}>
        <Typography.Text type="secondary">
          Dashboard BI đầy đủ (doanh thu, lợi nhuận, chuyển đổi, RFM khách hàng) sẽ được thêm ở Phase 3
          khi module ERP hoàn tất. Hiện tại hiển thị số liệu cơ bản từ Catalog + Inventory.
        </Typography.Text>
      </Card>
    </div>
  );
}
