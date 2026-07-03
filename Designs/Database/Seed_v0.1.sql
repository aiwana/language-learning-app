USE EnglishShadowingDB;
GO

BEGIN TRANSACTION;

-- ==============================================================================
-- 1. SEED USER TEST & STATISTICS (ĐÃ BỎ QUA)
-- TODO FOR BACKEND:
-- Khi thiết kế xong module Authentication và cơ chế mã hóa mật khẩu, vui lòng tự tạo tool/script seed tài khoản demo.
-- Yêu cầu tài khoản demo:
-- - username: 'demo' | email: 'demo@shadowspeak.local' | pass: '123456'
-- - is_vip: 0 
-- - Nhớ seed kèm 1 row vào bảng User_Statistics (hearts=5, exp=0)
-- ==============================================================================


-- ---------------------------------------------------------
-- 2. SEED GIÁO TRÌNH (CURRICULUM)
-- ---------------------------------------------------------
DECLARE @CurriculumCourseId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Courses WHERE title = N'Tiếng Anh lớp 6')
BEGIN
    INSERT INTO Courses (title, level, learning_mode, description)
    VALUES (N'Tiếng Anh lớp 6', 'Beginner', 'casual', N'Giáo trình Tiếng Anh cơ bản');
    SET @CurriculumCourseId = SCOPE_IDENTITY();
END
ELSE BEGIN SELECT @CurriculumCourseId = course_id FROM Courses WHERE title = N'Tiếng Anh lớp 6'; END

-- Lesson: Unit 1 My new school
DECLARE @CurriculumLessonId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Lessons WHERE course_id = @CurriculumCourseId AND title = N'Unit 1 My new school')
BEGIN
    INSERT INTO Lessons (course_id, title, lesson_order, duration) 
    VALUES (@CurriculumCourseId, N'Unit 1 My new school', 1, 180);
    SET @CurriculumLessonId = SCOPE_IDENTITY();

    -- Material: Dùng 2 row riêng biệt cho video và transcript
    INSERT INTO Lesson_Material (lesson_id, material_type, content_url) VALUES 
    (@CurriculumLessonId, 'video', 'https://www.youtube.com/watch?v=dummy_audio_6'),
    (@CurriculumLessonId, 'transcript', '/media/curriculum/grade-6/unit-1/transcript.txt');

    -- Sentences: Sử dụng [text] và sentence_order, KHÔNG có start_time / end_time
    INSERT INTO Lesson_Sentences (lesson_id, sentence_order, [text]) VALUES 
    (@CurriculumLessonId, 1, 'Oh, someone is knocking at the door.'),
    (@CurriculumLessonId, 2, 'Hi, Vy.'),
    (@CurriculumLessonId, 3, 'You are early!'),
    (@CurriculumLessonId, 4, 'Phong is having breakfast.'),
    (@CurriculumLessonId, 5, 'Hi, Mrs. Nguyen.');
    
    PRINT 'Đã tạo Curriculum: Tiếng Anh lớp 6';
END

-- ---------------------------------------------------------
-- 3. SEED VIDEO BANK - ĐỜI SỐNG (Casual)
-- ---------------------------------------------------------
DECLARE @CasualCourseId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Courses WHERE title = N'Video Bank - Đời sống')
BEGIN
    INSERT INTO Courses (title, level, learning_mode) 
    VALUES (N'Video Bank - Đời sống', 'Beginner', 'casual');
    SET @CasualCourseId = SCOPE_IDENTITY();
END
ELSE BEGIN SELECT @CasualCourseId = course_id FROM Courses WHERE title = N'Video Bank - Đời sống'; END

