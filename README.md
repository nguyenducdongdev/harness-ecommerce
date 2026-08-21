# Harness Ecommerce

Nền tảng thương mại điện tử cho chuỗi cửa hàng **nội thất ứng dụng** kiến trúc **Modular Monolith** trên **.NET 8**, thiết kế sẵn mở rộng: website, mobile app, ERP, DMS, hệ thống sản xuất dùng chung một API core. Triển khai **on-premise** với Docker, **0 đồng license phần mềm**.

## Tổng quan kiến trúc

```
harness-ecommerce/
├── backend/                  # .NET 8 Modular Monolith
│   ├── src/
│   │   ├── Harness.Api                      # Composition root (Program.cs, AppDbContext)
│   │   ├── Harness.BuildingBlocks.*          # Domain, Application, Infrastructure, Presentation
│   │   └── Harness.Modules.*                 # 12 module nghiệp vụ
│   └── tests/                                # Unit + Integration tests
├── web/                      # Website Next.js 14 (App Router, SSR/ISR, SEO)
├── admin/                    # Admin panel React + Vite + Ant Design
├── docker/                   # Hạ tầng on-premise (compose, Dockerfile, nginx)
└── .gitlab-ci.yml            # CI/CD: build → test → docker image
```

### Các module backend

| Module | Schema PostgreSQL | Trách nhiệm |
|---|---|---|
| Catalog | `catalog` | Sản phẩm, biến thể kích thước (rộng×sâu×cao), danh mục, thương hiệu, **combo phòng** |
| Order | `orders` | Giỏ → đơn hàng, máy trạng thái 7 bước |
| Inventory | `inventory` | **Tồn kho theo kho/showroom** (khả dụng, giữ chỗ, chuyển kho), xuất nhập |
| Customer | `customer` | Khách hàng, địa chỉ |
| Promotion | `promotion` | Voucher, flash sale |
| Payment | `orders` | Giao dịch VNPay/MoMo/COD, webhook |
| Shipping | `shipping` | Lô hàng GHN/GHTK, tracking |
| Loyalty | `customer` | Tích điểm, hạng thành viên |
| Review | `review` | Đánh giá sản phẩm có kiểm duyệt |
| Cms | `cms` | Banner, trang nội dung |
| Integration | `integration` | Event Outbox RabbitMQ, đồng bộ hệ thống ngoài |
| Organization | `organization` | Cửa hàng/Showroom, Ca làm việc, Chấm công, KPI nhân viên |

**Outbox Pattern**: mọi sự kiện quan trọng (tạo đơn, đổi tồn kho, thanh toán) được ghi vào bảng `integration.event_outbox` trong cùng transaction. Hangfire job publish lên RabbitMQ mỗi phút. ERP/DMS/sản xuất sau này chỉ cần subscribe exchange `harness.events`.

## Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (cho hạ tầng: PostgreSQL, Redis, RabbitMQ...)
- Git

## Hướng dẫn Chạy Local (Development)

### 1. Khởi động Hạ tầng Docker (PostgreSQL, Redis, RabbitMQ, Seq...)

```bash
cd docker
docker compose -f docker-compose.dev.yml up -d
# PostgreSQL: 5432, Redis: 6379, RabbitMQ: 5672 (UI: 15672), Seq: 5341
```

### 2. Khởi động Backend (.NET 8 Web API)

```bash
cd backend
dotnet restore HarnessEcommerce.sln
dotnet run --project src/Harness.Api
```

- **Swagger UI**: http://localhost:5080/swagger
- **Hangfire Dashboard**: http://localhost:5080/hangfire
- **Health Check**: http://localhost:5080/health

### 3. Khởi động Frontend Web (Next.js - End User Website)

```bash
cd web
npm install
npm run dev
```

- **Website Khách hàng**: http://localhost:3000

### 4. Khởi động Frontend Admin (React + Vite + Ant Design - CMS / Portal)

```bash
cd admin
npm install
npm run dev
```

- **Trang Quản trị Admin**: http://localhost:5173

---

## 🛠 Ghi chú khắc phục khi chạy Local (cập nhật 2026-08-21)

> Áp dụng khi cài đặt trên máy thực tế sau các bước ở trên. Mục này ghi lại cách đã xử lý
> để Backend **migrate + seed + chạy ổn định** trên máy này.

### 1. Xung đột cổng 5432 — PostgreSQL native vs Docker

**Triệu chứng:** Backend khởi động báo lỗi
`Npgsql.PostgresException: password authentication failed for user "harness"` (SQLSTATE `28P01`)
mặc dù đã chạy đúng `docker compose -f docker-compose.dev.yml up -d`.

**Nguyên nhân:** Máy có **PostgreSQL 16 native trên Windows** (service `postgresql-x64-16`) đang
chiếm cổng `5432`. Khi đó kết nối `Host=localhost;Port=5432` của app (qua `appsettings.json`)
đi vào Postgres **native** — nơi không có user `harness` / mật khẩu khác — chứ không vào container
Docker. (Lưu ý: test qua `docker exec ... psql -h localhost` vẫn "thành công" vì `pg_hba.conf` của
container dùng chế độ `trust` cho loopback, không phản ánh đúng đường kết nối thật từ host.)

