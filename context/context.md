Project: Hệ thống quản lý tài liệu nội bộ có kiểm soát phiên bản và cộng tác theo Workspace (KLTN)

Stack:
- .NET (ASP.NET Core backend + frontend dùng .NET blazor)
- PostgreSQL
- Redis
- Docker (full local dev)
- SignalR (realtime)

Architecture:
- Clean Architecture
- Client Server
- Không microservice
- Ưu tiên đơn giản, dễ chạy cho team 5 người

Core Concepts:
- User có global_role (user, admin hidden)
- Workspace có owner và member (role riêng trong workspace)
- File là logical container (1 file = 1 folder)
- Version là storage thật (full + diff)

Database:
- Dùng PostgreSQL + UUID
- Cache bằng Redis
- Không dùng EF Migration
- Dùng init-db.sql chạy qua Docker
- Version unique (file_id, version_number)

Storage:
- files.folder_path = folder
- versions.storage_path = file thật

Realtime:
- SignalR dùng cho update task, version, comment

Constraints:
- Team 5 người → tránh over-engineering
- Code phải dễ đọc, dễ maintain
- Ưu tiên chạy được hơn là tối ưu cực đoan

Dev Rules:
- Không magic code
- API rõ ràng
- Structure nhất quán