DECLARE @ScatterFocusLessonId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Lessons WHERE course_id = @CasualCourseId AND title = 'Scatter Focus')
BEGIN
    INSERT INTO Lessons (course_id, title, lesson_order, duration) 
    VALUES (@CasualCourseId, 'Scatter Focus', 1, 60);
    SET @ScatterFocusLessonId = SCOPE_IDENTITY();

    INSERT INTO Lesson_Material (lesson_id, material_type, content_url) VALUES 
    (@ScatterFocusLessonId, 'video', 'https://www.youtube.com/shorts/Y5Gmuq6y9l8'),
    (@ScatterFocusLessonId, 'transcript', '/media/video-bank/scatter-focus/transcript.txt');

    INSERT INTO Lesson_Sentences (lesson_id, sentence_order, [text]) VALUES 
    (@ScatterFocusLessonId, 1, 'This is a technique called scatter focus.'),
    (@ScatterFocusLessonId, 2, 'It allows your brain to connect completely unrelated ideas.'),
    (@ScatterFocusLessonId, 3, 'You just need to let your mind wander.'),
    (@ScatterFocusLessonId, 4, 'No phones, no distractions, just you.'),
    (@ScatterFocusLessonId, 5, 'Try it for 15 minutes a day.');
    PRINT 'Đã tạo Video Bank Casual: Scatter Focus';
END

-- ---------------------------------------------------------
-- 4. SEED VIDEO BANK - PROFESSIONAL
-- ---------------------------------------------------------
DECLARE @ProCourseId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Courses WHERE title = N'Video Bank - Professional')
BEGIN
    INSERT INTO Courses (title, level, learning_mode) 
    VALUES (N'Video Bank - Professional', 'Intermediate', 'professional');
    SET @ProCourseId = SCOPE_IDENTITY();
END
ELSE BEGIN SELECT @ProCourseId = course_id FROM Courses WHERE title = N'Video Bank - Professional'; END

-- Lesson 1: Lead by Example
DECLARE @LeadLessonId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Lessons WHERE course_id = @ProCourseId AND title = 'Lead by Example')
BEGIN
    INSERT INTO Lessons (course_id, title, lesson_order, duration) 
    VALUES (@ProCourseId, 'Lead by Example', 1, 45);
    SET @LeadLessonId = SCOPE_IDENTITY();

    INSERT INTO Lesson_Material (lesson_id, material_type, content_url) VALUES 
    (@LeadLessonId, 'video', 'https://www.youtube.com/shorts/iilcmCTUIyE'),
    (@LeadLessonId, 'transcript', '/media/video-bank/influence-by-doing/transcript.txt');

    INSERT INTO Lesson_Sentences (lesson_id, sentence_order, [text]) VALUES 
    (@LeadLessonId, 1, 'True leadership is about influence, not authority.'),
    (@LeadLessonId, 2, 'You have to lead by example every single day.'),
    (@LeadLessonId, 3, 'People watch what you do more than they listen to what you say.'),
    (@LeadLessonId, 4, 'Set the standard and others will follow.'),
    (@LeadLessonId, 5, 'That is how you build trust within a team.');
END

-- Lesson 2: Job Interview
DECLARE @InterviewLessonId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Lessons WHERE course_id = @ProCourseId AND title = 'Job Interview')
BEGIN
    INSERT INTO Lessons (course_id, title, lesson_order, duration) 
    VALUES (@ProCourseId, 'Job Interview', 2, 120);
    SET @InterviewLessonId = SCOPE_IDENTITY();

    INSERT INTO Lesson_Material (lesson_id, material_type, content_url) VALUES 
    (@InterviewLessonId, 'video', 'https://www.youtube.com/watch?v=w0YQwglgtTM'),
    (@InterviewLessonId, 'transcript', '/media/video-bank/job-interview-personnel/transcript.txt');

    INSERT INTO Lesson_Sentences (lesson_id, sentence_order, [text]) VALUES 
    (@InterviewLessonId, 1, 'Welcome, please take a seat.'),
    (@InterviewLessonId, 2, 'Could you tell me a little about yourself?'),
    (@InterviewLessonId, 3, 'I have over five years of experience in software development.'),
    (@InterviewLessonId, 4, 'What would you say is your biggest strength?'),
    (@InterviewLessonId, 5, 'I am very adaptable and thrive in fast-paced environments.');
    PRINT 'Đã tạo Video Bank Pro: Lead by Example & Job Interview';
END

-- ---------------------------------------------------------
-- 5. SEED VIDEO BANK - HỌC THUẬT (Khung trống)
-- ---------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM Courses WHERE title = N'Video Bank - Học Thuật')
BEGIN
    INSERT INTO Courses (title, level, learning_mode) 
    VALUES (N'Video Bank - Học Thuật', 'Advanced', 'academic');
    PRINT 'Đã tạo Video Bank Academic (Khung trống)';
END

COMMIT TRANSACTION;