> Kiểm tra ai đang giữ cổng 5432:
> ```powershell
> netstat -ano | findstr :5432
> tasklist /FI "PID eq <PID>"
> ```

**Giải pháp đã áp dụng ở máy này — DÙNG PostgreSQL native (không dùng container):**

```sql
-- chạy bằng psql với quyền superuser (VD: postgres), lệnh tạo role + database cho dự án
DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'harness') THEN
    CREATE ROLE harness LOGIN PASSWORD 'harness' SUPERUSER CREATEDB CREATEROLE;
  ELSE
    ALTER ROLE harness WITH LOGIN PASSWORD 'harness' SUPERUSER CREATEDB CREATEROLE;
  END IF;
END $$;
CREATE DATABASE harness OWNER harness;
```

**Lưu ý:** `harness` được tạo với quyền `SUPERUSER` để các migration tạo schema/tables/extension.
Nếu muốn dùng lại **Postgres Docker**, phải **dừng / đổi cổng** Postgres native (cần quyền Admin,
VD: `Stop-Service postgresql-x64-16`) để container giành lại `5432`.

### 2. Sửa lỗi migration filtered index (M3)

- **File:** `backend/src/Harness.Api/Persistence/Migrations/20260817142611_M3_InventoryAndCombos.cs`
- **Lỗi:** chạy migration trên **database mới** thất bại:
  `column "is_featured" does not exist` — index `IX_products_IsFeatured` dùng
  `filter: "is_featured = true"` (chữ thường) trong mệnh đề `WHERE`, trong khi cột thực tế là
  `"IsFeatured"` (phân biệt hoa/thường trong PostgreSQL).
- **Đã sửa thành:**
  ```csharp
  filter: "\"IsFeatured\" = true"
  ```

### 3. Migration mới đồng bộ model hiện tại (schema drift) — `AddProductModel3dUrl`

**Nguyên nhân:** một số thay đổi model có trong code nhưng **chưa có migration** tương ứng
(`Product.Model3dUrl`, và các bảng module Organization / Customer-chat), gây lỗi seed
`column "Model3dUrl" of relation "products" does not exist`.

**Đã thêm migration mới:** `backend/src/Harness.Api/Persistence/Migrations/20260821055443_AddProductModel3dUrl.cs`
- Thêm cột `catalog.products.Model3dUrl` (`text`, nullable).
- Tạo các bảng còn thiếu: `organization.stores`, `organization.attendance_records`,
  `organization.kpi_targets`, `customer.chat_sessions`, `customer.chat_messages`.

Nếu cần **tái tạo** migration này (sau khi rollback/nhánh khác), dùng công cụ EF (đã cài `dotnet-ef` 8.0):
```bash
cd backend
dotnet ef migrations add AddProductModel3dUrl --project src/Harness.Api --startup-project src/Harness.Api --output-dir Persistence/Migrations
```

### 4. Khi backend lỗi sau khi đổi DB → reset database sạch rồi migrate lại

Sau khi thay đổi connection string / migration, nên xoá sạch DB để `MigrateAsync` chạy lại
toàn bộ chuỗi migration từ đầu (Development tự migrate + seed):
```sql
SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='harness' AND pid<>pg_backend_pid();
DROP DATABASE IF EXISTS harness;
CREATE DATABASE harness OWNER harness;
```
Rồi chạy lại `cd backend && dotnet run --project src/Harness.Api`.

---

## Danh sách Cổng & URL các Dịch vụ Local

| Dịch vụ | Địa chỉ local (URL) | Mô tả / Ghi chú |
|---|---|---|
| **Website (End User)** | `http://localhost:3000` | Trang bán hàng Next.js cho Khách hàng |
| **Admin Panel** | `http://localhost:5173` | Quản trị cửa hàng, đơn hàng, chấm công, KPI |
| **Backend API & Swagger** | `http://localhost:5080/swagger` | Tài liệu API & Test Endpoints |
| **Hangfire Dashboard** | `http://localhost:5080/hangfire` | Monitor Background Jobs & Outbox Event Worker |
| **Health Check** | `http://localhost:5080/health` | Kiểm tra trạng thái kết nối DB / Cache |
| **PostgreSQL** | `localhost:5432` | Database (`harness` / `harness`) |
| **RabbitMQ Management** | `http://localhost:15672` | UI Quản lý Event Queue (`harness` / `harness`) |
| **Seq (Centralized Logs)** | `http://localhost:5341` | Hệ thống xem Log tập trung |
| **MinIO Console** | `http://localhost:9001` | S3 Object Storage chứa ảnh (`harness` / `harness123`) |

---

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

Nginx config mẫu tại `docker/nginx/nginx.conf` (reverse proxy `/api` → backend, `/` Next.js, chặn công khai cho Swagger/Hangfire).

## Ghi chú license

Toàn stack open-source: .NET 8 (MIT), PostgreSQL, Redis, RabbitMQ, Elasticsearch, MinIO, Next.js, React, Ant Design. Không chi phí license phần mềm khi triển khai.