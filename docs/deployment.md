# Chạy Nexora bằng Docker Compose

Đặt hai repository cạnh nhau:

```text
workspace/
├── my_rag-backend/
└── my_rag-frontend/
```

Nếu frontend ở nơi khác, đổi `FRONTEND_BUILD_CONTEXT` trong `.env` thành đường dẫn tới repository đó. Không cần image Docker Hub hoặc .NET SDK trên máy chạy Compose.

## Chạy lần đầu với database mới

1. Trong `my_rag-backend`, sao chép `.env.example` thành `.env`; điền `POSTGRES_PASSWORD`, `MINIO_ROOT_USER` và `MINIO_ROOT_PASSWORD`. Đổi `FRONTEND_ORIGIN`, `PUBLIC_API_URL` và `API_ALLOWED_HOSTS` nếu truy cập bằng địa chỉ khác `localhost`.
2. Trong chính `.env`, điền `ConnectionStrings__Postgres` với `Host=postgres;Port=5432` và database, user, password khớp các biến PostgreSQL ở trên. Điền `Jwt__SignaturePrivateKey`, `Storage__AccessKey`, `Storage__SecretKey` (khớp tài khoản MinIO), `Qdrant__ApiKey` nếu Qdrant bật xác thực, và các khóa AI/LiveKit cần dùng. Compose truyền các biến này cho cả migration và WebApi; không cần file JSON bí mật riêng. `.env` đã nằm trong `.gitignore` và `.dockerignore`.
3. Từ `my_rag-backend`, chạy:

```powershell
docker compose config --quiet
docker compose up -d --build
docker compose ps
docker compose logs --tail 100 migrate webapi frontend
```

Compose build WebApi và frontend tại máy, tạo database, chạy EF migration bằng service `migrate` rồi mới khởi động WebApi. Frontend nhận `PUBLIC_API_URL` khi container chạy; URL này phải truy cập được **từ trình duyệt**, không dùng tên service `webapi`. Mở `http://localhost:3000` để kiểm tra.

Các adapter MinIO và Qdrant đọc `Storage__*` và `Qdrant__*` từ cấu hình .NET. Khi chạy bằng Compose, endpoint được đặt thành `http://minio:9000` và `http://qdrant:6333`; khi chạy `dotnet run` trực tiếp, WebApi đọc các giá trị trong `.env` (mặc định localhost). Biến môi trường của tiến trình luôn được ưu tiên hơn `.env`.

Nếu cổng 8080 đã được sử dụng, đổi đồng thời `API_HOST_PORT` và cổng trong `PUBLIC_API_URL` (ví dụ `8081`). Mặc định MinIO/Qdrant dùng Docker named volume. Để dùng lại thư mục dữ liệu cũ, đặt `MINIO_MOUNT_TYPE=bind`, `MINIO_DATA_SOURCE=<đường dẫn thư mục MinIO>`, `QDRANT_MOUNT_TYPE=bind` và `QDRANT_DATA_SOURCE=<đường dẫn thư mục Qdrant>` trong `.env` trước khi khởi động.

## Chuyển dữ liệu PostgreSQL hiện có

Nếu đang có database cần giữ, thực hiện phần này **trước** `docker compose up -d --build` lần đầu. Dừng các thao tác ghi vào database cũ. Kiểm tra phiên bản nguồn, tạo backup **mới** bằng `pg_dump` cùng phiên bản chính hoặc mới hơn, rồi kiểm tra archive:

```powershell
$sourceUser = 'YOUR_SOURCE_USER'
pg_dump -h 127.0.0.1 -p 5432 -U $sourceUser -d Nexora_AI -Fc -f D:\backups\nexora-latest.dump
pg_restore --list D:\backups\nexora-latest.dump
```

Chuẩn bị thư mục `D:\backups` trước khi chạy. File `D:\RAG\16-09-2026.sql` là backup cũ, không thay cho bản vừa tạo. PostgreSQL trong Compose dùng cổng host `127.0.0.1:5433` để tránh xung đột với nguồn ở cổng 5432. Khởi động riêng các dịch vụ lưu trữ và restore vào **database trống**:

```powershell
docker compose up -d postgres minio qdrant
docker compose cp D:\backups\nexora-latest.dump postgres:/tmp/nexora.dump
docker compose exec -T postgres sh -c 'pg_restore --no-owner --no-privileges --exit-on-error -U "$POSTGRES_USER" -d "$POSTGRES_DB" /tmp/nexora.dump'
docker compose exec -T postgres rm /tmp/nexora.dump
```

So sánh số bản ghi người dùng, tài liệu, hội thoại và lịch sử EF migration giữa hai database. Sau đó chạy `docker compose up -d --build`; service `migrate` chỉ áp dụng migration còn thiếu. **Không** lặp lại restore trên volume đã chứa dữ liệu. Giữ database cũ và file backup cho đến khi đăng nhập, chat, tài liệu và cuộc họp hoạt động với database mới. Không dùng `docker compose down -v` vì lệnh đó xóa dữ liệu volume.
