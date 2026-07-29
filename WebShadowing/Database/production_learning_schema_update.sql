/*
  Production learning/user-owned schema update for SQL Server.
  - Select the target database before executing this file.
  - Every DDL/DML operation is guarded so the script can be retried safely.
  - Media columns contain URLs/provider references only; no media binary is stored.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* Existing aggregate extensions. */
IF COL_LENGTH('dbo.Users', 'row_version') IS NULL
    ALTER TABLE dbo.Users ADD row_version ROWVERSION NOT NULL;
GO

IF COL_LENGTH('dbo.Courses', 'course_type') IS NULL
    ALTER TABLE dbo.Courses ADD course_type VARCHAR(20) NOT NULL CONSTRAINT DF_Courses_CourseType_Production DEFAULT ('curriculum');
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Courses') AND name = 'CK_Courses_course_type')
    ALTER TABLE dbo.Courses DROP CONSTRAINT CK_Courses_course_type;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Courses') AND name = 'CK_Courses_CourseType')
    ALTER TABLE dbo.Courses WITH NOCHECK ADD CONSTRAINT CK_Courses_CourseType CHECK (course_type IN ('curriculum','video_bank','ai_saved'));
GO

IF COL_LENGTH('dbo.Lesson_Sentences', 'start_ms') IS NULL
    ALTER TABLE dbo.Lesson_Sentences ADD start_ms INT NULL;
IF COL_LENGTH('dbo.Lesson_Sentences', 'end_ms') IS NULL
    ALTER TABLE dbo.Lesson_Sentences ADD end_ms INT NULL;
IF COL_LENGTH('dbo.Lesson_Sentences', 'ipa') IS NULL
    ALTER TABLE dbo.Lesson_Sentences ADD ipa NVARCHAR(500) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Lesson_Sentences') AND name = 'CK_LessonSentences_Timestamps')
    ALTER TABLE dbo.Lesson_Sentences WITH NOCHECK ADD CONSTRAINT CK_LessonSentences_Timestamps
        CHECK ((start_ms IS NULL AND end_ms IS NULL) OR (start_ms >= 0 AND end_ms > start_ms));
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Lesson_Sentences') AND name = 'IX_LessonSentences_Lesson_Sentence')
    CREATE UNIQUE INDEX IX_LessonSentences_Lesson_Sentence ON dbo.Lesson_Sentences(lesson_id, sentence_id);
GO

IF COL_LENGTH('dbo.Lesson_Material', 'source_provider') IS NULL
    ALTER TABLE dbo.Lesson_Material ADD source_provider NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.Lesson_Material', 'source_id') IS NULL
    ALTER TABLE dbo.Lesson_Material ADD source_id NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.Lesson_Material', 'license_note') IS NULL
    ALTER TABLE dbo.Lesson_Material ADD license_note NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.Lesson_Material', 'source_review_status') IS NULL
    ALTER TABLE dbo.Lesson_Material ADD source_review_status VARCHAR(20) NOT NULL
        CONSTRAINT DF_LessonMaterial_SourceReviewStatus DEFAULT ('pending');
IF COL_LENGTH('dbo.Lesson_Material', 'source_reviewed_at') IS NULL
    ALTER TABLE dbo.Lesson_Material ADD source_reviewed_at DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Lesson_Material') AND name = 'CK_Material_SourceReviewStatus')
    ALTER TABLE dbo.Lesson_Material WITH NOCHECK ADD CONSTRAINT CK_Material_SourceReviewStatus
        CHECK (source_review_status IN ('pending','approved','rejected'));
GO

IF COL_LENGTH('dbo.User_Statistics', 'hearts') IS NULL
    ALTER TABLE dbo.User_Statistics ADD hearts INT NOT NULL CONSTRAINT DF_UserStatistics_Hearts_Production DEFAULT (5);
IF COL_LENGTH('dbo.User_Statistics', 'exp') IS NULL
    ALTER TABLE dbo.User_Statistics ADD exp INT NOT NULL CONSTRAINT DF_UserStatistics_Exp_Production DEFAULT (0);
