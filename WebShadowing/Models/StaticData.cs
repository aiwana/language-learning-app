using System;
using System.Collections.Generic;

namespace WebShadowing.Models;

public static class StaticData
{
    public static UserProfile DefaultProfile { get; set; } = new UserProfile
    {
        Id = "user-123",
        Name = "Nguyễn Văn A",
        Email = "vanya@example.com",
        Phone = "0987654321",
        Level = UserLevel.Casual,
        TargetAccent = TargetAccent.US,
        Goal = LearningGoal.Fluency50,
        IsPremium = true,
        PaymentMethod = "Credit Card"
    };

    public static UserStats DefaultStats { get; set; } = new UserStats
    {
        Streak = 3,
        LastPracticed = "2026-06-14T20:30:00Z",
        TotalSentences = 12,
        TotalTimeSeconds = 360,
        Exp = 420,
        Hearts = 5
    };

    public static List<FavoriteSentence> SampleFavorites { get; set; } = new List<FavoriteSentence>
    {
        new FavoriteSentence
        {
            Id = "fav-sample",
            LessonId = "lesson-1",
            LessonTitle = "Giao tiếp hằng ngày: Chuyện buổi sáng",
            Sentence = new Sentence
            {
                Id = "s1-2",
                Text = "Today, I made myself a perfect cup of hot coffee.",
                Translation = "Hôm nay, tôi tự pha cho mình một tách cà phê nóng thật tuyệt vời.",
                Ipa = "[təˈdeɪ, aɪ meɪd maɪˈsɛlf ə ˈpɜːrfɪkt kʌp ʌv hɑːt ˈkɔːfi]",
                StartTime = 5,
                EndTime = 9
            }
        }
    };

    public static List<Flashcard> SampleFlashcards { get; set; } = new List<Flashcard>
    {
        new Flashcard
        {
            Id = "fc-sample",
            Word = "environment",
            Meaning = "Danh từ: Môi trường tự nhiên xung quanh. Chú ý đọc nhấn ba: en-VI-ron-ment, âm N câm giữa từ.",
            Ipa = "[ɪnˈvaɪrənmənt]",
            SentenceContext = "Bối cảnh câu thoại: \"Transitioning to clean renewable energy is our ultimate solution.\"",
            LessonTitle = "Ô nhiễm môi trường & Hành động",
            Score = 42,
            NextReviewDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Box = 1
        }
    };

    public static List<Lesson> SavedAILessons { get; set; } = new List<Lesson>
    {
        new Lesson
        {
            Id = "gen-1",
            Title = "Hội thoại thử việc: Phỏng vấn tại Startup Công nghệ",
            Level = "Trung cấp",
            Topic = "Professional",
            Duration = "0:30",
            IsGenerated = true,
            IsDialogue = true,
            Sentences = new List<Sentence>
            {
                new Sentence { Id = "gen-1-s1", Text = "Why do you want to join our engineering department?", Translation = "Tại sao bạn muốn gia nhập bộ phận kỹ thuật của chúng tôi?", Ipa = "[waɪ duː juː wɒnt tuː dʒɔɪn ˈaʊər ˌɛndʒɪˈnɪərɪŋ dɪˈpɑːrtmənt]", StartTime = 0, EndTime = 5, SpeakerLabel = "HR Manager", IsDialogue = true },
                new Sentence { Id = "gen-1-s2", Text = "I am excited to build scalable solutions with modern technologies.", Translation = "Tôi rất hào hứng được xây dựng các giải pháp có tính mở rộng cao bằng công nghệ hiện đại.", Ipa = "[aɪ æm ɪkˈsaɪtɪd tuː bɪld ˈskeɪləbl səˈluːʃnz wɪð ˈmɒdərn tɛkˈnɒlədʒiz]", StartTime = 6, EndTime = 12, SpeakerLabel = "Candidate", IsDialogue = true }
            }
        }
    };

