# Nexora AI

Nexora AI là dự án nghiên cứu xây dựng không gian làm việc cho tổ chức, kết hợp quản lý tri thức, trợ lý AI và cộng tác. Người dùng có thể quản lý tài liệu, hỏi đáp với nguồn tham khảo, tham gia cuộc họp và sử dụng các chức năng theo vai trò được cấp

## Các chức năng chính 

| Thành phần | Mô tả |
| --- | --- |
| Trợ lý AI | Hội thoại, tìm kiếm tri thức từ tài liệu được phép truy cập và hiển thị nguồn tham khảo cho câu trả lời. |
| Kho tài liệu | Quản lý thư mục, tải tài liệu lên, trích xuất nội dung và lập chỉ mục để tìm kiếm. |
| Không gian quản trị | Quản lý tổ chức, phòng ban, người dùng, vai trò và quyền truy cập. |
| Cuộc họp | Tạo, theo dõi và tham gia cuộc họp trực tuyến qua LiveKit; hỗ trợ transcript lời nói bằng tiếng Việt và tiếng Anh. |
| Nền tảng kỹ thuật | ASP.NET Core Web API, Clean Architecture, CQRS/MediatR, PostgreSQL, MinIO và Qdrant; frontend dùng React/TypeScript. |

Các hình dưới đây minh họa giao diện của dự án nghiên cứu tại thời điểm viết README. Chức năng hiển thị có thể thay đổi trong quá trình phát triển.

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

## Cài đặt

Cần Docker và hai repository `my_rag-backend`, `my_rag-frontend` đặt cạnh nhau. Compose sẽ build ứng dụng từ mã nguồn.

1. Tạo `.env` từ `.env.example`; điền mật khẩu PostgreSQL, MinIO, chuỗi kết nối `ConnectionStrings__Postgres` và các khóa API cần dùng. `.env` không được commit.
2. Chạy `docker compose up -d --build` từ repository backend. Compose chạy PostgreSQL, MinIO, Qdrant, cập nhật schema rồi khởi động WebApi và frontend.
3. Mở `http://localhost:3000`.

Nếu cần chuyển dữ liệu PostgreSQL đang dùng, làm theo [hướng dẫn chuyển dữ liệu](docs/deployment.md) **trước bước 2**.

## Phát triển từ mã nguồn

Chạy `docker compose up -d postgres minio qdrant`. Khi backend chạy trực tiếp trên máy, PostgreSQL trong Compose ở `127.0.0.1:5433`; đổi `ConnectionStrings__Postgres` trong `.env` sang host và cổng này, hoặc đặt biến môi trường cùng tên để ghi đè. `dotnet run` và `dotnet ef` tự đọc `.env` ở thư mục gốc repository. Sau khi áp dụng migration:

```powershell
dotnet run --project src/Nexora.WebApi --launch-profile http
```

Frontend React/TypeScript nằm trong repository riêng `my_rag-frontend`; chạy `npm run dev` ở repository đó nếu phát triển giao diện từ mã nguồn.
