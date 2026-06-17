using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace WebShadowing.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? "";
        }

        private static string CleanJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return "{}";
            text = text.Trim();
            
            // Remove markdown code fences if present
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(7);
            }
            else if (text.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(3);
            }
            
            if (text.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(0, text.Length - 3);
            }
            
            return text.Trim();
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API key Gemini chưa được cấu hình.");
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            var jsonPayload = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            
            var root = doc.RootElement;
            var text = root.GetProperty("candidates")[0]
                           .GetProperty("content")
                           .GetProperty("parts")[0]
                           .GetProperty("text")
                           .GetString();

            return CleanJson(text ?? "{}");
        }

        public async Task<WordMeaningResult> GetWordMeaningAsync(string word, string context)
        {
            var prompt = $@"Dịch nghĩa từ tiếng Anh sau đây sang tiếng Việt: ""{word}"".
Ngữ cảnh của câu chứa từ này (nếu có): ""{context}"".
Hãy trả về một đối tượng JSON có định dạng chính xác như sau:
{{
  ""meaning"": ""Dịch nghĩa tiếng Việt tự nhiên, ngắn gọn và chính xác theo ngữ cảnh"",
  ""ipa"": ""phiên âm IPA của từ, ví dụ /wɜːrk/"",
  ""wordType"": ""từ loại bằng tiếng Việt, ví dụ: Danh từ, Động từ, Tính từ""
}}";

            var rawJson = await CallGeminiApiAsync(prompt);
            return JsonSerializer.Deserialize<WordMeaningResult>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new WordMeaningResult();
        }

        public async Task<EvaluateResult> EvaluatePronunciationAsync(string targetText, string transcript)
        {
            var prompt = $@"Hãy đóng vai một chuyên gia chấm điểm phát âm tiếng Anh.
Câu chuẩn: ""{targetText}""
Người học đã nói và được nhận diện giọng nói thành: ""{transcript}""

Hãy phân tích và so sánh giữa câu chuẩn và những gì người dùng đã nói để chấm điểm.
Hãy trả về một đối tượng JSON có định dạng chính xác như sau:
{{
  ""score"": 85,
  ""accuracy"": 80,
  ""fluency"": 85,
  ""intonation"": 80,
  ""words"": [
    {{
      ""word"": ""từ_trong_câu_chuẩn"",
      ""accuracyCode"": ""correct"",
      ""ipa"": ""phiên âm IPA của từ này"",
      ""correction"": ""Nhận xét ngắn gọn bằng tiếng Việt về từ này, ví dụ: 'Tốt', 'Cần chú ý phụ âm cuối /s/', 'Chưa nói từ này'""
    }}
  ],
  ""feedback"": ""Lời khuyên, nhận xét chung ngắn gọn của AI Coach bằng tiếng Việt để giúp người dùng cải thiện (khoảng 1-2 câu).""
}}

Lưu ý:
1. Ở danh sách ""words"", hãy liệt kê ĐÚNG tất cả các từ xuất hiện trong câu chuẩn ""{targetText}"" (giữ nguyên thứ tự).
2. Tùy thuộc vào mức độ khớp của transcript so với câu chuẩn để đặt accuracyCode tương ứng: 'correct' (nếu nói đúng), 'warning' (nếu nói gần đúng/sai trọng âm), 'incorrect' (nếu nói sai hoàn toàn/bỏ qua từ đó).
3. Đảm bảo toàn bộ phản hồi bằng định dạng JSON hợp lệ.";

            var rawJson = await CallGeminiApiAsync(prompt);
            return JsonSerializer.Deserialize<EvaluateResult>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new EvaluateResult();
        }

        public async Task<RolePlayResult> GenerateRolePlayAsync(string topic, string level)
        {
            var prompt = $@"Hãy đề xuất đúng 2 vai trò đối thoại tiếng Anh phù hợp nhất với chủ đề sau:
Chủ đề: ""{topic}""
Cấp độ học: ""{level}""

Hãy trả về một đối tượng JSON có định dạng chính xác như sau:
{{
  ""speakers"": [""Tên vai trò A"", ""Tên vai trò B""]
}}
Yêu cầu:
1. Trả về đúng 2 vai trò đối thoại viết bằng tiếng Anh ngắn gọn (ví dụ: ""Guest"" và ""Receptionist"" cho việc nhận phòng khách sạn, hoặc ""Customer"" và ""Barista"" cho việc mua cà phê).
2. Đảm bảo toàn bộ phản hồi bằng định dạng JSON hợp lệ.";

            var rawJson = await CallGeminiApiAsync(prompt);
            return JsonSerializer.Deserialize<RolePlayResult>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new RolePlayResult();
        }

        public async Task<RolePlayMessageResult> GetNextRolePlayMessageAsync(string topic, string level, string userRole, List<ChatMessageHistoryItem> history)
        {
            var historyText = new StringBuilder();
            if (history != null && history.Count > 0)
            {
                foreach (var msg in history)
                {
                    historyText.AppendLine($"{(msg.IsUser ? "User" : "AI")} ({msg.Speaker}): {msg.Text}");
                }
            }
            else
            {
                historyText.AppendLine("(No messages yet. This is the start of the conversation.)");
            }

            var prompt = $@"Bạn đang tham gia một cuộc hội thoại nhập vai (Roleplay) bằng tiếng Anh giao tiếp tự nhiên.
Chủ đề: ""{topic}""
Cấp độ: ""{level}""
Vai của người dùng (User): ""{userRole}""
Vai của bạn (AI): Hãy tự đóng vai đối thoại đối ứng phù hợp nhất với người dùng (ví dụ: nếu người dùng đóng vai Customer, bạn sẽ đóng vai Barista).

Lịch sử cuộc hội thoại cho đến hiện tại:
{historyText}

Nhiệm vụ của bạn:
Hãy viết câu thoại tiếp theo của AI (bản thân bạn) để phản hồi lại người dùng hoặc bắt đầu cuộc hội thoại chào mừng nếu lịch sử trống.
Yêu cầu:
1. Phản hồi tự nhiên, ngắn gọn, phù hợp với chủ đề và cấp độ ""{level}"". Chỉ dài từ 1 đến 2 câu ngắn (khoảng 10-25 từ).
2. Dịch nghĩa tiếng Việt cho câu thoại đó.
3. Cung cấp phiên âm IPA chuẩn cho cả câu nói (nằm trong ngoặc vuông, ví dụ: [haɪ ðɛr]).
4. Trả về một đối tượng JSON có định dạng chính xác như sau:
{{
  ""speaker"": ""Tên vai của AI (ví dụ: Barista)"",
  ""text"": ""Câu thoại tiếng Anh của AI"",
  ""translation"": ""Bản dịch nghĩa tiếng Việt"",
  ""ipa"": ""[Phiên âm IPA của cả câu tiếng Anh nằm trong ngoặc vuông]""
}}
Đảm bảo toàn bộ phản hồi bằng định dạng JSON hợp lệ.";

            var rawJson = await CallGeminiApiAsync(prompt);
            return JsonSerializer.Deserialize<RolePlayMessageResult>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new RolePlayMessageResult();
        }

      public async Task<GeneratedLessonResult> GenerateLessonAsync(string theme, string level)
{
    var prompt = $@"Create an English shadowing lesson with 3 to 4 sentences based on the following topic and level:
Topic/Request: ""{theme}""
Level: ""{level}"" (e.g., Casual, Professional, Academic)

STRICT RULES — follow exactly:
1. ALL sentences in the ""sentences"" array must be 100% in English only.
2. Do NOT copy, translate, or embed the topic name into any sentence literally.
3. Do NOT mix Vietnamese words into any English sentence under any circumstances.
4. Each sentence must sound like natural spoken American English relevant to the topic.
5. The ""title"" field should be a short descriptive Vietnamese title for the lesson.
6. The ""translation"" field for each sentence must be a natural Vietnamese translation of that English sentence.
7. Space out startTime/endTime with ~1-2 second gaps between sentences (e.g. 0.0→5.0, 6.0→11.0, 12.0→17.0).

Return a valid JSON object in exactly this format:
{{
  ""title"": ""Tiêu đề bài học tiếng Việt phù hợp"",
  ""level"": ""Cấp độ tiếng Việt (ví dụ: Cơ bản, Trung cấp, Nâng cao)"",
  ""topic"": ""English level label (e.g. Professional)"",
  ""sentences"": [
    {{
      ""id"": ""gen-s1"",
      ""text"": ""Complete English sentence only — no Vietnamese words"",
      ""translation"": ""Bản dịch tiếng Việt của câu trên"",
      ""ipa"": ""[IPA phonetic transcription of the English sentence]"",
      ""startTime"": 0.0,
      ""endTime"": 5.0
    }}
  ]
}}

Ensure the entire response is valid JSON only. No markdown, no explanation outside the JSON.";

    var rawJson = await CallGeminiApiAsync(prompt);
    
    var result = JsonSerializer.Deserialize<GeneratedLessonResult>(
        rawJson, 
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    ) ?? new GeneratedLessonResult();

    // Validate: reject any sentence containing Vietnamese characters
    result.Sentences = result.Sentences
        .Where(s => !HasVietnameseChars(s.Text))
        .ToList();

    return result;
}

