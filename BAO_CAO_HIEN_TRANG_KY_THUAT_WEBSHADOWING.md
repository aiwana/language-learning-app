# BÁO CÁO HIỆN TRẠNG KỸ THUẬT VÀ LUỒNG CHẠY DỰ ÁN WEBSHADOWING

> Tên sản phẩm đang được sử dụng trong giao diện/tài liệu: **ShadowSpeak AI / WebShadowing**
> Thời điểm khảo sát mã nguồn: **28/07/2026**
> Phạm vi: mã nguồn hiện có trong repository, bao gồm cả thay đổi đã stage, chưa stage và file chưa được Git theo dõi.

---

## 1. Mục đích và nguyên tắc của báo cáo

Báo cáo này mô tả **hệ thống đang thực sự được cài đặt trong mã nguồn**, không chỉ lặp lại ý tưởng trong README. Mọi nhận định được phân thành ba mức:

- **Đã có và đang được gọi trong code**: đã có controller/service/view hoặc luồng JavaScript tương ứng.
- **Có code nhưng chưa sẵn sàng production**: ví dụ payment thật có service nhưng controller hiện đi đường demo; nhiều nguồn video còn `pending`.
- **Chưa có bằng chứng triển khai**: ví dụ microservice độc lập, WebSocket/SignalR, CI/CD workflow, Docker, cloud deployment.

Điểm đặc biệt cần lưu ý là worktree tại thời điểm khảo sát có rất nhiều thay đổi chưa commit. Vì vậy báo cáo này phản ánh **ảnh chụp hiện tại trên máy**, có thể mới hơn `origin/main`. Khi nhóm merge hoặc bỏ bớt các thay đổi, báo cáo phải được cập nhật lại.

---

## 2. Tóm tắt điều hành

WebShadowing hiện là một ứng dụng web học tiếng Anh theo phương pháp shadowing, được xây dựng dưới dạng **modular monolith ASP.NET Core MVC trên .NET 10**. Ứng dụng dùng:

- ASP.NET Core MVC và Razor View cho giao diện server-rendered.
- JavaScript thuần ở trình duyệt cho phát media, thu microphone, gọi REST API và cập nhật UI.
- Cookie Authentication cho đăng nhập.
- Entity Framework Core 10 và Microsoft SQL Server cho dữ liệu.
- OpenAI cho sinh bài học, TTS, hội thoại, phiên âm/từ điển và một phương án fallback chấm phát âm.
- Azure Speech Pronunciation Assessment là provider chấm phát âm ưu tiên.
- `yt-dlp` là công cụ nội bộ chạy ngoài request web để lấy metadata và phụ đề thủ công từ video.
- YouTube IFrame API để phát đúng đoạn video theo timestamp.
- File audio giáo trình lưu trong `wwwroot/media/curriculum`.
- File TTS do AI sinh lưu cục bộ trong `wwwroot/media/generated`.
- xUnit cho unit test và integration test.

Kiến trúc hiện tại **không phải microservices**, **không có WebSocket/SignalR**, và **không dùng Gemini Live** dù README ở thư mục gốc đang mô tả như vậy. Luồng chấm phát âm hiện là request/response: trình duyệt ghi âm, chuyển sang WAV nếu có thể, upload toàn bộ file bằng `multipart/form-data`, backend gọi Azure Speech hoặc OpenAI, sau đó trả JSON.

Sản phẩm đã vượt qua mức demo UI đơn giản: auth, onboarding, course/lesson, transcript, shadowing, dictation, IPA match, AI dialogue, AI lesson, vocabulary, favorites, gamification, settings và schema subscription/payment đều đã có mã. Tuy nhiên mức hoàn thiện không đồng đều. Các rủi ro lớn nhất hiện nay là:

1. Controller checkout đang kích hoạt VIP demo ngay lập tức, bỏ qua `PaymentService.CreateCheckoutAsync`.
2. Không có pipeline CI/CD, Dockerfile hay hạ tầng cloud trong repository.
3. Schema được cập nhật bằng SQL thủ công, không dùng EF Migration; dễ lệch giữa môi trường.
4. README gốc sai khác đáng kể so với code hiện tại.
5. Chưa có role/admin authorization; nguồn nội dung chưa có quy trình duyệt hoàn chỉnh.
6. Health endpoint công khai trả cả số lượng user và course.
7. Nhiều API JSON thay đổi trạng thái chưa thể hiện cơ chế CSRF thống nhất.
8. Test hiện tập trung vào pronunciation, schema và gamification; nhiều module mới chưa có test.

---

## 3. Product vision, nhu cầu và phạm vi sản phẩm hiện thể hiện qua code

### 3.1. Product vision có thể suy ra

> Cung cấp một nền tảng luyện nghe–nói tiếng Anh cho người Việt bằng kỹ thuật shadowing, cho phép học theo nội dung video hoặc giáo trình, thu âm và nhận phản hồi phát âm, cá nhân hóa theo mục đích học, đồng thời duy trì động lực bằng tiến độ và gamification.

Đây là product vision **suy ra từ chức năng**, chưa thấy một tài liệu Product Vision chính thức, ngắn gọn và được version-control theo mẫu sản phẩm.

### 3.2. Nhóm người dùng hiện được code hỗ trợ

Code chưa định nghĩa persona dưới dạng tài liệu, nhưng onboarding cho thấy ba nhóm nhu cầu:

- `casual`: học giao tiếp đời thường.
- `academic`: học cho bài giảng, thuyết trình, nghiên cứu.
- `professional`: học cho phỏng vấn, họp và môi trường công việc.

Người dùng còn chọn:

- Accent: Anh–Mỹ (`en-us`) hoặc Anh–Anh (`en-gb`).
- Mục tiêu điểm phát âm: 50, 70 hoặc 90.
- Gói Free hoặc VIP; tuy nhiên quyền VIP ở onboarding chỉ được bật khi development stub được cho phép.

### 3.3. Phạm vi MVP đang hiện diện

Phần lõi hợp lý để gọi là MVP:

1. Đăng ký, đăng nhập, đăng xuất.
2. Onboarding chọn chế độ, accent, mục tiêu.
3. Xem thư viện khóa học và bài học đúng learning mode.
4. Phát video YouTube hoặc audio giáo trình.
5. Đồng bộ câu transcript với timestamp.
6. Ghi âm shadowing và chấm phát âm.
7. Lưu attempt, tiến độ, EXP, heart, streak.
8. Dictation và IPA match.
9. Thống kê/cài đặt cơ bản.

Các phần đang mở rộng hơn MVP:

- Sinh bài học AI kèm TTS.
- AI dialogue bằng text hoặc audio.
- Vocabulary và tự đưa từ sai nhiều lần vào sổ từ.
- Favorite sentence.
- Đổi learning mode bằng lượt miễn phí/EXP.
- Subscription và payment.

### 3.4. Những nội dung sản phẩm chưa nên tuyên bố hoàn thiện

- Thanh toán production.
- Quản trị nội dung và phân quyền admin.
- Quy trình kiểm duyệt bản quyền/phụ đề.
- Realtime streaming.
- Microservices.
- Cloud-native deployment và autoscaling.
- Theo dõi vận hành đầy đủ bằng metrics/tracing.

---

## 4. Công nghệ và thư viện thực tế

| Lớp | Công nghệ hiện dùng | Vai trò |
|---|---|---|
| Runtime/backend | .NET 10, ASP.NET Core | Host web, DI, middleware, MVC, REST API |
| UI server-side | Razor Views | Render trang và truyền model ban đầu |
| Frontend | HTML, CSS, Bootstrap assets, JavaScript thuần | UI tương tác, media, microphone, fetch API |
| Authentication | ASP.NET Core Cookie Authentication | Session đăng nhập 30 ngày |
| ORM | Entity Framework Core 10 SQL Server provider | Query, mapping entity, transaction, concurrency |
| Database | Microsoft SQL Server | Dữ liệu người dùng, bài học, attempt, tiến độ, payment |
| AI text/TTS/STT | OpenAI HTTP API | Sinh JSON/text, speech, transcription |
| Pronunciation | Azure Speech REST API; OpenAI fallback tùy cấu hình | Chấm phát âm |
| Video | YouTube IFrame API | Phát video theo đoạn |
| Transcript import | Python + `yt-dlp` | Lấy metadata và manual captions ngoài runtime |
| Cache | ASP.NET Core MemoryCache | Cache nội bộ một process |
| Rate limiting | ASP.NET Core Rate Limiter | Hạn chế endpoint AI theo user/IP |
| Compression | Brotli/Gzip response compression | Giảm kích thước response |
| Testing | xUnit, MVC Testing, EF InMemory, SQL Server integration | Unit/integration/API tests |
| Logging | Console, Debug, `ILogger` | Log backend cơ bản |

`WebShadowing.csproj` chỉ khai báo trực tiếp hai package EF Core (`SqlServer`, `Design`). Phần lớn framework feature đến từ shared framework `Microsoft.AspNetCore.App`.

---

## 5. Kiến trúc tổng thể đang chạy

### 5.1. Kiểu kiến trúc

Hệ thống là **monolith có phân lớp và chia module logic**:

```text
Browser
  ├─ Razor HTML/CSS
  ├─ JavaScript + Fetch
  ├─ MediaRecorder / Web Audio
  └─ YouTube IFrame API
          │ HTTP/HTTPS
          ▼
ASP.NET Core application (một process)
  ├─ Middleware pipeline
  ├─ MVC Controllers / API Controllers
  ├─ Services / business rules
  ├─ EF Core DbContext
  ├─ BackgroundService hết hạn subscription
  └─ Local static/generated media
          │
          ├─ SQL Server
          ├─ Azure Speech API
          ├─ OpenAI API
          ├─ MoMo/ZaloPay API (code có, checkout thật chưa được dùng)
          └─ YouTube ở phía browser
```

### 5.2. Vì sao chưa phải microservices

Các module auth, course, practice, gamification, AI, payment:

- Cùng compile vào một project.
- Chạy trong cùng process.
- Dùng chung `AppDbContext` và một SQL database.
- Không có service discovery, message broker, API gateway, distributed tracing hoặc deployment độc lập.

Điểm tốt là code có interface và service boundaries (`IAuthService`, `ICourseService`, `IPaymentService`...), nên có thể tách sau. Nhưng với quy mô hiện tại, modular monolith đơn giản hơn, rẻ hơn và phù hợp MVP hơn microservices. Tách sớm sẽ làm tăng độ phức tạp transaction, deployment và quan sát hệ thống.

### 5.3. Ràng buộc công nghệ

- Phải có .NET 10 SDK/runtime, không phải .NET 8 như README cũ.
- Phải có SQL Server và schema được chạy bằng SQL script.
- Những tính năng AI cần `OPENAI_API_KEY`.
- Chấm phát âm ưu tiên cần Azure Speech key và region.
- Trình duyệt cần quyền microphone và hỗ trợ `MediaRecorder`/Web Audio.
- YouTube lesson cần truy cập được YouTube IFrame API.
- Local storage hiện buộc file sinh ra nằm dưới web root.
- Memory cache và local generated media làm cho scale-out nhiều instance chưa an toàn nếu không có shared storage/cache.

---

## 6. Luồng khởi động ứng dụng

Entry point là `WebShadowing/Program.cs`.

### 6.1. Đọc cấu hình

ASP.NET Core đọc cấu hình mặc định từ:

- `appsettings.json`.
- `appsettings.{Environment}.json`.
- Environment variables.
- Các nguồn mặc định khác của host.

Ở Development, extension `AddDevelopmentEnvironmentFile()` còn tìm `.env` ở:

1. Current working directory.
2. Content root của project.
3. Thư mục cha của content root.

