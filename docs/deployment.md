# Chạy Nexora bằng Docker Compose

Đặt hai repository cạnh nhau:

```text
workspace/
├── my_rag-backend/
└── my_rag-frontend/
```

Nếu frontend ở nơi khác, đổi `frontend.build.context` trong `docker-compose.yml` thành đường dẫn tới repository đó. Không cần image Docker Hub hoặc .NET SDK trên máy chạy Compose.

## Kiểm thử WebApi trên máy

Sao chép `.env.example` thành `.env` và điền `ConnectionStrings__Postgres` tới PostgreSQL trên máy, ví dụ `Host=127.0.0.1;Port=5432;Database=Nexora_AI_V2;Username=postgres;Password=...`. Điền các khóa dịch vụ cần dùng. WebApi chạy từ Visual Studio hoặc `dotnet run` đọc chuỗi kết nối này; endpoint MinIO/Qdrant và provider Gemini mặc định lấy từ `src/Nexora.WebApi/appsettings.json`. `.env` chỉ chứa secret và chuỗi kết nối, đã được Git và Docker build bỏ qua.

Chỉ cần MinIO và Qdrant trong Docker khi kiểm thử local:

```powershell
docker compose up -d minio qdrant
dotnet run --project src/Nexora.WebApi
```

## Triển khai bằng Docker Compose

1. Chuẩn bị PostgreSQL `Nexora_AI_V2` trên máy với schema hiện tại. Compose không tạo PostgreSQL hoặc tự chạy EF migration.
2. Trong `.env`, điền `COMPOSE_POSTGRES_CONNECTION_STRING` tới cùng database với `ConnectionStrings__Postgres`, nhưng dùng `Host=host.docker.internal` khi PostgreSQL chạy trên máy Windows và WebApi chạy trong Docker Desktop. Trên server khác, dùng hostname/IP mà container truy cập được. Điền các credential và khóa dịch vụ cần dùng.
3. Trước khi triển khai production, sửa `docker-compose.yml`: thay đường dẫn bind Windows `D:/RAG/MinIO` và `D:/RAG/Qdrant` bằng đường dẫn trên server; đổi `ASPNETCORE_ENVIRONMENT: Development`, `Cors__AllowedOrigins__0`, `AllowedHosts`, cổng và `PUBLIC_API_URL` theo địa chỉ triển khai. `PUBLIC_API_URL` phải truy cập được từ trình duyệt. Embedding chỉ dùng `gemini-embedding-001`; API key nằm trong `.env`.
4. Từ `my_rag-backend`, chạy:

```powershell
docker compose config --quiet
docker compose up -d --no-deps --build webapi
docker compose up -d --no-deps --build frontend
docker compose ps webapi frontend
docker compose logs --tail 100 webapi frontend
```

Hai lệnh `--no-deps` chỉ build và chạy WebApi/frontend; MinIO và Qdrant phải đang chạy sẵn. WebApi đọc `COMPOSE_POSTGRES_CONNECTION_STRING`; frontend nhận `PUBLIC_API_URL` khi container chạy. Với cấu hình local hiện tại, mở `http://localhost:3000` để kiểm tra. EF migration vẫn lưu trong source để cập nhật schema thủ công khi cần.

Các adapter MinIO và Qdrant đọc `Storage__*` và `Qdrant__*` từ cấu hình .NET. Khi chạy bằng Compose, endpoint được đặt thành `http://minio:9000` và `http://qdrant:6333` trong `docker-compose.yml`; khi chạy `dotnet run` trực tiếp, WebApi đọc endpoint localhost từ `src/Nexora.WebApi/appsettings.json`. Biến môi trường của tiến trình luôn được ưu tiên hơn `.env` và appsettings.

Nếu cổng 8081 đã được sử dụng, đổi đồng thời cổng xuất bản của `webapi` và cổng trong `PUBLIC_API_URL` tại `docker-compose.yml`. Cấu hình local hiện dùng bind mount `D:/RAG/MinIO` và `D:/RAG/Qdrant`; đảm bảo các thư mục này tồn tại và Docker truy cập được trước khi khởi động.

## Chuyển dữ liệu PostgreSQL hiện có

Nếu chuyển sang PostgreSQL mới trên server, dừng ghi vào database nguồn, tạo bản backup mới bằng `pg_dump`, rồi dùng `pg_restore` trực tiếp vào database trống trên server. So sánh số user, tài liệu, hội thoại và lịch sử EF migration trước khi khởi động Compose. Giữ bản backup và database nguồn cho đến khi kiểm tra đăng nhập và các chức năng chính trên server.
