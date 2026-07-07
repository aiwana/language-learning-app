/*
Idempotent database update for authentication support.
Run this script on the target SQL Server instance before starting the app.
*/

SET NOCOUNT ON;
GO

IF COL_LENGTH('Users', 'phone') IS NULL
BEGIN
    ALTER TABLE Users ADD phone NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH('Users', 'learning_mode') IS NULL
BEGIN
    ALTER TABLE Users ADD learning_mode VARCHAR(20) NOT NULL CONSTRAINT DF_Users_LearningMode DEFAULT ('casual');
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Users_LearningMode' AND parent_object_id = OBJECT_ID('Users'))
    BEGIN
        ALTER TABLE Users ADD CONSTRAINT DF_Users_LearningMode DEFAULT ('casual') FOR learning_mode;
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Users_LearningMode')
BEGIN
    ALTER TABLE Users WITH NOCHECK ADD CONSTRAINT CK_Users_LearningMode CHECK (learning_mode IN ('casual','academic','professional'));
END
GO

IF COL_LENGTH('Users', 'pronunciation_target') IS NULL
BEGIN
    ALTER TABLE Users ADD pronunciation_target TINYINT NOT NULL CONSTRAINT DF_Users_PronunciationTarget DEFAULT (70);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Users_PronunciationTarget' AND parent_object_id = OBJECT_ID('Users'))
    BEGIN
        ALTER TABLE Users ADD CONSTRAINT DF_Users_PronunciationTarget DEFAULT (70) FOR pronunciation_target;
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Users_PronunciationTarget')
BEGIN
    ALTER TABLE Users WITH NOCHECK ADD CONSTRAINT CK_Users_PronunciationTarget CHECK (pronunciation_target IN (50,70,90));
END
GO

IF COL_LENGTH('Users', 'accent') IS NULL
BEGIN
    ALTER TABLE Users ADD accent VARCHAR(10) NOT NULL CONSTRAINT DF_Users_Accent DEFAULT ('en-us');
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Users_Accent' AND parent_object_id = OBJECT_ID('Users'))
    BEGIN
        ALTER TABLE Users ADD CONSTRAINT DF_Users_Accent DEFAULT ('en-us') FOR accent;
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Users_Accent')
BEGIN
    ALTER TABLE Users WITH NOCHECK ADD CONSTRAINT CK_Users_Accent CHECK (accent IN ('en-us','en-gb'));
END
GO

IF COL_LENGTH('Users', 'is_vip') IS NULL
BEGIN
    ALTER TABLE Users ADD is_vip BIT NOT NULL CONSTRAINT DF_Users_IsVip DEFAULT (0);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Users_IsVip' AND parent_object_id = OBJECT_ID('Users'))
    BEGIN
        ALTER TABLE Users ADD CONSTRAINT DF_Users_IsVip DEFAULT (0) FOR is_vip;
    END
END
GO

IF COL_LENGTH('Users', 'onboarding_completed') IS NULL
BEGIN
    ALTER TABLE Users ADD onboarding_completed BIT NOT NULL CONSTRAINT DF_Users_OnboardingCompleted DEFAULT (0);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Users_OnboardingCompleted' AND parent_object_id = OBJECT_ID('Users'))
    BEGIN
        ALTER TABLE Users ADD CONSTRAINT DF_Users_OnboardingCompleted DEFAULT (0) FOR onboarding_completed;
    END
END
GO

IF COL_LENGTH('User_Statistics', 'hearts') IS NULL
BEGIN
    ALTER TABLE User_Statistics ADD hearts INT NOT NULL CONSTRAINT DF_UserStatistics_Hearts DEFAULT (5);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_UserStatistics_Hearts' AND parent_object_id = OBJECT_ID('User_Statistics'))
    BEGIN
        ALTER TABLE User_Statistics ADD CONSTRAINT DF_UserStatistics_Hearts DEFAULT (5) FOR hearts;
    END
END
GO

IF COL_LENGTH('User_Statistics', 'exp') IS NULL
BEGIN
    ALTER TABLE User_Statistics ADD exp INT NOT NULL CONSTRAINT DF_UserStatistics_Exp DEFAULT (0);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_UserStatistics_Exp' AND parent_object_id = OBJECT_ID('User_Statistics'))
    BEGIN
        ALTER TABLE User_Statistics ADD CONSTRAINT DF_UserStatistics_Exp DEFAULT (0) FOR exp;
    END
END
GO

IF COL_LENGTH('Courses', 'learning_mode') IS NULL
BEGIN
    ALTER TABLE Courses ADD learning_mode VARCHAR(20) NOT NULL CONSTRAINT DF_Courses_LearningMode DEFAULT ('casual');
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Courses_LearningMode' AND parent_object_id = OBJECT_ID('Courses'))
    BEGIN
        ALTER TABLE Courses ADD CONSTRAINT DF_Courses_LearningMode DEFAULT ('casual') FOR learning_mode;
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Courses_LearningMode')
BEGIN
    ALTER TABLE Courses WITH NOCHECK ADD CONSTRAINT CK_Courses_LearningMode CHECK (learning_mode IN ('casual','academic','professional'));
END
GO
