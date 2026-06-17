using System;
using System.Collections.Generic;

namespace WebShadowing.Models;

public enum UserLevel
{
    Academic,
    Casual,
    Professional
}

public enum TargetAccent
{
    UK,
    US
}

public enum LearningGoal
{
    Fluency50,
    Comprehension70,
    Accent90
}

public class UserStats
{
    public int Streak { get; set; }
    public string? LastPracticed { get; set; }
    public int TotalSentences { get; set; }
    public int TotalTimeSeconds { get; set; }
    public int Exp { get; set; }
    public int Hearts { get; set; }
}

public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserLevel Level { get; set; }
    public TargetAccent TargetAccent { get; set; }
    public LearningGoal Goal { get; set; }
    public bool IsPremium { get; set; }
    public string? PaymentMethod { get; set; }
}

public class WordGrade
{
    public string Word { get; set; } = string.Empty;
    public string AccuracyCode { get; set; } = string.Empty; // 'correct' | 'incorrect' | 'warning'
    public string? Ipa { get; set; }
    public string? Correction { get; set; }
}

public class EvaluationResult
{
    public int Score { get; set; }
    public int Accuracy { get; set; }
    public int Fluency { get; set; }
    public int Intonation { get; set; }
    public List<WordGrade> Words { get; set; } = new();
    public string Feedback { get; set; } = string.Empty;
}

public class Sentence
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string Ipa { get; set; } = string.Empty;
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public bool? IsDialogue { get; set; }
    public string? SpeakerLabel { get; set; }
}

public class Lesson
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty; // "Cơ bản" | "Trung cấp" | "Nâng cao"
    public string Topic { get; set; } = string.Empty; // "Casual" | "Academic" | "Professional"
    public string Duration { get; set; } = string.Empty;
    public string? YoutubeId { get; set; }
    public string? VideoUrl { get; set; }
    public List<Sentence> Sentences { get; set; } = new();
    public bool? IsGenerated { get; set; }
    public bool? IsDialogue { get; set; }
    public List<string>? Speakers { get; set; }
}

public class Textbook
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Grade { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<Lesson> Units { get; set; } = new();
}

public class RolePlayMessage
{
    public string Id { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string Ipa { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public bool? IsPlayed { get; set; }
    public int? Score { get; set; }
    public string? SpokenText { get; set; }
}

public class PracticeHistory
{
    public string Id { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public string SentenceId { get; set; } = string.Empty;
    public string TargetText { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Accuracy { get; set; }
    public int Fluency { get; set; }
    public int Intonation { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public List<WordGrade> Words { get; set; } = new();
    public string Timestamp { get; set; } = string.Empty;
}

public class FavoriteSentence
{
    public string Id { get; set; } = string.Empty;
    public string LessonId { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public Sentence Sentence { get; set; } = new();
}

public class Flashcard
{
    public string Id { get; set; } = string.Empty;
    public string Word { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Ipa { get; set; } = string.Empty;
    public string SentenceContext { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public int Score { get; set; }
    public string NextReviewDate { get; set; } = string.Empty;
    public int Box { get; set; }
}

public class VideoLessonMock
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string Views { get; set; } = string.Empty;
    public string Likes { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string YoutubeId { get; set; } = string.Empty;
    public List<string> Subtitles { get; set; } = new();
}

