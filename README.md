

# ShadowSpeak AI - Web App Luyện Nói Tiếng Anh Theo Phương Pháp Shadowing Tích Hợp Trí Tuệ Nhân Tạo

**ShadowSpeak AI** là nền tảng hiện đại ứng dụng Trí tuệ nhân tạo (AI) kết hợp kiến trúc truyền tải dữ liệu thời gian thực giúp người học cải thiện kỹ năng phát âm, ngữ điệu và phản xạ tiếng Anh thông qua kỹ thuật nói đuổi (Shadowing).

---

## 🌟 Tổng Quan Đề Tài & Điểm Đột Phá Công Nghệ

Cốt lõi của phương pháp Shadowing là người học sẽ nghe một đoạn âm thanh mẫu (giọng chuẩn), sau đó lập tức lặp lại (như một cái bóng) để rèn luyện cơ miệng và phản xạ cơ mặt. 

### 🛑 Hạn chế của các hệ thống cũ
* Sử dụng giọng đọc nhân tạo (TTS) đều đều, vô cảm, thiếu ngữ điệu tự nhiên.
* Cơ chế ghi âm ngắt quãng: Thu âm thành file $\rightarrow$ Gửi file lên Server $\rightarrow$ Chờ phản hồi (gây độ trễ cao, ngắt mạch học tập).

### ✨ Điểm đột phá của ShadowSpeak AI
1. **Âm thanh có ngữ điệu tự nhiên (Prosody/Intonation):** Tích hợp luồng âm thanh thế hệ mới giúp giọng đọc mẫu của AI có đầy đủ cảm xúc, nhấn nhá, lên giọng xuống giọng y hệt người thật bản xứ (Accent: US English).
2. **Giao tiếp luồng song hướng thời gian thực (Bi-directional Streaming):** Loại bỏ hoàn toàn cơ chế thu âm bằng file thủ công. Hệ thống thiết lập đường truyền WebSockets song hướng chạy liên tục; giọng nói từ Micro của người học được băm nhỏ thành các gói dữ liệu và đẩy liên tục lên AI Server để phân tích ngay lập tức.

---

## 📐 Sơ Đồ Kiến Trúc Luồng Dữ Liệu Thời Gian Thực (System Architecture)

Dự án áp dụng mô hình **WebSocket Proxy Gateway** bảo mật. Client giao tiếp với Backend C# thông qua giao thức SignalR/WebSockets nội bộ, Backend đóng vai trò trung chuyển luồng Stream nhị phân lên Google AI Server nhằm bảo mật tuyệt đối API Key.

```mermaid
sequenceDiagram
    autonumber
    actor User as Người Học (Browser)
    participant Client as Frontend (AudioContext/JS)
    participant Server as Backend (.NET 8 Core MVC Proxy)
    participant Gemini as Google AI Server (Gemini Live API)

    User->>Client: Bật phòng luyện tập (Microphone On)
    Client->>Server: Khởi tạo kết nối song hướng (WebSocket Connection)
    Server->>Gemini: Thiết lập BidiGenerateContent Stream (wss://...)
    Gemini-->>Server: Trả về luồng âm thanh mẫu (Audio Stream có ngữ điệu)
    Server-->>Client: Trung chuyển gói âm thanh mẫu
    Client-->>User: Phát âm thanh mẫu ra Loa/Tai nghe
    
    User->>Client: Thực hiện nói đuổi (Shadowing hành động nói)
    Client->>Client: Băm nhỏ sóng âm từ Mic thành PCM 16-bit / 24kHz
    Client->>Server: Stream gói dữ liệu âm thanh thô liên tục (Binary Chunks)
    Server->>Gemini: Đẩy luồng dữ liệu âm thanh của người học lên AI Core
    Gemini->>Gemini: Phân tích sóng âm & Đối chiếu dữ liệu gốc (IPA)
    Gemini-->>Server: Trả về kết quả đánh giá chi tiết (Phát âm sai, Fluency, Chấm điểm)
    Server-->>Client: Đổ dữ liệu JSON kết quả ra màn hình
    Client-->>User: Hiển thị từ vựng phát âm sai (Màu đỏ) và điểm số thực tế

```

---

## 🎨 Tiêu Chuẩn Thiết Kế UI/UX & Hệ Thống Nhận Diện (Design System)

Giao diện được nghiên cứu cấu trúc kỹ lưỡng trên công cụ thiết kế chuyên nghiệp Figma, áp dụng các tiêu chuẩn thiết kế hiện đại phục vụ tối đa cho ứng dụng EdTech.

### 1. Nguyên tắc thiết kế (Design Principles)

* **Trực quan hóa dữ liệu (Data Visualization):** Sử dụng các biểu đồ tiến trình học tập, chuỗi ngày học liên tiếp (Streak) bằng đồ họa sống động để kích thích động lực học tập (Gamification).
* **Thiết kế tập trung (Focus-Oriented UI):** Phòng luyện tập Shadowing được tối giản hóa các thành phần gây xao nhãng, giúp người học tập trung hoàn toàn vào tai nghe và khẩu hình phát âm.
* **Tương phản chuẩn WCAG 2.1:** Đảm bảo độ tương phản chữ và nền đạt chuẩn AA, giúp người dùng không bị mỏi mắt khi học tập trong thời gian dài.

### 2. Thông số kỹ thuật Design System (Đã hiện thực hóa qua Tailwind CSS)

