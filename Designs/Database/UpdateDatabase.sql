-- Migration: course_type (Courses) + source, created_by_user_id (Lessons) + Unicode NVARCHAR
--          + 3 khung Video Bank + dọn bài mẫu + 2 lesson Đời sống
-- Chạy trên DB đã tạo từ DatabaseCreation.sql (chưa có các cột mới).
-- Script idempotent: chạy lại an toàn.
--
-- Sau khi chạy, Video Bank gồm:
--   • Video Bank — Professional  (0 lesson)
--   • Video Bank — Đời sống      (Scatter Focus, Lead by Example)
--   • Video Bank — Học Thuật     (0 lesson)
--
-- CÁCH CHẠY (hiển thị tiếng Việt đúng):
--   sqlcmd -S localhost -d EnglishShadowingDB -E -f 65001 -i UpdateDatabase.sql
-- SSMS: File .sql lưu UTF-8; kết quả SELECT hiển thị Unicode trong grid SSMS (không qua cmd cũ).
-- SeedVideoBank_UserContent.sql: tùy chọn (trùng STEP 6 nếu đã chạy file này).
-- SeedVideoBank_Professional_Academic.sql: Professional + Học Thuật (chạy sau UpdateDatabase).
-- FixUnicodeAndData.sql: đã gộp vào STEP 3b, 3c, 8 (file đã xóa).

USE EnglishShadowingDB;
GO

-- ========== STEP 1: Courses.course_type ==========
IF COL_LENGTH('dbo.Courses', 'course_type') IS NULL
BEGIN
    ALTER TABLE dbo.Courses
    ADD course_type VARCHAR(20) NOT NULL
        CONSTRAINT DF_Courses_CourseType DEFAULT 'curriculum';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Courses_CourseType' AND parent_object_id = OBJECT_ID('dbo.Courses')
)
BEGIN
    ALTER TABLE dbo.Courses
    ADD CONSTRAINT CK_Courses_CourseType
        CHECK (course_type IN ('video_bank', 'curriculum', 'ai_saved'));
END
GO

-- ========== STEP 2: Lessons.source + created_by_user_id ==========
IF COL_LENGTH('dbo.Lessons', 'source') IS NULL
BEGIN
    ALTER TABLE dbo.Lessons
    ADD source VARCHAR(20) NOT NULL
        CONSTRAINT DF_Lessons_Source DEFAULT 'curated';
END
GO

IF COL_LENGTH('dbo.Lessons', 'created_by_user_id') IS NULL
BEGIN
    ALTER TABLE dbo.Lessons
    ADD created_by_user_id BIGINT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Lessons_CreatedByUser' AND parent_object_id = OBJECT_ID('dbo.Lessons')
)
BEGIN
    ALTER TABLE dbo.Lessons
    ADD CONSTRAINT FK_Lessons_CreatedByUser
        FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(user_id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Lessons_Source' AND parent_object_id = OBJECT_ID('dbo.Lessons')
)
BEGIN
    ALTER TABLE dbo.Lessons
    ADD CONSTRAINT CK_Lessons_Source
        CHECK (source IN ('curated', 'ai'));
END
GO

-- ========== STEP 3: Gán course_type cho dữ liệu seed hiện có ==========
UPDATE dbo.Courses
SET course_type = 'curriculum'
WHERE title LIKE N'%Beginner%';

UPDATE dbo.Courses
SET course_type = 'video_bank'
WHERE title LIKE N'%Intermediate%'
   OR title LIKE N'%Video Bank%';
GO

-- ========== STEP 3b: Unicode — title NVARCHAR (tiếng Việt) ==========
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Courses') AND name = 'title'
      AND system_type_id = TYPE_ID('varchar')
)
BEGIN
    ALTER TABLE dbo.Courses ALTER COLUMN title NVARCHAR(255) NOT NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Lessons') AND name = 'title'
      AND system_type_id = TYPE_ID('varchar')
)
BEGIN
    ALTER TABLE dbo.Lessons ALTER COLUMN title NVARCHAR(255) NOT NULL;
END
GO

