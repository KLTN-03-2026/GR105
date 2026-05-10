<!--
Mục đích: Cập nhật mới
File path đối chiếu: /docs/ENDPOINTS.md
Giải thích: Cập nhật đầy đủ các endpoints từ AuthController, UserController, WorkspaceController, FileController, CommentController và FeedbacksController bao gồm các Use Case 6, 7, 8, 9.
-->

# Tài liệu API Endpoints (Cập nhật - Final Normal UCs)

Dưới đây là danh sách toàn bộ các API Endpoints hiện có của hệ thống Document Management System (DMS). Base URL mặc định: `http://localhost:5087`.

---

## 1. Authentication (`/api/auth`)
Các API liên quan đến xác thực người dùng.

### 1.1. Đăng ký & Đăng nhập
- **POST /api/auth/register**: Tạo tài khoản mới.
  - **Body**: `{ "username": "user123", "email": "user@example.com", "password": "yourpassword" }`
- **POST /api/auth/login**: Xác thực người dùng và nhận JWT Token.
  - **Body**: `{ "email": "user@example.com", "password": "yourpassword" }`
  - **Response**: `200 OK` kèm `token` và thông tin user.

### 1.2. Khôi phục mật khẩu (Phase 14)
- **POST /api/auth/forgot-password**: Yêu cầu link khôi phục mật khẩu.
  - **Body**: `{ "email": "user@example.com" }`
- **POST /api/auth/reset-password**: Đặt lại mật khẩu bằng Token một lần.
  - **Body**: `{ "token": "...", "newPassword": "NewPassword123!" }`

---

## 2. User (`/api/user`)
Quản lý thông tin tài khoản cá nhân (UC7). Yêu cầu **Authorization**.

### 2.1. Quản lý Profile
- **GET /api/user/me**: Lấy thông tin user đang đăng nhập (Profile đầy đủ).
- **PUT /api/user/me**: Cập nhật thông tin cá nhân (Username).
  - **Body**: `{ "username": "New Name" }`
- **PUT /api/user/me/password**: Đổi mật khẩu.
  - **Body**: `{ "oldPassword": "...", "newPassword": "..." }`

### 2.2. Thông tin User khác
- **GET /api/user/{id}**: Lấy thông tin cơ bản của một user bất kỳ theo ID.

---

## 3. Workspace & Invites (`/api/workspace`)
Quản lý không gian làm việc và thành viên (UC8). Yêu cầu **Authorization**.

### 3.1. Quản lý Workspace
- **POST /api/workspace**: Tạo Workspace mới.
- **GET /api/workspace**: Lấy danh sách Workspace sở hữu.
- **PUT /api/workspace/{id}**: Cập nhật thông tin (Chỉ Owner).
- **DELETE /api/workspace/{id}**: Xóa Workspace (Chỉ Owner).
- **POST /api/workspace/{id}/leave**: Rời khỏi Workspace (Owner không được phép).
- **GET /api/workspace/{id}/logs**: Lấy lịch sử hoạt động (Activity Logs) của Workspace (Hỗ trợ `limit`, `offset`) (Phase 13).

### 3.2. Quản lý Thành viên (Thủ công)
- **POST /api/workspace/{id}/members**: Thêm thành viên trực tiếp.
- **GET /api/workspace/{id}/members**: Lấy danh sách thành viên.
- **DELETE /api/workspace/{id}/members/{userIdToRemove}**: Đuổi thành viên.

### 3.3. Lời mời qua Link (Public Invite)
- **POST /api/workspace/{id}/invite-code**: Tạo/Reset mã mời ngẫu nhiên (Chỉ Owner).
- **PUT /api/workspace/{id}/invite-status**: Bật/tắt link mời.
  - **Body**: `{ "inviteEnabled": true }`
- **POST /api/workspace/join-by-code**: Tham gia bằng mã mời.
  - **Body**: `{ "inviteCode": "w_abc123" }`

### 3.4. Lời mời qua Email (Direct Invite)
- **POST /api/workspace/{id}/invites**: Gửi lời mời kèm Token đến Email.
  - **Body**: `{ "email": "target@example.com" }`
- **POST /api/workspace/invites/accept**: Chấp nhận lời mời qua Token (Bắt buộc Email đang đăng nhập phải khớp Email được mời).
  - **Body**: `{ "token": "..." }`

---

## 4. File & Versioning (`/api/workspaces/{workspaceId}/files`)
Quản lý tập tin, phiên bản và phục hồi. Yêu cầu **Authorization**.