#### Bảng màu chủ đạo (Color Palette)

| Thành phần | Mã màu (HEX) | Vai trò trò / Ý nghĩa |
| --- | --- | --- |
| **Primary Color** | `#4F46E5` | Indigo Premium - Đại diện cho công nghệ, trí tuệ nhân tạo. |
| **Neutral Slate** | `#E2E8F0` / `#1D293D` | Slate Gray - Sử dụng cho viền/nhãn dán và màu chữ. Tạo cảm giác lì, tinh tế, hiện đại. |
| **Gamification** | `#F59E0B` / `#F43F5E` | Orange Amber (Streak lửa) và Rose Crimson (Điểm số Tim). |

#### Hệ Thống Phông Chữ (Typography)

* **Font hiển thị tiêu đề:** `Space Grotesk` (Mang phong cách hình khối công nghệ, góc cạnh).
* **Font văn bản hệ thống:** `Inter` (Font chữ quốc dân tối ưu hóa hiển thị sắc nét trên mọi kích thước màn hình).
* **Font phiên âm quốc tế:** `JetBrains Mono` (Font chữ đơn cách - Monospace, giúp các ký tự phiên âm IPA đứng thẳng hàng, dễ đọc).

> 💡 **Giao diện đa nền văn minh:** Tích hợp bộ lọc chế độ Light/Dark Mode thời gian thực, đồng bộ tự động dựa theo cấu hình hệ điều hành của người dùng (`prefers-color-scheme`).

---

## 🛠️ Kiến Trúc Mã Nguồn & Công Nghệ Sử Dụng

### 1. Các công nghệ cốt lõi

* **Frontend Engine:** HTML5 Media Devices API, Tailwind CSS Engine CDN v3, Razor View Engine, Lucide Icons.
* **Backend Framework:** ASP.NET Core MVC (.NET 8.0) áp dụng nguyên lý Dependency Injection (DI) và lập trình bất đồng bộ (`async`/`await`).
* **Database Management:** Microsoft SQL Server kết hợp Entity Framework Core (Code-First) quản lý cơ sở dữ liệu quan hệ chặt chẽ.

### 2. Cấu Trúc Thư Mục Dự Án Chuẩn Doanh Nghiệp

```plaintext
/ (Workspace Root)
├── Controllers/
│   ├── AccountController.cs   <- Quản lý luồng Đăng nhập, Đăng ký, và Gói Premium
│   ├── ApiController.cs       <- Gateway kết nối luồng Live Stream WebSockets với Gemini AI
│   └── HomeController.cs      <- Điều hướng danh mục Khóa học, Thống kê, và Cài đặt
├── Models/
│   └── CourseModels.cs        <- Định nghĩa cấu trúc thực thể dữ liệu (Lesson, Sentence, UserProfile)
├── Services/
│   ├── GeminiService.cs       <- Xử lý logic kết nối API và truyền tải dữ liệu luồng âm thanh
│   └── LessonService.cs       <- Quản lý kho bài học SGK tĩnh và bài học do AI sinh ra
├── Views/
│   ├── _ViewImports.cshtml    <- Đăng ký Namespaces toàn cục và Razor Tag Helpers
│   ├── _ViewStart.cshtml      <- Định nghĩa Layout sườn mặc định của hệ thống View
│   ├── Account/               <- Giao diện Login, Register, Thiết lập lộ trình học cá nhân
│   ├── Home/                  <- Giao diện trang chủ Khóa học, Thống kê SRS, Phòng luyện nói Shadowing
│   └── Shared/
│       └── _Layout.cshtml     <- Thanh điều hướng đồng bộ (Streak, Hearts, EXP) và Darkmode
├── wwwroot/
│   ├── css/site.css           <- Custom CSS & cấu hình tối ưu hóa hiển thị giao diện
│   └── js/site.js             <- Tiện ích JavaScript hỗ trợ tương tác âm thanh thô
├── Program.cs                 <- Đăng ký DI, cấu hình Middleware Pipeline, và Routing chính
└── ShadowSpeakMvc.csproj      <- Quản lý các gói thư viện NuGet phụ thuộc hệ thống

```

---

## 🚀 Hướng Dẫn Cài Đặt & Khởi Chạy Dự Án (Local Development)

### Yêu cầu hệ thống

* .NET 8.0 SDK trở lên.
* Microsoft SQL Server.
* Một mã cấu hình Gemini API Key hợp lệ từ Google AI Studio.

### Các bước triển khai

**Bước 1: Sao chép mã nguồn về máy cá nhân**

```bash
git clone [https://github.com/your-username/ShadowSpeakMvc.git](https://github.com/your-username/ShadowSpeakMvc.git)
cd ShadowSpeakMvc

```

**Bước 2: Cấu hình môi trường** Mở file `appsettings.json` và điền mã API Key bảo mật của bạn vào:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Gemini": {
    "ApiKey": "MÃ_API_KEY_GEMINI_CỦA_BẠN_TẠI_ĐÂY"
  }
}

```

**Bước 3: Khôi phục các thư viện phụ thuộc (NuGet Packages)**

```bash
dotnet restore

```

**Bước 4: Biên dịch và chạy dự án**

```bash
dotnet watch run

```

> 🌐 **Kết quả:** Ứng dụng sẽ tự động kích hoạt trình duyệt và lắng nghe tại đường dẫn mặc định: `http://localhost:5000` hoặc `https://localhost:5001`.

```

```