-- ========== STEP 3c: Xóa material video-bank gắn nhầm vào giáo trình (legacy) ==========
DELETE FROM dbo.Lesson_Material
WHERE lesson_id IN (
    SELECT l.lesson_id
    FROM dbo.Lessons l
    INNER JOIN dbo.Courses c ON c.course_id = l.course_id
    WHERE c.course_type = N'curriculum'
      AND l.title LIKE N'My new school%'
)
AND material_type IN (N'video', N'transcript')
AND content_url LIKE N'%video-bank%';
GO

-- ========== STEP 4: Xóa bài giảng mẫu seed ban đầu ==========
DELETE l
FROM dbo.Lessons l
WHERE
    l.title IN (
        N'Greetings & Introductions',
        N'At the Coffee Shop',
        N'Job Interview Warm-up'
    )
    OR l.title LIKE N'Greetings & Introduction%'
    OR l.title LIKE N'At the Coffee Shop%'
    OR l.title LIKE N'Job Interview Warm-up%'
    OR EXISTS (
        SELECT 1
        FROM dbo.Lesson_Material m
        WHERE m.lesson_id = l.lesson_id
          AND (
              m.content_url LIKE N'%/media/beginner/lesson-1/%'
              OR m.content_url LIKE N'%/media/beginner/lesson-2/%'
              OR m.content_url LIKE N'%/media/intermediate/lesson-1/%'
              OR m.content_url LIKE N'%epfPE9CP-xo%'
          )
    );
GO

-- ========== STEP 5–6: 3 khung Video Bank + 2 lesson Đời sống ==========
SET NOCOUNT ON;
GO

DECLARE @now DATETIME2 = GETUTCDATE();
DECLARE @professionalId BIGINT;
DECLARE @doiSongId BIGINT;
DECLARE @hocThuatId BIGINT;
DECLARE @lesson1 BIGINT;
DECLARE @lesson2 BIGINT;
DECLARE @order1 INT;
DECLARE @order2 INT;

-- ----- 5a. Video Bank — Professional (0 lesson) -----
IF NOT EXISTS (
    SELECT 1 FROM dbo.Courses
    WHERE course_type = N'video_bank' AND title = N'Video Bank — Professional'
)
BEGIN
    SELECT TOP 1 @professionalId = course_id
    FROM dbo.Courses
    WHERE course_type = N'video_bank'
      AND title LIKE N'%Intermediate%'
    ORDER BY course_id;

    IF @professionalId IS NOT NULL
    BEGIN
        UPDATE dbo.Courses
        SET title = N'Video Bank — Professional',
            description = N'Video YouTube — phỏng vấn, công sở, kỹ năng nghề nghiệp.',
            level = N'Intermediate',
            updated_at = @now
        WHERE course_id = @professionalId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Courses (title, description, level, course_type, created_at, updated_at)
        VALUES (
            N'Video Bank — Professional',
            N'Video YouTube — phỏng vấn, công sở, kỹ năng nghề nghiệp.',
            N'Intermediate',
            N'video_bank',
            @now,
            @now
        );
        SET @professionalId = SCOPE_IDENTITY();
    END
END
ELSE
BEGIN
    SELECT @professionalId = course_id
    FROM dbo.Courses
    WHERE course_type = N'video_bank' AND title = N'Video Bank — Professional';

    UPDATE dbo.Courses
    SET description = N'Video YouTube — phỏng vấn, công sở, kỹ năng nghề nghiệp.',
        level = N'Intermediate',
        updated_at = @now
    WHERE course_id = @professionalId;
END

-- ----- 5b. Video Bank — Đời sống -----
SET @doiSongId = NULL;

SELECT TOP 1 @doiSongId = c.course_id
FROM dbo.Courses c
WHERE c.course_type = N'video_bank'
  AND (
      c.title = N'Video Bank — Đời sống'
      OR EXISTS (
          SELECT 1
          FROM dbo.Lessons l
          INNER JOIN dbo.Lesson_Material m ON m.lesson_id = l.lesson_id
          WHERE l.course_id = c.course_id
            AND (
                m.content_url LIKE N'%Y5Gmuq6y9l8%'
                OR m.content_url LIKE N'%iilcmCTUIyE%'
            )
      )
  )