    public static List<Lesson> StaticLessons { get; set; } = new List<Lesson>
    {
        new Lesson
        {
            Id = "lesson-1",
            Title = "Giao tiếp hằng ngày: Chuyện buổi sáng",
            Level = "Cơ bản",
            Topic = "Casual",
            Duration = "0:35",
            YoutubeId = "e8Z7rXg69g0",
            Sentences = new List<Sentence>
            {
                new Sentence { Id = "s1-1", Text = "I usually wake up around six in the morning.", Translation = "Tôi thường thức dậy vào khoảng sáu giờ sáng.", Ipa = "[aɪ ˈjuːʒuəli weɪk ʌp əˈraʊnd sɪks ɪn ðə ˈmɔːrnɪŋ]", StartTime = 0, EndTime = 4 },
                new Sentence { Id = "s1-2", Text = "Today, I made myself a perfect cup of hot coffee.", Translation = "Hôm nay, tôi tự pha cho mình một tách cà phê nóng thật tuyệt vời.", Ipa = "[təˈdeɪ, aɪ meɪd maɪˈsɛlf ə ˈpɜːrfɪkt kʌp ʌv hɑːt ˈkɔːfi]", StartTime = 5, EndTime = 9 },
                new Sentence { Id = "s1-3", Text = "It helps me stay active and focused throughout the day.", Translation = "Nó giúp tôi tỉnh táo và tập trung trong suốt cả ngày.", Ipa = "[ɪt hɛlps miː steɪ ˈæktɪv ænd ˈfoʊkəst θruːˈaʊt ðə deɪ]", StartTime = 10, EndTime = 15 },
                new Sentence { Id = "s1-4", Text = "A clean routine makes my life simple and relaxed.", Translation = "Một thói quen ngăn nắp giúp cuộc sống của tôi đơn giản và thư thái.", Ipa = "[ə kliːn ruːˈtiːn meɪks maɪ laɪf ˈsɪmpl ænd rɪˈlækst]", StartTime = 16, EndTime = 21 }
            }
        },
        new Lesson
        {
            Id = "lesson-2",
            Title = "Phát biểu hội thảo: Thách thức công nghệ",
            Level = "Nâng cao",
            Topic = "Professional",
            Duration = "0:42",
            YoutubeId = "9Kq89k6S_gI",
            Sentences = new List<Sentence>
            {
                new Sentence { Id = "s2-1", Text = "Artificial intelligence is rapidly transforming global business operations.", Translation = "Trí tuệ nhân tạo đang nhanh chóng làm thay đổi các hoạt động kinh doanh toàn cầu.", Ipa = "[ˌɑːrtɪˈfɪʃl ɪnˈtɛlɪdʒəns ɪz ˈræpɪdli trænsˈfɔːrmɪŋ ˈɡloʊbl ˈbɪznəs ˌɑːpəˈreɪʃnz]", StartTime = 0, EndTime = 6 },
                new Sentence { Id = "s2-2", Text = "Companies must adapt quickly to secure a competitive advantage.", Translation = "Các công ty phải thích ứng thật nhanh để bảo đảm lợi thế cạnh tranh.", Ipa = "[ˈkʌmpəniz mʌst əˈdæpt ˈkwɪkli tuː sɪˈkjʊr ə kəmˈpɛtətɪv ædˈvæntɪdʒ]", StartTime = 7, EndTime = 12 },
                new Sentence { Id = "s2-3", Text = "Innovation requires not only capital but also cultural agility.", Translation = "Sự đổi mới sáng tạo đòi hỏi không chỉ vốn nguồn lực mà còn cả sự linh hoạt về văn hóa.", Ipa = "[ˌɪnəˈveɪʃn rɪˈkwaɪərz nɑːt ˈoʊnli ˈkæpɪtl bʌt ˈɔːlsoʊ ˈkʌltʃərəl əˈdʒɪləti]", StartTime = 13, EndTime = 20 },
                new Sentence { Id = "s2-4", Text = "Therefore, our main focus should remain on talent development.", Translation = "Vì vậy, trọng tâm chính của chúng ta nên luôn luôn là phát triển tài năng.", Ipa = "[ˈðɛrfɔːr, ˈaʊər meɪn ˈfoʊkəs ʃʊd rɪˈmeɪn ɑːn ˈtælənt dɪˈvɛlɒpmənt]", StartTime = 21, EndTime = 27 }
            }
        },
        new Lesson
        {
            Id = "lesson-3",
            Title = "Học thuật: Ô nhiễm môi trường & Hành động",
            Level = "Trung cấp",
            Topic = "Academic",
            Duration = "0:38",
            YoutubeId = "O32S7M-N9j8",
            Sentences = new List<Sentence>
            {
                new Sentence { Id = "s3-1", Text = "Global carbon emissions continue to rise at an alarming speed.", Translation = "Lượng khí thải carbon toàn cầu tiếp tục tăng với tốc độ đáng báo động.", Ipa = "[ˈɡloʊbl ˈkɑːrbən ɪˈmɪʃnz kənˈtɪnjuː tuː raɪz æt ən əˈlɑːrmɪŋ spiːd]", StartTime = 0, EndTime = 5 },
                new Sentence { Id = "s3-2", Text = "This trend significantly accelerates the cycle of climate change.", Translation = "Xu hướng này làm tăng tốc đáng kể chu kỳ biến đổi khí hậu.", Ipa = "[ðɪs trɛnd sɪɡˈnɪfɪkəntli ækˈsɛləreɪts ðə ˈsaɪkl ʌv ˈklaɪmət tʃeɪndʒ]", StartTime = 6, EndTime = 11 },
                new Sentence { Id = "s3-3", Text = "Transitioning to clean renewable energy is our ultimate solution.", Translation = "Chuyển dịch sang năng lượng tái tạo sạch là giải pháp tối ưu của chúng ta.", Ipa = "[trænˈzɪʃənɪŋ tuː kliːn rɪˈnjuːəbl ˈɛnərdʒi ɪz ˈaʊər ˈʌltəmət səˈluːʃn]", StartTime = 12, EndTime = 18 },
                new Sentence { Id = "s3-4", Text = "Every small action contributes directly to preserving our biodiversity.", Translation = "Mỗi hành động nhỏ đều đóng góp trực tiếp vào việc bảo tồn đa dạng sinh học.", Ipa = "[ˈɛvri smɔːl ˈækʃn kənˈtrɪbjuːts dɪˈrɛktli tuː prɪˈzɜːrvɪŋ ˈaʊər ˌbaɪoʊdaɪˈvɜːrsəti]", StartTime = 19, EndTime = 25 }
            }
        },
        new Lesson
        {
            Id = "lesson-4",
            Title = "Du lịch & Ẩm thực: Đặt đồ ăn tại London",
            Level = "Cơ bản",
            Topic = "Casual",
            Duration = "0:28",
            Sentences = new List<Sentence>
            {
                new Sentence { Id = "s4-1", Text = "Pardon me, could I take a look at the dinner menu please?", Translation = "Xin lỗi, tôi có thể xem qua thực đơn bữa tối được không?", Ipa = "[ˈpɑːrdn miː, kʊd aɪ teɪk ə lʊk æt ðə ˈdɪnər ˈmɛnjuː pliːz]", StartTime = 0, EndTime = 4 },
                new Sentence { Id = "s4-2", Text = "I would like to try your local specialties tonight.", Translation = "Tôi muốn thưởng thức những món đặc sản địa phương của các bạn tối nay.", Ipa = "[aɪ wʊd laɪk tuː traɪ jɔːr ˈloʊkl ˈspɛʃəltiz təˈnaɪt]", StartTime = 5, EndTime = 9 },
                new Sentence { Id = "s4-3", Text = "Also, does this traditional dish contain any dairy or seafood?", Translation = "Ngoài ra, món ăn truyền thống này có chứa bơ sữa hay hải sản không?", Ipa = "[ˈɔːlsoʊ, dʌz ðɪs trəˈdɪʃənl dɪʃ kənˈteɪn ˈɛni ˈdɛri ɔːr ˈsiːfuːd]", StartTime = 10, EndTime = 14 }
            }
        }
    };