Giá trị trong `.env` chỉ được thêm nếu environment variable cùng tên chưa tồn tại. Cách này tiện cho local, nhưng parser đơn giản, không hỗ trợ đầy đủ mọi cú pháp `.env`.

### 6.2. Đăng ký dependency

DI container đăng ký:

- `AppDbContext` dạng scoped, dùng SQL Server.
- Các domain service dạng scoped.
- `OpenAiLanguageReferenceService` dạng singleton.
- `HttpClientFactory`.
- Memory cache giới hạn size 2.000 đơn vị.
- Options có validation khi startup cho gamification, vocabulary, mode change, AI lesson/dialogue, storage và payment.
- `SubscriptionExpiryService` dạng hosted service.
- `TimeProvider.System`.

Ưu điểm: controller phụ thuộc interface, dễ test và thay provider. Nhược điểm: toàn bộ module cùng nằm một composition root khá dài; nên tách thành extension như `AddPracticeModule`, `AddAiModule`.

### 6.3. Authentication và Data Protection

Cookie auth:

- Login path: `/Home/Authen`.
- Logout path: `/Account/Logout`.
- Access denied path: `/Home/Authen`.
- Sliding expiration.
- Hết hạn sau 30 ngày.
- API không bị redirect HTML khi chưa login; thay vào đó trả HTTP 401.

Data Protection key được ghi vào `App_Data/DataProtectionKeys` để cookie còn giải mã được sau restart trên cùng filesystem. Trong multi-instance cloud, thư mục local này không đủ; cần shared key ring hoặc managed store.

### 6.4. Middleware pipeline theo thứ tự

1. Response compression.
2. Middleware tự thêm header:
   - `X-Content-Type-Options: nosniff`
   - `Referrer-Policy: strict-origin-when-cross-origin`
   - `X-Frame-Options: SAMEORIGIN`
3. Ngoài Development:
   - Exception handler `/Home/Error`
   - HSTS
   - HTTPS redirection
4. Routing.
5. Status code page đổi 404 web page thành redirect `/Home/NotFoundPage`; API giữ JSON/status.
6. Authentication.
7. `OnboardingGuardMiddleware`.
8. Rate limiter.
9. Authorization.
10. Static assets, attribute routes và conventional MVC route.

Thứ tự auth trước onboarding là đúng vì guard cần đọc `ClaimsPrincipal`. Rate limiter đứng trước authorization nghĩa là request đã xác thực cookie nhưng chưa qua policy authorization vẫn có thể được tính quota; không nghiêm trọng nhưng cần chủ ý.

---

## 7. Giao thức HTTP, MVC và Request–Response

### 7.1. Hai kiểu endpoint

Ứng dụng dùng song song:

- **MVC page controller**: `HomeController`, `AccountController` trả Razor View hoặc redirect.
- **REST-like API controller**: route dưới `/api/...`, trả JSON và status code.

Ví dụ:

- `GET /Home/Index` → render trang chủ.
- `GET /Home/LessonDetail/{id}` → render studio học.
- `POST /Account/Login` → form POST + anti-forgery + redirect.
- `GET /api/courses` → JSON danh sách.
- `POST /api/practice/evaluate-shadowing` → multipart audio → JSON kết quả.
- `POST /api/practice/evaluate-answer` → JSON answer → JSON kết quả.

### 7.2. HTTP method

- GET: đọc course, lesson, library, vocabulary, profile, balance.
- POST: login/register, chấm bài, sinh AI, đổi trạng thái nghiệp vụ, checkout.
- PUT: cập nhật profile/settings/onboarding.
- DELETE: xóa favorite, vocabulary, saved AI lesson.

Các route khá nhất quán, nhưng một số action đổi trạng thái dùng POST (`mastered`, `review`, `cancel`, `mode`) thay vì PATCH. Đây vẫn là lựa chọn chấp nhận được nếu API được tài liệu hóa.

### 7.3. Status code thực tế

- 200: thành công và có JSON.
- 204: webhook/favorite/delete thành công không cần body.
- 400: input/audio/idempotency sai.
- 401: chưa xác thực hoặc webhook signature sai.
- 403: chưa onboarding hoặc lesson sai mode.
- 404: không tìm thấy resource.
- 409: idempotency key xung đột hoặc thiếu IPA.
- 429: vượt rate limit.
- 503: AI/pronunciation/database không sẵn sàng.

Điểm cần cải thiện: response lỗi chưa dùng một chuẩn thống nhất như RFC 7807 `ProblemDetails`; hiện có `ApiErrorDto`, anonymous object, plain `Unauthorized()` và message khác nhau.

---

## 8. Luồng xác thực và phân quyền

### 8.1. Đăng ký

Luồng thực tế:

1. Người dùng mở `/Home/Authen`.
2. Form gửi `POST /Account/Register`.
3. MVC model validation chạy trước; action có `[ValidateAntiForgeryToken]`.
4. `AuthService.RegisterAsync` kiểm tra email, mật khẩu tối thiểu 8 ký tự, họ tên.
5. Email được trim và lowercase.
6. Kiểm tra email tồn tại.
7. Username được tạo từ họ tên; nếu trùng thì nối số tăng dần.
8. Tạo `User` với mode casual, target 70, accent en-us, chưa VIP, chưa onboarding.
9. Mật khẩu được hash bằng `PasswordHasher<User>` của ASP.NET Core Identity.
10. Tạo `UserStatistic` với 0 EXP, 0 streak và số heart tối đa từ config.
11. Lưu trong database transaction.
12. Tạo cookie đăng nhập persistent 30 ngày.
13. Redirect sang bước chọn level/onboarding.

Điểm tốt:

- Không lưu mật khẩu plaintext.
- Thông báo login thất bại không phân biệt email tồn tại hay mật khẩu sai.
- Có transaction khi tạo user và statistics.
- Redirect URL được kiểm tra bằng `Url.IsLocalUrl`, giảm open redirect.

Điểm yếu:

- Chưa có email verification.
- Chưa có forgot/reset password.
- Chưa có lockout/brute-force rate limit cho login.
- Chưa có MFA.
- Query `u.Email.ToLower()` có thể làm giảm khả năng dùng index; nên lưu normalized email riêng và unique index.
- Vòng lặp tạo username có thể race giữa hai request; database constraint bắt lỗi nhưng UX còn chung chung.

### 8.2. Đăng nhập

1. `POST /Account/Login`, có anti-forgery.
2. Tìm user theo normalized email.
3. `PasswordHasher.VerifyHashedPassword`.
4. Tạo claims:
   - NameIdentifier = user ID.
   - Name = full name.
   - Email.
   - custom `username`.
5. Phát cookie.
6. Nếu chưa onboarding thì chuyển đến bước onboarding; nếu hoàn tất thì về return URL nội bộ hoặc home.

### 8.3. Onboarding guard

Sau authentication, middleware đọc user ID từ claim rồi query profile:

- Nếu onboarding xong: request đi tiếp.
- Nếu API và chưa onboarding: trả 403 JSON kèm onboarding URL.
- Nếu web page và chưa onboarding: redirect sang `/Home/Authen?step=level`.
- Bỏ qua auth page, account routes, error/privacy, một số user onboarding API và static assets.

Trong code có bypass nếu header `X-Test-User` tồn tại. Dù mục tiêu là test, đây là một dấu hiệu nguy hiểm nếu header này làm thay đổi hành vi production. Guard chỉ bỏ qua onboarding chứ không tự tạo authentication, nhưng vẫn nên giới hạn bypass theo `IsEnvironment("Testing")`.

### 8.4. Phân quyền

Hiện tại chủ yếu có hai trạng thái:

- Anonymous.
- Authenticated (`[Authorize]`).

Chưa có:

- Role `Admin`, `ContentReviewer`, `Teacher`.
- Policy-based authorization.
- Claim/permission chi tiết.

Quyền truy cập dữ liệu cá nhân thường được bảo vệ bằng cách lấy user ID từ cookie và thêm `Where(item.UserId == userId)`. Đây là cách đúng để hạn chế IDOR, nhưng cần test cho từng endpoint.

---

## 9. Luồng thư viện, khóa học và bài học

### 9.1. Chọn learning mode

Mode hiện tại lấy từ database thông qua `UserContextService`, không tin trực tiếp query string. Service cache preferences trong lifetime của request.

`CourseService` lọc course/lesson theo:

- Learning mode của user.
- Loại course/lesson.
- Trạng thái dữ liệu.
- Mục tiêu phát âm khi dựng DTO.

Nếu cố mở lesson thuộc mode khác, service trả trạng thái forbidden và controller trả 403 hoặc view phù hợp.

### 9.2. Trang chủ

`HomeController.Index`:

1. Yêu cầu authenticated.
2. Lấy mode thật của user.
3. Lấy dữ liệu library/course.
4. Dựng view model cho ba section.
5. Razor render card bài học và navigation.

### 9.3. Lesson detail

1. Browser gọi `GET /Home/LessonDetail/{id}`.
2. Controller lấy mode/target/accent từ user context.
3. `CourseService.GetLessonAsync` load lesson, material và câu.
4. `LessonContentService` bổ sung câu từ transcript file nếu cần.
5. View `LessonDetail.cshtml` nhúng lesson data cho JavaScript.
6. JavaScript chọn YouTube hoặc audio player dựa trên media.
7. Transcript được render thành danh sách câu; người dùng chọn câu để nghe và luyện.

---

## 10. Luồng transcript và nhập video

### 10.1. Công cụ thật đang dùng

Transcript video được nhập bằng script Python:

- `tools/video-import/import_video.py`
- `tools/video-import/import_batch.py`
- công cụ ngoài `yt-dlp`

Script này **không chạy trong HTTP request**. Đây là lựa chọn đúng vì:

- `yt-dlp` nặng và thời gian chạy không ổn định.
- Website nguồn có thể thay đổi/chặn.
- Không nên để user tùy ý làm server tải nội dung.
- Cần bước review bản quyền và chất lượng.

### 10.2. Chính sách phụ đề

Mặc định importer gọi:

```text
--write-subs
```

và **không** gọi `--write-auto-subs`. Do đó mặc định chỉ nhận phụ đề do chủ kênh/người đăng cung cấp. Nếu không có manual caption:

- Transcript được ghi thành mảng rỗng.
- Import report báo `no_manual_caption_file`.
- Nội dung cần xử lý thủ công.

Chỉ khi truyền `--allow-auto-captions`, script mới cho phép phụ đề tự động. Những bản này phải giữ trạng thái `pending` hoặc `rejected` cho tới khi người kiểm duyệt sửa và duyệt.

Lý do sản phẩm:

- Auto caption thường sai tên riêng, dấu câu và timestamp.
- Shadowing phụ thuộc rất mạnh vào câu đúng và timeline đúng.
- Sửa phụ đề thủ công sau khi đưa vào database làm tăng chi phí và dễ lệch giữa text với video.

### 10.3. Dữ liệu importer tạo ra

Trong `wwwroot/media/video-bank/<slug>/`:

- `transcript.json`: danh sách câu.
- `source-metadata.json`: URL, provider, source ID, title, duration, channel, license.
- `import-report.json`: số cue và cảnh báo chất lượng.

Mỗi câu transcript có:

- `sentence_order`
- `start_time`
- `end_time`
- `text`
- `translation`
- `ipa`

Video không được download/rehost. Database giữ URL nguồn và frontend phát qua YouTube IFrame.

### 10.4. Xử lý VTT

Importer:

1. Dùng `yt-dlp -J --skip-download` lấy metadata.
2. Tải VTT của ngôn ngữ ưu tiên `en,en-US,en-GB`.
3. Parse timestamp.
4. Loại HTML tag, style marker và chuẩn hóa whitespace.
5. Gộp các cue liên tiếp có text trùng.
6. Đánh lại `sentence_order`.
7. Cảnh báo cue quá ngắn, rolling/duplicate prefix hoặc số cue quá cao.