IF COL_LENGTH('dbo.User_Statistics', 'row_version') IS NULL
    ALTER TABLE dbo.User_Statistics ADD row_version ROWVERSION NOT NULL;
GO

UPDATE dbo.User_Statistics SET average_score = 0 WHERE average_score IS NULL;
ALTER TABLE dbo.User_Statistics ALTER COLUMN average_score DECIMAL(5,2) NOT NULL;
INSERT INTO dbo.User_Statistics (user_id, total_sessions, average_score, streak_days, last_practice_at, hearts, exp)
SELECT u.user_id, 0, 0, 0, NULL, 5, 0
FROM dbo.Users AS u
WHERE NOT EXISTS (SELECT 1 FROM dbo.User_Statistics AS s WHERE s.user_id = u.user_id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.User_Statistics') AND name = 'CK_UserStatistics_Hearts')
    ALTER TABLE dbo.User_Statistics WITH NOCHECK ADD CONSTRAINT CK_UserStatistics_Hearts CHECK (hearts >= 0);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.User_Statistics') AND name = 'CK_UserStatistics_Exp')
    ALTER TABLE dbo.User_Statistics WITH NOCHECK ADD CONSTRAINT CK_UserStatistics_Exp CHECK (exp >= 0);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.User_Statistics') AND name = 'CK_UserStatistics_TotalSessions')
    ALTER TABLE dbo.User_Statistics WITH NOCHECK ADD CONSTRAINT CK_UserStatistics_TotalSessions CHECK (total_sessions >= 0);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.User_Statistics') AND name = 'CK_UserStatistics_AverageScore')
    ALTER TABLE dbo.User_Statistics WITH NOCHECK ADD CONSTRAINT CK_UserStatistics_AverageScore CHECK (average_score BETWEEN 0 AND 100);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.User_Statistics') AND name = 'CK_UserStatistics_StreakDays')
    ALTER TABLE dbo.User_Statistics WITH NOCHECK ADD CONSTRAINT CK_UserStatistics_StreakDays CHECK (streak_days >= 0);
GO

/* One row per user/lesson/tab; current sentence is constrained to the same lesson. */
IF OBJECT_ID('dbo.User_Lesson_Progress', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.User_Lesson_Progress (
        progress_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_User_Lesson_Progress PRIMARY KEY,
        user_id BIGINT NOT NULL,
        lesson_id BIGINT NOT NULL,
        practice_tab VARCHAR(20) NOT NULL,
        current_sentence_id BIGINT NULL,
        status VARCHAR(20) NOT NULL CONSTRAINT DF_UserLessonProgress_Status DEFAULT ('not_started'),
        completed_sentence_count INT NOT NULL CONSTRAINT DF_UserLessonProgress_Count DEFAULT (0),
        progress_percent DECIMAL(5,2) NOT NULL CONSTRAINT DF_UserLessonProgress_Percent DEFAULT (0),
        last_position_ms INT NULL,
        started_at DATETIME2 NULL,
        completed_at DATETIME2 NULL,
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_UserLessonProgress_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        row_version ROWVERSION NOT NULL,
        CONSTRAINT FK_UserLessonProgress_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id) ON DELETE CASCADE,
        CONSTRAINT FK_UserLessonProgress_Lesson FOREIGN KEY (lesson_id) REFERENCES dbo.Lessons(lesson_id),
        CONSTRAINT FK_UserLessonProgress_CurrentSentence FOREIGN KEY (lesson_id, current_sentence_id) REFERENCES dbo.Lesson_Sentences(lesson_id, sentence_id),
        CONSTRAINT UQ_UserLessonProgress_User_Lesson_Tab UNIQUE (user_id, lesson_id, practice_tab),
        CONSTRAINT CK_UserLessonProgress_Tab CHECK (practice_tab IN ('shadowing','ai-dialogue','dictation','ipa-match')),
        CONSTRAINT CK_UserLessonProgress_Status CHECK (status IN ('not_started','in_progress','completed')),
        CONSTRAINT CK_UserLessonProgress_Count CHECK (completed_sentence_count >= 0),
        CONSTRAINT CK_UserLessonProgress_Percent CHECK (progress_percent BETWEEN 0 AND 100),
        CONSTRAINT CK_UserLessonProgress_Position CHECK (last_position_ms IS NULL OR last_position_ms >= 0),
        CONSTRAINT CK_UserLessonProgress_CompletedAt CHECK (status <> 'completed' OR completed_at IS NOT NULL)
    );
END
GO

IF OBJECT_ID('dbo.User_Sentence_Progress', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.User_Sentence_Progress (
        sentence_progress_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_User_Sentence_Progress PRIMARY KEY,
        user_id BIGINT NOT NULL,
        sentence_id BIGINT NOT NULL,
        practice_tab VARCHAR(20) NOT NULL,
        status VARCHAR(20) NOT NULL CONSTRAINT DF_UserSentenceProgress_Status DEFAULT ('not_started'),
        best_score DECIMAL(5,2) NULL,
        attempt_count INT NOT NULL CONSTRAINT DF_UserSentenceProgress_Attempts DEFAULT (0),
        last_attempt_at DATETIME2 NULL,
        completed_at DATETIME2 NULL,
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_UserSentenceProgress_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        row_version ROWVERSION NOT NULL,
        CONSTRAINT FK_UserSentenceProgress_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id) ON DELETE CASCADE,
        CONSTRAINT FK_UserSentenceProgress_Sentence FOREIGN KEY (sentence_id) REFERENCES dbo.Lesson_Sentences(sentence_id),
        CONSTRAINT UQ_UserSentenceProgress_User_Sentence_Tab UNIQUE (user_id, sentence_id, practice_tab),
        CONSTRAINT CK_UserSentenceProgress_Tab CHECK (practice_tab IN ('shadowing','ai-dialogue','dictation','ipa-match')),
        CONSTRAINT CK_UserSentenceProgress_Status CHECK (status IN ('not_started','in_progress','completed')),
        CONSTRAINT CK_UserSentenceProgress_Score CHECK (best_score IS NULL OR best_score BETWEEN 0 AND 100),
        CONSTRAINT CK_UserSentenceProgress_Attempts CHECK (attempt_count >= 0),
        CONSTRAINT CK_UserSentenceProgress_CompletedAt CHECK (status <> 'completed' OR completed_at IS NOT NULL)
    );
END
GO

IF OBJECT_ID('dbo.Word_Error_Statistics', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Word_Error_Statistics (
        word_error_stat_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Word_Error_Statistics PRIMARY KEY,
        user_id BIGINT NOT NULL,
        normalized_word NVARCHAR(100) NOT NULL,
        display_word NVARCHAR(100) NOT NULL,
        consecutive_error_count INT NOT NULL CONSTRAINT DF_WordErrorStats_Consecutive DEFAULT (0),
        total_error_count INT NOT NULL CONSTRAINT DF_WordErrorStats_Total DEFAULT (0),
        last_error_at DATETIME2 NULL,
        last_attempted_at DATETIME2 NULL,
        last_sentence_id BIGINT NULL,
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_WordErrorStats_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        row_version ROWVERSION NOT NULL,
        CONSTRAINT FK_WordErrorStats_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id) ON DELETE CASCADE,
        CONSTRAINT FK_WordErrorStats_LastSentence FOREIGN KEY (last_sentence_id) REFERENCES dbo.Lesson_Sentences(sentence_id),
        CONSTRAINT UQ_WordErrorStats_User_Word UNIQUE (user_id, normalized_word),
        CONSTRAINT CK_WordErrorStats_Consecutive CHECK (consecutive_error_count >= 0),
        CONSTRAINT CK_WordErrorStats_Total CHECK (total_error_count >= consecutive_error_count)
    );
END
GO

IF OBJECT_ID('dbo.Vocabulary_Items', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Vocabulary_Items (
        vocabulary_item_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Vocabulary_Items PRIMARY KEY,
        user_id BIGINT NOT NULL,
        normalized_word NVARCHAR(100) NOT NULL,
        display_word NVARCHAR(100) NOT NULL,
        language_code VARCHAR(10) NOT NULL CONSTRAINT DF_VocabularyItems_Language DEFAULT ('en'),
        ipa NVARCHAR(100) NULL,
        meaning NVARCHAR(MAX) NULL,
        note NVARCHAR(MAX) NULL,
        example_sentence NVARCHAR(MAX) NULL,
        source_sentence_id BIGINT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_VocabularyItems_CreatedAt DEFAULT (SYSUTCDATETIME()),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_VocabularyItems_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        row_version ROWVERSION NOT NULL,
        CONSTRAINT FK_VocabularyItems_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id) ON DELETE CASCADE,
        CONSTRAINT FK_VocabularyItems_SourceSentence FOREIGN KEY (source_sentence_id) REFERENCES dbo.Lesson_Sentences(sentence_id),
        CONSTRAINT UQ_VocabularyItems_User_Word_Language UNIQUE (user_id, normalized_word, language_code)
    );
END
GO

IF OBJECT_ID('dbo.Favorite_Sentences', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Favorite_Sentences (
        favorite_sentence_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Favorite_Sentences PRIMARY KEY,
        user_id BIGINT NOT NULL,
        sentence_id BIGINT NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_FavoriteSentences_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_FavoriteSentences_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id) ON DELETE CASCADE,
        CONSTRAINT FK_FavoriteSentences_Sentence FOREIGN KEY (sentence_id) REFERENCES dbo.Lesson_Sentences(sentence_id),
        CONSTRAINT UQ_FavoriteSentences_User_Sentence UNIQUE (user_id, sentence_id)
    );
END
GO

IF OBJECT_ID('dbo.User_Settings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.User_Settings (
        user_settings_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_User_Settings PRIMARY KEY,
        user_id BIGINT NOT NULL,
        auto_save_ai_lessons BIT NOT NULL CONSTRAINT DF_UserSettings_AutoSave DEFAULT (0),
        show_translation BIT NOT NULL CONSTRAINT DF_UserSettings_Translation DEFAULT (1),
        show_captions BIT NOT NULL CONSTRAINT DF_UserSettings_Captions DEFAULT (1),
        theme VARCHAR(10) NOT NULL CONSTRAINT DF_UserSettings_Theme DEFAULT ('system'),
        playback_rate DECIMAL(3,2) NOT NULL CONSTRAINT DF_UserSettings_Playback DEFAULT (1),
        created_at DATETIME2 NOT NULL CONSTRAINT DF_UserSettings_CreatedAt DEFAULT (SYSUTCDATETIME()),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_UserSettings_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        row_version ROWVERSION NOT NULL,
        CONSTRAINT FK_UserSettings_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id) ON DELETE CASCADE,
        CONSTRAINT UQ_UserSettings_User UNIQUE (user_id),
        CONSTRAINT CK_UserSettings_Theme CHECK (theme IN ('system','light','dark')),
        CONSTRAINT CK_UserSettings_PlaybackRate CHECK (playback_rate BETWEEN 0.5 AND 2.0)
    );
