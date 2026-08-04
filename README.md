# WebShadowing

WebShadowing là ứng dụng web học tiếng Anh theo phương pháp shadowing. Ứng dụng hiện là một ASP.NET Core MVC modular monolith: trình duyệt ghi âm thành tệp, gửi request HTTP lên backend, backend gọi dịch vụ đánh giá phát âm và trả kết quả JSON. Repository hiện không dùng WebSocket, SignalR, Gemini Live hoặc microservices.

## Công nghệ

- .NET 10 / ASP.NET Core MVC
- Entity Framework Core 10 và SQL Server
- Razor Views, JavaScript và CSS
- Cookie authentication
- Azure Speech Pronunciation Assessment; có thể cấu hình OpenAI làm fallback
- xUnit cho unit test và integration test

## Chức năng hiện có

- Thư viện khóa học, bài học, transcript và audio/video
- Shadowing, dictation và IPA match
- Từ vựng, câu yêu thích và theo dõi từ phát âm sai
- Gamification: EXP, tim, streak và thống kê
- AI lesson, TTS và AI dialogue
- Hồ sơ, cài đặt và subscription/payment schema

Checkout kích hoạt VIP trực tiếp chỉ là luồng demo. Endpoint này tự động bị vô hiệu hóa ngoài môi trường `Development` và `Testing`; chưa được xem là payment production.

## Chạy local

Yêu cầu .NET SDK 10 và SQL Server.

```powershell
Copy-Item WebShadowing/appsettings.Development.json.example WebShadowing/appsettings.Development.json
dotnet restore WebShadowing/WebShadowing.slnx
dotnet run --project WebShadowing/WebShadowing.csproj
```

Cấu hình connection string trong `WebShadowing/appsettings.Development.json`, User Secrets hoặc biến môi trường. Không commit API key hay secret.

Ứng dụng mặc định đọc cấu hình:

- `ConnectionStrings:DefaultConnection`
- `AzureSpeech:ApiKey` và `AzureSpeech:Region`
- `PronunciationAssessment:EnableOpenAiFallback`
- `OpenAI:ApiKey`
- các section `Gamification`, `Vocabulary`, `AiLesson`, `AiDialogue`, `Storage` và `Payment`

## Database

Schema hiện được quản lý bằng SQL scripts, chưa dùng EF migrations. Khởi tạo database theo thứ tự phù hợp với môi trường:

1. `Designs/Database/DatabaseCreation.sql`
2. `Designs/Database/Schema_v0p1_extension.sql`
3. `WebShadowing/Database/production_learning_schema_update.sql`
4. `WebShadowing/Database/project_completion_schema_update.sql`

Luôn chạy integration test trên database test riêng, không dùng database phát triển hoặc production.

## Kiểm thử

```powershell
dotnet build WebShadowing/WebShadowing.slnx
dotnet test WebShadowing.UnitTests/WebShadowing.UnitTests.csproj
dotnet test WebShadowing.AuthFlowTests/WebShadowing.AuthFlowTests.csproj
dotnet test WebShadowing.DatabaseIntegrationTests/WebShadowing.DatabaseIntegrationTests.csproj
```

Database integration tests có thể cần connection string riêng và chỉ nên chạy khi đã chuẩn bị SQL Server test.

## Endpoint vận hành

`GET /health` chỉ trả trạng thái kết nối tối thiểu và timestamp. Endpoint không công khai số lượng user/course hoặc dữ liệu nghiệp vụ.

## Giới hạn hiện tại

- Generated media, Data Protection keys và cache vẫn dùng tài nguyên local của một instance.
- Payment production, admin/content moderation, CI/CD, object storage, distributed cache và observability đầy đủ chưa nằm trong snapshot hiện tại.
- Nội dung transcript phải được ingest thành `LessonSentences` trước khi publish nếu cần thực hiện và lưu bài luyện; runtime read-path không tự ghi database.