### 10.5. Luồng đọc transcript trong runtime

`LessonContentService`:

1. Nhận danh sách `LessonMaterial`.
2. Chọn material loại transcript.
3. Resolve URL tương đối thành file dưới web root.
4. Chặn path thoát khỏi web root.
5. Deserialize JSON.
6. Chuẩn hóa và sắp xếp câu.
7. So với `Lesson_Sentences` trong database.
8. Nếu transcript có câu chưa có trong DB, service có thể persist câu để tạo ID thật.
9. Merge transcript timeline/text với dữ liệu DB.
10. Trả DTO cho lesson.

Ưu điểm: nội dung file dễ nhập nhưng attempt vẫn tham chiếu sentence ID thật. Nhược điểm: một read path có thể phát sinh write vào DB; điều này gây bất ngờ, có thể race và làm runtime phụ thuộc quyền ghi. Tốt hơn là material ingestion phải hoàn tất trước khi publish.

### 10.6. Trạng thái duyệt nguồn

Schema có:

- `source_provider`
- `source_id`
- `license_note`
- `source_review_status`: pending/approved/rejected
- thời điểm review

Nhưng chưa có admin UI, role reviewer hay workflow duyệt. Seed hiện yêu cầu giữ nhiều nguồn ở `pending`. Do đó không nên tuyên bố toàn bộ video đã được duyệt bản quyền.

---

## 11. Audio giáo trình và phát media

Khác với giả định “audio để version 2”, snapshot hiện tại đã có audio giáo trình từ grade 6 đến grade 9, dạng MP3/WAV, kèm `transcript.json`.

`LessonContentService.InferCurriculumAudioUrl`:

1. Tìm transcript URL thuộc `/media/curriculum/`.
2. Resolve thư mục thật dưới web root.
3. Tìm file `.mp3`, `.wav`, `.m4a`, `.ogg` hoặc `.webm`.
4. Chọn file đầu tiên theo tên.
5. Trả URL public.

Frontend:

- Nếu có `youtubeId`, dùng YouTube IFrame.
- Nếu chỉ có `audioUrl`, dùng HTML `<audio>`.
- Nếu câu có timestamp, seek tới start và pause tại end.
- Nếu audio không có timestamp, có thể phát toàn file, nhưng UI thông báo phần dictation theo từng câu vẫn đang phát triển.

Vì vậy trạng thái đúng là:

- **Phát và shadowing với audio đã có.**
- **Dictation theo đoạn audio chưa hoàn chỉnh cho lesson không có timestamp.**
- TTS AI cũng đã có, không phải toàn bộ audio bị hoãn.

---

## 12. Luồng Shadowing và chấm phát âm

### 12.1. Trình duyệt ghi âm

Trong `lesson-shadowing.js`:

1. Người dùng chọn câu.
2. Có thể phát đoạn mẫu theo timestamp.
3. Khi bấm ghi, frontend dừng media mẫu để tránh dính tiếng.
4. Tạo idempotency key cho attempt.
5. Xin quyền microphone bằng `navigator.mediaDevices.getUserMedia`.
6. Dùng `MediaRecorder` gom audio chunk.
7. Sau khi dừng, tạo Blob.
8. Cố decode qua Web Audio và encode mono PCM WAV.
9. Nếu chuyển đổi thất bại, giữ blob gốc để người dùng nghe lại, nhưng không gửi chấm nếu format không phù hợp.
10. Cho phép playback bản ghi.
11. Gửi `multipart/form-data` đến `/api/practice/evaluate-shadowing`.

Đây là upload file hoàn chỉnh theo request/response, không phải stream thời gian thực.

### 12.2. Request

Form gồm:

- `lessonId`
- `sentenceId`
- `sentenceIndex`
- `audio`

Header bắt buộc:

- `Idempotency-Key`

Giới hạn controller: 10 MB cộng multipart overhead. Endpoint bị rate limit 10 request/phút theo user; nếu chưa có user thì theo IP.

### 12.3. Validation backend

Backend kiểm tra:

- Có audio.
- Không vượt 10 MB.
- Idempotency key có và không dài quá 100.
- MIME/format được hỗ trợ.
- Header WAV hợp lệ.
- Thời lượng không vượt cấu hình.
- User đã xác thực.
- Lesson thuộc learning mode.
- Sentence thật sự thuộc lesson.

### 12.4. Idempotency

Trước khi gọi provider, service tìm `Practice_Attempts` theo `(UserId, IdempotencyKey)`.

- Nếu đã có và cùng source: trả lại kết quả đã lưu, không thưởng/phạt lần nữa.
- Nếu key đã dùng cho attempt khác: trả 409.
- Database có unique index bảo vệ ở lớp cuối.

Đây là áp dụng tốt của reliable programming vì browser/network có thể retry.

### 12.5. Provider chấm phát âm

`HybridPronunciationAssessmentService`:

1. Ưu tiên `AzurePronunciationAssessmentService`.
2. Nếu Azure lỗi:
   - Nếu `EnableOpenAiFallback=false`, trả lỗi.
   - Nếu bật fallback, gọi `OpenAiPronunciationAssessmentService`.
3. Provider call có timeout cấu hình.

Azure Speech nhận reference text, accent/language và audio. OpenAI là fallback tổng quát, không nên được xem có độ chính xác chuyên dụng ngang Azure.

Nếu không có key hoặc provider lỗi, endpoint trả 503. Code chủ ý **không sinh score giả**.

### 12.6. Tính score

`PronunciationScoreProfileService` tính overall score theo learning mode, sử dụng trọng số component nếu provider có dữ liệu; nếu không thì fallback provider overall. Sau đó:

```text
passed = score >= pronunciationTarget
```

Target là 50, 70 hoặc 90 từ profile.

### 12.7. Lưu attempt và cập nhật hệ thống

Kết quả verified được chuyển sang `GamificationService.ProcessVerifiedAttemptAsync`, nơi:

- Tạo `PracticeAttempt`.
- Lưu provider, provider reference, transcript, feedback, score, pass/fail.
- Cập nhật progress của sentence và lesson.
- Cộng EXP nếu lần đầu hoàn thành đạt.
- Trừ heart nếu thất bại và thuộc bài tiêu tốn heart.
- VIP được miễn heart penalty.
- Cập nhật streak theo business date Việt Nam.
- Ghi ledger với balance snapshot.
- Bảo vệ duplicate bằng source ID/unique index/transaction.

Response trả:

- score, passed, target.
- provider, transcript, feedback.
- word-level feedback.
- gamification transaction và balance.

### 12.8. Theo dõi từ phát âm sai

`WordErrorTracker` có logic:

- Chuẩn hóa từng từ.
- Tăng consecutive/total error.
- Reset consecutive khi phát âm đúng.
- Khi một từ sai liên tiếp đạt ngưỡng 3, thêm vào vocabulary và lấy IPA/meaning nếu thiếu.

Cần xác nhận bằng wiring/test rằng tracker luôn được gọi trong luồng production hiện tại; test có bao phủ hành vi streak của từ, nhưng thiết kế nên tránh gọi AI dictionary trong cùng transaction dài.

---

## 13. Dictation và IPA Match

Cả hai gọi `POST /api/practice/evaluate-answer` với JSON và idempotency key.

### Dictation

- Expected answer là `sentence.Text`.
- Backend normalize chữ hoa/thường, dấu câu và khoảng trắng.
- So sánh exact sau normalize.
- Đúng: 100, sai: 0.

### IPA Match

- Tách IPA của câu thành token.
- Client gửi `targetIndex`.
- Backend xác nhận index hợp lệ.
- Normalize IPA và so sánh exact.
- Thiếu IPA trả 409.

Hai bài này dùng `server-answer-validator`, không tốn AI. Ưu điểm là rẻ và deterministic. Nhược điểm là chấm nhị phân 0/100; chưa hỗ trợ gần đúng, typo distance hoặc phản hồi chi tiết.

---

## 14. Gamification

Config mặc định:

- Hoàn thành câu lần đầu: +20 EXP.
- Attempt fail: -1 heart.
- Đổi 1 heart: -100 EXP.
- Tối đa 5 heart.
- Timezone nghiệp vụ: Asia/Ho_Chi_Minh.

### Nguyên tắc

- Thưởng completion chỉ một lần cho lần pass đầu.
- Retry không nhân đôi thưởng/phạt nhờ idempotency.
- Heart không xuống dưới 0.
- VIP không mất heart.
- Streak chỉ tăng tối đa một lần mỗi business date.
- Nghỉ cách ngày làm reset logic streak.
- Exchange heart có idempotency key.
- Ledger lưu delta và snapshot balance sau giao dịch.

### Ưu điểm

- Business rule tách một phần vào `GamificationPolicy`.
- Có database constraint và integration test concurrency.
- Có ledger audit thay vì chỉ sửa tổng số.

### Điểm cần cải thiện

- Làm rõ behavior khi hết heart: hiện cần kiểm tra UI/service có khóa luyện hay vẫn cho attempt.
- Cơ chế hồi heart theo thời gian chưa thấy.
- Cần dashboard/audit cho ledger bất thường.
- Trạng thái statistics và ledger là dữ liệu dẫn xuất; cần reconciliation job.

---

## 15. Từ vựng và favorite

### Vocabulary

API cho phép:

- Liệt kê có paging và filter active/mastered.
- Xem chi tiết.
- Thêm từ.
- Đánh dấu mastered.
- Đưa về review.
- Xóa.
- Gửi audio chấm phát âm từ.

Nếu thiếu IPA/meaning, `VocabularyService` gọi language reference AI. Unique key theo `(UserId, NormalizedWord, LanguageCode)` tránh trùng từ.

### Favorite sentence

User có thể thêm/xóa/liệt kê câu yêu thích. Query luôn gắn user ID. Unique index `(UserId, SentenceId)` ngăn trùng.

Điểm cần cải thiện:

- Chuẩn hóa từ hiện chỉ giữ letter và dấu `-`; cần test Unicode/apostrophe.
- Chưa thấy SRS đúng nghĩa như SM-2; `review_count`, status và last reviewed chỉ là nền tảng.
- Các API phát sinh thay đổi từ fetch cần chiến lược CSRF thống nhất.

---

## 16. AI lesson generation và TTS

### Luồng sinh bài

1. User gửi topic và số câu đến `POST /api/ai-lessons/generate`.
2. Rate limit 5/phút.
3. Backend đọc mode/accent từ database.
4. Prompt yêu cầu JSON gồm title và segments: text, Vietnamese translation, IPA, speaker.
5. OpenAI Chat Completions được gọi với model mặc định `gpt-4o`, JSON mode.
6. Backend parse và kiểm tra tối thiểu 3 câu.
7. Với từng câu, gọi OpenAI Speech API model `tts-1-hd`.
8. Voice mặc định:
   - US: `nova`
   - GB: `fable`
9. MP3 được ghi dưới `wwwroot/media/generated/ai-lesson-<guid>/`.
10. Preview được lưu DB và hết hạn sau 30 phút.
11. Nếu setting auto-save bật, preview được lưu thành `User_Saved_Lessons`.
12. User có thể save/list/delete.

### Ưu điểm

- Prompt được điều chỉnh theo mode.
- JSON được validate thay vì đưa thẳng ra UI.
- Preview có expiration và ownership.
- TTS key ở server.
- Path scope được sanitize và kiểm tra nằm dưới web root.

### Nhược điểm/rủi ro