END
GO

INSERT INTO dbo.User_Settings (user_id)
SELECT u.user_id FROM dbo.Users AS u
WHERE NOT EXISTS (SELECT 1 FROM dbo.User_Settings AS s WHERE s.user_id = u.user_id);
GO

IF OBJECT_ID('dbo.Mode_Change_History', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Mode_Change_History (
        mode_change_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Mode_Change_History PRIMARY KEY,
        user_id BIGINT NOT NULL,
        from_mode VARCHAR(20) NOT NULL,
        to_mode VARCHAR(20) NOT NULL,
        changed_by VARCHAR(20) NOT NULL,
        reason NVARCHAR(500) NULL,
        changed_at DATETIME2 NOT NULL CONSTRAINT DF_ModeChangeHistory_ChangedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_ModeChangeHistory_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT CK_ModeChangeHistory_FromMode CHECK (from_mode IN ('casual','academic','professional')),
        CONSTRAINT CK_ModeChangeHistory_ToMode CHECK (to_mode IN ('casual','academic','professional')),
        CONSTRAINT CK_ModeChangeHistory_ChangedBy CHECK (changed_by IN ('user','admin','system','onboarding')),
        CONSTRAINT CK_ModeChangeHistory_ActualChange CHECK (from_mode <> to_mode)
    );
    CREATE INDEX IX_ModeChangeHistory_User_ChangedAt ON dbo.Mode_Change_History(user_id, changed_at);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Mode_Change_History') AND name = 'IX_ModeChangeHistory_User_ChangedAt')
    CREATE INDEX IX_ModeChangeHistory_User_ChangedAt ON dbo.Mode_Change_History(user_id, changed_at);
GO

/* Reuse and expand the v0.1 saved lesson table so existing rows remain intact. */
IF OBJECT_ID('dbo.User_Saved_Lessons', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.User_Saved_Lessons (
        saved_lesson_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_User_Saved_Lessons PRIMARY KEY,
        user_id BIGINT NOT NULL,
        title NVARCHAR(255) NOT NULL,
        learning_mode VARCHAR(20) NOT NULL,
        content NVARCHAR(MAX) NOT NULL,
        source_provider NVARCHAR(100) NULL,
        source_id NVARCHAR(255) NULL,
        media_url NVARCHAR(MAX) NULL,
        license_note NVARCHAR(1000) NULL,
        source_review_status VARCHAR(20) NOT NULL CONSTRAINT DF_UserSavedLessons_ReviewStatus DEFAULT ('pending'),
        source_reviewed_at DATETIME2 NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_UserSavedLessons_CreatedAt DEFAULT (SYSUTCDATETIME()),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_UserSavedLessons_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        row_version ROWVERSION NOT NULL,
        CONSTRAINT FK_UserSavedLessons_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id) ON DELETE CASCADE,
        CONSTRAINT CK_UserSavedLessons_LearningMode CHECK (learning_mode IN ('casual','academic','professional')),
        CONSTRAINT CK_UserSavedLessons_SourceReviewStatus CHECK (source_review_status IN ('pending','approved','rejected'))
    );
END
GO

IF COL_LENGTH('dbo.User_Saved_Lessons', 'source_provider') IS NULL ALTER TABLE dbo.User_Saved_Lessons ADD source_provider NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.User_Saved_Lessons', 'source_id') IS NULL ALTER TABLE dbo.User_Saved_Lessons ADD source_id NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.User_Saved_Lessons', 'media_url') IS NULL ALTER TABLE dbo.User_Saved_Lessons ADD media_url NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.User_Saved_Lessons', 'license_note') IS NULL ALTER TABLE dbo.User_Saved_Lessons ADD license_note NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.User_Saved_Lessons', 'source_review_status') IS NULL ALTER TABLE dbo.User_Saved_Lessons ADD source_review_status VARCHAR(20) NOT NULL CONSTRAINT DF_UserSavedLessons_ReviewStatus_Upgrade DEFAULT ('pending');
IF COL_LENGTH('dbo.User_Saved_Lessons', 'source_reviewed_at') IS NULL ALTER TABLE dbo.User_Saved_Lessons ADD source_reviewed_at DATETIME2 NULL;
IF COL_LENGTH('dbo.User_Saved_Lessons', 'row_version') IS NULL ALTER TABLE dbo.User_Saved_Lessons ADD row_version ROWVERSION NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.User_Saved_Lessons') AND name = 'CK_UserSavedLessons_SourceReviewStatus')
    ALTER TABLE dbo.User_Saved_Lessons WITH NOCHECK ADD CONSTRAINT CK_UserSavedLessons_SourceReviewStatus
        CHECK (source_review_status IN ('pending','approved','rejected'));
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.User_Saved_Lessons') AND name = 'IX_UserSavedLessons_User_UpdatedAt')
    CREATE INDEX IX_UserSavedLessons_User_UpdatedAt ON dbo.User_Saved_Lessons(user_id, updated_at);