ORDER BY CASE WHEN c.title = N'Video Bank — Đời sống' THEN 0 ELSE 1 END, c.course_id;

IF @doiSongId IS NULL
BEGIN
    INSERT INTO dbo.Courses (title, description, level, course_type, created_at, updated_at)
    VALUES (
        N'Video Bank — Đời sống',
        N'Video ngắn YouTube — chủ đề đời sống, tư duy, thói quen.',
        N'Beginner',
        N'video_bank',
        @now,
        @now
    );
    SET @doiSongId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.Courses
    SET title = N'Video Bank — Đời sống',
        description = N'Video ngắn YouTube — chủ đề đời sống, tư duy, thói quen.',
        level = N'Beginner',
        updated_at = @now
    WHERE course_id = @doiSongId;
END

-- ----- 5c. Video Bank — Học Thuật (0 lesson) -----
IF NOT EXISTS (
    SELECT 1 FROM dbo.Courses
    WHERE course_type = N'video_bank' AND title = N'Video Bank — Học Thuật'
)
BEGIN
    INSERT INTO dbo.Courses (title, description, level, course_type, created_at, updated_at)
    VALUES (
        N'Video Bank — Học Thuật',
        N'Video YouTube — học thuật, nghiên cứu, thuyết trình, từ vựng chuyên ngành.',
        N'Advanced',
        N'video_bank',
        @now,
        @now
    );
    SET @hocThuatId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT @hocThuatId = course_id
    FROM dbo.Courses
    WHERE course_type = N'video_bank' AND title = N'Video Bank — Học Thuật';

    UPDATE dbo.Courses
    SET description = N'Video YouTube — học thuật, nghiên cứu, thuyết trình, từ vựng chuyên ngành.',
        level = N'Advanced',
        updated_at = @now
    WHERE course_id = @hocThuatId;
END

-- ----- 6. Nạp 2 video vào Video Bank — Đời sống (idempotent) -----
-- Video 1: Scatter Focus
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
        SELECT MAX(lesson_order) FROM dbo.Lessons WHERE course_id = @doiSongId
    ), 0) + 1;

    INSERT INTO dbo.Lessons (course_id, title, description, lesson_order, duration, source, created_by_user_id)
    VALUES (
        @doiSongId,
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
    SET course_id = @doiSongId,
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

-- Video 2: Lead by Example
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
        SELECT MAX(lesson_order) FROM dbo.Lessons WHERE course_id = @doiSongId
    ), 0) + 1;

    INSERT INTO dbo.Lessons (course_id, title, description, lesson_order, duration, source, created_by_user_id)
    VALUES (
        @doiSongId,
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
    SET course_id = @doiSongId,
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

-- ----- 7. Dọn course Video Bank trùng / không thuộc 3 khung chính -----
DELETE c
FROM dbo.Courses c
WHERE c.course_type = N'video_bank'
  AND c.title NOT IN (
      N'Video Bank — Professional',
      N'Video Bank — Đời sống',
      N'Video Bank — Học Thuật'
  )
  AND NOT EXISTS (SELECT 1 FROM dbo.Lessons l WHERE l.course_id = c.course_id);
GO

-- ========== STEP 8: Chuẩn hóa tên giáo trình Lớp 6 (legacy encoding) ==========
UPDATE dbo.Courses
SET title = N'Tiếng Anh lớp 6',
    description = N'Sách giáo khoa Tiếng Anh lớp 6.',
    updated_at = GETUTCDATE()
WHERE course_type = N'curriculum'
  AND (
      title LIKE N'%lop 6%'
      OR title LIKE N'%lớp 6%'
      OR title LIKE N'Tiếng Anh%6%'
  )
  AND title <> N'Tiếng Anh lớp 6';
GO

-- ========== Kiểm tra sau migration ==========
SELECT course_id, title, course_type, level
FROM dbo.Courses
WHERE course_type = N'video_bank'
ORDER BY course_id;

SELECT c.title AS course_title, l.lesson_id, l.lesson_order, l.title AS lesson_title
FROM dbo.Lessons l
JOIN dbo.Courses c ON c.course_id = l.course_id
WHERE c.course_type = N'video_bank'
ORDER BY c.course_id, l.lesson_order;
GO