### 4.1. Tập tin (Files)
- **POST /upload**: Tải lên tập tin mới hoặc tạo phiên bản mới cho tập tin cũ.
- **GET /**: Lấy danh sách tập tin (hỗ trợ `limit`, `offset`).
- **GET /search**: Tìm kiếm tập tin theo tên và nội dung (Full-Text Search) (Phase 12).
  - **Query**: `?q=keyword`
- **DELETE /{fileId}**: Xóa mềm (Soft Delete) tập tin.
- **POST /{fileId}/restore**: Khôi phục tập tin từ thùng rác.

### 4.2. Thùng rác (Trash)
- **GET /trash**: Lấy danh sách tập tin đã xóa mềm.

### 4.3. Phiên bản & Tải về
- **GET /{fileId}/versions**: Lấy lịch sử các phiên bản của tập tin.
- **GET /{fileId}/download**: Tải về raw file (Hỗ trợ query `?versionId=...`).

### 4.4. Phân tích & Xem trước
- **GET /{fileId}/versions/{versionId}/preview**: Trích xuất nội dung văn bản để preview.
- **GET /{fileId}/diff**: So sánh JSON Github-style giữa 2 phiên bản.
- **GET /{fileId}/versions/{versionId}/view**: Xem trực tiếp File (Streaming Video/Audio, Auto-convert Image sang webp).

---

## 5. Comments (`/api/workspaces/{workspaceId}/files/{fileId}/comments`)
Hệ thống bình luận Realtime (UC9). Yêu cầu **Authorization**.

- **GET /**: Lấy danh sách bình luận của File (Query `?versionId=...` để lấy của version).
- **POST /**: Tạo bình luận mới.
  - **Body**: `{ "content": "...", "versionId": null }`
- **PUT /{commentId}**: Sửa nội dung bình luận (Chỉ tác giả).
- **DELETE /{commentId}**: Xóa bình luận (Tác giả hoặc Owner).

*(Hỗ trợ SignalR Realtime thông qua Hub: `/hubs/workspace` với các event `comment_created`, `comment_updated`, `comment_deleted`)*

---

## 6. Feedbacks (`/api/feedbacks`)
Thu thập phản hồi hệ thống (UC6). Yêu cầu **Authorization**.

- **POST /**: Gửi phản hồi, góp ý.
  - **Body**: `{ "content": "Hệ thống rất tốt!" }`

---

## Ghi chú Quan trọng
1. **Phân quyền**: Toàn bộ API (trừ Auth) yêu cầu JWT Token trong Header (`Authorization: Bearer <token>`).
2. **Rate Limiting**: 
   - Global: 100 requests / phút / User (hoặc IP).
   - Auth (/login): 10 requests / phút / IP (Chống Brute-force).
3. **Soft Delete**: File bị xóa sẽ nằm trong `trash`. Worker ngầm dọn dẹp sau 30 ngày.

---

## 7. Admin (`/api/admin`)
Hệ thống API dành riêng cho Quản trị viên (Phase 15 - Admin UCs). **BẮT BUỘC YÊU CẦU JWT Token có Role = "admin"**.

### 7.1. Quản lý Tài khoản (ad_uc1)
- **GET /api/admin/users**: Lấy danh sách toàn bộ User (Hỗ trợ Pagination, Search).
- **POST /api/admin/users**: Tạo tài khoản (Bỏ qua email verify, tự băm mật khẩu).
  - **Body**: `{ "email": "...", "username": "...", "password": "...", "role": "admin|user" }`
- **PUT /api/admin/users/{id}**: Cập nhật Username / Role.
- **PUT /api/admin/users/{id}/lock**: Bật/Tắt khóa tài khoản (`is_locked`). Tài khoản bị khóa sẽ bị chặn tại hàm Login.

### 7.2. Quản lý Workspace Hệ thống (ad_uc2)
- **GET /api/admin/workspaces**: Xem tất cả Workspaces (Kèm số lượng File/Member & Email Owner).
- **GET /api/admin/workspaces/{id}**: Xem chi tiết 1 Workspace.
- **DELETE /api/admin/workspaces/{id}**: Xóa cứng (Hard Delete) toàn bộ Workspace và file bên trong (Dành cho việc dọn dẹp vi phạm).

### 7.3. Giám sát Log Hệ thống (ad_uc3)
- **GET /api/admin/logs**: Xem toàn bộ Activity Log của hệ thống (Vượt quyền Workspace). Hỗ trợ filter theo `userId`, `workspaceId`, `action`.

### 7.4. Quản lý Phản hồi (ad_uc4)
- **GET /api/admin/feedbacks**: Xem danh sách các Feedbacks từ user.
- **PUT /api/admin/feedbacks/{id}/status**: Đổi trạng thái xử lý feedback.
  - **Body**: `{ "status": "resolved|open" }`

### 7.5. Giám sát Yêu cầu Đổi mật khẩu (ad_uc5)
- **GET /api/admin/password-reset-requests**: Theo dõi các yêu cầu khôi phục mật khẩu để phòng ngừa spam.