using Microsoft.EntityFrameworkCore;
using WebShadowing.Models;

namespace WebShadowing.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<UserCourse> UserCourses => Set<UserCourse>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonMaterial> LessonMaterials => Set<LessonMaterial>();
    public DbSet<LessonSentence> LessonSentences => Set<LessonSentence>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<UserRecording> UserRecordings => Set<UserRecording>();
    public DbSet<Transcript> Transcripts => Set<Transcript>();
    public DbSet<AiFeedback> AiFeedbacks => Set<AiFeedback>();
    public DbSet<UserStatistic> UserStatistics => Set<UserStatistic>();
    public DbSet<UserLessonProgress> UserLessonProgress => Set<UserLessonProgress>();
    public DbSet<UserSentenceProgress> UserSentenceProgress => Set<UserSentenceProgress>();
    public DbSet<PracticeAttempt> PracticeAttempts => Set<PracticeAttempt>();
    public DbSet<GamificationLedgerEntry> GamificationLedger => Set<GamificationLedgerEntry>();
    public DbSet<WordErrorStatistic> WordErrorStatistics => Set<WordErrorStatistic>();
    public DbSet<VocabularyItem> VocabularyItems => Set<VocabularyItem>();
    public DbSet<FavoriteSentence> FavoriteSentences => Set<FavoriteSentence>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<ModeChangeHistory> ModeChangeHistory => Set<ModeChangeHistory>();
    public DbSet<SavedAiLesson> SavedAiLessons => Set<SavedAiLesson>();
    public DbSet<SavedAiLessonSegment> SavedAiLessonSegments => Set<SavedAiLessonSegment>();
    public DbSet<VipSubscription> VipSubscriptions => Set<VipSubscription>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<AiDialogueSession> AiDialogueSessions => Set<AiDialogueSession>();
    public DbSet<AiDialogueTurn> AiDialogueTurns => Set<AiDialogueTurn>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiDialogueSession>(entity =>
        {
            entity.HasIndex(item => new { item.UserId, item.LastActivityAt });
            entity.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Lesson).WithMany().HasForeignKey(item => item.LessonId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<AiDialogueTurn>(entity =>
        {
            entity.HasIndex(item => new { item.DialogueSessionId, item.CreatedAt });
            entity.HasOne(item => item.Session).WithMany(item => item.Turns).HasForeignKey(item => item.DialogueSessionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserCourse>(entity =>
        {
            entity.HasKey(uc => new { uc.UserId, uc.CourseId });
            entity.ToTable("Users_Courses", table => table.HasCheckConstraint(
                "CK_Users_Courses_Progress", "[Progress] >= 0 AND [Progress] <= 100"));

            entity.HasOne(uc => uc.User)
                .WithMany(u => u.UserCourses)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uc => uc.Course)
                .WithMany(c => c.UserCourses)
                .HasForeignKey(uc => uc.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserStatistic>(entity =>
        {
            entity.HasIndex(us => us.UserId).IsUnique();
            entity.ToTable("User_Statistics", table =>
            {
                table.HasCheckConstraint("CK_UserStatistics_TotalSessions", "total_sessions >= 0");
                table.HasCheckConstraint("CK_UserStatistics_AverageScore", "average_score >= 0 AND average_score <= 100");
                table.HasCheckConstraint("CK_UserStatistics_StreakDays", "streak_days >= 0");
                table.HasCheckConstraint("CK_UserStatistics_Hearts", "hearts >= 0");
                table.HasCheckConstraint("CK_UserStatistics_Exp", "exp >= 0");
            });

            entity.HasOne(us => us.User)
                .WithOne(u => u.Statistics)
                .HasForeignKey<UserStatistic>(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(us => us.Hearts).HasDefaultValue(5);
            entity.Property(us => us.Exp).HasDefaultValue(0);
            entity.Property(us => us.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(c => c.Level).HasMaxLength(20);
            entity.Property(c => c.LearningMode).HasDefaultValue(LearningModes.Casual);
            entity.Property(c => c.CourseType).HasDefaultValue(CourseTypes.Curriculum);
            entity.ToTable("Courses", table =>
            {
                table.HasCheckConstraint("CK_Courses_Level", "[Level] IN ('Beginner','Intermediate','Advanced')");
                table.HasCheckConstraint("CK_Courses_LearningMode", "learning_mode IN ('casual','academic','professional')");
                table.HasCheckConstraint("CK_Courses_CourseType", "course_type IN ('curriculum','video_bank','ai_saved')");
            });
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasIndex(l => new { l.CourseId, l.LessonOrder }).IsUnique();
            entity.ToTable("Lessons", table =>
            {
                table.HasCheckConstraint("CK_Lessons_Order", "lesson_order >= 0");
                table.HasCheckConstraint("CK_Lessons_Duration", "duration >= 0");
            });

            entity.HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonMaterial>(entity =>
        {
            entity.ToTable("Lesson_Material", table =>
            {
                table.HasCheckConstraint("CK_Material_Type", "material_type IN ('audio','video','transcript','text')");
                table.HasCheckConstraint("CK_Material_SourceReviewStatus", "source_review_status IN ('pending','approved','rejected')");
            });
            entity.HasOne(m => m.Lesson)
                .WithMany(l => l.Materials)
                .HasForeignKey(m => m.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonSentence>(entity =>
        {
            entity.HasIndex(s => new { s.LessonId, s.SentenceOrder }).IsUnique();
            entity.HasAlternateKey(s => new { s.LessonId, s.SentenceId })
                .HasName("AK_LessonSentences_Lesson_Sentence");
            entity.ToTable("Lesson_Sentences", table =>
            {
                table.HasCheckConstraint("CK_LessonSentences_Order", "sentence_order >= 0");
                table.HasCheckConstraint("CK_LessonSentences_Timestamps", "(start_ms IS NULL AND end_ms IS NULL) OR (start_ms >= 0 AND end_ms > start_ms)");
            });

            entity.HasOne(s => s.Lesson)
                .WithMany(l => l.Sentences)
                .HasForeignKey(s => s.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PracticeSession>(entity =>
        {
            entity.ToTable("Practice_Sessions", table =>
            {
                table.HasCheckConstraint("CK_Session_Score", "overall_score IS NULL OR (overall_score >= 0 AND overall_score <= 100)");
                table.HasCheckConstraint("CK_Session_Timestamps", "completed_at IS NULL OR completed_at >= started_at");
            });
            entity.HasOne(ps => ps.User)
                .WithMany(u => u.PracticeSessions)
                .HasForeignKey(ps => ps.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(ps => ps.Lesson)
                .WithMany(l => l.PracticeSessions)
                .HasForeignKey(ps => ps.LessonId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<UserRecording>(entity =>
        {
            entity.ToTable("User_Recordings", table =>
                table.HasCheckConstraint("CK_UserRecordings_Duration", "duration >= 0"));
            entity.HasOne(r => r.Session)
                .WithMany(ps => ps.Recordings)
                .HasForeignKey(r => r.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.ToTable("Transcripts", table => table.HasCheckConstraint(
                "CK_Transcript_Confidence", "confidence_score IS NULL OR (confidence_score >= 0 AND confidence_score <= 100)"));
            entity.HasOne(t => t.Recording)
                .WithMany(r => r.Transcripts)
                .HasForeignKey(t => t.RecordingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiFeedback>(entity =>
        {
            entity.ToTable("AI_Feedback", table =>
            {
                table.HasCheckConstraint("CK_AiFeedback_Pronunciation", "pronunciation_score IS NULL OR (pronunciation_score >= 0 AND pronunciation_score <= 100)");
                table.HasCheckConstraint("CK_AiFeedback_Fluency", "fluency_score IS NULL OR (fluency_score >= 0 AND fluency_score <= 100)");
                table.HasCheckConstraint("CK_AiFeedback_Accuracy", "accuracy_score IS NULL OR (accuracy_score >= 0 AND accuracy_score <= 100)");
            });
            entity.HasOne(f => f.Session)
                .WithMany(ps => ps.AiFeedbacks)
                .HasForeignKey(f => f.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.LearningMode).HasDefaultValue(LearningModes.Casual);
            entity.Property(u => u.PronunciationTarget).HasDefaultValue(PronunciationTargets.Comprehension70);
            entity.Property(u => u.Accent).HasDefaultValue(Accents.EnUs);
            entity.Property(u => u.IsVip).HasDefaultValue(false);
            entity.Property(u => u.OnboardingCompleted).HasDefaultValue(false);
            entity.Property(u => u.RowVersion).IsRowVersion();
            entity.ToTable("Users", table =>
            {
                table.HasCheckConstraint("CK_Users_LearningMode", "learning_mode IN ('casual','academic','professional')");
                table.HasCheckConstraint("CK_Users_PronunciationTarget", "pronunciation_target IN (50,70,90)");
                table.HasCheckConstraint("CK_Users_Accent", "accent IN ('en-us','en-gb')");
            });
        });

        modelBuilder.Entity<UserLessonProgress>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.LessonId, p.PracticeTab }).IsUnique();
            entity.Property(p => p.RowVersion).IsRowVersion();
            entity.ToTable("User_Lesson_Progress", table =>
            {
                table.HasCheckConstraint("CK_UserLessonProgress_Tab", "practice_tab IN ('shadowing','ai-dialogue','dictation','ipa-match')");
                table.HasCheckConstraint("CK_UserLessonProgress_Status", "status IN ('not_started','in_progress','completed')");
                table.HasCheckConstraint("CK_UserLessonProgress_Count", "completed_sentence_count >= 0");
                table.HasCheckConstraint("CK_UserLessonProgress_Percent", "progress_percent >= 0 AND progress_percent <= 100");
                table.HasCheckConstraint("CK_UserLessonProgress_Position", "last_position_ms IS NULL OR last_position_ms >= 0");
                table.HasCheckConstraint("CK_UserLessonProgress_CompletedAt", "status <> 'completed' OR completed_at IS NOT NULL");
            });
            entity.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.Lesson).WithMany().HasForeignKey(p => p.LessonId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(p => p.CurrentSentence).WithMany()
                .HasForeignKey(p => new { p.LessonId, p.CurrentSentenceId })
                .HasPrincipalKey(s => new { s.LessonId, s.SentenceId })
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<UserSentenceProgress>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.SentenceId, p.PracticeTab }).IsUnique();
            entity.Property(p => p.RowVersion).IsRowVersion();
            entity.ToTable("User_Sentence_Progress", table =>
            {
                table.HasCheckConstraint("CK_UserSentenceProgress_Tab", "practice_tab IN ('shadowing','ai-dialogue','dictation','ipa-match')");
                table.HasCheckConstraint("CK_UserSentenceProgress_Status", "status IN ('not_started','in_progress','completed')");
                table.HasCheckConstraint("CK_UserSentenceProgress_Score", "best_score IS NULL OR (best_score >= 0 AND best_score <= 100)");
                table.HasCheckConstraint("CK_UserSentenceProgress_Attempts", "attempt_count >= 0");
                table.HasCheckConstraint("CK_UserSentenceProgress_CompletedAt", "status <> 'completed' OR completed_at IS NOT NULL");
            });
            entity.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.Sentence).WithMany().HasForeignKey(p => p.SentenceId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PracticeAttempt>(entity =>
        {
            entity.HasIndex(a => new { a.UserId, a.IdempotencyKey }).IsUnique();
            entity.HasIndex(a => new { a.UserId, a.AttemptedAt });
            entity.ToTable("Practice_Attempts", table =>
            {
                table.HasCheckConstraint("CK_PracticeAttempts_Tab", "practice_tab IN ('shadowing','ai-dialogue','dictation','ipa-match')");
                table.HasCheckConstraint("CK_PracticeAttempts_Exercise", "exercise_type IN ('pronunciation','shadowing','dictation','ipa_match','ai_dialogue')");
                table.HasCheckConstraint("CK_PracticeAttempts_Result", "result IN ('pending','passed','failed','abandoned')");
                table.HasCheckConstraint("CK_PracticeAttempts_TargetScore", "target_score >= 0 AND target_score <= 100");
                table.HasCheckConstraint("CK_PracticeAttempts_Score", "score IS NULL OR (score >= 0 AND score <= 100)");
                table.HasCheckConstraint("CK_PracticeAttempts_Source", "(CASE WHEN sentence_id IS NULL THEN 0 ELSE 1 END + CASE WHEN saved_segment_id IS NULL THEN 0 ELSE 1 END) = 1");
            });
            entity.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(a => a.Session).WithMany().HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(a => a.Sentence).WithMany().HasForeignKey(a => a.SentenceId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(a => a.SavedSegment).WithMany().HasForeignKey(a => a.SavedSegmentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<GamificationLedgerEntry>(entity =>
        {
            entity.HasIndex(entry => new { entry.UserId, entry.SourceType, entry.SourceId }).IsUnique();
            entity.HasIndex(entry => new { entry.UserId, entry.CreatedAt });
            entity.ToTable("Gamification_Ledger", table =>
            {
                table.HasCheckConstraint("CK_GamificationLedger_SourceType", "source_type IN ('sentence_completion','attempt_penalty','daily_activity','heart_exchange')");
                table.HasCheckConstraint("CK_GamificationLedger_ExpBalance", "exp_balance >= 0");
                table.HasCheckConstraint("CK_GamificationLedger_HeartsBalance", "hearts_balance >= 0");
                table.HasCheckConstraint("CK_GamificationLedger_StreakBalance", "streak_balance >= 0");
            });
            entity.HasOne(entry => entry.User).WithMany().HasForeignKey(entry => entry.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(entry => entry.Attempt).WithMany().HasForeignKey(entry => entry.AttemptId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<WordErrorStatistic>(entity =>
        {
            entity.HasIndex(s => new { s.UserId, s.NormalizedWord }).IsUnique();
            entity.Property(s => s.RowVersion).IsRowVersion();
            entity.ToTable("Word_Error_Statistics", table =>
            {
                table.HasCheckConstraint("CK_WordErrorStatistics_Consecutive", "consecutive_error_count >= 0");
                table.HasCheckConstraint("CK_WordErrorStatistics_Total", "total_error_count >= 0 AND total_error_count >= consecutive_error_count");
            });
            entity.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.LastSentence).WithMany().HasForeignKey(s => s.LastSentenceId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<VocabularyItem>(entity =>
        {
            entity.HasIndex(v => new { v.UserId, v.NormalizedWord, v.LanguageCode }).IsUnique();
            entity.Property(v => v.RowVersion).IsRowVersion();
            entity.HasOne(v => v.User).WithMany().HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(v => v.SourceSentence).WithMany().HasForeignKey(v => v.SourceSentenceId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<FavoriteSentence>(entity =>
        {
            entity.HasIndex(f => new { f.UserId, f.SentenceId }).IsUnique();
            entity.HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.Sentence).WithMany().HasForeignKey(f => f.SentenceId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasIndex(s => s.UserId).IsUnique();
            entity.Property(s => s.RowVersion).IsRowVersion();
            entity.Property(s => s.ShowTranslation).HasDefaultValue(true);
            entity.Property(s => s.ShowCaptions).HasDefaultValue(true);
            entity.Property(s => s.Theme).HasDefaultValue(ThemePreferences.System);
            entity.Property(s => s.PlaybackRate).HasDefaultValue(1m);
            entity.ToTable("User_Settings", table =>
            {
                table.HasCheckConstraint("CK_UserSettings_Theme", "theme IN ('system','light','dark')");
                table.HasCheckConstraint("CK_UserSettings_PlaybackRate", "playback_rate >= 0.5 AND playback_rate <= 2.0");
            });
            entity.HasOne(s => s.User).WithOne().HasForeignKey<UserSettings>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ModeChangeHistory>(entity =>
        {
            entity.HasIndex(h => new { h.UserId, h.ChangedAt });
            entity.ToTable("Mode_Change_History", table =>
            {
                table.HasCheckConstraint("CK_ModeChangeHistory_FromMode", "from_mode IN ('casual','academic','professional')");
                table.HasCheckConstraint("CK_ModeChangeHistory_ToMode", "to_mode IN ('casual','academic','professional')");
                table.HasCheckConstraint("CK_ModeChangeHistory_ChangedBy", "changed_by IN ('user','admin','system','onboarding')");
                table.HasCheckConstraint("CK_ModeChangeHistory_ActualChange", "from_mode <> to_mode");
            });
            entity.HasOne(h => h.User).WithMany().HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SavedAiLesson>(entity =>
        {
            entity.HasIndex(l => new { l.UserId, l.UpdatedAt });
            entity.Property(l => l.RowVersion).IsRowVersion();
            entity.ToTable("User_Saved_Lessons", table =>
            {
                table.HasCheckConstraint("CK_UserSavedLessons_LearningMode", "learning_mode IN ('casual','academic','professional')");
                table.HasCheckConstraint("CK_UserSavedLessons_SourceReviewStatus", "source_review_status IN ('pending','approved','rejected')");
            });
            entity.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavedAiLessonSegment>(entity =>
        {
            entity.HasIndex(s => new { s.SavedLessonId, s.SegmentOrder }).IsUnique();
            entity.ToTable("Saved_AI_Lesson_Segments", table =>
            {
                table.HasCheckConstraint("CK_SavedAiLessonSegments_Order", "segment_order >= 0");
                table.HasCheckConstraint("CK_SavedAiLessonSegments_Timestamps", "(start_ms IS NULL AND end_ms IS NULL) OR (start_ms >= 0 AND end_ms > start_ms)");
            });
            entity.HasOne(s => s.SavedLesson).WithMany(l => l.Segments)
                .HasForeignKey(s => s.SavedLessonId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VipSubscription>(entity =>
        {
            entity.HasIndex(s => new { s.Provider, s.ProviderSubscriptionId }).IsUnique();
            entity.HasIndex(s => new { s.UserId, s.Status });
            entity.Property(s => s.RowVersion).IsRowVersion();
            entity.ToTable("VIP_Subscriptions", table =>
            {
                table.HasCheckConstraint("CK_VipSubscriptions_BillingPeriod", "billing_period IN ('monthly','yearly','lifetime')");
                table.HasCheckConstraint("CK_VipSubscriptions_Status", "status IN ('pending','active','past_due','cancelled','expired')");
                table.HasCheckConstraint("CK_VipSubscriptions_Dates", "ends_at IS NULL OR ends_at > starts_at");
            });
            entity.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasIndex(p => new { p.Provider, p.IdempotencyKey }).IsUnique();
            entity.HasIndex(p => new { p.Provider, p.ProviderTransactionId }).IsUnique();
            entity.HasIndex(p => new { p.UserId, p.CreatedAt });
            entity.ToTable("Payment_Transactions", table =>
            {
                table.HasCheckConstraint("CK_PaymentTransactions_Type", "transaction_type IN ('purchase','renewal','refund')");
                table.HasCheckConstraint("CK_PaymentTransactions_Status", "status IN ('pending','succeeded','failed','refunded')");
                table.HasCheckConstraint("CK_PaymentTransactions_Amount", "amount >= 0");
                table.HasCheckConstraint("CK_PaymentTransactions_Currency", "LEN(currency) = 3 AND currency = UPPER(currency)");
            });
            entity.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(p => p.Subscription).WithMany(s => s.PaymentTransactions)
                .HasForeignKey(p => p.SubscriptionId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
