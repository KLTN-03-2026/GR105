

# Tài liệu API Endpoints

Dưới đây là danh sách toàn bộ các API Endpoints hiện có của hệ thống Document Management System (DMS). Base URL mặc định khi chạy ở môi trường phát triển là `http://localhost:5087` (hoặc cấu hình tương ứng).

---

## 1. Authentication (`/api/auth`)
Các API liên quan đến xác thực người dùng.

### 1.1. Đăng nhập
- **Endpoint**: `POST /api/auth/login`
- **Mô tả**: Gửi thông tin đăng nhập và nhận về JWT Token.
- **Request Body** (JSON):
  - `email` (string)
  - `password` (string)
- **Response**: Trả về JWT Token (kèm các thông tin theo cấu hình nếu có).

### 1.2. Đăng ký
- **Endpoint**: `POST /api/auth/register`
- **Mô tả**: Tạo một tài khoản mới.
- **Request Body** (JSON):
  - `email` (string)
  - `password` (string)
  - `firstName` / `lastName` (nếu hệ thống yêu cầu cấu trúc chuẩn)
- **Response**: `200 OK` kèm thông tin đăng ký thành công.

---

## 2. User (`/api/user`)
Các API dùng để lấy thông tin tài khoản, quyền hạn.

### 2.1. Lấy thông tin User theo ID
- **Endpoint**: `GET /api/user/{id}`
- **Mô tả**: Trả về thông tin cơ bản của một User.
- **Authorization**: Không bắt buộc (hoặc phụ thuộc tùy biến Route).
- **Response**: `200 OK` với id, email của User tương ứng.

### 2.2. Lấy thông tin User hiện tại (Me)
- **Endpoint**: `GET /api/user/me`
- **Mô tả**: Lấy thông tin tài khoản của chính user đang gọi API dựa vào Token.
- **Authorization**: **Có** (`Bearer Token`)
- **Response**: `200 OK` chứa thông tin id, email của chủ Token.

---

## 3. Workspace (`/api/workspace`)
Các API dùng để quản lý không gian làm việc. Toàn bộ Workspace Controller đều yêu cầu **Authorization (Bearer Token)**.

### 3.1. Tạo Workspace mới
- **Endpoint**: `POST /api/workspace`
- **Mô tả**: Tạo một Workspace mới. User gắn liền với Token sẽ là Owner của Workspace này.
- **Authorization**: **Có**
- **Request Body** (JSON): `CreateWorkspaceRequest`
  - `name` (string): Tên Workspace
  - `description` (string) (tùy chọn)
- **Response**: `201 Created` kèm thông tin Workspace.

### 3.2. Lấy danh sách Workspace đã tạo/sở hữu
- **Endpoint**: `GET /api/workspace`
- **Mô tả**: Trả về danh sách tất cả các Workspace mà User đang yêu cầu được phân quyền là Owner.
- **Authorization**: **Có**
- **Response**: `200 OK` danh sách Workspaces.

### 3.3. Cập nhật Workspace
- **Endpoint**: `PUT /api/workspace/{id}`
- **Mô tả**: Thay đổi thông tin Workspace. (Yêu cầu phải là Owner).
- **Authorization**: **Có**
- **Tham số URL**:
  - `id`: Guid của Workspace cần sửa.
- **Request Body** (JSON): `UpdateWorkspaceRequest`
  - `name` (string)
  - `description` (string)
- **Response**: `200 OK` (hoặc Message trạng thái).

### 3.4. Xóa Workspace
- **Endpoint**: `DELETE /api/workspace/{id}`
- **Mô tả**: Xóa vĩnh viễn một Workspace. (Yêu cầu phải là Owner).
- **Authorization**: **Có**
- **Tham số URL**:
  - `id`: Guid của Workspace cần xóa.
- **Response**: `200 OK` (hoặc Message trạng thái).