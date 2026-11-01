# Chat với tài liệu bằng Ollama

WebApi dùng `qwen3.5:4b` để tạo câu trả lời và `qwen3-embedding:0.6b` để tìm tài liệu. Cả hai chạy trên Ollama nội bộ; người dùng không cần cấu hình API key Gemini hoặc Claude.

## Cấu hình

| Biến | Mục đích |
| --- | --- |
| `Chat__EncryptionKeyBase64` | Base64 của 32 byte ngẫu nhiên, giữ ổn định để đọc lịch sử chat đã mã hóa. |
| `Ollama__Url` | URL Ollama mà WebApi truy cập được; Compose dùng `http://ollama:11434/`. |
| `Chat__Model` | Model trả lời, mặc định `qwen3.5:4b`. |
| `Embedding__Model` | Model tạo vector, mặc định `qwen3-embedding:0.6b`. |
| `Chat__DepartmentMatchThreshold` | Ngưỡng kiểm tra tài liệu thuộc phòng ban, mặc định `0.70`. |
| `Chat__MinimumRagScore` | Điểm cosine thấp nhất để một chunk được đưa vào context và danh sách nguồn, mặc định `0.65`. |
| `Chat__EvidenceAssessmentTimeoutSeconds` | Giới hạn thời gian cho lần gọi Ollama kiểm tra căn cứ, mặc định 60 giây. |

Tải model một lần vào volume Ollama:

```powershell
docker compose up -d ollama
docker compose exec ollama ollama pull qwen3.5:4b
docker compose exec ollama ollama pull qwen3-embedding:0.6b
```

`GET /api/chat/models` trả model local mà không đọc khóa của từng người dùng. `POST /api/chat/turns/stream` tiếp tục phát `started`, `sources`, `delta`, `completed` và `error` qua SSE. WebApi giữ nguyên quyền truy cập tài liệu và mã hóa lịch sử chat. Nếu Ollama ngừng hoạt động hoặc thiếu model, lượt chat thất bại và giao diện báo lỗi; hệ thống không gọi dịch vụ AI trả phí.

## Luồng chọn cách trả lời

1. Lời chào hoặc small talk độc lập (chẳng hạn `Xin chào`, `How are you?`) đi thẳng tới Ollama, không tạo embedding hoặc gọi Qdrant. Nếu câu có thêm nội dung hỏi về công ty/tài liệu, hệ thống xử lý phần hỏi qua RAG.
2. Các câu hỏi còn lại tìm kiếm vector. Hệ thống kiểm tra phạm vi phòng ban trước. Nếu kết quả tốt nhất thuộc tài liệu phòng ban mà người dùng không được xem và vượt `Chat__DepartmentMatchThreshold`, câu trả lời bị từ chối và không đọc nội dung chunk.
3. Sau kiểm tra quyền, chunk có cosine score từ `Chat__MinimumRagScore` trở lên chỉ là ứng viên. Ollama local kiểm tra đoạn trích có trả lời đúng câu hỏi hay không; backend xác nhận đoạn trích nguyên văn và chỉ đưa nguồn đã được chấp nhận vào `sources`, context và citation.
4. Nếu không có căn cứ, câu hỏi về số liệu hiện tại hoặc thông tin nội bộ nhận phản hồi thiếu căn cứ; yêu cầu giới thiệu chung có thể được trả lời ở mức khái quát, không có citation. Lỗi embedding, Qdrant hoặc Ollama làm lượt chat thất bại, không tự chuyển sang câu trả lời tổng quát.

Câu hỏi và nguồn truy xuất được đánh dấu thành các phần riêng. Nguồn tài liệu được xem là dữ liệu không đáng tin, không phải chỉ thị cho model. System Prompt đã xuất bản của organization điều khiển cách diễn đạt; mã nguồn chỉ giữ quy tắc bảo mật tối thiểu. Bước đánh giá thêm một lần gọi Ollama và có thể đánh giá sai với model nhỏ, nên cần đo độ trễ và chất lượng trên câu hỏi thật. Bản nháp System Prompt chưa xuất bản không ảnh hưởng tới lượt chat.
