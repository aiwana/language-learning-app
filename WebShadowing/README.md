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