- Mỗi câu gọi một TTS request tuần tự, tăng latency và chi phí.
- Model names được hard-code trong config cũ, có thể hết vòng đời.
- Chưa thấy moderation/safety filter cho topic và output.
- Không có quota chi phí theo ngày/tháng, chỉ có rate limit.
- File preview hết hạn trong DB nhưng file audio sinh ra chưa thấy cleanup tương ứng.
- Local storage không phù hợp nhiều instance/container ephemeral.
- AI output tự được đánh dấu `approved`; cần quy ước rõ review cho nội dung giáo dục.

---

## 17. AI Dialogue

### Text

1. Tạo session qua `/api/ai-dialogue/sessions`.
2. Session gắn user, lesson tùy chọn, mode và timeout.
3. User gửi text.
4. Lịch sử turn được đưa vào OpenAI.
5. Assistant trả text.
6. Backend tạo TTS audio cho reply.
7. Lưu user/assistant turns.
8. Frontend render bubble và phát audio.

### Audio

1. Browser dùng `MediaRecorder` ghi `audio/webm`.
2. Gửi multipart đến `/sessions/{id}/audio`.
3. OpenAI transcription chuyển speech thành text.
4. Phần còn lại đi cùng pipeline text.

Rate limit 12/phút; session tối đa 30 turns, timeout mặc định 15 phút.

Đây vẫn là request/response từng message, không phải realtime conversation hay WebSocket.

---

## 18. Profile, settings và đổi mode

User có thể:

- Đổi full name, phone.
- Đổi pronunciation target, accent, theme.
- Bật auto-save AI lessons.
- Đổi learning mode.

Mode change được quản lý bằng:

- Một số lượt miễn phí mỗi tháng, mặc định 1.
- Sau đó tốn 200 EXP.
- VIP không giới hạn nếu config bật.
- Mỗi lần đổi có `Mode_Change_History`.

Điểm tốt: mode là business state trên server, không tin query string. Điểm cần cải thiện: tính “tháng” đang dựa UTC start of month, trong khi gamification có business timezone; cần thống nhất timezone nghiệp vụ.

---

## 19. Payment và subscription: phần thật và phần demo

### 19.1. Schema và service thật đã có

Code có:

- `VIP_Subscriptions`
- `Payment_Transactions`
- MoMo create order + HMAC signature.
- ZaloPay create order + MAC.
- Webhook verification dùng constant-time comparison.
- Kiểm tra amount/order.
- Idempotency.
- Activate subscription sau webhook.
- Hosted service hết hạn subscription.

### 19.2. Nhưng checkout hiện tại là demo

`PaymentController.Checkout` **không gọi** `PaymentService.CreateCheckoutAsync`. Thay vào đó:

1. Kiểm tra provider/period.
2. Nếu idempotency key đã có, trả thành công.
3. Tạo subscription `active` với provider `demo`.
4. Tạo transaction `succeeded`.
5. Set `user.IsVip = true`.
6. Lưu DB và trả “VIP đã được kích hoạt”.

Như vậy người dùng không chuyển sang cổng thanh toán thật và không cần webhook để nhận VIP.

Đây là điểm phải nói rõ khi demo:

> Module tích hợp MoMo/ZaloPay đã có ở service và webhook, nhưng luồng checkout đang cố ý dùng demo activation cho milestone hiện tại. Chưa được coi là thanh toán production.

### 19.3. Vấn đề cần sửa trước production

- Controller phải gọi `CreateCheckoutAsync`.
- Chỉ webhook có signature hợp lệ mới activate VIP.
- Idempotency query trong demo checkout hiện tìm chỉ bằng key, chưa scoped provider/user; cần thống nhất với unique constraint.
- Validate idempotency key rỗng/độ dài.
- Bảo vệ replay webhook và state transition.
- Có refund/cancel/renewal thật.
- Secret phải ở secret manager.
- Audit log và reconciliation với provider.
- Test webhook bằng fixture chính thức.

---

## 20. ORM và thiết kế dữ liệu

### 20.1. EF Core

`AppDbContext` có các nhóm bảng:

- Identity/profile: Users, User_Settings, Mode_Change_History.
- Learning catalog: Courses, Lessons, Lesson_Material, Lesson_Sentences.
- Enrollment/progress: Users_Courses, User_Lesson_Progress, User_Sentence_Progress.
- Practice legacy/core: Practice_Sessions, User_Recordings, Transcripts, AI_Feedback, Practice_Attempts.
- Gamification: User_Statistics, Gamification_Ledger.
- Personal learning: Word_Error_Statistics, Vocabulary_Items, Favorite_Sentences.
- AI content: User_Saved_Lessons, Saved_AI_Lesson_Segments, AI_Lesson_Previews, AI_Dialogue_Sessions, AI_Dialogue_Turns.
- Commercial: VIP_Subscriptions, Payment_Transactions.

### 20.2. Điểm thiết kế tốt

- Unique index cho idempotency và natural uniqueness.
- Check constraint cho enum-like string, score, timestamp và amount.
- RowVersion cho optimistic concurrency ở nhiều bảng mutable.
- Cascade delete được chọn lọc; nhiều quan hệ quan trọng dùng `NoAction` để tránh cascade path nguy hiểm.
- Query đọc thường có `AsNoTracking`.
- DTO projection giảm tải entity.
- Có transaction ở nghiệp vụ cần atomicity.

### 20.3. Schema management

README nói rõ **không dùng EF Migration**. Nhóm chạy các file:

- `Designs/Database/DatabaseCreation.sql`
- các schema extension.
- `WebShadowing/Database/production_learning_schema_update.sql`
- `project_completion_schema_update.sql`
- seed scripts.

Ưu điểm:

- Kiểm soát SQL cụ thể.
- Có thể viết migration idempotent và kiểm thử constraint SQL Server thật.

Nhược điểm:

- Dễ quên thứ tự script.
- Khó biết môi trường đang ở version nào.
- Deploy cần quyền và runbook thủ công.
- Mapping EF và SQL script dễ lệch.
- Rollback không rõ.

Khuyến nghị: hoặc dùng EF Migration chuẩn, hoặc giữ SQL migration nhưng thêm bảng `SchemaVersions`, một migration runner và pipeline tự kiểm tra checksum/order.

---

## 21. Bảo mật và riêng tư

### 21.1. Những gì đã làm tốt

- Password hashing chuẩn framework.
- Cookie auth, server-side key.
- API chưa login trả 401 thay vì redirect HTML.
- Anti-forgery trên form auth/onboarding MVC.
- `Url.IsLocalUrl` chống open redirect.
- HTTPS redirect/HSTS ở non-development.
- Security headers cơ bản.
- Rate limiting cho AI.
- Request size limit audio.
- Validate MIME, WAV và duration.
- Idempotency chống double processing.
- Payment webhook dùng HMAC và fixed-time equals.
- Secrets config rỗng trong tracked appsettings.
- Path traversal check cho transcript và generated media.
- Không tạo score giả khi AI lỗi.

### 21.2. Thiếu sót quan trọng

1. **CSRF cho JSON API thay đổi trạng thái**
   Cookie tự được browser gửi. Nhiều fetch POST/PUT/DELETE chưa thấy anti-forgery token/header đồng bộ. `SameSite` mặc định có giúp giảm rủi ro nhưng không thay thế một chiến lược rõ ràng.

2. **Chưa có Content-Security-Policy**
   Ứng dụng nhúng YouTube và script, cần CSP được thiết kế cẩn thận.

3. **Chưa có role/admin**
   Không thể mở importer/admin review an toàn.

4. **Login chưa rate limit/lockout**.

5. **PII và voice data**
   Email, phone, recording/transcript và lỗi phát âm là dữ liệu cá nhân. Chưa thấy retention policy, consent cụ thể, delete/export account workflow, encryption policy hay data processing disclosure đầy đủ.

6. **AI privacy**
   Audio/text được gửi đến Azure/OpenAI; cần thông báo người dùng, data-flow inventory và điều khoản.

7. **Logging response body từ AI lỗi**
   `OpenAiApiClient` log tối đa 500 ký tự body. Cần chắc không log prompt/PII hoặc secret.

8. **Health endpoint lộ count**
   `/health` công khai trả số user/course. Liveness chỉ nên trả trạng thái tối thiểu; readiness chi tiết nên giới hạn nội bộ.

9. **Test bypass header**
   `X-Test-User` không nên tác động production middleware.

10. **Checkout demo**
    Không được bật ở production.

### 21.3. Privacy cần bổ sung

- Consent trước microphone và gửi AI.
- Mục đích xử lý, nhà cung cấp, vùng dữ liệu.
- Retention cho raw audio/generated audio/transcript.
- Download/delete user data.
- Xóa file vật lý khi xóa record.
- Không thu voice quá mức cần thiết.
- Phân loại dữ liệu và access log.

---

## 22. Reliable Programming

Các kỹ thuật đã áp dụng:

- Nullable reference types.
- async/await và cancellation token xuyên nhiều tầng.
- Input validation cả controller và service.
- Database constraint.
- Transaction.
- Idempotency.
- Optimistic concurrency với rowversion.
- Provider timeout/fallback.
- Không giả lập kết quả AI khi provider hỏng.
- Error mapping sang status code.
- Source ownership filter trong query.
- Timezone business rõ cho streak.
- Hosted job hết hạn subscription.
- Health check database.

Điểm cần cải thiện:

- Typed `HttpClient` với timeout/retry/circuit breaker có chọn lọc.
- Không retry POST AI/payment một cách mù; dùng idempotency.
- Outbox pattern nếu sau này có event/email.
- Cleanup job cho preview/generated media/session.
- Reconciliation statistics/ledger/subscription.
- Transaction boundary rõ khi service gọi external API.
- Standard exception middleware + ProblemDetails.
- Tránh catch `Exception` quá rộng nếu cần phân biệt cancellation.
- Structured event IDs và correlation ID.
- Clock abstraction dùng nhất quán; hiện nhiều nơi gọi `DateTime.UtcNow` trực tiếp dù đã đăng ký `TimeProvider`.

---

## 23. Testing hiện tại

### 23.1. Unit/API tests có trong repository

`WebShadowing.UnitTests` dùng xUnit, EF InMemory và MVC Testing. Test hiện thấy:

- Trọng số score theo mode.
- Fallback overall score.
- Rate limit pronunciation.
- Thiếu idempotency key.
- Idempotency tránh duplicate attempt.
- Word error streak.
- Reject audio MIME sai.
- Reject audio quá thời lượng.
- Cho phép audio đúng giới hạn.
- Transcript-only sentence được persist và có ID thật.
- Ghép sentence theo order khi punctuation khác.
- Hybrid provider không fallback khi flag tắt.
- Hybrid provider fallback khi flag bật.

### 23.2. Integration tests

`WebShadowing.AuthFlowTests`:

- Gamification policy.
- Reward/penalty/VIP/retry/exchange/concurrency consistency.

`WebShadowing.DatabaseIntegrationTests`:

- Upgrade populated legacy schema.
- Script idempotent.
- SQL Server enforcement cho uniqueness/check/rowversion concurrency.

Database test tạo database riêng `EnglishShadowingDB_Test_<guid>` và xóa khi kết thúc. Đây là cách tốt hơn dùng SQLite để test hành vi SQL Server.

### 23.3. Trạng thái build/test tại thời điểm khảo sát

- Riêng web project đã compile ra `WebShadowing.dll` trên .NET 10.
- Build toàn solution với `--no-restore` thất bại vì hai test project chưa có `obj/project.assets.json`.
- Đây là lỗi thiếu NuGet restore, không phải bằng chứng test fail.
- Chưa chạy full test vì dependency của test project chưa được restore trong snapshot kiểm tra.

Không nên ghi “tất cả test pass” cho đến khi chạy:

```powershell
dotnet restore WebShadowing/WebShadowing.slnx
dotnet test WebShadowing/WebShadowing.slnx
```

