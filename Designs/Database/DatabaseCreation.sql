CREATE DATABASE EnglishShadowingDB;
GO

USE EnglishShadowingDB;
GO

CREATE TABLE Users (
    user_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE Courses (
    course_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    title NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    level VARCHAR(20) NOT NULL,
    course_type VARCHAR(20) NOT NULL DEFAULT 'curriculum',
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT CK_Courses_Level
        CHECK (level IN ('Beginner', 'Intermediate', 'Advanced')),

    CONSTRAINT CK_Courses_CourseType
        CHECK (course_type IN ('video_bank', 'curriculum', 'ai_saved'))
);
GO

CREATE TABLE Users_Courses (
    user_id BIGINT NOT NULL,
    course_id BIGINT NOT NULL,
    enrolled_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    progress DECIMAL(5,2) NOT NULL DEFAULT 0,

    CONSTRAINT PK_Users_Courses
        PRIMARY KEY (user_id, course_id),

    CONSTRAINT FK_Users_Courses_User
        FOREIGN KEY (user_id)
        REFERENCES Users(user_id)
        ON DELETE CASCADE,

    CONSTRAINT FK_Users_Courses_Course
        FOREIGN KEY (course_id)
        REFERENCES Courses(course_id)
        ON DELETE CASCADE,

    CONSTRAINT CK_Users_Courses_Progress
        CHECK (progress BETWEEN 0 AND 100)
);
GO

CREATE TABLE Lessons (
    lesson_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    course_id BIGINT NOT NULL,
    title NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    lesson_order INT NOT NULL,
    duration INT NOT NULL, -- seconds
    created_by_user_id BIGINT NULL,
    source VARCHAR(20) NOT NULL DEFAULT 'curated',

    CONSTRAINT FK_Lessons_Course
        FOREIGN KEY (course_id)
        REFERENCES Courses(course_id)
        ON DELETE CASCADE,

    CONSTRAINT FK_Lessons_CreatedByUser
        FOREIGN KEY (created_by_user_id)
        REFERENCES Users(user_id),

    CONSTRAINT CK_Lessons_Source
        CHECK (source IN ('curated', 'ai')),

    CONSTRAINT UQ_Lessons_Course_Order
        UNIQUE (course_id, lesson_order)
);
GO

CREATE TABLE Lesson_Material (
    material_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    lesson_id BIGINT NOT NULL,
    material_type VARCHAR(20) NOT NULL,
    content_url NVARCHAR(MAX) NOT NULL,

    CONSTRAINT FK_Material_Lesson
        FOREIGN KEY (lesson_id)
        REFERENCES Lessons(lesson_id)
        ON DELETE CASCADE,

    CONSTRAINT CK_Material_Type
        CHECK (
            material_type IN (
                'audio',
                'video',
                'transcript',
                'text'
            )
        )
);
GO

CREATE TABLE Practice_Sessions (
    session_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    lesson_id BIGINT NOT NULL,
    started_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    completed_at DATETIME2 NULL,
    overall_score DECIMAL(5,2) NULL,

    CONSTRAINT FK_Session_User
        FOREIGN KEY (user_id)
        REFERENCES Users(user_id)
        ON DELETE CASCADE,

    CONSTRAINT FK_Session_Lesson
        FOREIGN KEY (lesson_id)
        REFERENCES Lessons(lesson_id)
        ON DELETE CASCADE,

    CONSTRAINT CK_Session_Score
        CHECK (
            overall_score IS NULL OR
            overall_score BETWEEN 0 AND 100
        )
);
GO

CREATE TABLE User_Recordings (
    recording_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    session_id BIGINT NOT NULL,
    audio_url NVARCHAR(MAX) NOT NULL,
    duration INT NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Recording_Session
        FOREIGN KEY (session_id)
        REFERENCES Practice_Sessions(session_id)
        ON DELETE CASCADE
);
GO

CREATE TABLE Transcripts (
    transcript_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    recording_id BIGINT NOT NULL,
    transcript_text NVARCHAR(MAX) NOT NULL,
    confidence_score DECIMAL(5,2) NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Transcript_Recording
        FOREIGN KEY (recording_id)
        REFERENCES User_Recordings(recording_id)
        ON DELETE CASCADE,

    CONSTRAINT CK_Transcript_Confidence
        CHECK (
            confidence_score IS NULL OR
            confidence_score BETWEEN 0 AND 100
        )
);
GO

CREATE TABLE AI_Feedback (
    feedback_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    session_id BIGINT NOT NULL,
    pronunciation_score DECIMAL(5,2) NULL,
    fluency_score DECIMAL(5,2) NULL,
    accuracy_score DECIMAL(5,2) NULL,
    feedback_text NVARCHAR(MAX),
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Feedback_Session
        FOREIGN KEY (session_id)
        REFERENCES Practice_Sessions(session_id)
        ON DELETE CASCADE,

    CONSTRAINT CK_Pronunciation_Score
        CHECK (
            pronunciation_score IS NULL OR
            pronunciation_score BETWEEN 0 AND 100
        ),

    CONSTRAINT CK_Fluency_Score
        CHECK (
            fluency_score IS NULL OR
            fluency_score BETWEEN 0 AND 100
        ),

    CONSTRAINT CK_Accuracy_Score
        CHECK (
            accuracy_score IS NULL OR
            accuracy_score BETWEEN 0 AND 100
        )
);
GO

CREATE TABLE User_Statistics (
    stat_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL UNIQUE,
    total_sessions INT NOT NULL DEFAULT 0,
    average_score DECIMAL(5,2) DEFAULT 0,
    streak_days INT NOT NULL DEFAULT 0,
    last_practice_at DATETIME2 NULL,

    CONSTRAINT FK_Statistics_User
        FOREIGN KEY (user_id)
        REFERENCES Users(user_id)
        ON DELETE CASCADE
);
GO