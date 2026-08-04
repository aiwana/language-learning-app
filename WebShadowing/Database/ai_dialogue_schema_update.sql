IF OBJECT_ID('dbo.AI_Dialogue_Sessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_Dialogue_Sessions (
        dialogue_session_id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id BIGINT NOT NULL,
        lesson_id BIGINT NULL,
        learning_mode VARCHAR(20) NOT NULL,
        status VARCHAR(20) NOT NULL CONSTRAINT DF_AiDialogueSessions_Status DEFAULT ('active'),
        turn_count INT NOT NULL CONSTRAINT DF_AiDialogueSessions_TurnCount DEFAULT (0),
        created_at DATETIME2 NOT NULL CONSTRAINT DF_AiDialogueSessions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        last_activity_at DATETIME2 NOT NULL CONSTRAINT DF_AiDialogueSessions_LastActivityAt DEFAULT (SYSUTCDATETIME()),
        ended_at DATETIME2 NULL,
        CONSTRAINT FK_AiDialogueSessions_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id),
        CONSTRAINT FK_AiDialogueSessions_Lesson FOREIGN KEY (lesson_id) REFERENCES dbo.Lessons(lesson_id) ON DELETE SET NULL,
        CONSTRAINT CK_AiDialogueSessions_Mode CHECK (learning_mode IN ('casual','academic','professional')),
        CONSTRAINT CK_AiDialogueSessions_Status CHECK (status IN ('active','completed','expired')),
        CONSTRAINT CK_AiDialogueSessions_TurnCount CHECK (turn_count >= 0)
    );
    CREATE INDEX IX_AiDialogueSessions_User_LastActivityAt ON dbo.AI_Dialogue_Sessions(user_id, last_activity_at);
END;

IF OBJECT_ID('dbo.AI_Dialogue_Turns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AI_Dialogue_Turns (
        dialogue_turn_id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        dialogue_session_id BIGINT NOT NULL,
        role VARCHAR(20) NOT NULL,
        [text] NVARCHAR(MAX) NOT NULL,
        audio_url NVARCHAR(2048) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_AiDialogueTurns_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AiDialogueTurns_Session FOREIGN KEY (dialogue_session_id) REFERENCES dbo.AI_Dialogue_Sessions(dialogue_session_id) ON DELETE CASCADE,
        CONSTRAINT CK_AiDialogueTurns_Role CHECK (role IN ('user','assistant'))
    );
    CREATE INDEX IX_AiDialogueTurns_Session_CreatedAt ON dbo.AI_Dialogue_Turns(dialogue_session_id, created_at);
END;
