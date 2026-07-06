USE EnglishShadowingDB;
GO

IF COL_LENGTH('Courses','learning_mode') IS NULL
BEGIN
    ALTER TABLE Courses ADD learning_mode VARCHAR(20) NOT NULL CONSTRAINT DF_Courses_learning_mode DEFAULT('casual');
    ALTER TABLE Courses ADD CONSTRAINT CK_Courses_learning_mode CHECK (learning_mode IN ('casual','academic','professional'));
END
GO

IF COL_LENGTH('Courses','course_type') IS NULL
BEGIN
    ALTER TABLE Courses
    ADD course_type VARCHAR(20) NOT NULL
        CONSTRAINT DF_Courses_course_type DEFAULT ('curriculum');
END
GO

IF NOT EXISTS (
    SELECT *
    FROM sys.check_constraints
    WHERE name = 'CK_Courses_course_type'
)
BEGIN
    ALTER TABLE Courses
    ADD CONSTRAINT CK_Courses_course_type
    CHECK (course_type IN ('curriculum','video_bank'));
END
GO

IF OBJECT_ID('dbo.User_Saved_Lessons','U') IS NULL
BEGIN
CREATE TABLE User_Saved_Lessons(
    saved_lesson_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    title NVARCHAR(255) NOT NULL,
    learning_mode VARCHAR(20) NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_UserSavedLessons_User FOREIGN KEY(user_id) REFERENCES Users(user_id) ON DELETE CASCADE,
    CONSTRAINT CK_UserSavedLessons_LearningMode CHECK (learning_mode IN ('casual','academic','professional'))
);
END
GO

IF COL_LENGTH('Courses','course_type') IS NOT NULL
BEGIN
    UPDATE Courses SET course_type='video_bank' WHERE course_type='curriculum' AND title LIKE 'Video Bank%';
    UPDATE Courses SET course_type='curriculum' WHERE course_type IS NULL;
END
GO
