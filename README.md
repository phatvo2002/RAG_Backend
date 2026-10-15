# Nexora AI

Nexora AI là dự án nghiên cứu xây dựng không gian làm việc cho tổ chức, kết hợp quản lý tri thức, trợ lý AI và cộng tác. Người dùng có thể quản lý tài liệu, hỏi đáp với nguồn tham khảo, tham gia cuộc họp và sử dụng các chức năng theo vai trò được cấp

## Các chức năng chính 

| Thành phần | Mô tả |
| --- | --- |
| Trợ lý AI | Hội thoại, tìm kiếm tri thức từ tài liệu được phép truy cập và hiển thị nguồn tham khảo cho câu trả lời. |
| Kho tài liệu | Quản lý thư mục, tải tài liệu lên, trích xuất nội dung và lập chỉ mục để tìm kiếm. |
| Không gian quản trị | Quản lý tổ chức, phòng ban, người dùng, vai trò và quyền truy cập. |
| Cuộc họp | Quản lý cuộc họp và kết nối âm thanh, hình ảnh qua LiveKit. Phụ đề trực tiếp cần worker riêng và chưa nằm trong bản chạy được hướng dẫn ở đây. |
| Nền tảng kỹ thuật | ASP.NET Core Web API, Clean Architecture, CQRS/MediatR, PostgreSQL, MinIO và Qdrant; frontend dùng React/TypeScript. |

Các hình dưới đây minh họa giao diện của dự án nghiên cứu tại thời điểm viết README. Chức năng hiển thị có thể thay đổi trong quá trình phát triển.

## Giao diện minh họa

### Đăng nhập

![Màn hình đăng nhập Nexora AI](docs/images/login.png)

### Trợ lý AI

![Không gian trò chuyện với trợ lý AI](docs/images/ai-assistant.png)

### Kho tài liệu

![Giao diện quản lý tài liệu và trạng thái lập chỉ mục](docs/images/document-repository.png)

### Quản lý vai trò và quyền

![Giao diện cấu hình vai trò và quyền truy cập](docs/images/roles-permissions.png)

## Cài đặt

Cần Docker, PostgreSQL và image backend/frontend trên Docker Hub.

1. Tạo `.env` cho MinIO và file cấu hình bí mật của WebApi từ [`secrets.example.json`](src/Nexora.WebApi/secrets.example.json). Giữ các khóa ngoài Git.
2. Chạy MinIO và Qdrant: `docker compose up -d minio qdrant`.
3. Cập nhật database, sau đó chạy image backend và frontend.

Xem [hướng dẫn triển khai bằng Docker](docs/deployment.md) để có lệnh cụ thể và cách cấu hình.

## Phát triển từ mã nguồn

Sao chép `src/Nexora.WebApi/secrets.example.json` thành `src/Nexora.WebApi/secrets.local.json`, điền thông tin cho môi trường phát triển rồi chạy `docker compose up -d` để có MinIO/Qdrant. PostgreSQL vẫn cần được chuẩn bị riêng. Sau khi áp dụng migration:

```powershell
dotnet run --project src/Nexora.WebApi --launch-profile http
```

Frontend React/TypeScript nằm trong repository riêng `my_rag-frontend`; chạy `npm run dev` ở repository đó nếu phát triển giao diện từ mã nguồn.
