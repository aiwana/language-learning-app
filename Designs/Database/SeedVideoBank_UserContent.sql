-- Nạp 2 video YouTube vào Video Bank — Đời sống (idempotent — chạy lại an toàn)
-- Chạy SAU: UpdateDatabase.sql (STEP 6 trong UpdateDatabase đã bao gồm nội dung này — file này tùy chọn)
-- Khuyến nghị: sqlcmd -S localhost -d EnglishShadowingDB -E -f 65001 -i SeedVideoBank_UserContent.sql

USE EnglishShadowingDB;
GO

SET NOCOUNT ON;
GO

DECLARE @videoBankCourseId BIGINT;
DECLARE @lesson1 BIGINT;
DECLARE @lesson2 BIGINT;
DECLARE @order1 INT;
DECLARE @order2 INT;

-- Course Video Bank — Đời sống
SELECT TOP 1 @videoBankCourseId = c.course_id
FROM dbo.Courses c
WHERE c.course_type = 'video_bank'
  AND c.title = N'Video Bank — Đời sống'
ORDER BY c.course_id;

IF @videoBankCourseId IS NULL
BEGIN
    SELECT TOP 1 @videoBankCourseId = c.course_id
    FROM dbo.Courses c
    WHERE c.course_type = 'video_bank'
      AND EXISTS (
          SELECT 1 FROM dbo.Lessons l
          INNER JOIN dbo.Lesson_Material m ON m.lesson_id = l.lesson_id
          WHERE l.course_id = c.course_id
            AND (m.content_url LIKE N'%Y5Gmuq6y9l8%' OR m.content_url LIKE N'%iilcmCTUIyE%')
      )
    ORDER BY c.course_id;
END

IF @videoBankCourseId IS NULL
BEGIN
    SELECT TOP 1 @videoBankCourseId = course_id
    FROM dbo.Courses
    WHERE course_type = 'video_bank'
    ORDER BY course_id;
END

IF @videoBankCourseId IS NULL
BEGIN
    INSERT INTO dbo.Courses (title, description, level, course_type, created_at, updated_at)
    VALUES (
        N'Video Bank — Đời sống',
        N'Video ngắn YouTube cho shadowing.',
        N'Beginner',
        N'video_bank',
        GETUTCDATE(),
        GETUTCDATE()
    );
    SET @videoBankCourseId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.Courses
    SET title = N'Video Bank — Đời sống',
        description = N'Video ngắn YouTube cho shadowing.'
    WHERE course_id = @videoBankCourseId;
END

IF @videoBankCourseId IS NULL
BEGIN
    RAISERROR(N'Không tạo được course Video Bank. Dừng script.', 16, 1);
    RETURN;
END

-- ========== Video 1: Scatter Focus ==========
SET @lesson1 = NULL;

SELECT TOP 1 @lesson1 = m.lesson_id
FROM dbo.Lesson_Material m
WHERE m.content_url LIKE N'%Y5Gmuq6y9l8%';

IF @lesson1 IS NULL
BEGIN
    SELECT TOP 1 @lesson1 = lesson_id
    FROM dbo.Lessons
    WHERE title LIKE N'Scatter Focus%'
    ORDER BY lesson_id;
END

IF @lesson1 IS NULL
BEGIN
    SET @order1 = ISNULL((
        SELECT MAX(lesson_order) FROM dbo.Lessons WHERE course_id = @videoBankCourseId
    ), 0) + 1;

    INSERT INTO dbo.Lessons (course_id, title, description, lesson_order, duration, source, created_by_user_id)
    VALUES (
        @videoBankCourseId,
        N'Scatter Focus — Brilliant Ideas',
        N'Ý tưởng hay thường đến khi ta không cố tập trung — scatter focus.',
        @order1,
        54,
        N'curated',
        NULL
    );
    SET @lesson1 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.Lessons
    SET course_id = @videoBankCourseId,
        title = N'Scatter Focus — Brilliant Ideas',
        description = N'Ý tưởng hay thường đến khi ta không cố tập trung — scatter focus.'
    WHERE lesson_id = @lesson1;
END

IF NOT EXISTS (
    SELECT 1 FROM dbo.Lesson_Material
    WHERE lesson_id = @lesson1 AND material_type = N'video'
)
    INSERT INTO dbo.Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@lesson1, N'video', N'https://www.youtube.com/shorts/Y5Gmuq6y9l8');

IF NOT EXISTS (
    SELECT 1 FROM dbo.Lesson_Material
    WHERE lesson_id = @lesson1 AND material_type = N'transcript'
)
    INSERT INTO dbo.Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@lesson1, N'transcript', N'/media/video-bank/scatter-focus/transcript.txt');

-- ========== Video 2: Lead by Example ==========
SET @lesson2 = NULL;

SELECT TOP 1 @lesson2 = m.lesson_id
FROM dbo.Lesson_Material m
WHERE m.content_url LIKE N'%iilcmCTUIyE%';

IF @lesson2 IS NULL
BEGIN
    SELECT TOP 1 @lesson2 = lesson_id
    FROM dbo.Lessons
    WHERE title LIKE N'Lead by Example%'
    ORDER BY lesson_id;
END

IF @lesson2 IS NULL
BEGIN
    SET @order2 = ISNULL((
        SELECT MAX(lesson_order) FROM dbo.Lessons WHERE course_id = @videoBankCourseId
    ), 0) + 1;

    INSERT INTO dbo.Lessons (course_id, title, description, lesson_order, duration, source, created_by_user_id)
    VALUES (
        @videoBankCourseId,
        N'Lead by Example — Influence',
        N'Cách tốt nhất để ảnh hưởng người khác là tự làm gương.',
        @order2,
        12,
        N'curated',
        NULL
    );
    SET @lesson2 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.Lessons
    SET course_id = @videoBankCourseId,
        title = N'Lead by Example — Influence',
        description = N'Cách tốt nhất để ảnh hưởng người khác là tự làm gương.'
    WHERE lesson_id = @lesson2;
END

IF NOT EXISTS (
    SELECT 1 FROM dbo.Lesson_Material
    WHERE lesson_id = @lesson2 AND material_type = N'video'
)
    INSERT INTO dbo.Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@lesson2, N'video', N'https://www.youtube.com/shorts/iilcmCTUIyE');

IF NOT EXISTS (
    SELECT 1 FROM dbo.Lesson_Material
    WHERE lesson_id = @lesson2 AND material_type = N'transcript'
)
    INSERT INTO dbo.Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@lesson2, N'transcript', N'/media/video-bank/influence-by-doing/transcript.txt');

-- ========== Kết quả ==========
SELECT c.course_id, c.title, c.course_type,
       l.lesson_id, l.lesson_order, l.title AS lesson_title,
       m.material_type, m.content_url
FROM dbo.Courses c
JOIN dbo.Lessons l ON l.course_id = c.course_id
LEFT JOIN dbo.Lesson_Material m ON m.lesson_id = l.lesson_id
WHERE c.course_id = @videoBankCourseId
ORDER BY l.lesson_order, m.material_id;
GO
