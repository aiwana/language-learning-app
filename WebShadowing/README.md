# WebShadowing — Backend (Issue #4)

EF Core map schema từ:

- `Designs/Database/DatabaseCreation.sql`
- `Designs/Database/Schema_v0p1_extension.sql`

Không dùng EF Migration để tạo/sửa schema.

## Connection string

`appsettings.json` giữ `DefaultConnection` rỗng. Local:

1. Copy `appsettings.Development.json.example` → `appsettings.Development.json` (gitignored)
2. Hoặc User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=EnglishShadowingDB;Trusted_Connection=True;TrustServerCertificate=True"
```

## OpenAI cho chấm phát âm và từ điển

Không ghi API key vào `appsettings.json` hay source code. Trước khi chạy local, đặt biến môi trường:

```powershell
$env:OPENAI_API_KEY="your-new-key"
dotnet run
```

Khi không có key hoặc OpenAI tạm lỗi, endpoint chấm phát âm trả HTTP 503. Hệ thống không sinh điểm, transcript hay kết quả đạt giả lập.

Hai endpoint AI có rate limit theo tài khoản đăng nhập: chấm phát âm tối đa 10 yêu cầu/phút và từ điển/IPA tối đa 30 yêu cầu/phút.

## Provider chấm phát âm chuyên dụng

Luồng `evaluate-shadowing` ưu tiên provider chuyên dụng (Azure Speech Pronunciation Assessment) và chỉ fallback sang OpenAI khi bật cờ cấu hình.

- `AzureSpeech:ApiKey`, `AzureSpeech:Region`: cấu hình provider chuyên dụng.
- `PronunciationAssessment:EnableOpenAiFallback`: bật/tắt fallback OpenAI.
- `PronunciationAssessment:MaxAudioDurationSeconds`: giới hạn thời lượng audio WAV.
- `PronunciationAssessment:ProviderTimeoutSeconds`: timeout khi gọi provider.

Client cần gửi `Idempotency-Key` trong header khi gọi `POST /api/practice/evaluate-shadowing` để tránh lưu trùng `Practice_Attempts` khi retry.

## Chạy & kiểm tra

```bash
dotnet build
dotnet run
```

- Trang chủ: `http://localhost:5026` (giữ FE issue #8)
- Health: `GET /health` → `database: connected`, `users`, `courses` count

## SQL (chạy trước)

```bash
sqlcmd -S localhost -d master -E -i Designs/Database/DatabaseCreation.sql
sqlcmd -S localhost -d EnglishShadowingDB -E -i Designs/Database/Schema_v0p1_extension.sql
sqlcmd -S localhost -d EnglishShadowingDB -E -i Database/production_learning_schema_update.sql
sqlcmd -S localhost -d EnglishShadowingDB -E -i Database/project_completion_schema_update.sql
sqlcmd -S localhost -d EnglishShadowingDB -E -i Database/admin_schema_update.sql
sqlcmd -S localhost -d EnglishShadowingDB -E -i Designs/Database/Seed_video_bank_sources.sql
```

### Admin panel

Sau khi chạy `Database/admin_schema_update.sql`, promote 1 tài khoản:

```sql
UPDATE dbo.Users
SET role = 'admin', updated_at = SYSUTCDATETIME()
WHERE email = N'your-admin@example.com';
```

Đăng nhập bằng tài khoản đó → link **Admin** trên navbar → `/Admin/Users`.
Admin có thể xem/tìm user, ẩn đăng nhập (`is_active=0`), grant/revoke VIP, và xem usage theo practice tab 30 ngày.

## SQL Server integration tests

The test project uses SQL Server, not SQLite. Each test factory derives server and credentials from `WEBSHADOWING_TEST_SQLSERVER`, replaces its database name with an isolated `EnglishShadowingDB_Test_<guid>` name, and deletes that test database during cleanup.

```powershell
$env:WEBSHADOWING_TEST_SQLSERVER="Server=localhost;Database=ignored;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True"
dotnet test ..\WebShadowing.AuthFlowTests\WebShadowing.AuthFlowTests.csproj
```

The configured login needs permission to create and drop databases. Never point the test variable at credentials that should not have that permission; the supplied database name is intentionally ignored as a safety measure.

## Real video-bank import with yt-dlp

Do not run yt-dlp inside a student-facing web request. Use the internal importer
in `tools/video-import` to fetch real metadata/captions, create
`transcript.json`, then map those files in `Designs/Database/Seed_video_bank_sources.sql`.

```powershell
python -m pip install -U yt-dlp
python tools/video-import/import_video.py "https://www.youtube.com/watch?v=VIDEO_ID" `
  --slug "my-real-lesson" `
  --lesson-id 123
```

Với danh sách video seed sẵn, chạy batch:

```powershell
python tools/video-import/import_batch.py --continue-on-error --only-missing
```

`Designs/Database/Seed_video_bank_sources.sql` đã gắn sẵn các transcript
JSON đã import vào `Lesson_Material`, nên không cần chạy thêm SQL gộp từ
`tools/video-import`.

The app keeps the original video URL and plays it through the existing
YouTube/IFrame flow. Keep `source_review_status = 'pending'` until the
source/license has been reviewed.
