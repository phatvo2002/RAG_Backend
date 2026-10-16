# Nexora AI

Nexora AI là dự án nghiên cứu về quản lý tri thức, trợ lý AI và cộng tác trong tổ chức.

## Chức năng chính

| Thành phần | Mô tả |
| --- | --- |
| Trợ lý AI | Hội thoại, tìm kiếm tri thức từ tài liệu được phép truy cập và hiển thị nguồn tham khảo cho câu trả lời. |
| Kho tài liệu | Quản lý thư mục, tải tài liệu lên, trích xuất nội dung và lập chỉ mục để tìm kiếm. |
| Không gian quản trị | Quản lý tổ chức, phòng ban, người dùng, vai trò và quyền truy cập. |
| Cuộc họp | Tạo, theo dõi và tham gia cuộc họp trực tuyến qua LiveKit; hỗ trợ transcript lời nói bằng tiếng Việt và tiếng Anh. |
| Nền tảng | ASP.NET Core Web API, PostgreSQL, MinIO, Qdrant và React/TypeScript. |

## Giao diện minh họa

### Đăng nhập

![Màn hình đăng nhập Nexora AI](docs/images/login.png)

### Trợ lý AI

![Không gian trò chuyện với trợ lý AI](docs/images/ai-assistant.png)

### Kho tài liệu

![Giao diện quản lý tài liệu và trạng thái lập chỉ mục](docs/images/document-repository.png)

### Cuộc họp

![Danh sách và thông tin chi tiết cuộc họp](docs/images/meetings.png)

### Quản lý vai trò và quyền

![Giao diện cấu hình vai trò và quyền truy cập](docs/images/roles-permissions.png)

## Cài đặt bằng Docker

Cần Docker Compose. Đặt repository backend này và [repository frontend](https://github.com/phatvo2002/RAG_Frontend) cạnh nhau; frontend cần có `Dockerfile` ở thư mục gốc:

```text
workspace/
├── my_rag-backend/
└── my_rag-frontend/
```

Từ thư mục `workspace`, tải frontend và tạo `.env` trong backend:

```sh
git clone https://github.com/phatvo2002/RAG_Frontend.git my_rag-frontend
cd my_rag-backend
cp .env.example .env
```

Điền các giá trị chính trong `.env`:

| Biến | Cách điền |
| --- | --- |
| PostgreSQL | `POSTGRES_PASSWORD` phải khớp mật khẩu trong `ConnectionStrings__Postgres`; giữ `Host=postgres;Port=5432`. |
| MinIO | `Storage__AccessKey`/`Storage__SecretKey` phải khớp `MINIO_ROOT_USER`/`MINIO_ROOT_PASSWORD`. |
| Khóa máy chủ | Điền `Jwt__SignaturePrivateKey` (ít nhất 32 ký tự). Tạo **hai khóa Base64 32 byte khác nhau** cho `ProviderKeys__EncryptionKeyBase64` và `Chat__EncryptionKeyBase64` ([cách tạo khóa](docs/personal-provider-keys.md)). |
| AI và cuộc họp | `Embedding__ApiKey` cho tìm kiếm tài liệu; `LiveKit__ApiKey`/`LiveKit__ApiSecret` khi dùng phòng họp. |

`.env` không được đưa lên Git. API key Gemini/Claude cho chat được từng người dùng thêm trong trang **Tài khoản**; `Qdrant__ApiKey` có thể để trống nếu Qdrant không bật xác thực.

Chạy trong thư mục backend:

```sh
docker compose up -d --build
docker compose ps
```

Mở [giao diện](http://localhost:3000) hoặc [Swagger API](http://localhost:8080/swagger). Compose tự chạy PostgreSQL, MinIO, Qdrant và EF migration. Nếu cổng 8080 bận, đổi cả `API_HOST_PORT` và cổng trong `PUBLIC_API_URL` (ví dụ `8081`).

Khi sửa code, chạy lại `docker compose up -d --build`. Xem lỗi bằng `docker compose logs --tail=100 webapi frontend`. Nếu dùng lại database cũ, giữ nguyên **hai khóa mã hóa cũ** và làm theo [hướng dẫn chuyển dữ liệu](docs/deployment.md) trước khi chạy. Compose không tự tạo tài khoản đăng nhập.
