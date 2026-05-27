# language-learning-app
Web application for assistance with language learning

# Web App Luyện Nói Tiếng Anh Theo Phương Pháp Shadowing Tích Hợp Trí Tuệ Nhân Tạo

Chào mừng bạn đến với kho lưu trữ mã nguồn của dự án **Web App Luyện Nói Tiếng Anh theo phương pháp Shadowing**. Đây là một nền tảng hiện đại ứng dụng Trí tuệ nhân tạo (AI) giúp người học cải thiện kỹ năng phát âm, ngữ điệu và phản xạ tiếng Anh thông qua kỹ thuật nói đuổi (Shadowing).

---

## Tổng Quan Đề Tài

Cốt lõi của phương pháp Shadowing là người dùng sẽ nghe một đoạn âm thanh mẫu (giọng chuẩn), sau đó đồng thời hoặc ngay lập tức lặp lại (như một cái bóng). Hệ thống này hỗ trợ người dùng bằng cách tự động ghi âm, chuyển đổi giọng nói thành văn bản, so sánh với văn bản gốc và sử dụng Mô hình ngôn ngữ lớn (LLM) để đánh giá chi tiết.

### Quy trình cốt lõi:
1. **Nghe:** Người dùng nghe giọng đọc chuẩn từ hệ thống.
2. **Nói & Thu âm:** Người dùng thực hiện shadowing và hệ thống ghi âm trực tiếp qua trình duyệt.
3. **Phân tích:** AI tiến hành chuyển đổi giọng nói thành văn bản (STT).
4. **Đánh giá:** LLM so sánh transcript với văn bản gốc để chấm điểm và đưa ra nhận xét chi tiết.

---

## Kiến Trúc Hệ Thống & Công Nghệ Sử Dụng

### 1. Trí Tuệ Nhân Tạo (AI & API Integration)
* **Text-to-Speech (TTS):** Sử dụng các tính năng tích hợp sẵn trên trình duyệt (Web Speech API) chạy hoàn toàn bằng JavaScript ở phía Client để tối ưu hiệu năng và không tốn chi phí API, hoặc tùy chọn cấu hình mở rộng với *OpenAI TTS* / *Google Cloud Text-to-Speech*.
* **Speech-to-Text (STT):** Tích hợp **OpenAI Whisper API** hoặc **Google Speech-to-Text** để chuyển đổi file ghi âm của người dùng thành văn bản (Transcript) một cách chính xác.
* **Chấm điểm & Nhận xét (LLM):** Sử dụng **Google Gemini API** hoặc **GPT-4o-mini**. Hệ thống sẽ đưa văn bản gốc kèm transcript của người dùng vào prompt thiết kế sẵn để AI nhận diện các từ phát âm sai, từ bị bỏ sót và đưa ra lời khuyên cải thiện cụ thể.

### 2. Frontend (Giao diện người dùng)


### 3. Backend (Xử lý nghiệp vụ)
* **Framework:** **C# (.NET Core Web API)**.
* **Đặc điểm:** Xây dựng cấu trúc hệ thống chặt chẽ, hướng đối tượng (OOP) rõ ràng, hiệu năng xử lý request cao, bảo mật và tích hợp hoàn hảo với hệ sinh thái cơ sở dữ liệu.

### 4. Database (Cơ sở dữ liệu)
* **Hệ quản trị CSDL:** **Microsoft SQL Server**.
* **Chức năng:** Lưu trữ thông tin tài khoản người dùng, danh sách các bài học/chủ đề tiếng Anh, lịch sử luyện tập chi tiết và bảng điểm số/xếp hạng.

### 5. Thiết kế UI/UX
* **Figma**