và chạy riêng SQL integration test với `WEBSHADOWING_TEST_SQLSERVER`.

### 23.4. Khoảng trống test

- Auth register/login/logout/onboarding end-to-end.
- Authorization/IDOR từng endpoint.
- CSRF.
- Course/library filtering.
- AI lesson parse, quota, cleanup.
- AI dialogue ownership/timeout/max turn.
- Vocabulary/favorite/profile/mode.
- Payment demo và payment thật/webhook/replay.
- Subscription expiry.
- UI browser tests cho microphone/media.
- Accessibility.
- Load/performance.
- Security/static analysis.

---

## 24. DevOps và quản lý mã nguồn

### 24.1. Git evidence

Repository có:

- Remote GitHub `aiwana/language-learning-app`.
- Nhiều feature branch theo issue, ví dụ auth, course API, frontend course, shadowing studio, schema.
- Merge commits từ Pull Request có ghi `resolves #...` hoặc `closes #...`.
- Lịch sử thể hiện ít nhất bốn contributor.
- Commit tách theo BE/FE/DB/feature tương đối rõ.

Điều này là bằng chứng nhóm đã áp dụng:

- Feature branching.
- Issue-driven development.
- Pull Request merge.
- Liên kết commit/PR với issue.
- Chia module giữa frontend/backend/database.

### 24.2. Những gì không có bằng chứng local

Không có thư mục `.github`, nên snapshot không có:

- GitHub Actions.
- Issue templates.
- Pull request template.
- CODEOWNERS.
- Dependabot.
- Security workflow.

Git history không đủ để kết luận milestone/project board/backlog/acceptance criteria được tổ chức tốt. Muốn chứng minh phần Agile này, nhóm cần chụp/export từ GitHub hoặc đưa tài liệu tương ứng vào repo.

### 24.3. Worktree risk

Tại thời điểm khảo sát:

- Có rất nhiều file staged, modified, deleted và untracked.
- Nhánh hiện tại trùng commit với `origin/main` nhưng chứa khối lượng thay đổi local lớn.
- Một số file có cả staged và unstaged changes (`MM`, `AM`).

Rủi ro:

- Khó review đúng một feature.
- Dễ commit lẫn.
- Build của người khác không tái hiện snapshot.
- Báo cáo có thể mô tả code chưa được merge.

Khuyến nghị:

- Tách PR nhỏ theo feature.
- Không gom payment, AI, vocabulary, gamification, UI vào một PR.
- Dùng `git diff --cached --check`.
- Restore/test trước commit.
- Mỗi PR ghi acceptance criteria và evidence.

### 24.4. CI/CD còn thiếu

Mức cơ bản nên có:

1. Restore.
2. Build Release với warning policy.
3. Unit test.
4. Integration test theo môi trường phù hợp.
5. Format/analyzer.
6. Secret scan.
7. Dependency vulnerability scan.
8. Publish artifact/container.
9. Deploy staging.
10. Smoke test `/health`.
11. Manual approval production.

---

## 25. Cloud-based software và triển khai

### 25.1. Những yếu tố đã có lợi cho cloud

- Cấu hình qua environment variable.
- Stateless auth cookie về mặt app session, nếu chia sẻ Data Protection key.
- SQL Server external.
- Health endpoint.
- Response compression.
- HTTPS/HSTS production.
- Hosted background service.
- External AI/payment APIs.

### 25.2. Những yếu tố chưa cloud-ready

- Generated media lưu disk local.
- Data Protection key lưu disk local.
- Memory cache cục bộ.
- Không Dockerfile/compose.
- Không IaC.
- Không deployment manifest.
- Không object storage/CDN.
- Không distributed cache.
- Không metrics/tracing.
- Background service chạy trên mọi replica, có thể xử lý expiry trùng.
- Schema deployment thủ công.

### 25.3. Kiến trúc cloud phù hợp giai đoạn tiếp theo

Giữ modular monolith, triển khai:

- Một ASP.NET Core web service/container.
- Managed SQL Server/Azure SQL.
- Object storage cho generated audio.
- Shared Data Protection key store.
- Secret manager.
- Central logging/APM.
- CDN cho static media nếu giấy phép cho phép.
- Scheduled job/worker duy nhất cho cleanup/expiry.

Chỉ cân nhắc tách service khi có nhu cầu rõ:

- AI generation worker có queue vì latency/chi phí.
- Media ingestion worker chạy `yt-dlp` trong trusted environment.
- Payment service cần isolation/compliance.

---

## 26. Áp dụng kiến thức môn học

| Chủ đề | Biểu hiện trong dự án | Mức hiện tại |
|---|---|---|
| Software Products | Vision học shadowing, mode/persona, MVP và mở rộng AI | Có nhưng tài liệu product chưa chuẩn hóa |
| Agile Software Engineering | Branch theo issue, PR merge, commit `resolves/closes` | Có bằng chứng Git; thiếu artifact milestone/backlog local |
| Features, scenarios and stories | Feature map rõ từ controller/UI | Code có; thiếu user story/acceptance criteria tập trung |
| Software Architecture | MVC, service layer, DI, repository qua DbContext, modular monolith | Khá rõ |
| Cloud-based software | Env config, health, external services | Một phần; chưa cloud-ready |
| Microservices architecture | Có service boundary/interface | Chưa phải microservices |
| Security and Privacy | Cookie, hashing, anti-forgery form, HMAC, rate limit | Có nền tảng; còn gap CSRF/privacy/admin |
| Reliable Programming | idempotency, transaction, constraint, timeout, fallback | Là điểm mạnh |
| Testing | unit/API/SQL Server integration | Có nhưng coverage module chưa đều |
| DevOps and Code Management | GitHub PR/issue branches | Có SCM; chưa có CI/CD |
| HTTP | GET/POST/PUT/DELETE, status code, multipart, JSON | Áp dụng trực tiếp |
| MVC | Controller–Model/Service–Razor View | Kiến trúc chính |
| ORM – Entity Framework | DbContext, DbSet, mapping, LINQ, concurrency | Áp dụng sâu |
| Request–Response | fetch/form → controller → service → DB/provider → JSON/view | Luồng chính |
| Xác thực và phân quyền | Cookie auth, `[Authorize]`, onboarding guard | Auth tốt cơ bản; authorization còn thô |
| Tích hợp và triển khai website | OpenAI, Azure, YouTube, payment code | Tích hợp nhiều; triển khai production chưa có bằng chứng |
| AI có trách nhiệm | Không giả score, review source, manual-caption-first | Có chủ ý; cần privacy/quota/evaluation |

---

## 27. Ưu điểm tổng thể

1. **Bám bài toán thật**: media, timestamp, microphone, scoring, progress liên kết thành một flow học có ý nghĩa.
2. **Phân lớp tương đối sạch**: controller mỏng ở nhiều module, service/interface rõ, DI đầy đủ.
3. **Dữ liệu có tính toàn vẹn cao**: constraint, unique index, rowversion, delete behavior.
4. **Idempotency được dùng đúng chỗ**: practice, heart exchange, payment.
5. **Không che giấu lỗi AI bằng dữ liệu giả**.
6. **Provider strategy hợp lý**: Azure chuyên dụng, OpenAI fallback có cờ.
7. **Manual-caption-first** giảm transcript sai và lệch timeline.
8. **Không chạy `yt-dlp` trong web request**.
9. **Gamification có ledger và concurrency test**.
10. **API ownership thường gắn user từ claim**.
11. **Có SQL Server integration test**, không chỉ mock/in-memory.
12. **Có nền tảng mở rộng** cho AI lesson, dialogue, vocabulary và payment.

---

## 28. Nhược điểm và technical debt

### Mức Critical

- Checkout demo cấp VIP trực tiếp; tuyệt đối không dùng production.
- Chưa có quy trình deploy/schema/secret production có thể kiểm chứng.
- CSRF strategy cho cookie-authenticated JSON mutations chưa rõ.

### Mức High

- README mô tả sai runtime và kiến trúc.
- Không có CI/CD.
- Không có admin/content moderation authorization.
- Local media/key/cache không scale-out.
- Privacy/retention cho voice và AI chưa đầy đủ.
- Test coverage thiếu nhiều module quan trọng.
- Worktree quá lớn và chưa commit.

### Mức Medium

- Health endpoint lộ count.
- API error contract không thống nhất.
- Read transcript có thể write DB.
- AI cost/quota và cleanup chưa đầy đủ.
- Login thiếu lockout/rate limit.
- Nhiều `DateTime.UtcNow` chưa dùng `TimeProvider`.
- Không có CSP/correlation/tracing.
- Schema SQL thủ công chưa có version runner.

### Mức Low

- Naming ShadowSpeak AI/WebShadowing chưa thống nhất.
- Một số controller/service viết rất cô đọng, khó review.
- Tài liệu encoding tiếng Việt có dấu hiệu mojibake trong terminal.
- REST verb/style chưa hoàn toàn nhất quán.

---

## 29. Backlog cải thiện đề xuất

### P0 – Trước khi demo chính thức

1. Đồng bộ README với .NET 10 và kiến trúc request/response hiện tại.
2. Gắn nhãn rõ “Demo payment”; hoặc vô hiệu checkout nếu môi trường production.
3. Restore, build, chạy toàn bộ unit test; lưu kết quả.
4. Chạy SQL integration test trên database test riêng.
5. Commit/tách PR các thay đổi hiện tại.
6. Kiểm tra toàn bộ lesson demo có transcript/timestamp và source status.
7. Không tuyên bố video `pending` là đã duyệt bản quyền.
8. Chuẩn bị fallback demo khi Azure/OpenAI/YouTube mất mạng.

### P1 – Trước MVP public

1. CSRF token cho toàn bộ mutation API hoặc đổi sang auth scheme phù hợp.
2. Login rate limiting/lockout và reset password.
3. Tắt/bảo vệ `X-Test-User`.
4. Health endpoint tối thiểu; readiness nội bộ.
5. Đưa generated media và Data Protection keys sang shared managed storage.
6. Thêm cleanup preview/audio/session.
7. Chuẩn hóa `ProblemDetails`.
8. CI workflow: restore/build/test/secret/dependency scan.
9. Role/policy cho admin và reviewer.
10. Privacy policy thực tế, consent microphone/AI và delete account/data.

### P2 – Hoàn thiện sản phẩm

1. Payment thật đi qua service + webhook, bỏ activation demo.
2. SRS từ vựng.
3. Chấm dictation gần đúng và feedback theo từ.
4. Timestamp editor/review UI.
5. Quota AI theo gói và cost analytics.
6. Observability: correlation ID, metrics, traces, dashboards.
7. Accessibility và browser E2E.
8. Object storage/CDN.
9. Schema version runner.

### P3 – Khi có scale thật

1. Queue cho AI/TTS/media jobs.
2. Worker riêng cho ingestion/cleanup.
3. Distributed cache.
4. Tách payment hoặc AI worker nếu có lý do vận hành/compliance.
5. Autoscaling, circuit breaker, centralized telemetry.

---

## 30. Kịch bản demo kỹ thuật bám đúng hệ thống

Phần này không thay thế test scenario chính thức; đây là thứ tự trình bày luồng code.

