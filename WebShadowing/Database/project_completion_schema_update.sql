/*
  Chức năng: bổ sung schema cho vocabulary/favorite/settings/AI lesson/VIP/dialogue.
  Phụ trách schema: Minh. Hải Anh phối hợp seed và database integration test.
  Cảnh báo: tuy mục tiêu là chạy lặp an toàn, test migration hiện phải luôn được chạy
  trước production; không dựa chỉ vào comment "safe to rerun".
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.Vocabulary_Items', 'review_status') IS NULL
    ALTER TABLE dbo.Vocabulary_Items ADD review_status VARCHAR(20) NOT NULL CONSTRAINT DF_VocabularyItems_ReviewStatus DEFAULT ('active');
IF COL_LENGTH('dbo.Vocabulary_Items', 'last_reviewed_at') IS NULL
    ALTER TABLE dbo.Vocabulary_Items ADD last_reviewed_at DATETIME2 NULL;
IF COL_LENGTH('dbo.Vocabulary_Items', 'review_count') IS NULL
    ALTER TABLE dbo.Vocabulary_Items ADD review_count INT NOT NULL CONSTRAINT DF_VocabularyItems_ReviewCount DEFAULT (0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Vocabulary_Items') AND name = 'CK_VocabularyItems_ReviewStatus')
    ALTER TABLE dbo.Vocabulary_Items WITH NOCHECK ADD CONSTRAINT CK_VocabularyItems_ReviewStatus CHECK (review_status IN ('active','mastered'));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Vocabulary_Items') AND name = 'CK_VocabularyItems_ReviewCount')
    ALTER TABLE dbo.Vocabulary_Items WITH NOCHECK ADD CONSTRAINT CK_VocabularyItems_ReviewCount CHECK (review_count >= 0);
GO

IF COL_LENGTH('dbo.Saved_AI_Lesson_Segments', 'ipa') IS NULL
    ALTER TABLE dbo.Saved_AI_Lesson_Segments ADD ipa NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.Saved_AI_Lesson_Segments', 'audio_url') IS NULL
    ALTER TABLE dbo.Saved_AI_Lesson_Segments ADD audio_url NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.Lesson_Sentences', 'ipa') IS NULL
    ALTER TABLE dbo.Lesson_Sentences ADD ipa NVARCHAR(500) NULL;
GO

IF OBJECT_ID('dbo.AI_Lesson_Previews', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_Lesson_Previews (
        preview_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AI_Lesson_Previews PRIMARY KEY,
        user_id BIGINT NOT NULL,
        prompt NVARCHAR(1000) NOT NULL,
        title NVARCHAR(255) NOT NULL,
        learning_mode VARCHAR(20) NOT NULL,
        accent VARCHAR(10) NOT NULL,
        content_json NVARCHAR(MAX) NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_AiLessonPreviews_CreatedAt DEFAULT (SYSUTCDATETIME()),
        expires_at DATETIME2 NOT NULL,
        saved_lesson_id BIGINT NULL,
        CONSTRAINT FK_AiLessonPreviews_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id) ON DELETE CASCADE,
        CONSTRAINT FK_AiLessonPreviews_SavedLesson FOREIGN KEY (saved_lesson_id) REFERENCES dbo.User_Saved_Lessons(saved_lesson_id),
        CONSTRAINT CK_AiLessonPreviews_LearningMode CHECK (learning_mode IN ('casual','academic','professional'))
    );
    CREATE INDEX IX_AiLessonPreviews_User_ExpiresAt ON dbo.AI_Lesson_Previews(user_id, expires_at);
END
GO

IF OBJECT_ID('dbo.AI_Dialogue_Sessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_Dialogue_Sessions (
        dialogue_session_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AI_Dialogue_Sessions PRIMARY KEY,
        user_id BIGINT NOT NULL,
        lesson_id BIGINT NULL,
        learning_mode VARCHAR(20) NOT NULL,
        status VARCHAR(20) NOT NULL CONSTRAINT DF_AiDialogueSessions_Status DEFAULT ('active'),
        turn_count INT NOT NULL CONSTRAINT DF_AiDialogueSessions_TurnCount DEFAULT (0),
        created_at DATETIME2 NOT NULL CONSTRAINT DF_AiDialogueSessions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        last_activity_at DATETIME2 NOT NULL CONSTRAINT DF_AiDialogueSessions_LastActivityAt DEFAULT (SYSUTCDATETIME()),
        ended_at DATETIME2 NULL,
        CONSTRAINT FK_AiDialogueSessions_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT FK_AiDialogueSessions_Lesson FOREIGN KEY (lesson_id) REFERENCES dbo.Lessons(lesson_id),
        CONSTRAINT CK_AiDialogueSessions_Mode CHECK (learning_mode IN ('casual','academic','professional')),
        CONSTRAINT CK_AiDialogueSessions_Status CHECK (status IN ('active','completed','expired')),
        CONSTRAINT CK_AiDialogueSessions_TurnCount CHECK (turn_count >= 0)
    );
    CREATE INDEX IX_AiDialogueSessions_User_LastActivityAt ON dbo.AI_Dialogue_Sessions(user_id, last_activity_at);
END
GO

IF OBJECT_ID('dbo.AI_Dialogue_Turns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_Dialogue_Turns (
        dialogue_turn_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AI_Dialogue_Turns PRIMARY KEY,
        dialogue_session_id BIGINT NOT NULL,
        role VARCHAR(20) NOT NULL,
        [text] NVARCHAR(MAX) NOT NULL,
        audio_url NVARCHAR(MAX) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_AiDialogueTurns_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AiDialogueTurns_Session FOREIGN KEY (dialogue_session_id) REFERENCES dbo.AI_Dialogue_Sessions(dialogue_session_id) ON DELETE CASCADE,
        CONSTRAINT CK_AiDialogueTurns_Role CHECK (role IN ('user','assistant'))
    );
    CREATE INDEX IX_AiDialogueTurns_Session_CreatedAt ON dbo.AI_Dialogue_Turns(dialogue_session_id, created_at);
END
GO
