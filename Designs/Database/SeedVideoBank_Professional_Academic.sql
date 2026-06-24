-- Nạp 1 video Professional + 1 video Học Thuật (idempotent)
-- Chạy SAU: UpdateDatabase.sql
-- sqlcmd -S localhost -d EnglishShadowingDB -E -f 65001 -i SeedVideoBank_Professional_Academic.sql

USE EnglishShadowingDB;
GO

SET NOCOUNT ON;
GO

DECLARE @now DATETIME2 = GETUTCDATE();
DECLARE @professionalCourseId BIGINT;
DECLARE @academicCourseId BIGINT;
DECLARE @lessonInterview BIGINT;
DECLARE @lessonMl BIGINT;
DECLARE @orderInterview INT;
DECLARE @orderMl INT;

-- ----- Course: Video Bank — Professional -----
SELECT @professionalCourseId = course_id
FROM dbo.Courses
WHERE course_type = N'video_bank' AND title = N'Video Bank — Professional';

IF @professionalCourseId IS NULL
BEGIN
    RAISERROR(N'Chưa có course Video Bank — Professional. Chạy UpdateDatabase.sql trước.', 16, 1);
    RETURN;
END

-- ----- Course: Video Bank — Học Thuật -----
SELECT @academicCourseId = course_id
FROM dbo.Courses
WHERE course_type = N'video_bank' AND title = N'Video Bank — Học Thuật';

IF @academicCourseId IS NULL
BEGIN
    RAISERROR(N'Chưa có course Video Bank — Học Thuật. Chạy UpdateDatabase.sql trước.', 16, 1);
    RETURN;
END

-- ========== Video 1: Job Interview (w0YQwglgtTM) → Professional ==========
SET @lessonInterview = NULL;

SELECT TOP 1 @lessonInterview = m.lesson_id
FROM dbo.Lesson_Material m
WHERE m.content_url LIKE N'%w0YQwglgtTM%';

IF @lessonInterview IS NULL
BEGIN
    SELECT TOP 1 @lessonInterview = lesson_id
    FROM dbo.Lessons
    WHERE title LIKE N'Job Interview%Personnel%'
       OR title LIKE N'Job Interview — Office%'
    ORDER BY lesson_id;
END

IF @lessonInterview IS NULL
BEGIN
    SET @orderInterview = ISNULL((
        SELECT MAX(lesson_order) FROM dbo.Lessons WHERE course_id = @professionalCourseId
    ), 0) + 1;

    INSERT INTO dbo.Lessons (course_id, title, description, lesson_order, duration, source, created_by_user_id)
    VALUES (
        @professionalCourseId,
        N'Job Interview — Office Position',
        N'Phỏng vấn xin việc văn phòng — Mrs. Stevens và Giám đốc Nhân sự.',
        @orderInterview,
        113,
        N'curated',
        NULL
    );
    SET @lessonInterview = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.Lessons
    SET course_id = @professionalCourseId,
        title = N'Job Interview — Office Position',
        description = N'Phỏng vấn xin việc văn phòng — Mrs. Stevens và Giám đốc Nhân sự.',
        duration = 113
    WHERE lesson_id = @lessonInterview;
END

IF NOT EXISTS (
    SELECT 1 FROM dbo.Lesson_Material
    WHERE lesson_id = @lessonInterview AND material_type = N'video'
)
    INSERT INTO dbo.Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@lessonInterview, N'video', N'https://www.youtube.com/watch?v=w0YQwglgtTM');

IF NOT EXISTS (
    SELECT 1 FROM dbo.Lesson_Material
    WHERE lesson_id = @lessonInterview AND material_type = N'transcript'
)
    INSERT INTO dbo.Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@lessonInterview, N'transcript', N'/media/video-bank/job-interview-personnel/transcript.txt');

-- ========== Video 2: Machine Learning (VhfS-G6PDLE) → Học Thuật ==========
SET @lessonMl = NULL;

SELECT TOP 1 @lessonMl = m.lesson_id
FROM dbo.Lesson_Material m
WHERE m.content_url LIKE N'%VhfS-G6PDLE%';

IF @lessonMl IS NULL
BEGIN
    SELECT TOP 1 @lessonMl = lesson_id
    FROM dbo.Lessons
    WHERE title LIKE N'Machine Learning%'
    ORDER BY lesson_id;
END

IF @lessonMl IS NULL
BEGIN
    SET @orderMl = ISNULL((
        SELECT MAX(lesson_order) FROM dbo.Lessons WHERE course_id = @academicCourseId
    ), 0) + 1;

    INSERT INTO dbo.Lessons (course_id, title, description, lesson_order, duration, source, created_by_user_id)
    VALUES (
        @academicCourseId,
        N'Machine Learning — Explain Like I''m Five',
        N'Giải thích machine learning đơn giản qua ví dụ robot và ảnh mèo/chó.',
        @orderMl,
        42,
        N'curated',
        NULL
    );
    SET @lessonMl = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.Lessons
    SET course_id = @academicCourseId,
        title = N'Machine Learning — Explain Like I''m Five',
        description = N'Giải thích machine learning đơn giản qua ví dụ robot và ảnh mèo/chó.',
        duration = 42
    WHERE lesson_id = @lessonMl;
END

IF NOT EXISTS (
    SELECT 1 FROM dbo.Lesson_Material
    WHERE lesson_id = @lessonMl AND material_type = N'video'
)
    INSERT INTO dbo.Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@lessonMl, N'video', N'https://www.youtube.com/watch?v=VhfS-G6PDLE');

IF NOT EXISTS (
    SELECT 1 FROM dbo.Lesson_Material
    WHERE lesson_id = @lessonMl AND material_type = N'transcript'
)
    INSERT INTO dbo.Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@lessonMl, N'transcript', N'/media/video-bank/ml-explained-simply/transcript.txt');

-- ========== Kết quả ==========
SELECT c.course_id, c.title AS course_title, l.lesson_id, l.lesson_order, l.title AS lesson_title,
       m.material_type, m.content_url
FROM dbo.Courses c
JOIN dbo.Lessons l ON l.course_id = c.course_id
LEFT JOIN dbo.Lesson_Material m ON m.lesson_id = l.lesson_id
WHERE c.course_id IN (@professionalCourseId, @academicCourseId)
ORDER BY c.course_id, l.lesson_order, m.material_id;
GO