// Thêm helper method này vào class GeminiService
private static bool HasVietnameseChars(string text)
{
    if (string.IsNullOrEmpty(text)) return false;
    return text.Any(c => "đăơưịọảẻẽỉõùúủũụấầẩẫậắặẵẳồổỗộớờởỡợếềểễệàáâãèéêìíòóôõùúýăđơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỷỹỵ"
        .Contains(char.ToLower(c)));
}
    }
    public class WordMeaningResult
    {
        public string Meaning { get; set; } = string.Empty;
        public string Ipa { get; set; } = string.Empty;
        public string WordType { get; set; } = string.Empty;
    }

    public class EvaluateResult
    {
        public int Score { get; set; }
        public int Accuracy { get; set; }
        public int Fluency { get; set; }
        public int Intonation { get; set; }
        public List<WordGradeResult> Words { get; set; } = new();
        public string Feedback { get; set; } = string.Empty;
    }

    public class WordGradeResult
    {
        public string Word { get; set; } = string.Empty;
        public string AccuracyCode { get; set; } = string.Empty;
        public string Ipa { get; set; } = string.Empty;
        public string Correction { get; set; } = string.Empty;
    }

    public class RolePlayResult
    {
        public List<RolePlayMessageResult> Messages { get; set; } = new();
        public List<string> Speakers { get; set; } = new();
    }

    public class RolePlayMessageResult
    {
        public string Id { get; set; } = string.Empty;
        public string Speaker { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Ipa { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public bool IsPlayed { get; set; }
    }

    public class ChatMessageHistoryItem
    {
        public string Speaker { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsUser { get; set; }
    }

    public class GeneratedLessonResult
    {
        public string Title { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public List<GeneratedSentenceResult> Sentences { get; set; } = new();
    }

    public class GeneratedSentenceResult
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Ipa { get; set; } = string.Empty;
        public double StartTime { get; set; }
        public double EndTime { get; set; }
    }
}