GO

IF OBJECT_ID('dbo.Saved_AI_Lesson_Segments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Saved_AI_Lesson_Segments (
        saved_segment_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Saved_AI_Lesson_Segments PRIMARY KEY,
        saved_lesson_id BIGINT NOT NULL,
        segment_order INT NOT NULL,
        [text] NVARCHAR(MAX) NOT NULL,
        translation NVARCHAR(MAX) NULL,
        speaker NVARCHAR(100) NULL,
        start_ms INT NULL,
        end_ms INT NULL,
        CONSTRAINT FK_SavedAiLessonSegments_Lesson FOREIGN KEY (saved_lesson_id) REFERENCES dbo.User_Saved_Lessons(saved_lesson_id) ON DELETE CASCADE,
        CONSTRAINT UQ_SavedAiLessonSegments_Lesson_Order UNIQUE (saved_lesson_id, segment_order),
        CONSTRAINT CK_SavedAiLessonSegments_Order CHECK (segment_order >= 0),
        CONSTRAINT CK_SavedAiLessonSegments_Timestamps CHECK ((start_ms IS NULL AND end_ms IS NULL) OR (start_ms >= 0 AND end_ms > start_ms))
    );
END
GO

INSERT INTO dbo.Saved_AI_Lesson_Segments (saved_lesson_id, segment_order, [text])
SELECT l.saved_lesson_id, 0, l.content
FROM dbo.User_Saved_Lessons AS l
WHERE NOT EXISTS (SELECT 1 FROM dbo.Saved_AI_Lesson_Segments AS s WHERE s.saved_lesson_id = l.saved_lesson_id);
GO

