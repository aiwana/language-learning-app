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
```
