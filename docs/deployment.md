# Hướng dẫn triển khai bằng Docker

Chạy các lệnh từ thư mục gốc repository. Cần **Docker Compose**, một PostgreSQL truy cập được, hai image Docker Hub của backend/frontend và repository này để chạy migration. Ví dụ dưới đây dùng Linux và tag UAT `u1.0.1`; bản chính thức dùng tag `v1.0.1`.

1. Sao chép `.env.example` thành `.env`, điền tài khoản MinIO và thêm `COMPOSE_PROJECT_NAME=nexora`.
2. Tạo file secret WebApi **ngoài repository** từ [`secrets.example.json`](../src/Nexora.WebApi/secrets.example.json), ví dụ `/srv/nexora/runtime/webapi-uat.json`. Điền kết nối PostgreSQL, khóa JWT, khóa MinIO (trùng `.env`) và các API key cần dùng. Khi WebApi chạy trong Docker, đặt `Storage:Endpoint` là `http://minio:9000`, `Qdrant:Endpoint` là `http://qdrant:6333` và `Cors:AllowedOrigins` là URL frontend. Không đưa file secret vào Git hoặc image.
3. Chạy MinIO, Qdrant và cập nhật database. PostgreSQL phải chạy sẵn; máy thực hiện migration cần .NET 10 SDK, `dotnet-ef` 10.x và biến môi trường `ConnectionStrings__Postgres` trỏ tới database từ máy đó:

```bash
docker compose up -d minio qdrant
dotnet ef database update --project src/Nexora.Infrastructure \
  --startup-project src/Nexora.WebApi --context NexoraDbContext
```

4. Thay địa chỉ image, đường dẫn secret và cổng frontend trong ví dụ rồi chạy:

```bash
BACKEND_IMAGE="docker.io/your-user/nexora-webapi:u1.0.1"
FRONTEND_IMAGE="docker.io/your-user/nexora-frontend:u1.0.1"
FRONTEND_PORT=80

docker run -d --name nexora-webapi --restart unless-stopped \
  --network nexora_default -p 8080:8080 \
  --mount type=bind,src=/srv/nexora/runtime/webapi-uat.json,dst=/app/secrets.local.json,readonly \
  -e ASPNETCORE_ENVIRONMENT=Staging "$BACKEND_IMAGE"
docker run -d --name nexora-frontend --restart unless-stopped \
  -p 3000:"$FRONTEND_PORT" "$FRONTEND_IMAGE"
```

Mở `http://localhost:3000`; kiểm tra `docker logs nexora-webapi` nếu API không khởi động. Image frontend phải được build với `VITE_API_URL` trỏ tới URL WebApi mà trình duyệt truy cập được. Với production, thay tag bằng `v1.0.1`, dùng secret tương ứng và đặt `ASPNETCORE_ENVIRONMENT=Production`. Thay các địa chỉ image mẫu khi có link publish.