IF OBJECT_ID('dbo.Practice_Attempts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Practice_Attempts (
        attempt_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Practice_Attempts PRIMARY KEY,
        user_id BIGINT NOT NULL,
        session_id BIGINT NULL,
        sentence_id BIGINT NULL,
        saved_segment_id BIGINT NULL,
        practice_tab VARCHAR(20) NOT NULL,
        exercise_type VARCHAR(30) NOT NULL,
        target_score DECIMAL(5,2) NOT NULL,
        score DECIMAL(5,2) NULL,
        result VARCHAR(20) NOT NULL,
        assessment_provider NVARCHAR(100) NULL,
        provider_reference_id NVARCHAR(255) NULL,
        transcript_text NVARCHAR(MAX) NULL,
        feedback_text NVARCHAR(MAX) NULL,
        idempotency_key NVARCHAR(100) NOT NULL,
        attempted_at DATETIME2 NOT NULL CONSTRAINT DF_PracticeAttempts_AttemptedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_PracticeAttempts_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT FK_PracticeAttempts_Session FOREIGN KEY (session_id) REFERENCES dbo.Practice_Sessions(session_id),
        CONSTRAINT FK_PracticeAttempts_Sentence FOREIGN KEY (sentence_id) REFERENCES dbo.Lesson_Sentences(sentence_id),
        CONSTRAINT FK_PracticeAttempts_SavedSegment FOREIGN KEY (saved_segment_id) REFERENCES dbo.Saved_AI_Lesson_Segments(saved_segment_id),
        CONSTRAINT UQ_PracticeAttempts_User_Idempotency UNIQUE (user_id, idempotency_key),
        CONSTRAINT CK_PracticeAttempts_Tab CHECK (practice_tab IN ('shadowing','ai-dialogue','dictation','ipa-match')),
        CONSTRAINT CK_PracticeAttempts_Exercise CHECK (exercise_type IN ('pronunciation','shadowing','dictation','ipa_match','ai_dialogue')),
        CONSTRAINT CK_PracticeAttempts_Result CHECK (result IN ('pending','passed','failed','abandoned')),
        CONSTRAINT CK_PracticeAttempts_TargetScore CHECK (target_score BETWEEN 0 AND 100),
        CONSTRAINT CK_PracticeAttempts_Score CHECK (score IS NULL OR score BETWEEN 0 AND 100),
        CONSTRAINT CK_PracticeAttempts_Source CHECK ((CASE WHEN sentence_id IS NULL THEN 0 ELSE 1 END + CASE WHEN saved_segment_id IS NULL THEN 0 ELSE 1 END) = 1)
    );
    CREATE INDEX IX_PracticeAttempts_User_AttemptedAt ON dbo.Practice_Attempts(user_id, attempted_at);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Practice_Attempts') AND name = 'IX_PracticeAttempts_User_AttemptedAt')
    CREATE INDEX IX_PracticeAttempts_User_AttemptedAt ON dbo.Practice_Attempts(user_id, attempted_at);
