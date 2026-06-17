using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers
{
    public class HomeController : Controller
    {
        private readonly GeminiService _geminiService;

        public HomeController(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        private static readonly Dictionary<string, (string meaning, string ipa, string wordType)> CommonDict = new()
    {
        { "i", ("Tôi, mình", "/aɪ/", "Đại từ") },
        { "you", ("Bạn, bạn ấy", "/juː/", "Đại từ") },
        { "he", ("Anh ấy", "/hiː/", "Đại từ") },
        { "she", ("Cô ấy", "/ʃiː/", "Đại từ") },
        { "we", ("Chúng tôi", "/wiː/", "Đại từ") },
        { "they", ("Họ, chúng nó", "/ðeɪ/", "Đại từ") },
        { "is", ("Là, ở (to be)", "/ɪz/", "Động từ") },
        { "are", ("Là, ở (to be)", "/ɑːr/", "Động từ") },
        { "am", ("Là, ở (to be)", "/æm/", "Động từ") },
        { "the", ("Mạo từ xác định", "/ðə/", "Mạo từ") },
        { "a", ("Một (mạo từ)", "/ə/", "Mạo từ") },
        { "an", ("Một (mạo từ)", "/ən/", "Mạo từ") },
        { "and", ("Và", "/ænd/", "Liên từ") },
        { "or", ("Hoặc", "/ɔːr/", "Liên từ") },
        { "but", ("Nhưng", "/bʌt/", "Liên từ") },
        { "not", ("Không", "/nɒt/", "Trạng từ") },
        { "in", ("Trong, ở trong", "/ɪn/", "Giới từ") },
        { "on", ("Trên", "/ɒn/", "Giới từ") },
        { "at", ("Tại, ở", "/æt/", "Giới từ") },
        { "to", ("Đến, để", "/tuː/", "Giới từ") },
        { "for", ("Cho, vì", "/fɔːr/", "Giới từ") },
        { "with", ("Với, cùng", "/wɪð/", "Giới từ") },
        { "from", ("Từ", "/frɒm/", "Giới từ") },
        { "have", ("Có", "/hæv/", "Động từ") },
        { "has", ("Có (ngôi 3)", "/hæz/", "Động từ") },
        { "do", ("Làm", "/duː/", "Động từ") },
        { "does", ("Làm (ngôi 3)", "/dʌz/", "Động từ") },
        { "can", ("Có thể", "/kæn/", "Động từ khiếm khuyết") },
        { "will", ("Sẽ", "/wɪl/", "Động từ khiếm khuyết") },
        { "would", ("Sẽ (lịch sự)", "/wʊd/", "Động từ khiếm khuyết") },
        { "should", ("Nên", "/ʃʊd/", "Động từ khiếm khuyết") },
        { "what", ("Cái gì, gì", "/wʌt/", "Đại từ nghi vấn") },
        { "where", ("Ở đâu", "/wɛr/", "Trạng từ nghi vấn") },
        { "when", ("Khi nào", "/wɛn/", "Trạng từ nghi vấn") },
        { "how", ("Như thế nào", "/haʊ/", "Trạng từ nghi vấn") },
        { "why", ("Tại sao", "/waɪ/", "Trạng từ nghi vấn") },
        { "who", ("Ai", "/huː/", "Đại từ nghi vấn") },
        { "this", ("Cái này", "/ðɪs/", "Đại từ chỉ định") },
        { "that", ("Cái đó", "/ðæt/", "Đại từ chỉ định") },
        { "my", ("Của tôi", "/maɪ/", "Tính từ sở hữu") },
        { "your", ("Của bạn", "/jɔːr/", "Tính từ sở hữu") },
        { "good", ("Tốt, giỏi", "/ɡʊd/", "Tính từ") },
        { "very", ("Rất", "/ˈvɛri/", "Trạng từ") },
        { "like", ("Thích", "/laɪk/", "Động từ") },
        { "go", ("Đi", "/ɡoʊ/", "Động từ") },
        { "come", ("Đến", "/kʌm/", "Động từ") },
        { "know", ("Biết", "/noʊ/", "Động từ") },
        { "think", ("Nghĩ, suy nghĩ", "/θɪŋk/", "Động từ") },
        { "want", ("Muốn", "/wɒnt/", "Động từ") },
        { "see", ("Nhìn, thấy", "/siː/", "Động từ") },
        { "make", ("Làm, tạo", "/meɪk/", "Động từ") },
        { "time", ("Thời gian", "/taɪm/", "Danh từ") },
        { "people", ("Người, mọi người", "/ˈpiːpl/", "Danh từ") },
        { "school", ("Trường học", "/skuːl/", "Danh từ") },
        { "family", ("Gia đình", "/ˈfæməli/", "Danh từ") },
        { "life", ("Cuộc sống", "/laɪf/", "Danh từ") },
        { "work", ("Công việc, làm việc", "/wɜːrk/", "Danh từ/Động từ") },
        { "morning", ("Buổi sáng", "/ˈmɔːrnɪŋ/", "Danh từ") },
        { "today", ("Hôm nay", "/təˈdeɪ/", "Trạng từ") },
        { "about", ("Về, khoảng", "/əˈbaʊt/", "Giới từ") },
        { "hello", ("Xin chào", "/hɛˈloʊ/", "Thán từ") },
        { "thank", ("Cảm ơn", "/θæŋk/", "Động từ") },
        { "please", ("Xin vui lòng", "/pliːz/", "Trạng từ") },
        { "yes", ("Vâng, có", "/jɛs/", "Thán từ") },
        { "no", ("Không", "/noʊ/", "Trạng từ") },
        { "name", ("Tên", "/neɪm/", "Danh từ") },
        { "student", ("Học sinh, sinh viên", "/ˈstjuːdnt/", "Danh từ") },
        { "teacher", ("Giáo viên", "/ˈtiːtʃər/", "Danh từ") },
        { "english", ("Tiếng Anh", "/ˈɪŋɡlɪʃ/", "Danh từ/Tính từ") },
        { "nice", ("Đẹp, dễ chịu", "/naɪs/", "Tính từ") },
        { "meet", ("Gặp gỡ", "/miːt/", "Động từ") },
        { "new", ("Mới", "/njuː/", "Tính từ") },
        { "also", ("Cũng", "/ˈɔːlsoʊ/", "Trạng từ") },
        { "sure", ("Chắc chắn", "/ʃʊr/", "Tính từ") },
        { "great", ("Tuyệt vời", "/ɡreɪt/", "Tính từ") },
        { "love", ("Yêu, thích", "/lʌv/", "Động từ") },
        { "really", ("Thực sự", "/ˈrɪəli/", "Trạng từ") },
        { "just", ("Chỉ, vừa mới", "/dʒʌst/", "Trạng từ") },
        { "some", ("Một vài, một số", "/sʌm/", "Tính từ") },
        { "more", ("Nhiều hơn", "/mɔːr/", "Tính từ/Trạng từ") },
        { "every", ("Mỗi, mọi", "/ˈɛvri/", "Tính từ") }
    };

    private void PopulateLayoutData()
    {
        ViewBag.UserProfile = StaticData.DefaultProfile;
        ViewBag.UserStats = StaticData.DefaultStats;
    }

    public IActionResult Index()
    {
        PopulateLayoutData();
        ViewBag.Lessons = StaticData.StaticLessons;
        ViewBag.Textbooks = StaticData.Textbooks;
        ViewBag.VideoLessons = StaticData.VideoLessons;
        ViewBag.SavedAILessons = StaticData.SavedAILessons;
        return View(StaticData.StaticLessons);
    }

    public IActionResult LessonDetail(string id)
    {
        PopulateLayoutData();
        
        // 1. Search in static lessons
        var lesson = StaticData.StaticLessons.Find(l => l.Id == id);
        
        // 2. Search in textbook units
        if (lesson == null)
        {
            foreach (var tb in StaticData.Textbooks)
            {
                var unit = tb.Units.Find(u => u.Id == id);
                if (unit != null)
                {
                    lesson = unit;
                    break;
                }
            }
        }
        
        // 3. Search in saved AI lessons
        if (lesson == null)
        {
            lesson = StaticData.SavedAILessons.Find(l => l.Id == id);
        }

        // 4. Search in video lessons (convert to Lesson if found)
        if (lesson == null)
        {
            var video = StaticData.VideoLessons.Find(v => v.Id == id);
            if (video != null)
            {
                lesson = new Lesson
                {
                    Id = video.Id,
                    Title = video.Title,
                    Level = video.Level,
                    Topic = video.Topic,
                    Duration = video.Duration,
                    YoutubeId = video.YoutubeId,
                    Sentences = new List<Sentence>()
                };

                // Add subtitles as sentences
                for (int i = 0; i < video.Subtitles.Count; i++)
                {
                    var text = video.Subtitles[i];
                    var trans = i == 0 ? "Xin chào! Chào mừng bạn đến với The Coffee House, tôi có thể lấy gì cho bạn?" : "Chào! Tôi muốn gọi một cốc latte yến mạch cỡ lớn mang đi, làm ơn.";
                    var ipa = i == 0 ? "[hɛˈloʊ ðɛr ˈwɛlkəm tuː ðə ˈkɔːfi haʊs wʌt kæn aɪ ɡɛt juː]" : "[haɪ aɪ wʊd laɪk tuː ˈɔːrdər ə lɑːrdʒ ˈoʊtmiːl ˈlɑːteɪ tuː ɡoʊ pliːz]";
                    lesson.Sentences.Add(new Sentence
                    {
                        Id = $"vid-s-{i}",
                        Text = text,
                        Translation = trans,
                        Ipa = ipa,
                        StartTime = i * 5,
                        EndTime = (i + 1) * 5
                    });
                }
            }
        }

        if (lesson == null)
        {
            return RedirectToAction("Index");
        }
        
        return View(lesson);
    }

    public IActionResult Stats()
    {
        PopulateLayoutData();
        ViewBag.Flashcards = StaticData.SampleFlashcards;
        ViewBag.Favorites = StaticData.SampleFavorites;
        return View();
    }

    public IActionResult Settings()
    {
        PopulateLayoutData();
        return View();
    }

    public IActionResult Auth()
    {
        PopulateLayoutData();
        return View();
    }

    private static string CleanPunctuation(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var sb = new System.Text.StringBuilder();
        foreach (char c in input)
        {
            if (!char.IsPunctuation(c) && c != '`' && c != '^' && c != '~' && c != '=' && c != '+' && c != '<' && c != '>')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    [HttpPost("api/evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.TargetText))
        {
            return BadRequest(new { error = "Missing targetText parameter" });
        }

        try
        {
            var result = await _geminiService.EvaluatePronunciationAsync(request.TargetText, request.Transcript ?? "");
            return Json(new {
                score = result.Score,
                accuracy = result.Accuracy,
                fluency = result.Fluency,
                intonation = result.Intonation,
                words = result.Words.Select(w => new {
                    word = w.Word,
                    accuracyCode = w.AccuracyCode,
                    ipa = w.Ipa,
                    correction = w.Correction
                }).ToList(),
                feedback = result.Feedback
            });
        }
        catch (Exception ex)
        {
            // Fallback to old mock behavior
            var targetText = request.TargetText;
            var transcript = request.Transcript ?? "";

            var cleanTarget = CleanPunctuation(targetText.ToLower()).Trim();
            var cleanSpoke = CleanPunctuation(transcript.ToLower()).Trim();

            var targetWords = CleanPunctuation(targetText)
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var spokeWords = cleanSpoke.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var spokeSet = new HashSet<string>(spokeWords);

            double matchedCount = 0;
            var wordsResult = new List<WordGrade>();

            foreach (var w in targetWords)
            {
                var cleanWord = w.ToLower();
                var isMatched = spokeSet.Contains(cleanWord);
                string code = "incorrect";
                string label = "Cần nói rõ";

                if (isMatched)
                {
                    matchedCount++;
                    code = "correct";
                    label = "Tốt";
                }
                else
                {
                    var closeMatch = spokeWords.FirstOrDefault(sw => sw.StartsWith(cleanWord.Length >= 3 ? cleanWord.Substring(0, 3) : cleanWord) 
                        || cleanWord.StartsWith(sw.Length >= 3 ? sw.Substring(0, 3) : sw));
                    if (closeMatch != null)
                    {
                        code = "warning";
                        label = "Chú ý trọng âm";
                        matchedCount += 0.5;
                    }
                }

                wordsResult.Add(new WordGrade
                {
                    Word = w,
                    AccuracyCode = code,
                    Ipa = $"/{cleanWord}/",
                    Correction = label
                });
            }

            double matchRatio = targetWords.Length > 0 ? (matchedCount / targetWords.Length) : 1;
            int score = (int)Math.Round(62 + (matchRatio * 33));
            int accuracy = (int)Math.Round(65 + (matchRatio * 30));
            int fluency = cleanSpoke.Length > 0 ? (int)Math.Round(60 + (matchRatio * 32)) : 50;
            int intonation = cleanSpoke.Length > 0 ? (int)Math.Round(58 + (matchRatio * 34)) : 50;

            return Json(new {
                score = score,
                accuracy = accuracy,
                fluency = fluency,
                intonation = intonation,
                words = wordsResult.Select(w => new {
                    word = w.Word,
                    accuracyCode = w.AccuracyCode,
                    ipa = w.Ipa,
                    correction = w.Correction
                }).ToList(),
                feedback = $"[MÔ PHỎNG PHÂN TÍCH] Kết nối Gemini tạm thời gián đoạn. Tỷ lệ khớp từ vựng: {Math.Round(matchRatio * 100)}%. Chi tiết lỗi: {ex.Message}"
            });
        }
    }

    [HttpPost("api/word-meaning")]
    public async Task<IActionResult> WordMeaning([FromBody] WordMeaningRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Word))
        {
            return BadRequest(new { error = "Missing word parameter" });
        }

        var cleanWord = request.Word.Trim().ToLower();
        cleanWord = CleanPunctuation(cleanWord);

        try
        {
            var result = await _geminiService.GetWordMeaningAsync(cleanWord, request.Context ?? "");
            return Json(new {
                meaning = result.Meaning,
                ipa = result.Ipa,
                wordType = result.WordType
            });
        }
        catch
        {
            if (CommonDict.TryGetValue(cleanWord, out var value))
            {
                return Json(new {
                    meaning = value.meaning,
                    ipa = value.ipa,
                    wordType = value.wordType
                });
            }

            return Json(new {
                meaning = $"Từ \"{cleanWord}\" — hãy nâng cấp VIP hoặc kết nối Internet để tra nghĩa mở rộng.",
                ipa = $"/{cleanWord}/",
                wordType = "Chưa xác định"
            });
        }
    }

    [HttpPost("api/generate-roleplay")]
    public async Task<IActionResult> GenerateRolePlay([FromBody] GenerateRolePlayRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Topic))
        {
            return BadRequest(new { error = "Missing topic parameter" });
        }

        var topic = request.Topic;

        try
        {
            var result = await _geminiService.GenerateRolePlayAsync(topic, request.Level ?? "General");
            return Json(new {
                messages = new List<object>(),
                speakers = result.Speakers
            });
        }
        catch
        {
            return Json(new {
                messages = new List<object>(),
                speakers = new List<string> { "Customer", "Barista" }
            });
        }
    }

    [HttpPost("api/roleplay-chat")]
    public async Task<IActionResult> RolePlayChat([FromBody] RolePlayChatRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Topic))
        {
            return BadRequest(new { error = "Missing topic parameter" });
        }

        try
        {
            var historyItems = new List<ChatMessageHistoryItem>();
            if (request.History != null)
            {
                foreach (var h in request.History)
                {
                    historyItems.Add(new ChatMessageHistoryItem
                    {
                        Speaker = h.Speaker,
                        Text = h.Text,
                        IsUser = h.IsUser
                    });
                }
            }

            var nextMessage = await _geminiService.GetNextRolePlayMessageAsync(
                request.Topic,
                request.Level ?? "General",
                request.UserRole ?? "User",
                historyItems
            );

            return Json(new {
                id = $"rp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                speaker = nextMessage.Speaker,
                text = nextMessage.Text,
                translation = nextMessage.Translation,
                ipa = nextMessage.Ipa,
                isUser = false,
                isPlayed = false
            });
        }
        catch (Exception ex)
        {
            var fallback = new {
                id = $"rp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                speaker = "Partner",
                text = "That sounds interesting! Let's continue talking about this topic.",
                translation = "Nghe thú vị đấy! Hãy tiếp tục nói về chủ đề này nhé.",
                ipa = "[ðæt saʊndz ˈɪntrəstɪŋ lɛts kənˈtɪnjuː ˈtɔːkɪŋ əˈbaʊt ðɪs ˈtɑːpɪk]",
                isUser = false,
                isPlayed = false
            };
            return Json(fallback);
        }
    }

    [HttpPost("api/generate-lesson")]
    public async Task<IActionResult> GenerateLesson([FromBody] GenerateLessonRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Theme))
        {
            return BadRequest(new { error = "Missing theme parameter" });
        }

        var theme = request.Theme;
        var level = request.Level ?? "Casual";
        var difficulty = level == "Academic" ? "Nâng cao" : level == "Professional" ? "Trung cấp" : "Cơ bản";

        try
        {
            var result = await _geminiService.GenerateLessonAsync(theme, level);
            
            var newLesson = new Lesson
            {
                Id = $"gen-ai-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                Title = string.IsNullOrEmpty(result.Title) ? $"Hội thoại: {theme}" : result.Title,
                Level = string.IsNullOrEmpty(result.Level) ? difficulty : result.Level,
                Topic = string.IsNullOrEmpty(result.Topic) ? level : result.Topic,
                Duration = $"0:{result.Sentences.Count * 6}",
                IsGenerated = true,
                Sentences = new List<Sentence>()
            };

            var sentencesList = new List<object>();
            for (int i = 0; i < result.Sentences.Count; i++)
            {
                var s = result.Sentences[i];
                var sentenceId = string.IsNullOrEmpty(s.Id) ? $"gen-s-{i + 1}" : s.Id;
                
                newLesson.Sentences.Add(new Sentence
                {
                    Id = sentenceId,
                    Text = s.Text,
                    Translation = s.Translation,
                    Ipa = s.Ipa,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                });

                sentencesList.Add(new
                {
                    id = sentenceId,
                    text = s.Text,
                    translation = s.Translation,
                    ipa = s.Ipa,
                    startTime = s.StartTime,
                    endTime = s.EndTime
                });
            }

            StaticData.SavedAILessons.Add(newLesson);

            return Json(new {
                id = newLesson.Id,
                title = newLesson.Title,
                level = newLesson.Level,
                topic = newLesson.Topic,
                sentences = sentencesList,
                isGenerated = true
            });
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[GenerateLesson ERROR] {ex.Message}");
            var sentences = new List<object>
            {
                new {
                    id = $"gen-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-1",
                    text = "Effective communication is the key to success in any workplace.",
                    translation = "Giao tiếp hiệu quả là chìa khóa dẫn đến thành công trong bất kỳ môi trường làm việc nào.",
                    ipa = "[ɪˈfɛktɪv kəˌmjuːnɪˈkeɪʃən ɪz ðə kiː tuː səkˈsɛs ɪn ˈɛni ˈwɜːrkpleɪs]",
                    startTime = 0.0,
                    endTime = 5.0
                },
                new {
                    id = $"gen-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-2",
                    text = "It helps me communicate more effectively with foreigners.",
                    translation = "Nó giúp tôi giao tiếp hiệu quả hơn với người nước ngoài.",
                    ipa = "[ɪt hɛlps miː kəˈmjuːnɪkeɪt mɔːr ɪˈfɛktɪvli wɪð ˈfɔːrənərz]",
                    startTime = 6.0,
                    endTime = 11.0
                },
                new {
                    id = $"gen-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-3",
                    text = "Let's practice shadowing this dialog together every day.",
                    translation = "Hãy cùng nhau luyện nhại đoạn thoại này mỗi ngày nhé.",
                    ipa = "[lɛts ˈpræktɪs ˈʃædoʊɪŋ ðɪs ˈdaɪəlɔːɡ təˈɡɛðər ˈɛvri deɪ]",
                    startTime = 12.0,
                    endTime = 17.0
                }
            };

            var newLesson = new Lesson
            {
                Id = $"gen-ai-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                Title = "Hội thoại luyện tập tiếng Anh",  
                Level = difficulty,
                Topic = level,
                Duration = "0:25",
                IsGenerated = true,
                Sentences = new List<Sentence>
                {
                    new Sentence {
                        Id = "gen-s1",
                        Text = "Effective communication is the key to success in any workplace.",
                        Translation = "Giao tiếp hiệu quả là chìa khóa dẫn đến thành công trong bất kỳ môi trường làm việc nào.",
                        Ipa = "[ɪˈfɛktɪv kəˌmjuːnɪˈkeɪʃən ɪz ðə kiː tuː səkˈsɛs ɪn ˈɛni ˈwɜːrkpleɪs]",
                        StartTime = 0, EndTime = 5
                    },
                    new Sentence {
                        Id = "gen-s2",
                        Text = "It helps me communicate more effectively with foreigners.",
                        Translation = "Nó giúp tôi giao tiếp hiệu quả hơn với người nước ngoài.",
                        Ipa = "[ɪt hɛlps miː kəˈmjuːnɪkeɪt mɔːr ɪˈfɛktɪvli wɪð ˈfɔːrənərz]",
                        StartTime = 6, EndTime = 11
                    },
                    new Sentence {
                        Id = "gen-s3",
                        Text = "Let's practice shadowing this dialog together every day.",
                        Translation = "Hãy cùng nhau luyện nhại đoạn thoại này mỗi ngày nhé.",
                        Ipa = "[lɛts ˈpræktɪs ˈʃædoʊɪŋ ðɪs ˈdaɪəlɔːɡ təˈɡɛðər ˈɛvri deɪ]",
                        StartTime = 12, EndTime = 17
                    }
                }
            };
            StaticData.SavedAILessons.Add(newLesson);

            return Json(new {
                id = newLesson.Id,
                title = newLesson.Title,
                level = newLesson.Level,
                topic = newLesson.Topic,
                sentences = sentences,
                isGenerated = true
            });
        }

            
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    }

    public class EvaluateRequest
    {
        public string TargetText { get; set; } = string.Empty;
        public string Transcript { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string UserGoal { get; set; } = string.Empty;
    }

    public class WordMeaningRequest
    {
        public string Word { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
    }

    public class GenerateRolePlayRequest
    {
        public string Topic { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public List<SentenceRequestDto>? ExistingSentences { get; set; }
    }

    public class SentenceRequestDto
    {
        public string Text { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Ipa { get; set; } = string.Empty;
        public string? SpeakerLabel { get; set; }
    }

    public class GenerateLessonRequest
    {
        public string Level { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
    }

    public class RolePlayChatRequest
    {
        public string Topic { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public List<ChatMessageRequestDto>? History { get; set; }
    }

    public class ChatMessageRequestDto
    {
        public string Speaker { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsUser { get; set; }
    }
}
