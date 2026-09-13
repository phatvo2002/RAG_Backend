# RAG document upload

## Luồng đã triển khai

`POST /api/documents` nhận `multipart/form-data` với field `file`. API lưu file gốc vào MinIO, tạo metadata trong `document.Documents` và `document.DocumentVersions`, sau đó đưa `versionId` vào queue in-memory.

Worker nền thực hiện:

1. Đọc file từ MinIO.
2. TXT/MD đọc trực tiếp; DOCX đọc `word/document.xml`; PDF dùng `pdftotext` trước.
3. Nếu PDF không có text layer, chạy OCRmyPDF với Tesseract `vie+eng` rồi đọc text lại bằng `pdftotext`.
4. Chuẩn hóa và chia nội dung thành chunk 1.200 ký tự, overlap 200 ký tự.
5. Lưu nội dung chunk trong MinIO và metadata trong `document.DocumentChunks`/`document.DocumentChunkContents`.
6. Gọi embedding provider tương thích OpenAI, upsert vector vào Qdrant.
7. Lưu `rag.RagIndexes`/`rag.RagIndexRecords` và chuyển `DocumentVersion` cùng `Document` sang `READY`.

Nếu một bước thất bại, version và document chuyển sang `FAILED`. Queue hiện tại là in-memory theo đúng MVP; restart process sẽ không tự khôi phục item chưa xử lý.

## Cấu hình local

Không đặt secret vào `appsettings.json`. Có thể dùng biến môi trường PowerShell:

```powershell
$env:Storage__AccessKey = "admin"
$env:Storage__SecretKey = "admin123456"
$env:Storage__Endpoint = "http://localhost:9000"
$env:Storage__Bucket = "nexora-documents"
$env:Qdrant__Endpoint = "http://localhost:6333"
$env:Qdrant__CollectionName = "nexora_documents"
$env:Qdrant__ApiKey = ""
$env:Embedding__Endpoint = "https://api.openai.com/v1/embeddings"
$env:Embedding__ApiKey = "<embedding-api-key>"
$env:Embedding__Model = "text-embedding-3-small"
$env:Embedding__Dimension = "1536"
```

`Storage__*` là cấu hình của Web API; `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD` trong Compose chỉ là credential khởi tạo MinIO. Nếu dùng Qdrant API key, cấu hình cùng một giá trị cho `QDRANT_API_KEY` và `Qdrant__ApiKey`.

## Kiểm tra API

```powershell
curl.exe -X POST "http://localhost:5265/api/documents" `
  -H "Authorization: Bearer <access-token>" `
  -F "file=@C:\data\handbook.pdf"

curl.exe -X GET "http://localhost:5265/api/documents/<document-id>/status" `
  -H "Authorization: Bearer <access-token>"
```

Response upload trả `documentId`, `versionId` và trạng thái ban đầu `QUEUED`. Poll endpoint status đến khi `READY` hoặc `FAILED`.

## API catalog

Sau khi cập nhật database, chạy `scripts/seed-api-functions.sql` hoặc gọi flow sync catalog để thêm:

- `DOCUMENT.UPLOAD` — `POST /api/documents`
- `DOCUMENT.READ` — `GET /api/documents/{documentId:guid}`
- `DOCUMENT.STATUS` — `GET /api/documents/{documentId:guid}/status`

Quyền vẫn theo `User -> Role -> RoleApiFunction`; API không nhận `OrganizationId` từ client.
