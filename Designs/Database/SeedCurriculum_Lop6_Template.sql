-- Khung giáo trình SGK Tiếng Anh lớp 6 (CHƯA có học liệu — chờ em cung cấp audio/transcript)
-- Chạy khi em đã sẵn sàng tạo course; mỗi unit = 1 Lesson, materials thêm sau.

USE EnglishShadowingDB;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Courses WHERE title = N'Tiếng Anh lớp 6')
BEGIN
    DECLARE @courseId BIGINT;
    DECLARE @now DATETIME2 = GETUTCDATE();

    INSERT INTO dbo.Courses (title, description, level, course_type, created_at, updated_at)
    VALUES (
        N'Tiếng Anh lớp 6',
        N'Sách giáo khoa Tiếng Anh lớp 6 — Global Success / chương trình mới.',
        'Beginner',
        'curriculum',
        @now,
        @now
    );
    SET @courseId = SCOPE_IDENTITY();

    INSERT INTO dbo.Lessons (course_id, title, description, lesson_order, duration, source, created_by_user_id)
    VALUES
        (@courseId, N'My new school',       N'Unit 1 — Chưa có học liệu.', 1,  300, 'curated', NULL),
        (@courseId, N'My house',             N'Unit 2 — Chưa có học liệu.', 2,  300, 'curated', NULL),
        (@courseId, N'My friends',           N'Unit 3 — Chưa có học liệu.', 3,  300, 'curated', NULL),
        (@courseId, N'My neighbourhood',     N'Unit 4 — Chưa có học liệu.', 4,  300, 'curated', NULL),
        (@courseId, N'Natural wonders of Vietnam', N'Unit 5 — Chưa có học liệu.', 5, 300, 'curated', NULL),
        (@courseId, N'Our Tet holiday',      N'Unit 6 — Chưa có học liệu.', 6,  300, 'curated', NULL);

    -- Khi em có transcript JSON, chạy thêm:
    -- INSERT INTO Lesson_Material (lesson_id, material_type, content_url)
    -- VALUES (@lessonId, 'transcript', '/media/curriculum/lop-6/unit-1/transcript.txt');
END
GO

-- Lớp 7–12: lặp pattern tương tự khi em xác nhận tên unit từng sách.
-- course_type luôn là 'curriculum'
-- Bài AI đã lưu: course_type = 'ai_saved', source = 'ai' (app tự tạo khi user bấm Lưu)

SELECT course_id, title, course_type FROM dbo.Courses WHERE title LIKE N'Tiếng Anh lớp%';
GO
