# Harness Ecommerce

Nền tảng thương mại điện tử cho chuỗi cửa hàng **nội thất ứng dụng** — kiến trúc **Modular Monolith** trên **.NET 8**, thiết kế sẵn để mở rộng: website, mobile app, ERP, DMS, hệ thống sản xuất dùng chung một API core. Triển khai **on-premise** với Docker, **0 đồng license phần mềm**.

## Tổng quan kiến trúc

```
harness-ecommerce/
├── backend/                  # .NET 8 Modular Monolith
│   ├── src/
│   │   ├── Harness.Api                      # Composition root (Program.cs, AppDbContext)
│   │   ├── Harness.BuildingBlocks.*          # Domain, Application, Infrastructure, Presentation
│   │   └── Harness.Modules.*                 # 11 module nghiệp vụ
│   └── tests/                                # Unit + Integration tests
├── web/                      # Website Next.js 14 (App Router, SSR/ISR, SEO)
├── admin/                    # Admin panel React + Vite + Ant Design
├── docker/                   # Hạ tầng on-premise (compose, Dockerfile, nginx)
└── .gitlab-ci.yml            # CI/CD: build → test → docker image
```

### Các module backend

| Module | Schema PostgreSQL | Trách nhiệm |
|---|---|---|
| Catalog | `catalog` | Sản phẩm, biến thể kích thước (rộng×sâu×cao), danh mục, thương hiệu |
| Order | `orders` | Giỏ→đơn hàng, máy trạng thái 7 bước |
| Inventory | `inventory` | Tồn kho theo kho/showroom, xuất nhập |
| Customer | `customer` | Khách hàng, địa chỉ |
| Promotion | `promotion` | Voucher, flash sale |
| Payment | `orders` | Giao dịch VNPay/MoMo/COD, webhook |
| Shipping | `shipping` | Lô hàng GHN/GHTK, tracking |
| Loyalty | `customer` | Tích điểm, hạng thành viên |
| Review | `review` | Đánh giá sản phẩm có kiểm duyệt |
| Cms | `cms` | Banner, trang nội dung |
| Integration | `integration` | Event Outbox → RabbitMQ, đồng bộ hệ thống ngoài |

**Outbox Pattern**: mọi sự kiện quan trọng (tạo đơn, đổi tồn kho, thanh toán) được ghi vào bảng `integration.event_outbox` trong cùng transaction — Hangfire job publish lên RabbitMQ mỗi phút. ERP/DMS/sản xuất sau này chỉ cần subscribe exchange `harness.events`.

## Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (cho hạ tầng: PostgreSQL, Redis, RabbitMQ...)
- Git

## Chạy dự án (Development)

### 1. Hạ tầng

```bash
cd docker
docker compose -f docker-compose.dev.yml up -d
# PostgreSQL :5432, Redis :6379, RabbitMQ :5672 (UI :15672), Seq :5341
```

### 2. Backend

```bash
cd backend
dotnet restore HarnessEcommerce.sln
dotnet tool install --global dotnet-ef        # nếu chưa có
dotnet ef migrations add InitialCreate -p src/Harness.Api -s src/Harness.Api
dotnet run --project src/Harness.Api
# Swagger:  http://localhost:5080/swagger
# Hangfire: http://localhost:5080/hangfire
# Health:   http://localhost:5080/health
```

Lần đầu chạy ở chế độ Development, hệ thống tự migrate + seed 8 sản phẩm nội thất mẫu (kèm biến thể kích thước), 8 danh mục, 4 thương hiệu, 3 kho/showroom.

### 3. Website

```bash
cd web
npm install
cp .env.example .env.local     # API_URL=http://localhost:5080
npm run dev
# http://localhost:3000
```

### 4. Admin

```bash
cd admin
npm install
npm run dev
# http://localhost:5173 (đăng nhập stub — bất kỳ tài khoản nào cũng vào được ở Phase 1)
```

## Test

```bash
cd backend
dotnet test tests/Harness.UnitTests/Harness.UnitTests.csproj
dotnet test tests/Harness.IntegrationTests/Harness.IntegrationTests.csproj
```

## Triển khai Production (on-premise)

```bash
# Trên server Ubuntu có Docker:
cd docker
docker compose up -d           # PostgreSQL, Redis, RabbitMQ, ES, MinIO, Seq, Grafana...

# Build & chạy API
cd ../backend
docker build -f ../docker/api/Dockerfile -t harness-api .
docker run -d -p 5080:8080 \
  -e ConnectionStrings__PostgreSQL="Host=<db-host>;Database=harness;Username=harness;Password=***" \
  -e ConnectionStrings__Redis="<redis-host>:6379" \
  -e RabbitMq__HostName=<rabbit-host> \
  harness-api
```

Nginx config mẫu tại `docker/nginx/nginx.conf` (reverse proxy `/api` → backend, `/` → Next.js, chặn IP công khai cho Swagger/Hangfire).

## Cổng dịch vụ

| Dịch vụ | Cổng | Ghi chú |
|---|---|---|
| API .NET | 5080 | Swagger tại /swagger |
| Website Next.js | 3000 | |
| Admin | 5173 | dev, production build tĩnh |
| PostgreSQL | 5432 | user/pass: harness/harness |
| RabbitMQ UI | 15672 | harness/harness |
| Seq (logs) | 5341 | UI web |
| MinIO UI | 9001 | harness/harness123 |
| Grafana | 3000→3000 | admin/admin (đổi pass khi lên production) |

## Lộ trình

- **Phase 1 (M0 ✓ + M1 ✓)**: Nền tảng — modular monolith, catalog (+ JSONB attributes, Elasticsearch indexer/tìm kiếm, upload ảnh MinIO/local), order, inventory, phí ship theo thể tích, web, admin, CI/CD
- **Phase 2**: Auth JWT + OTP, thanh toán VNPay/MoMo sandbox, quiz tư vấn nội thất, đánh giá
- **Phase 3**: ERP (kế toán, công nợ), DMS (chuyển kho, đối soát), sản xuất (BOM)
- **Phase 4**: Mobile app React Native, đồng bộ sàn TMĐT, AR, tách microservices

Chi tiết trong `plan.md` (không commit).

## Ghi chú license

Toàn bộ stack là open-source: .NET 8 (MIT), PostgreSQL, Redis, RabbitMQ, Elasticsearch, MinIO, Next.js, React, Ant Design. Không có chi phí license phần mềm khi triển khai.