GO

/* Immutable reward/penalty/exchange audit. Unique sources make retries safe. */
IF OBJECT_ID('dbo.Gamification_Ledger', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Gamification_Ledger (
        ledger_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Gamification_Ledger PRIMARY KEY,
        user_id BIGINT NOT NULL,
        attempt_id BIGINT NULL,
        source_type VARCHAR(30) NOT NULL,
        source_id NVARCHAR(200) NOT NULL,
        reason NVARCHAR(100) NOT NULL,
        exp_delta INT NOT NULL CONSTRAINT DF_GamificationLedger_ExpDelta DEFAULT (0),
        hearts_delta INT NOT NULL CONSTRAINT DF_GamificationLedger_HeartsDelta DEFAULT (0),
        streak_delta INT NOT NULL CONSTRAINT DF_GamificationLedger_StreakDelta DEFAULT (0),
        exp_balance INT NOT NULL,
        hearts_balance INT NOT NULL,
        streak_balance INT NOT NULL,
        is_vip BIT NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_GamificationLedger_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_GamificationLedger_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT FK_GamificationLedger_Attempt FOREIGN KEY (attempt_id) REFERENCES dbo.Practice_Attempts(attempt_id),
        CONSTRAINT UQ_GamificationLedger_User_Source UNIQUE (user_id, source_type, source_id),
        CONSTRAINT CK_GamificationLedger_SourceType CHECK (source_type IN ('sentence_completion','attempt_penalty','daily_activity','heart_exchange')),
        CONSTRAINT CK_GamificationLedger_ExpBalance CHECK (exp_balance >= 0),
        CONSTRAINT CK_GamificationLedger_HeartsBalance CHECK (hearts_balance >= 0),
        CONSTRAINT CK_GamificationLedger_StreakBalance CHECK (streak_balance >= 0)
    );
    CREATE INDEX IX_GamificationLedger_User_CreatedAt ON dbo.Gamification_Ledger(user_id, created_at);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Gamification_Ledger') AND name = 'IX_GamificationLedger_User_CreatedAt')
    CREATE INDEX IX_GamificationLedger_User_CreatedAt ON dbo.Gamification_Ledger(user_id, created_at);
GO

IF OBJECT_ID('dbo.VIP_Subscriptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VIP_Subscriptions (
        subscription_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VIP_Subscriptions PRIMARY KEY,
        user_id BIGINT NOT NULL,
        plan_code VARCHAR(50) NOT NULL,
        billing_period VARCHAR(20) NOT NULL,
        status VARCHAR(20) NOT NULL,
        provider NVARCHAR(100) NOT NULL,
        provider_subscription_id NVARCHAR(255) NOT NULL,
        starts_at DATETIME2 NOT NULL,
        ends_at DATETIME2 NULL,
        cancelled_at DATETIME2 NULL,
        auto_renew BIT NOT NULL CONSTRAINT DF_VipSubscriptions_AutoRenew DEFAULT (0),
        created_at DATETIME2 NOT NULL CONSTRAINT DF_VipSubscriptions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        updated_at DATETIME2 NOT NULL CONSTRAINT DF_VipSubscriptions_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        row_version ROWVERSION NOT NULL,
        CONSTRAINT FK_VipSubscriptions_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT UQ_VipSubscriptions_Provider_Id UNIQUE (provider, provider_subscription_id),
        CONSTRAINT CK_VipSubscriptions_BillingPeriod CHECK (billing_period IN ('monthly','yearly','lifetime')),
        CONSTRAINT CK_VipSubscriptions_Status CHECK (status IN ('pending','active','past_due','cancelled','expired')),
        CONSTRAINT CK_VipSubscriptions_Dates CHECK (ends_at IS NULL OR ends_at > starts_at)
    );
    CREATE INDEX IX_VipSubscriptions_User_Status ON dbo.VIP_Subscriptions(user_id, status);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.VIP_Subscriptions') AND name = 'IX_VipSubscriptions_User_Status')
    CREATE INDEX IX_VipSubscriptions_User_Status ON dbo.VIP_Subscriptions(user_id, status);
GO

IF OBJECT_ID('dbo.Payment_Transactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payment_Transactions (
        payment_transaction_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payment_Transactions PRIMARY KEY,
        user_id BIGINT NOT NULL,
        subscription_id BIGINT NULL,
        provider NVARCHAR(100) NOT NULL,
        provider_transaction_id NVARCHAR(255) NOT NULL,
        idempotency_key NVARCHAR(100) NOT NULL,
        transaction_type VARCHAR(20) NOT NULL,
        status VARCHAR(20) NOT NULL,
        amount DECIMAL(18,2) NOT NULL,
        currency CHAR(3) NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_PaymentTransactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        processed_at DATETIME2 NULL,
        CONSTRAINT FK_PaymentTransactions_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT FK_PaymentTransactions_Subscription FOREIGN KEY (subscription_id) REFERENCES dbo.VIP_Subscriptions(subscription_id),
        CONSTRAINT UQ_PaymentTransactions_Provider_Idempotency UNIQUE (provider, idempotency_key),
        CONSTRAINT UQ_PaymentTransactions_Provider_Transaction UNIQUE (provider, provider_transaction_id),
        CONSTRAINT CK_PaymentTransactions_Type CHECK (transaction_type IN ('purchase','renewal','refund')),
        CONSTRAINT CK_PaymentTransactions_Status CHECK (status IN ('pending','succeeded','failed','refunded')),
        CONSTRAINT CK_PaymentTransactions_Amount CHECK (amount >= 0),
        CONSTRAINT CK_PaymentTransactions_Currency CHECK (LEN(currency) = 3 AND currency = UPPER(currency))
    );
    CREATE INDEX IX_PaymentTransactions_User_CreatedAt ON dbo.Payment_Transactions(user_id, created_at);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Payment_Transactions') AND name = 'IX_PaymentTransactions_User_CreatedAt')
    CREATE INDEX IX_PaymentTransactions_User_CreatedAt ON dbo.Payment_Transactions(user_id, created_at);
GO

/* Existing practice history now prevents deleting its user/lesson instead of cascading history away. */
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.Practice_Sessions') AND name = 'FK_Session_User')
BEGIN
    ALTER TABLE dbo.Practice_Sessions DROP CONSTRAINT FK_Session_User;
    ALTER TABLE dbo.Practice_Sessions ADD CONSTRAINT FK_Session_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id);
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.Practice_Sessions') AND name = 'FK_Session_Lesson')
BEGIN
    ALTER TABLE dbo.Practice_Sessions DROP CONSTRAINT FK_Session_Lesson;
    ALTER TABLE dbo.Practice_Sessions ADD CONSTRAINT FK_Session_Lesson FOREIGN KEY (lesson_id) REFERENCES dbo.Lessons(lesson_id);
END
GO
