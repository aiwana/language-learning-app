/*
===========================================================
 Purpose:
    Extend DatabaseCreation.sql for v0.1 features.
===========================================================
*/

USE EnglishShadowingDB;
GO

/*=========================================================
  USERS
=========================================================*/

IF COL_LENGTH('Users', 'phone') IS NULL
BEGIN
    ALTER TABLE Users
    ADD phone VARCHAR(20) NULL;
END
GO

IF COL_LENGTH('Users', 'learning_mode') IS NULL
BEGIN
    ALTER TABLE Users
    ADD learning_mode VARCHAR(20)
        NOT NULL
        CONSTRAINT DF_Users_LearningMode DEFAULT ('casual');
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.check_constraints
    WHERE name = 'CK_Users_LearningMode'
)
BEGIN
    ALTER TABLE Users
    ADD CONSTRAINT CK_Users_LearningMode
    CHECK (
        learning_mode IN (
            'casual',
            'academic',
            'professional'
        )
    );
END
GO

IF COL_LENGTH('Users', 'pronunciation_target') IS NULL
BEGIN
    ALTER TABLE Users
    ADD pronunciation_target TINYINT
        NOT NULL
        CONSTRAINT DF_Users_PronunciationTarget DEFAULT (70);
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.check_constraints
    WHERE name = 'CK_Users_PronunciationTarget'
)
BEGIN
    ALTER TABLE Users
    ADD CONSTRAINT CK_Users_PronunciationTarget
    CHECK (
        pronunciation_target IN (50,70,90)
    );
END
GO

IF COL_LENGTH('Users', 'accent') IS NULL
BEGIN
    ALTER TABLE Users
    ADD accent VARCHAR(10)
        NOT NULL
        CONSTRAINT DF_Users_Accent DEFAULT ('en-us');
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.check_constraints
    WHERE name = 'CK_Users_Accent'
)
BEGIN
    ALTER TABLE Users
    ADD CONSTRAINT CK_Users_Accent
    CHECK (
        accent IN ('en-us','en-gb')
    );
END
GO

IF COL_LENGTH('Users', 'is_vip') IS NULL
BEGIN
    ALTER TABLE Users
    ADD is_vip BIT
        NOT NULL
        CONSTRAINT DF_Users_IsVip DEFAULT (0);
END
GO


/*=========================================================
  USER_STATISTICS
=========================================================*/

IF COL_LENGTH('User_Statistics', 'hearts') IS NULL
BEGIN
    ALTER TABLE User_Statistics
    ADD hearts INT
        NOT NULL
        CONSTRAINT DF_UserStatistics_Hearts DEFAULT (5);
END
GO

IF COL_LENGTH('User_Statistics', 'exp') IS NULL
BEGIN
    ALTER TABLE User_Statistics
    ADD exp INT
        NOT NULL
        CONSTRAINT DF_UserStatistics_Exp DEFAULT (0);
END
GO


/*=========================================================
  COURSES
=========================================================*/

IF COL_LENGTH('Courses', 'learning_mode') IS NULL
BEGIN
    ALTER TABLE Courses
    ADD learning_mode VARCHAR(20)
        NOT NULL
        CONSTRAINT DF_Courses_LearningMode DEFAULT ('casual');
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.check_constraints
    WHERE name = 'CK_Courses_LearningMode'
)
BEGIN
    ALTER TABLE Courses
    ADD CONSTRAINT CK_Courses_LearningMode
    CHECK (
        learning_mode IN (
            'casual',
            'academic',
            'professional'
        )
    );
END
GO


/*=========================================================
  LESSON_SENTENCES
=========================================================*/

IF OBJECT_ID('Lesson_Sentences', 'U') IS NULL
BEGIN
    CREATE TABLE Lesson_Sentences
    (
        sentence_id BIGINT IDENTITY(1,1) PRIMARY KEY,

        lesson_id BIGINT NOT NULL,

        sentence_order INT NOT NULL,

        [text] NVARCHAR(MAX) NOT NULL,

        translation NVARCHAR(MAX) NULL,

        CONSTRAINT FK_LessonSentences_Lesson
            FOREIGN KEY (lesson_id)
            REFERENCES Lessons(lesson_id)
            ON DELETE CASCADE,

        CONSTRAINT UQ_LessonSentences_Order
            UNIQUE (
                lesson_id,
                sentence_order
            )
    );
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name = 'IX_LessonSentences_Lesson_Order'
)
BEGIN
    CREATE INDEX IX_LessonSentences_Lesson_Order
    ON Lesson_Sentences
    (
        lesson_id,
        sentence_order
    );
END
GO