1. Mở trang auth, đăng ký tài khoản.
2. Giải thích password hash, transaction và cookie claim.
3. Chọn casual/academic/professional, accent và target.
4. Cho thấy onboarding guard chặn route trước khi hoàn thành.
5. Mở Home; giải thích course được lọc bằng mode trong DB.
6. Mở một YouTube lesson có manual captions.
7. Cho thấy transcript JSON, timestamp và YouTube seek/pause.
8. Nêu rõ importer `yt-dlp` chạy offline, manual captions mặc định, không download video.
9. Thu âm một câu; xem multipart request và idempotency key.
10. Giải thích Azure-first/OpenAI-fallback, score threshold.
11. Cho thấy attempt, progress, EXP/heart/streak và ledger.
12. Retry cùng key để chứng minh không cộng thưởng hai lần.
13. Demo dictation/IPA match deterministic.
14. Mở audio curriculum; giải thích audio đã hỗ trợ nhưng dictation theo câu cần timestamp.
15. Nếu API key ổn định, demo AI lesson/TTS hoặc AI dialogue.
16. Nếu demo VIP, nói rõ đó là activation demo, không gọi cổng thanh toán thật.
17. Mở health và test report.
18. Kết thúc bằng giới hạn và roadmap thay vì tuyên bố mọi phần production-ready.

---

## 31. Gợi ý user story và acceptance criteria rút ra từ code

Nhóm có thể đưa các mục này lên GitHub sau; đây không phải bằng chứng chúng đã tồn tại trước đó.

### Story: Shadowing một câu

> Là người học đã đăng nhập và hoàn tất onboarding, tôi muốn nghe một câu mẫu, ghi âm câu của mình và nhận điểm để biết mình đã đạt mục tiêu phát âm chưa.

Acceptance criteria:

- Lesson phải thuộc mode của user.
- Câu phải thuộc lesson.
- Có thể phát đúng đoạn theo timestamp.
- Audio upload tối đa 10 MB và đúng định dạng/thời lượng.
- Request có idempotency key.
- Provider lỗi trả 503, không tạo điểm giả.
- Attempt thành công được lưu một lần.
- Pass/fail dựa trên target của user.
- Retry cùng key không đổi balance lần hai.

### Story: Nhập video an toàn

> Là content maintainer, tôi muốn nhập video có phụ đề thủ công để giảm lỗi text/timeline trước khi đưa vào bài học.

Acceptance criteria:

- Import không chạy trong web request.
- Không download/rehost video.
- Mặc định không dùng auto caption.
- Ghi transcript, metadata và quality report.
- Nguồn mới ở trạng thái pending.
- Chỉ reviewer có quyền đổi approved trong phiên bản có admin.

### Story: Học bằng audio giáo trình

> Là học sinh, tôi muốn phát audio giáo trình và xem transcript để luyện shadowing theo nội dung trong chương trình.

Acceptance criteria:

- App tìm được file audio cùng thư mục transcript.
- Path không thoát web root.
- Nếu có timestamp, phát đúng đoạn.
- Nếu không có timestamp, cho phát toàn file và thông báo giới hạn của dictation.

---

## 32. Kết luận

WebShadowing hiện là một **modular monolith MVC có nhiều chức năng thật**, trong đó luồng mạnh nhất là:

```text
Auth + onboarding
→ course/lesson theo mode
→ video/audio + transcript có timeline
→ ghi âm bằng browser
→ HTTP upload có idempotency
→ Azure/OpenAI assessment
→ lưu attempt/progress
→ cập nhật EXP/heart/streak
```

Dự án đã áp dụng đáng kể kiến thức HTTP, MVC, EF Core, authentication, reliable programming, testing và quản lý mã nguồn theo issue/PR. Tuy nhiên, cần diễn đạt đúng rằng cloud, microservices, realtime streaming, CI/CD và payment production **chưa hoàn thiện hoặc chưa tồn tại trong snapshot**.

Hướng phù hợp nhất không phải tách microservices ngay, mà là:

1. Ổn định modular monolith.
2. Đồng bộ tài liệu với code.
3. Khóa các lỗ hổng security/privacy.
4. Hoàn thiện test và CI.
5. Chuẩn hóa deploy, schema, storage và observability.
6. Sau đó mới tách worker/service ở những nơi có nhu cầu vận hành thật.

Đây sẽ là cách trình bày trung thực, có chiều sâu kỹ thuật và phù hợp với tinh thần kỹ nghệ phần mềm hiện đại: xây MVP có phạm vi, đo được chất lượng, kiểm soát rủi ro, phát triển lặp và không phóng đại mức trưởng thành của sản phẩm.

---

# PHỤ LỤC KIỂM TRA LẠI TOÀN BỘ CODEBASE — 28/07/2026

> Phần này là bản cập nhật có độ ưu tiên cao hơn các mô tả cũ nếu hai phần khác nhau. Việc kiểm tra được thực hiện trực tiếp trên snapshot code hiện tại: 16 controller, 18 Razor view/partial, 8 tệp JavaScript phía trình duyệt, 47 tệp service, cấu hình khởi động, EF Core, migration/SQL, media, công cụ import và ba project kiểm thử.

## 33. Bản đồ đầy đủ các trang và trạng thái giao diện

Ứng dụng không có 18 “trang độc lập”. Nhiều tệp Razor là layout, partial hoặc component. Các URL có giao diện thật được liệt kê đầy đủ dưới đây.

| URL/trạng thái | Controller/action | View | Cách hoạt động và quyền truy cập |
|---|---|---|---|
| `/` hoặc `/Home/Index` | `Home.Index` | `Home/Index.cshtml` | Trang thư viện, bắt buộc đăng nhập và hoàn thành onboarding. Tải khóa giáo trình, Video Bank, bài AI đã lưu và sau đó JavaScript tải thêm draft/bài AI. |
| `/Home/Index?mode=...` | `Home.Index` | `Home/Index.cshtml` | Tham số đổi mode chỉ có tác dụng override trong Development; production dùng mode lưu trong tài khoản. |
| `/Home/LessonDetail/{id}?mode=...` | `Home.LessonDetail` | `Home/LessonDetail.cshtml` | Chi tiết bài giáo trình hoặc Video Bank; server kiểm tra bài có thuộc mode của user. |
| `/ai-lessons/preview/{previewId}` | `Home.AiLessonPreview` | dùng lại `Home/LessonDetail.cshtml` | Mở draft AI thuộc đúng user; URL chứa GUID; draft hết hạn hoặc không thuộc user trả `404`. |
| `/ai-lessons/{savedLessonId}` | `Home.AiLessonDetail` | dùng lại `Home/LessonDetail.cshtml` | Mở bài AI đã lưu, không hết hạn; kiểm tra ownership. |
| `/Home/Stats` | `Home.Stats` | `Home/Stats.cshtml` | Trang “Tiến trình & Thẻ nhớ”: KPI streak/EXP/tim, đổi EXP lấy tim, flashcard từ vựng và câu yêu thích. Dữ liệu động qua API. |
| `/Home/Settings` | `Home.Settings` | `Home/Settings.cshtml` | Trang tài khoản: hồ sơ, mode, mục tiêu phát âm, auto-save AI, theme, VIP/demo checkout và đăng xuất. |
| `/Home/Settings?checkout=vip` | `Home.Settings` | cùng view Settings | Tự mở khu vực nâng cấp VIP; không phải trang thanh toán riêng. |
| `/Home/Authen?step=login` | `Home.Authen` | `Home/Authen.cshtml` | Đăng nhập bằng form POST có anti-forgery. |
| `/Home/Authen?step=register` | `Home.Authen` | cùng view Authen | Đăng ký rồi chuyển sang onboarding. |
| `/Home/Authen?step=level` | `Home.Authen` | cùng view Authen | Chọn mục đích/mode và accent. |
| `/Home/Authen?step=goal` | `Home.Authen` | cùng view Authen | Chọn mục tiêu điểm phát âm, sau đó hiện lựa chọn Free/VIP. |
| `/Home/Privacy` | `Home.Privacy` | `Home/Privacy.cshtml` | Quyền riêng tư và điều khoản; cho phép anonymous. Nội dung hiện mới là bản tóm tắt sản phẩm, chưa phải chính sách pháp lý hoàn chỉnh. |
| `/Home/NotFoundPage` và lỗi 404 MVC | `Home.NotFoundPage` | `Home/NotFound.cshtml` | Trang 404 tùy biến. API không bị redirect sang HTML. |
| `/Home/Error` hoặc exception production | `Home.Error` | `Shared/Error.cshtml` | Trang lỗi chung, `no-store`, không lộ exception nội bộ. |
| `/health` | `Health.Get` | JSON, không có view | Health check DB; trả `200 healthy` hoặc `503 unhealthy`, không lộ số lượng bảng/bản ghi. |

Các tệp giao diện hỗ trợ, không có URL riêng:

- `Shared/_Layout.cshtml`: navbar desktop, nội dung chính, footer, CSS/JS chung và user stats.
- `Shared/_BottomNav.cshtml`: điều hướng mobile tới Khóa học, Tiến trình, Tài khoản.
- `Home/_CourseSection.cshtml`: render một hàng khóa học giáo trình.
- `Home/_LessonCard.cshtml`: dùng chung cho card bài thường và bài AI đã lưu.
- `Shared/Components/UserNavStats/Default.cshtml`: streak, tim, EXP, VIP/FREE trên desktop.
- `Shared/Components/UserNavStats/Mobile.cshtml` và `MobileHeart.cshtml`: bản thu gọn cho mobile.
- `_ViewImports.cshtml`, `_ViewStart.cshtml`, `_ValidationScriptsPartial.cshtml`: hạ tầng Razor/validation, không phải trang.

Như vậy codebase hiện **không có** trang admin, dashboard quản trị nội dung, trang thanh toán riêng, trang quên/đặt lại mật khẩu, xác minh email, lịch sử giao dịch, trang chi tiết tiến trình theo bài, hay trang đối thoại AI độc lập. Không nên demo những trang này như chức năng đã có.

## 34. Luồng trang Khóa học và card bài học

### 34.1 Khi mở trang

1. Cookie middleware xác thực user.
2. `OnboardingGuardMiddleware` chặn user chưa onboarding; MVC redirect về Authen, API trả `403` kèm `onboardingUrl`.
3. `Home.Index` lấy learning mode thực tế của user.
4. `CourseService` lấy giáo trình và Video Bank theo mode.
5. Server lấy `UserSavedLessons` cùng các segment AI đã lưu để render.
6. Trình duyệt chạy `lesson-ai-generator.js`, gọi song song:
   - `GET /api/ai-lessons/previews`;
   - `GET /api/ai-lessons`.
7. Client ghép draft và bài đã lưu thành khối **Bài AI**. Draft hiển thị nhãn **Bản nháp**, thời gian còn lại, nút Lưu/Xóa; bài saved hiển thị **Đã lưu** và nút Xóa.

Hiện có một điểm trùng giao diện: `Index.cshtml` còn render khối server **Bài đã lưu**, trong khi JavaScript cũng đưa bài saved vào khối **Bài AI**. Cùng một bài có khả năng xuất hiện hai nơi. Lý do là luồng server-render cũ chưa được gỡ khi bổ sung thư viện AI động. Cần hợp nhất thành một nguồn render duy nhất; ưu tiên server render toàn bộ initial state rồi JavaScript chỉ cập nhật sau thao tác để giảm nhấp nháy, gọi API lặp và card trùng.

### 34.2 Khi bấm card

- Card thường mở `/Home/LessonDetail/{lessonId}`.
- Draft AI mở `/ai-lessons/preview/{previewId}`.
- Bài AI đã lưu mở `/ai-lessons/{savedLessonId}`.
- Cả ba dùng chung `LessonDetail.cshtml`; server tạo một `LessonDetailViewModel` khác nhau theo nguồn.
- Chuyển tab trong bài học chỉ đổi panel bằng JavaScript trên cùng DOM, **không tải lại trang**. Chỉ lần đầu mở card mới thực hiện HTTP navigation và render Razor.

Tìm kiếm trên trang lọc card client-side theo `data-title` và `data-desc`; nó không tìm trong DB và không phân trang. “Xem tất cả” Video Bank cũng là thao tác hiện/ẩn trên dữ liệu đã render.

## 35. Luồng chi tiết trang bài học và từng tab

