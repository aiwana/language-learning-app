# Phân công chức năng trong codebase WebShadowing

> Cập nhật theo phân công nhóm cung cấp ngày 28/07/2026. “Phụ trách” là người chịu trách nhiệm chính về chức năng, không đồng nghĩa là người duy nhất từng sửa mọi dòng trong tệp. Tệp dùng chung cần review chéo vì một thay đổi có thể ảnh hưởng nhiều chức năng.

## 1. Minh Anh — giao diện và trải nghiệm bài học

### Phạm vi chính

- Thiết kế giao diện tổng thể và responsive.
- Trang Khóa học/thư viện, card giáo trình, Video Bank và Bài AI.
- Tab Shadowing Studio.
- Tab Đối thoại AI.
- Bài học tạo sinh bằng AI: form tạo, draft 24 giờ, card, preview, lưu/xóa và giao diện chi tiết.

### Tệp chính

| Khu vực | Tệp | Vai trò |
|---|---|---|
| Trang Khóa học | `Views/Home/Index.cshtml`, `_CourseSection.cshtml`, `_LessonCard.cshtml` | Render thư viện, section và card. |
| Bài học | `Views/Home/LessonDetail.cshtml` | Trang dùng chung cho bài curated, Video Bank, AI draft và AI saved. |
| Shadowing | `wwwroot/js/lesson-shadowing.js` | Media, câu hiện tại, ghi âm browser, upload chấm phát âm, tooltip từ. |
| AI lesson | `Controllers/AiLessonsController.cs`, `Services/AiLessonGenerationService.cs` | Generate, TTS, draft 24 giờ, save/delete và ownership. |
| AI lesson UI | `wwwroot/js/lesson-ai-generator.js`, `ai-lesson-detail.js` | Tạo card/draft, điều hướng preview, lưu/xóa trong chi tiết. |
| AI dialogue | `Controllers/AiDialogueController.cs`, `Services/AiDialogueService.cs` | Session VIP, text/voice, transcription, chat reply và TTS. |
| CSS bài học | `wwwroot/css/lesson-detail.css` | Toolbar tab, player, shadowing, dialogue và responsive. |
| CSS tổng thể | `wwwroot/css/site.css` | Design token, layout, navbar, library/card và responsive dùng chung. |

### Điểm giao với thành viên khác

- `LessonDetail.cshtml` và `lesson-practice-tabs.js` chứa cả tab của Minh; thay đổi cấu trúc tab cần Minh Anh review giao diện.
- Shadowing gọi API chấm điểm và lưu lỗi từ do Minh phụ trách; thay đổi request/response cần hai người thống nhất.
- Card bài AI xuất hiện trong trang Khóa học nhưng dữ liệu lưu ở database; thay đổi schema cần Minh review.

## 2. Minh — database, auth và logic luyện tập

### Phạm vi chính

- Thiết kế database, EF Core mapping, schema/migration.
- Đăng ký, đăng nhập, cookie session, onboarding và phân quyền.
- Thanh toán/VIP. Hiện mới có demo activation trong Development/Testing và khung webhook; checkout production chưa hoàn thiện.
- Tab Nghe chép chính tả.
- Tab Ghép IPA.
- Chấm bài, lưu attempt/progress, nhận diện từ sai liên tiếp và đưa vào sổ từ.

### Tệp chính

| Khu vực | Tệp | Vai trò |
|---|---|---|
| Database | `Data/AppDbContext.cs`, `Models/*.cs`, `Database/*.sql` | Entity, quan hệ, constraint, schema update. |
| Auth/onboarding | `Controllers/AccountController.cs`, `Services/AuthService.cs`, `Views/Home/Authen.cshtml` | Login/register/logout, cookie, onboarding. |
| Guard | `Middleware/OnboardingGuardMiddleware.cs` | Chặn user chưa hoàn tất onboarding. |
| Practice backend | `Controllers/PracticeController.cs`, `Services/PracticeEvaluationService.cs` | Nhận audio/answer, chấm, persist attempt và progress. |
| Phát âm | `AzurePronunciationAssessmentService.cs`, `OpenAiPronunciationAssessmentService.cs`, `HybridPronunciationAssessmentService.cs` | Azure-first, OpenAI fallback theo cấu hình. |
| Sai liên tiếp | `Services/WordErrorTracker.cs`, `Services/VocabularyService.cs` | Theo dõi lỗi từ và tạo/cập nhật vocabulary. |
| Dictation/IPA | `wwwroot/js/lesson-practice-tabs.js`, `LanguageReferenceController.cs` | Điều khiển bài nghe chép, tạo/lưu IPA và chấm matching. |
| Payment | `PaymentController.cs`, `PaymentService.cs`, `SubscriptionService.cs`, `SubscriptionExpiryService.cs` | Checkout demo, webhook, entitlement và expiry. |