    public static List<Textbook> Textbooks { get; set; } = new List<Textbook>
    {
        new Textbook
        {
            Id = "tb-grade6",
            Name = "Tiếng Anh Lớp 6",
            Grade = 6,
            Description = "Hội thoại cơ bản cho học sinh lớp 6 — Chào hỏi, gia đình, trường học",
            Units = new List<Lesson>
            {
                new Lesson
                {
                    Id = "tb6-unit1",
                    Title = "Unit 1: Greetings — Chào hỏi làm quen",
                    Level = "Cơ bản",
                    Topic = "Casual",
                    Duration = "0:28",
                    IsDialogue = true,
                    Speakers = new List<string> { "Nam", "Lan" },
                    Sentences = new List<Sentence>
                    {
                        new Sentence { Id = "tb6-u1-s1", Text = "Hi! My name is Nam. What is your name?", Translation = "Xin chào! Mình tên Nam. Bạn tên gì?", Ipa = "[haɪ maɪ neɪm ɪz næm wʌt ɪz jɔːr neɪm]", StartTime = 0, EndTime = 4, SpeakerLabel = "Nam", IsDialogue = true },
                        new Sentence { Id = "tb6-u1-s2", Text = "Hello Nam! I am Lan. Nice to meet you!", Translation = "Chào Nam! Mình là Lan. Rất vui được gặp bạn!", Ipa = "[hɛˈloʊ næm aɪ æm læn naɪs tuː miːt juː]", StartTime = 5, EndTime = 9, SpeakerLabel = "Lan", IsDialogue = true }
                    }
                },
                new Lesson
                {
                    Id = "tb6-unit2",
                    Title = "Unit 2: My Family — Gia đình của tôi",
                    Level = "Cơ bản",
                    Topic = "Casual",
                    Duration = "0:30",
                    IsDialogue = true,
                    Speakers = new List<string> { "Teacher", "Minh" },
                    Sentences = new List<Sentence>
                    {
                        new Sentence { Id = "tb6-u2-s1", Text = "Minh, can you tell me about your family?", Translation = "Minh, em có thể kể về gia đình mình không?", Ipa = "[mɪn kæn juː tɛl miː əˈbaʊt jɔːr ˈfæməli]", StartTime = 0, EndTime = 4, SpeakerLabel = "Teacher", IsDialogue = true },
                        new Sentence { Id = "tb6-u2-s2", Text = "Yes! There are four people in my family.", Translation = "Vâng! Gia đình em có bốn người.", Ipa = "[jɛs ðɛr ɑːr fɔːr ˈpiːpl ɪn maɪ ˈfæməli]", StartTime = 5, EndTime = 9, SpeakerLabel = "Minh", IsDialogue = true }
                    }
                }
            }
        },
        new Textbook
        {
            Id = "tb-grade7",
            Name = "Tiếng Anh Lớp 7",
            Grade = 7,
            Description = "Hội thoại trung cấp cho lớp 7 — Sở thích, thể thao, thời gian rảnh",
            Units = new List<Lesson>
            {
                new Lesson
                {
                    Id = "tb7-unit1",
                    Title = "Unit 1: Hobbies — Sở thích của bạn",
                    Level = "Cơ bản",
                    Topic = "Casual",
                    Duration = "0:30",
                    IsDialogue = true,
                    Speakers = new List<string> { "Anna", "Duc" },
                    Sentences = new List<Sentence>
                    {
                        new Sentence { Id = "tb7-u1-s1", Text = "Duc, what do you usually do in your free time?", Translation = "Đức, bạn thường làm gì vào thời gian rảnh?", Ipa = "[dʌk wʌt duː juː ˈjuːʒuəli duː ɪn jɔːr friː taɪm]", StartTime = 0, EndTime = 5, SpeakerLabel = "Anna", IsDialogue = true },
                        new Sentence { Id = "tb7-u1-s2", Text = "I like playing football and reading comic books.", Translation = "Mình thích đá bóng và đọc truyện tranh.", Ipa = "[aɪ laɪk ˈpleɪɪŋ ˈfʊtbɔːl ænd ˈriːdɪŋ ˈkɒmɪk bʊks]", StartTime = 6, EndTime = 10, SpeakerLabel = "Duc", IsDialogue = true }
                    }
                }
            }
        }
    };