### 35.1 Dữ liệu chung

Server nhúng initial lesson JSON vào trang. `lesson-shadowing.js` điều khiển media, câu hiện tại, thu âm, chấm phát âm, tooltip từ và subtitle. `lesson-practice-tabs.js` điều khiển yêu thích, nghe chép, ghép IPA và đối thoại AI. `ai-lesson-detail.js` chỉ được dùng khi nguồn là AI để Lưu/Xóa.

Nút Shadowing Studio, Đối thoại AI, Nghe chép, Ghép IPA cùng nút Lưu/Xóa bài AI nằm trên thanh tab. Desktop hiện icon và chữ; CSS responsive chỉ giữ icon cho Lưu/Xóa trên mobile. Việc chuyển tab không tạo request lấy lại toàn trang.

### 35.2 Shadowing Studio

1. User chọn một câu/subtitle.
2. Với video có timeline, player seek tới `StartSeconds` và dừng ở `EndSeconds`.
3. Với audio segment AI, phát MP3 riêng của segment.
4. Nếu không có media phù hợp, trình duyệt có thể dùng `speechSynthesis` làm fallback đọc câu; đây là giọng của hệ điều hành/trình duyệt, chất lượng không đồng nhất.
5. User cấp quyền microphone. `MediaRecorder` ghi trong trình duyệt; client chuyển dữ liệu cần thiết sang WAV và tạo playback cục bộ để user nghe lại.
6. Khi chấm, client POST multipart tới `/api/practice/evaluate-shadowing`, gồm audio, định dạng, lesson/sentence hoặc AI segment/preview, accent, context và idempotency key.
7. Server giới hạn request/audio, xác minh user có quyền học bài, rồi gọi `HybridPronunciationAssessmentService`.
8. Azure Speech Pronunciation Assessment là provider ưu tiên khi cấu hình đủ; OpenAI là fallback chỉ khi bật `AllowOpenAiFallback`. Không phải Web Speech API của trình duyệt làm speech-to-text.
9. Provider trả transcript/điểm thành phần; `PronunciationScoreProfileService` tính overall theo trọng số mode khi có component score.
10. Với bài thường, `PracticeEvaluationService` lưu attempt/progress, cập nhật lỗi từ, vocabulary, EXP/tim/streak theo transaction và idempotency.
11. Với draft/bài AI, audio vẫn được provider chấm thật nhưng hiện **không lưu attempt, không cộng/trừ gamification**, vì schema tiến trình đang ràng buộc `LessonSentence` của bài curated. Đây là chủ ý tránh ghi dữ liệu sai quan hệ; cần mở rộng schema nguồn bài trước khi bật.

Nhận xét hiển thị là dữ liệu do provider AI tạo/phân tích, sau đó server chuẩn hóa; không phải một giảng viên thủ công. Vì vậy phải coi là phản hồi hỗ trợ, không phải đánh giá ngôn ngữ tuyệt đối.

Điểm cần sửa: `HomeController` hiện xác định cờ “AI phát âm đã cấu hình” chủ yếu từ OpenAI key, nên trường hợp chỉ có Azure hợp lệ có thể làm UI báo sai trạng thái. Cờ này cần dựa trên provider thực tế đã sẵn sàng.

### 35.3 Đối thoại AI

- Chỉ VIP mới dùng; Free thấy màn nâng cấp.
- Bài curated gửi `lessonId`; service lấy tiêu đề và tối đa 6 câu làm ngữ cảnh.
- Mở tab tạo session DB và AI sinh lời chào; reply text được OpenAI TTS đổi thành MP3 để trình duyệt phát.
- Gửi text: lưu turn user, lấy tối đa 20 turn gần nhất, gọi chat model, lưu reply, sinh audio.
- Gửi voice: browser `MediaRecorder` gửi nguyên blob âm thanh tới `/audio`; OpenAI transcription đổi thành text, rồi đi qua đúng luồng chat + TTS. Raw voice user không được lưu vào DB; DB giữ transcript và turn. MP3 reply AI được lưu dưới static media.
- Session tối đa 30 lượt và hết hiệu lực sau 15 phút không hoạt động.
- Accent user ảnh hưởng lựa chọn voice/TTS nhưng không đảm bảo một giọng địa phương tuyệt đối như một voice chuyên biệt được kiểm thử.
- Bài AI hiện không dùng được tab này vì bảng dialogue đang tham chiếu `LessonId` curated; view chủ động vô hiệu hóa thay vì gửi ID giả. Muốn hỗ trợ phải thêm `SavedLessonId/PreviewId` hoặc mô hình `LessonSource`.

### 35.4 Nghe chép

- Với video hoặc câu có `AudioUrl`/timeline, nút nghe phát đúng đoạn của câu hiện tại.
- User gõ đáp án; bài curated gọi `/api/practice/evaluate-answer`, server chuẩn hóa và so khớp đáp án.
- Bài AI có MP3 theo segment nên client có thể chấm cục bộ từ text segment.
- Bài audio giáo trình không có timestamp đáng tin cậy chỉ có thể phát toàn file, không biết chính xác câu bắt đầu/kết thúc. Vì vậy UI chủ động báo “sắp ra mắt” thay vì cắt audio bằng ước lượng.

Đây không phải do “audio không thể có timeline”, mà do dữ liệu audio hiện chưa trải qua bước forced alignment. Giải pháp V2: dùng WhisperX/Montreal Forced Aligner/Azure word timestamps trong worker offline, lưu `StartSeconds/EndSeconds`, reviewer kiểm tra lệch timeline rồi mới bật dictation. Không nên chạy tool nặng này trong request web vì CPU/RAM/thời gian xử lý và rủi ro timeout.

### 35.5 Ghép IPA

- Với câu curated chưa có IPA, client gọi `/api/sentence-ipa`; server tách tối đa 40 từ, gọi `ILanguageReferenceService` lấy IPA theo batch, ghép và lưu lại DB.
- Bài AI đã nhận IPA ngay trong JSON sinh bài; hiện dùng dữ liệu AI tạo sinh, không bảo đảm tương đương từ điển phát âm chuẩn.
- Các lựa chọn được JavaScript tạo bằng cách trộn đáp án đúng với distractor từ tập IPA của bài. Đây là random hóa bài tập, không phải AI gọi lại mỗi lần.
- Chấm bằng so sánh lựa chọn với IPA chuẩn trong initial state; không cần request AI mới.
- Accent Anh-Anh/Anh-Mỹ ảnh hưởng request/provider và giọng TTS. Tuy nhiên IPA đã lưu ở mức sentence/segment chỉ có một trường, không có hai bản `ipaUk`/`ipaUs`; đổi accent không đảm bảo mọi phiên âm cũ đổi theo. Đây là thiếu sót dữ liệu cần sửa bằng IPA theo accent và cache key gồm accent.

### 35.6 Tra nghĩa, yêu thích

Click từ gọi `/api/word-meaning` với từ và ngữ cảnh; service dùng nguồn ngôn ngữ/AI đã cấu hình và cache, không phải dữ liệu được dịch thủ công. Popup là lớp UI trong `LessonDetail.cshtml`; nền cần đủ đục, z-index và contrast để không chồng chữ.

Nút bookmark gọi `/api/favorites`; server kiểm tra sentence tồn tại và lưu theo user. Hiện favorite gắn với sentence curated, chưa có mô hình favorite cho AI segment.

## 36. Bài giảng tạo sinh bởi AI — luồng thật hiện tại

1. User nhập chủ đề và level trên trang Khóa học.
2. `POST /api/ai-lessons/generate`, rate limit 5 lần/phút/user.
3. OpenAI chat model sinh JSON có title và 3–12 segment: text, bản dịch, IPA, speaker.
4. Server gọi TTS **tuần tự cho từng segment**, lưu mỗi MP3 trong `wwwroot/media/generated/...`.
5. Server tạo `AI_Lesson_Previews` với `CreatedAt`, `ExpiresAt = CreatedAt + 1440 phút`, JSON snapshot và trạng thái draft.
6. Nếu `AutoSaveAiLessons=true`, service lưu ngay thành `User_Saved_Lessons`; nếu không, vẫn có draft DB 24 giờ.
7. Client điều hướng tới `/ai-lessons/preview/{previewId}` để học như bài bình thường.
8. Lưu tạo `User_Saved_Lessons` và `Saved_AI_Lesson_Segments`, sau đó gắn `SavedLessonId` vào preview.
9. Draft hiển thị thời gian còn lại từ UTC. Service có logic nâng các draft lịch sử từng tạo theo lifetime 30 phút sang lifetime 24 giờ khi gọi danh sách, nhằm tương thích dữ liệu cũ.
10. Hết hạn: khi danh sách/generate chạy, record preview quá hạn bị xóa và biến mất khỏi UI.

Các giới hạn và lý do:

- TTS chạy tuần tự làm request tạo bài chậm và dễ timeout khi nhiều segment. Lý do ban đầu là luồng MVP đơn giản, dễ giữ đúng thứ tự. Nên chuyển sang job queue/background worker, có trạng thái `GeneratingText → GeneratingAudio → Ready/Failed`, retry từng segment.
- Preview/saved bị xóa nhưng code chưa xóa chắc chắn thư mục MP3 tương ứng, gây orphan file và tăng dung lượng. Cần lưu media asset ownership và có cleanup worker.
- Nội dung AI đang gắn `SourceReviewStatus=Approved` khi lưu, nhưng chưa có reviewer người thật. Nên đổi thành `ai-generated`/`unreviewed`, hiển thị cảnh báo và chỉ `approved` sau workflow duyệt.
- Không có version prompt/model trên từng lesson, nên khó audit hoặc tái tạo. Cần lưu prompt template version, model, voice, accent, timestamp và moderation result.
- Nếu TTS hỏng giữa chừng, có nguy cơ còn file rác/preview không hoàn chỉnh. Cần transaction logic ở metadata và cleanup compensation.
- Bài AI chưa tham gia đầy đủ dialogue, progress, favorite và gamification vì mô hình dữ liệu đang tối ưu cho lesson curated. Không nên bỏ tính năng sang V2 hoàn toàn; giữ MVP generate–preview–save–delete–shadowing, còn lifecycle/job queue và tích hợp tiến trình đưa vào milestone V2.

## 37. Xác thực, phân quyền và request–response

- Cookie authentication, không phải JWT.
- Mật khẩu do `AuthService` hash/verify bằng `PasswordHasher<User>`; không lưu plaintext.
- Form login/register/logout/onboarding server dùng anti-forgery. Các API JSON dựa vào SameSite cookie; một số endpoint mutation chưa áp `[ValidateAntiForgeryToken]` đồng nhất, cần audit CSRF toàn bộ API.
- Cookie hết hạn 30 ngày, sliding expiration; login path `/Home/Authen`, access denied cũng quay về Authen.
- API khi chưa đăng nhập trả `401`, không redirect HTML.
- Authorization hiện chủ yếu là authenticated user + ownership + VIP/mode; chưa có role Admin/Editor/Reviewer.
- Return URL dùng `Url.IsLocalUrl`, giảm open redirect.
- Rate limit: pronunciation 10/phút, language reference 30/phút, AI generation 5/phút, dialogue 12/phút; không queue, vượt hạn mức trả `429`.
- Upload shadowing tối đa 10 MB, vocabulary 5 MB; duration được kiểm tra chắc hơn với WAV, còn MP3 chưa được giải mã để xác minh duration đầy đủ.
- Idempotency dùng cho practice, đổi tim và checkout demo để tránh ghi lặp.

## 38. API đầy đủ theo nhóm

Không phải “trang”, nhưng đây là toàn bộ bề mặt backend mà các trang đang dùng:

