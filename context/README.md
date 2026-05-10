# Document Management System (DMS) - Backend

Dự án Backend cho Hệ thống Quản lý Tài liệu (DMS).

## Yêu cầu hệ thống (Prerequisites)
- [Docker & Docker Compose](https://www.docker.com/) (để chạy PostgreSQL và Redis)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Trình soạn thảo mã (VS Code, Visual Studio, JetBrains Rider)

## Thiết lập môi trường phát triển (Development Environment)

### 1. Khởi động Cơ sở dữ liệu (PostgreSQL & Redis)
Hệ thống sử dụng Docker Compose để cấu hình nhanh các dịch vụ cần thiết. File cấu hình được đặt tại `context/docker-compose.yml`.

Chạy lệnh sau tại thư mục gốc của dự án:
```bash
cd context
docker-compose up -d
```
Lệnh này sẽ khởi động:
- **PostgreSQL**: port `5432` với user `dev`, password `dev`, và database `dms`. (Kèm script khởi tạo `init-db.sql`)
- **Redis**: port `6379`.

### 2. Thiết lập biến môi trường (.env)
Ứng dụng yêu cầu một file `.env` chứa các thông tin kết nối và cấu hình bảo mật. 

Di chuyển vào thư mục `backend` (nơi chứa source code chính) hoặc nơi chứa template `.example.env`, tiến hành copy file thành `.env`:
```bash
cd backend
cp .example.env .env
```
*(Nếu bạn đang dùng Windows Command Prompt, sử dụng lệnh `copy .example.env .env`)*

Tiếp theo, mở file `.env` vừa tạo và điền đầy đủ các thông tin:
```ini
DB_HOST=localhost
DB_PORT=5432
DB_NAME=dms
DB_USER=dev
DB_PASS=dev

REDIS_URL=localhost:6379

# jwt_secret cần một chuỗi ký tự ngẫu nhiên dài (ít nhất 32 ký tự)
JWT_SECRET=your_super_secret_jwt_key_here_12345
JWT_ISSUER=your_issuer
JWT_AUDIENCE=your_audience
JWT_EXPIRE_MINUTES=60

# CORS settings (comma-separated list of allowed origins for Blazor/Frontend)
CORS_ORIGINS=http://localhost:5000,https://localhost:5001,http://localhost:3000,http://localhost:5173

# Admin Seeding (Mandatory, for first run)
DEFAULT_ADMIN_EMAIL=
DEFAULT_ADMIN_PASSWORD=
```

### 3. Cài đặt các gói NuGet (Restore packages)
```bash
dotnet restore
```

### 4. Khởi chạy dự án
```bash
dotnet run
```
Dự án sẽ khởi chạy và lắng nghe ở port được cấu hình (ví dụ: `http://localhost:5087`).

## Tài liệu API
Vui lòng tham khảo file danh sách các endpoint tại [context/ENDPOINTS.md](context/ENDPOINTS.md).