    public static List<VideoLessonMock> VideoLessons { get; set; } = new List<VideoLessonMock>
    {
        new VideoLessonMock
        {
            Id = "vid-casual-1",
            Title = "Daily Conversational British English in a Local London Coffee Shop",
            Level = "Cơ bản",
            Topic = "Casual",
            Duration = "03:15",
            Speaker = "Easy British Club",
            Views = "12.4K views",
            Likes = "96% helpful",
            YoutubeId = "e8Z7rXg69g0",
            ImageUrl = "https://images.unsplash.com/photo-1507133750040-4a8f57021571?q=80&w=400&auto=format&fit=crop",
            Subtitles = new List<string>
            {
                "Hello there! Welcome to The Coffee House, what can I get you?",
                "Hi! I would like to order a large oatmeal latte to go, please."
            }
        },
        new VideoLessonMock
        {
            Id = "vid-prof-1",
            Title = "Mastering Silicon Valley Tech Job Interviews & Culture Fit",
            Level = "Nâng cao",
            Topic = "Professional",
            Duration = "07:40",
            Speaker = "Tech Career Coach",
            Views = "18.7K views",
            Likes = "97% helpful",
            YoutubeId = "9Kq89k6S_gI",
            ImageUrl = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?q=80&w=400&auto=format&fit=crop",
            Subtitles = new List<string>
            {
                "Tell me about a time you handled a severe conflict inside a project deadline.",
                "Well, in my previous role, we had a major architectural disagreement before release."
            }
        }
    };
}