- Auth/account: POST `/Account/Login`, `/Account/Register`, `/Account/CompleteOnboarding`, `/Account/Logout`.
- User: GET/PUT `/api/user/profile`, PUT `/api/user/settings`, POST `/api/user/mode`, GET `/api/user/me`, PUT `/api/user/onboarding`.
- Library: GET `/api/library`, `/api/courses`, `/api/courses/{id}`, `/api/lessons/{id}`.
- Practice: POST `/api/practice/evaluate-shadowing`, `/api/practice/evaluate-answer`.
- Reference: POST `/api/word-meaning`, `/api/word-ipa/batch`, `/api/sentence-ipa`.
- AI lesson: POST `/api/ai-lessons/generate`, `/save`; GET `/api/ai-lessons`, `/previews`; DELETE preview hoặc saved lesson.
- Dialogue: POST session, POST text/audio, GET session dưới `/api/ai-dialogue/sessions`.
- Vocabulary: list/detail/add/mastered/review/delete/pronunciation dưới `/api/vocabulary`.
- Favorites: list/add/delete dưới `/api/favorites`.
- Gamification: GET balance, POST exchange-heart.
- Subscription/payment: GET/cancel subscription; demo checkout; MoMo/ZaloPay webhook.
- Operations: GET `/health`.

## 39. Dữ liệu, ORM và media

EF Core 10 + SQL Server quản lý 24 nhóm bảng: user/course/enrollment/lesson/material/sentence/transcript; practice session/recording/feedback/attempt/progress; gamification/word error/vocabulary/favorite/settings/mode history; saved AI lesson/segments/preview; VIP subscription/payment; dialogue session/turn.

Ứng dụng là **modular monolith**, không phải microservices. Controller → service → `AppDbContext` nằm cùng process và cùng DB. Đây là lựa chọn phù hợp MVP vì triển khai đơn giản, transaction xuyên module dễ và chi phí vận hành thấp. Nhược điểm là AI/TTS/cleanup/background expiry cùng chia tài nguyên với web request; scale độc lập chưa được.

Media hiện gồm:

- audio giáo trình và `transcript.json` theo grade/unit;
- Video Bank chỉ lưu URL/metadata/transcript/timeline, không rehost video;
- MP3 tạo sinh AI/TTS trong static `wwwroot/media/generated`;
- bản thu shadowing thông thường không được dùng như kho audio lâu dài trong luồng đánh giá hiện tại.

Video import là công cụ offline Python trong `tools/video-import`, mặc định ưu tiên phụ đề do uploader cung cấp và tạo report/source metadata. Không nên đổi sang auto-caption mặc định vì sai text/timeline ảnh hưởng trực tiếp đáp án và điểm. Tuy vậy code vẫn cần reviewer/admin workflow thực sự; hiện tool + trạng thái dữ liệu chưa tạo thành một UI kiểm duyệt hoàn chỉnh.

## 40. Startup, cloud, vận hành và bảo mật

Pipeline thực tế:

```text
load .env (Development)
→ DI/EF/HTTP clients/cache/rate limit/options
→ Data Protection keys
→ cookie auth
→ response compression
→ SubscriptionExpiryService
→ exception handler/HSTS/HTTPS
→ security headers
→ routing
→ 404 handling
→ authentication
→ onboarding guard
→ rate limiter
→ authorization
→ static assets
→ controller routes
```

Điểm tốt: key Data Protection được persist; HSTS/HTTPS production; `nosniff`, SAMEORIGIN, strict-origin referrer; health check không lộ dữ liệu; checkout demo fail-closed ngoài Development/Testing; webhook có xác minh chữ ký/MAC trong service; option được validate khi start.

Điểm thiếu:

- Chưa có Content-Security-Policy.
- Chưa có secret manager/cloud identity chính thức; key phải đến từ environment/user secrets, tuyệt đối không commit.
- Chưa thấy Dockerfile, compose, manifest cloud hay workflow CI trong snapshot.
- `SubscriptionExpiryService` quét expiry trong web process; đã xử lý cancellation để tránh lỗi khi shutdown, nhưng nhiều instance có thể cùng quét. Nên dùng distributed lock hoặc scheduled job.
- Memory cache/rate limit là theo instance; scale-out sẽ không có giới hạn toàn cục.
- MP3 ở local disk không bền khi deploy container/đa instance. Cần object storage + CDN + signed/private policy phù hợp.
- Chưa có structured tracing, metrics, alert, correlation ID và dashboard vận hành.
- Privacy page nói dữ liệu audio gửi AI nhưng chưa có retention/consent/delete-account/export-data đầy đủ.

## 41. Payment và VIP

Settings gọi checkout MoMo/ZaloPay nhưng `PaymentController.Checkout` hiện là **demo activation**: chỉ trong Development/Testing, click tạo subscription + transaction “demo” và bật `User.IsVip` ngay. Production trả `503`; đây là hành vi an toàn, không phải thanh toán thật.

Webhook MoMo/ZaloPay tồn tại và chuyển payload tới `PaymentService` để kiểm signature/MAC, nhưng chưa đủ cơ sở gọi là luồng checkout production end-to-end vì UI chưa tạo order/redirect/QR thật và credentials mặc định để trống. Text mojibake trong một số message controller cho thấy encoding source/resource cần chuẩn hóa UTF-8.

Hủy subscription chỉ tắt gia hạn/trạng thái theo service; background service đồng bộ user VIP khi hết hạn. Cần test thêm webhook replay, amount/currency/order ownership, race giữa cancel-expire-webhook và audit log.

## 42. Kiểm thử thực có và khoảng trống

Ba project test hiện có:

- Unit: hybrid Azure/OpenAI fallback, score profile, lesson transcript read-only behavior, practice MIME/duration/idempotency/word errors, rate limit, health information disclosure, production checkout fail-closed.
- AuthFlowTests: policy và integration cho reward/penalty/VIP/retry/exchange/concurrency. Tên project lịch sử không còn phản ánh chính xác vì test chủ yếu gamification.
- DatabaseIntegrationTests: kiểm tra nâng cấp schema legacy có dữ liệu, migration idempotent, uniqueness, check constraint và row-version concurrency trên SQL Server. Ở lần chạy 28/07/2026, test uniqueness/concurrency đạt nhưng test nâng schema/idempotency **không đạt**: script cố drop `dbo.User_Saved_Lessons` khi bảng còn bị foreign key tham chiếu, rồi cố tạo lại object cùng tên. Đây là lỗi migration thật cần xử lý, không được ghi nhận là test xanh.

Chưa thấy test tự động cho:

- login/register/onboarding cookie end-to-end dù project mang tên AuthFlow;
- toàn bộ route/page trả đúng status và view;
- AI generate–preview 24h–save–delete–ownership–cleanup;
- dialogue text/audio/VIP/expiry/30 turns;
- UI responsive, tab switching, popup dictionary, card trùng và accessibility;
- payment webhook fixtures;
- XSS/CSRF/IDOR/security headers;
- transcript/video import quality;
- browser test Playwright/Selenium;
- CI chạy build/test trên push/PR.

## 43. DevOps, code management và Agile ở hiện trạng thật

Snapshot có solution và test projects, SQL cập nhật production, README và công cụ import. Tuy nhiên không có `.github/workflows`, Docker hay deployment-as-code. Vì vậy “DevOps” hiện mới ở mức build/test/config thủ công và health endpoint, chưa phải CI/CD.

Git worktree đang có rất nhiều tệp modified/staged/untracked cùng lúc. Điều này cho thấy một feature batch lớn chưa được chia nhỏ; rủi ro review khó, merge conflict và lẫn phạm vi. Báo cáo không thể xác nhận issue/milestone/PR trên GitHub chỉ từ local code. Để thể hiện Agile trung thực:

1. Tạo milestone MVP Stabilization, AI Lesson V1, Production Readiness.
2. Mỗi issue có persona/story, acceptance criteria, test evidence, lý do hoãn.
3. Chia PR theo vertical slice: AI draft lifecycle; AI lesson detail; progress UI; payment hardening; test/CI.
4. Bắt buộc review ít nhất một người, CI xanh, secret scan và migration review.
5. Gắn quyết định kỹ thuật vào ADR: modular monolith, uploader captions only, Azure-first assessment, local media chỉ development.

## 44. Danh sách cải thiện ưu tiên, có lý do

### P0 — trước demo/merge

1. Hợp nhất hai khối bài AI saved trên Index. Lý do: card trùng làm user hiểu sai số bài.
2. Chạy build và toàn bộ test; thêm smoke test mọi URL ở mục 33. Lý do: thay đổi CSS/layout gần đây có thể làm hỏng Stats/Settings/mobile mà unit test không bắt được.
3. Audit CSRF cho mọi API mutation dùng cookie. Lý do: `[Authorize]` không tự ngăn cross-site request.
4. Đảm bảo không có key thật trong `appsettings.Development.json`; rotate nếu từng commit. Lý do: key OpenAI/Azure/payment có thể phát sinh chi phí và lộ dữ liệu.
5. Chuẩn hóa UTF-8 các message mojibake. Lý do: response lỗi tiếng Việt hiện có thể không đọc được.
6. Sửa migration `User_Saved_Lessons`: xác định và drop/recreate các foreign key phụ thuộc theo thứ tự an toàn, hoặc dùng migration thay đổi tại chỗ; chạy script hai lần phải cùng thành công. Lý do: database integration test hiện thất bại và có thể chặn nâng cấp production.

### P1 — hoàn thiện MVP

1. Cleanup MP3 khi preview hết hạn/xóa; có retry và orphan scanner.
2. Thêm AI lesson source abstraction để progress/favorite/dialogue/gamification không phụ thuộc `LessonSentence`.
3. Sửa provider-ready flag để Azure-only vẫn hiện thu âm đúng.
4. Lưu IPA theo accent (`en-US`, `en-GB`) và model/source version.
5. Tạo background job cho generation/TTS; UI polling trạng thái.
6. Bổ sung email verification, reset password, account delete/export và retention policy.
7. E2E responsive cho Khóa học, LessonDetail, Stats, Settings, Authen, 404/Error.

### P2 — production/cloud

1. Object storage/CDN cho audio; DB chỉ lưu asset key/URL.
2. Distributed cache/rate limit/job lock.
3. Checkout provider end-to-end và reconciliation.
4. CI/CD, container, environment promotion, rollback và backup restore drill.
5. OpenTelemetry/log correlation/metrics/alerts.
6. Admin/reviewer UI cho transcript và nội dung AI.
7. Chỉ cân nhắc tách AI worker thành service riêng sau khi đã có queue, observability và tải thực chứng minh nhu cầu.

## 45. Kết luận sau lần kiểm tra lại

Luồng chạy thật hiện tại là:

```text
Cookie auth + onboarding
→ thư viện theo learning mode
→ card curated / video / AI draft / AI saved
→ một trang LessonDetail dùng chung
→ tab đổi client-side, không reload
→ media + transcript/timeline
→ browser MediaRecorder
→ Azure-first/OpenAI-fallback assessment
→ curated lesson lưu tiến trình và gamification
→ AI lesson hiện chấm được nhưng chưa tích hợp đầy đủ tiến trình
```

Phần đã mạnh nhất là modular MVC/EF Core, ownership, luồng bài học, transcript có timeline, recording–assessment, gamification có idempotency và AI lesson có draft 24 giờ. Phần cần nói rõ khi bảo vệ là payment production, cloud deployment, microservices, CI/CD, forced alignment cho audio, review nội dung AI và tiến trình đầy đủ cho bài AI **chưa hoàn thiện**. Các phần này bị hoãn không phải vì không biết làm, mà vì MVP đang ưu tiên một luồng học chạy được và tránh đưa dữ liệu/điểm sai vào hệ thống trước khi có schema, worker và quy trình kiểm duyệt phù hợp.