### Trạng thái thanh toán

- Đã có model subscription/transaction, API, demo checkout và adapter webhook.
- Production checkout cố ý trả `503`; chưa có order/redirect/QR sandbox end-to-end.
- Không được trình bày “thanh toán đã hoàn thiện”. Việc tiếp theo là sửa migration, tích hợp provider sandbox, test signature/idempotency và chỉ kích hoạt VIP sau webhook đã xác minh.

## 3. Hải Anh — nội dung học, tiến trình, tài khoản và kiểm thử

### Phạm vi chính

- Xây dựng/seed bài học trong database, material và transcript.
- Trang Tiến trình & Thẻ nhớ.
- Trang Tài khoản/cài đặt.
- Kịch bản test và bằng chứng kiểm thử.

### Tệp chính

| Khu vực | Tệp | Vai trò |
|---|---|---|
| Nội dung bài học | `Services/CourseService.cs`, `LessonContentService.cs`, `wwwroot/media/**`, SQL seed | Đọc khóa/bài/material/transcript và xây dữ liệu học. |
| Trang tiến trình | `Views/Home/Stats.cshtml`, `wwwroot/js/vocabulary-flashcard.js` | KPI, tim/EXP, flashcard vocabulary, favorites. |
| Stats/gamification | `UserStatsService.cs`, `GamificationService.cs`, `GamificationController.cs` | Read model và giao dịch EXP/tim/streak. |
| Trang tài khoản | `Views/Home/Settings.cshtml`, `wwwroot/js/settings.js` | Hồ sơ, learning setting, auto-save, theme và VIP UI. |
| Account backend | `UserController.cs`, `UserProfileService.cs`, `ModeChangeService.cs` | Cập nhật profile/settings và policy đổi mode. |
| Test | `WebShadowing.UnitTests/**`, `WebShadowing.AuthFlowTests/**`, `WebShadowing.DatabaseIntegrationTests/**` | Unit, integration, authorization, database và kịch bản hồi quy. |

## 4. Quy tắc làm việc với tệp dùng chung

1. Người phụ trách chính triển khai và tự kiểm tra acceptance criteria.
2. Tệp dùng chung phải có review của người phụ trách chức năng bị ảnh hưởng.
3. PR thay đổi API contract phải cập nhật đồng thời controller/service/DTO/JavaScript/test.
4. PR thay đổi schema phải kèm SQL hoặc migration, test nâng cấp dữ liệu cũ và hướng rollback.
5. PR giao diện phải kiểm tra desktop, tablet, mobile, light/dark, loading/empty/error.
6. Issue đóng chỉ xác nhận phạm vi trong acceptance criteria đã làm; không tự động có nghĩa toàn bộ module production-ready.

## 5. Ma trận review chéo

| Thay đổi | Người làm chính | Người cần review |
|---|---|---|
| Library/card/AI lesson UI | Minh Anh | Minh nếu đổi schema/API; Hải Anh nếu đổi nguồn bài |
| Shadowing recording/UI | Minh Anh | Minh review scoring/persistence/security |
| Dictation/IPA | Minh | Minh Anh review UI/UX; Hải Anh review dữ liệu câu |
| Auth/onboarding | Minh | Minh Anh review màn hình; Hải Anh bổ sung test |
| Database/schema | Minh | Hải Anh review seed/test migration |
| Stats/flashcard/account | Hải Anh | Minh review policy/API; Minh Anh review responsive |
| AI dialogue | Minh Anh | Minh review VIP/auth/privacy; Hải Anh viết test |
| Payment production | Minh | Hải Anh viết integration test; Minh Anh/Hải Anh review Settings UI |

## 6. Quy ước comment trong code

Các comment đầu tệp dùng mẫu:

```text
Chức năng: module/trang/luồng mà tệp phục vụ.
Phụ trách chính: tên thành viên.
Phối hợp: thành viên cần review nếu thay đổi contract dùng chung.
```

Comment chỉ mô tả ranh giới trách nhiệm và lý do nghiệp vụ. Không lặp lại từng câu lệnh hiển nhiên và không thay thế tài liệu API/test.
