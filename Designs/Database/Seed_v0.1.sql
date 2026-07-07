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
    INSERT INTO Courses (title, level, learning_mode, course_type, description)
    VALUES (N'Tiếng Anh lớp 6', 'Beginner', 'casual', 'curriculum', N'Giáo trình Tiếng Anh cơ bản');
    SET @CurriculumCourseId = SCOPE_IDENTITY();
END
ELSE BEGIN SELECT @CurriculumCourseId = course_id FROM Courses WHERE title = N'Tiếng Anh lớp 6'; END

UPDATE Courses SET course_type = 'curriculum' WHERE course_id = @CurriculumCourseId;

-- Lesson: Unit 1 My new school
DECLARE @CurriculumLessonId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Lessons WHERE course_id = @CurriculumCourseId AND title = N'Unit 1 My new school')
BEGIN
    INSERT INTO Lessons (course_id, title, lesson_order, duration) 
    VALUES (@CurriculumCourseId, N'Unit 1 My new school', 1, 62);
    SET @CurriculumLessonId = SCOPE_IDENTITY();

    INSERT INTO Lesson_Material (lesson_id, material_type, content_url) VALUES 
    (@CurriculumLessonId, 'transcript', '/media/curriculum/grade-6/unit-1/transcript.txt'),
    (@CurriculumLessonId, 'audio', '/media/curriculum/grade-6/unit-1/unit-1-getting-started-ex-1.wav');
    
    PRINT 'Đã tạo Curriculum: Tiếng Anh lớp 6';
END
ELSE BEGIN
    SELECT @CurriculumLessonId = lesson_id
    FROM Lessons
    WHERE course_id = @CurriculumCourseId AND title = N'Unit 1 My new school';
END

UPDATE Lessons
SET duration = 62
WHERE lesson_id = @CurriculumLessonId;

-- Curriculum Unit 1 dùng audio mẫu thật cho Exercise 1. Các unit sau vẫn có thể dùng AI TTS nếu chưa có file.
-- Unit 1 không có video. Luôn xóa video material cũ để trang chi tiết không render khung YouTube rỗng.
DELETE FROM Lesson_Material
WHERE lesson_id = @CurriculumLessonId
  AND material_type = 'video';

IF EXISTS (SELECT 1 FROM Lesson_Material WHERE lesson_id = @CurriculumLessonId AND material_type = 'transcript')
    UPDATE Lesson_Material
    SET content_url = '/media/curriculum/grade-6/unit-1/transcript.txt'
    WHERE lesson_id = @CurriculumLessonId AND material_type = 'transcript';
ELSE
    INSERT INTO Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@CurriculumLessonId, 'transcript', '/media/curriculum/grade-6/unit-1/transcript.txt');

IF EXISTS (SELECT 1 FROM Lesson_Material WHERE lesson_id = @CurriculumLessonId AND material_type = 'audio')
    UPDATE Lesson_Material
    SET content_url = '/media/curriculum/grade-6/unit-1/unit-1-getting-started-ex-1.wav'
    WHERE lesson_id = @CurriculumLessonId AND material_type = 'audio';
ELSE
    INSERT INTO Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@CurriculumLessonId, 'audio', '/media/curriculum/grade-6/unit-1/unit-1-getting-started-ex-1.wav');

-- Unit 1 Exercise 1: Getting Started dialogue. Keep this in sync with the transcript JSON file.
DELETE FROM Lesson_Sentences WHERE lesson_id = @CurriculumLessonId;

INSERT INTO Lesson_Sentences (lesson_id, sentence_order, [text], translation) VALUES
(@CurriculumLessonId, 1, N'Hi, Vy.', N'Chào Vy.'),
(@CurriculumLessonId, 2, N'Hi, Phong. Are you ready?', N'Chào Phong. Cậu đã sẵn sàng chưa?'),
(@CurriculumLessonId, 3, N'Just a minute.', N'Chỉ một phút nữa thôi.'),
(@CurriculumLessonId, 4, N'Oh, this is Duy, my new friend.', N'À, đây là Duy, bạn mới của tớ.'),
(@CurriculumLessonId, 5, N'Hi, Duy. Nice to meet you.', N'Chào Duy, rất vui khi được gặp cậu.'),
(@CurriculumLessonId, 6, N'Hi, Phong. I live near here, and we go to the same school!', N'Chào Phong. Tớ sống ở gần đây, và chúng ta đi học cùng trường đó!'),
(@CurriculumLessonId, 7, N'Good. Hmm, your school bag looks heavy.', N'Vậy thì hay quá. Này, cặp cậu trông nặng nhỉ.'),
(@CurriculumLessonId, 8, N'Yes! I have new books, and we have new subjects to study.', N'Ừ, tớ có nhiều sách, và bọn tớ có nhiều môn để học.'),
(@CurriculumLessonId, 9, N'And a new uniform, Duy! You look smart!', N'Và cậu còn có đồng phục mới nữa. Duy này, trông cậu rất bảnh đó!'),
(@CurriculumLessonId, 10, N'Thanks, Phong. We always look smart in our uniforms.', N'Cảm ơn nhé Phong. Bọn mình đều trông rất là bảnh bao trong bộ đồng phục.'),
(@CurriculumLessonId, 11, N'Let me put on my uniform. Then we can go.', N'Để tớ mặc đồng phục, rồi sau đó chúng ta đi nha.');

-- ---------------------------------------------------------
-- 3. SEED VIDEO BANK - ĐỜI SỐNG (Casual)
-- ---------------------------------------------------------
DECLARE @CasualCourseId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Courses WHERE title = N'Video Bank - Đời sống')
BEGIN
    INSERT INTO Courses (title, level, learning_mode, course_type)
    VALUES (N'Video Bank - Đời sống', 'Beginner', 'casual', 'video_bank');
    SET @CasualCourseId = SCOPE_IDENTITY();
END
ELSE BEGIN SELECT @CasualCourseId = course_id FROM Courses WHERE title = N'Video Bank - Đời sống'; END

UPDATE Courses SET course_type = 'video_bank' WHERE course_id = @CasualCourseId;

DECLARE @ScatterFocusLessonId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Lessons WHERE course_id = @CasualCourseId AND title = 'Scatter Focus')
BEGIN
    INSERT INTO Lessons (course_id, title, lesson_order, duration) 
    VALUES (@CasualCourseId, 'Scatter Focus', 1, 61);
    SET @ScatterFocusLessonId = SCOPE_IDENTITY();

    INSERT INTO Lesson_Material (lesson_id, material_type, content_url) VALUES 
    (@ScatterFocusLessonId, 'video', 'https://www.youtube.com/watch?v=Y5Gmuq6y9l8'),
    (@ScatterFocusLessonId, 'transcript', '/media/video-bank/scatter-focus/transcript.txt');

    INSERT INTO Lesson_Sentences (lesson_id, sentence_order, [text]) VALUES 
    (@ScatterFocusLessonId, 1, 'If you think back to when your best, most brilliant ideas strike you'),
    (@ScatterFocusLessonId, 2, 'you''re rarely focused on something.'),
    (@ScatterFocusLessonId, 3, 'I call this mode "scatter focus"'),
    (@ScatterFocusLessonId, 4, 'and the research shows that it lets our mind come up with ideas.'),
    (@ScatterFocusLessonId, 5, 'It lets our mind plan,');
    PRINT 'Đã tạo Video Bank Casual: Scatter Focus';
END
ELSE BEGIN
    SELECT @ScatterFocusLessonId = lesson_id
    FROM Lessons
    WHERE course_id = @CasualCourseId AND title = 'Scatter Focus';
END

UPDATE Lessons
SET duration = 61
WHERE lesson_id = @ScatterFocusLessonId;

IF EXISTS (SELECT 1 FROM Lesson_Material WHERE lesson_id = @ScatterFocusLessonId AND material_type = 'video')
    UPDATE Lesson_Material
    SET content_url = 'https://www.youtube.com/watch?v=Y5Gmuq6y9l8'
    WHERE lesson_id = @ScatterFocusLessonId AND material_type = 'video';
ELSE
    INSERT INTO Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@ScatterFocusLessonId, 'video', 'https://www.youtube.com/watch?v=Y5Gmuq6y9l8');

IF EXISTS (SELECT 1 FROM Lesson_Material WHERE lesson_id = @ScatterFocusLessonId AND material_type = 'transcript')
    UPDATE Lesson_Material
    SET content_url = '/media/video-bank/scatter-focus/transcript.txt'
    WHERE lesson_id = @ScatterFocusLessonId AND material_type = 'transcript';
ELSE
    INSERT INTO Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@ScatterFocusLessonId, 'transcript', '/media/video-bank/scatter-focus/transcript.txt');

DELETE FROM Lesson_Sentences WHERE lesson_id = @ScatterFocusLessonId;

INSERT INTO Lesson_Sentences (lesson_id, sentence_order, [text], translation) VALUES
(@ScatterFocusLessonId, 1, 'If you think back to when your best, most brilliant ideas strike you', N'Nếu bạn ngẫm lại thời điểm mà những ý tưởng tuyệt vời và sáng suốt nhất lóe lên trong đầu,'),
(@ScatterFocusLessonId, 2, 'you''re rarely focused on something.', N'bạn hiếm khi đang tập trung vào một thứ gì đó.'),
(@ScatterFocusLessonId, 3, 'I call this mode "scatter focus"', N'Tôi gọi trạng thái này là "sự tập trung phân tán"'),
(@ScatterFocusLessonId, 4, 'and the research shows that it lets our mind come up with ideas.', N'và nghiên cứu cho thấy nó giúp tâm trí chúng ta nghĩ ra các ý tưởng.'),
(@ScatterFocusLessonId, 5, 'It lets our mind plan,', N'Nó cho phép tâm trí chúng ta lên kế hoạch,'),
(@ScatterFocusLessonId, 6, 'because of where our mind wanders to.', N'nhờ vào những nơi mà tâm trí chúng ta lang thang tới.'),
(@ScatterFocusLessonId, 7, 'I''m an anti-hustler.', N'Tôi là một người phản đối lối sống hối hả.'),
(@ScatterFocusLessonId, 8, 'I''m one of the laziest people you''ll ever meet,', N'Tôi là một trong những người lười biếng nhất mà bạn từng gặp,'),
(@ScatterFocusLessonId, 9, 'and I think that''s what gives me so many ideas to talk and write about.', N'và tôi nghĩ đó chính là điều mang lại cho tôi rất nhiều ý tưởng để nói và viết.'),
(@ScatterFocusLessonId, 10, 'We like to think of distraction as the enemy of focus.', N'Chúng ta thường nghĩ sự xao nhãng là kẻ thù của sự tập trung.'),
(@ScatterFocusLessonId, 11, 'It is not.', N'Không phải vậy.'),
(@ScatterFocusLessonId, 12, 'It is a symptom of why we find it difficult to focus,', N'Nó chỉ là triệu chứng giải thích lý do tại sao chúng ta thấy khó tập trung,'),
(@ScatterFocusLessonId, 13, 'which is the fact that our mind is overstimulated.', N'đó là vì tâm trí của chúng ta đang bị kích thích quá mức.'),
(@ScatterFocusLessonId, 14, 'I have a challenge for you:', N'Tôi có một thử thách dành cho bạn:'),
(@ScatterFocusLessonId, 15, 'so for two weeks, make your mind less stimulated.', N'trong hai tuần tới, hãy làm cho tâm trí của bạn bớt bị kích thích đi.'),
(@ScatterFocusLessonId, 16, 'There are so many great features on phones, on devices,', N'Có rất nhiều tính năng tuyệt vời trên điện thoại, trên các thiết bị,'),
(@ScatterFocusLessonId, 17, 'that''ll let us eliminate a lot of the time we waste on our devices.', N'cho phép chúng ta loại bỏ phần lớn thời gian lãng phí trên đó.'),
(@ScatterFocusLessonId, 18, 'Use those features not only to become aware of how you spend your time,', N'Hãy sử dụng những tính năng đó không chỉ để nhận thức được cách bạn tiêu tốn thời gian,'),
(@ScatterFocusLessonId, 19, 'but how you can spend less so you have more ideas.', N'mà còn để biết cách dùng thiết bị ít hơn nhằm có thêm nhiều ý tưởng mới.');

-- ---------------------------------------------------------
-- 4. SEED VIDEO BANK - PROFESSIONAL
-- ---------------------------------------------------------
DECLARE @ProCourseId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Courses WHERE title = N'Video Bank - Professional')
BEGIN
    INSERT INTO Courses (title, level, learning_mode, course_type)
    VALUES (N'Video Bank - Professional', 'Intermediate', 'professional', 'video_bank');
    SET @ProCourseId = SCOPE_IDENTITY();
END
ELSE BEGIN SELECT @ProCourseId = course_id FROM Courses WHERE title = N'Video Bank - Professional'; END

UPDATE Courses SET course_type = 'video_bank' WHERE course_id = @ProCourseId;

-- Lesson 1: Lead by Example
DECLARE @LeadLessonId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Lessons WHERE course_id = @ProCourseId AND title = 'Lead by Example')
BEGIN
    INSERT INTO Lessons (course_id, title, lesson_order, duration) 
    VALUES (@ProCourseId, 'Lead by Example', 1, 12);
    SET @LeadLessonId = SCOPE_IDENTITY();

    INSERT INTO Lesson_Material (lesson_id, material_type, content_url) VALUES 
    (@LeadLessonId, 'video', 'https://www.youtube.com/shorts/iilcmCTUIyE'),
    (@LeadLessonId, 'transcript', '/media/video-bank/influence-by-doing/transcript.txt');
END
ELSE BEGIN
    SELECT @LeadLessonId = lesson_id
    FROM Lessons
    WHERE course_id = @ProCourseId AND title = 'Lead by Example';
END

UPDATE Lessons
SET duration = 12
WHERE lesson_id = @LeadLessonId;

IF EXISTS (SELECT 1 FROM Lesson_Material WHERE lesson_id = @LeadLessonId AND material_type = 'video')
    UPDATE Lesson_Material
    SET content_url = 'https://www.youtube.com/shorts/iilcmCTUIyE'
    WHERE lesson_id = @LeadLessonId AND material_type = 'video';
ELSE
    INSERT INTO Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@LeadLessonId, 'video', 'https://www.youtube.com/shorts/iilcmCTUIyE');

IF EXISTS (SELECT 1 FROM Lesson_Material WHERE lesson_id = @LeadLessonId AND material_type = 'transcript')
    UPDATE Lesson_Material
    SET content_url = '/media/video-bank/influence-by-doing/transcript.txt'
    WHERE lesson_id = @LeadLessonId AND material_type = 'transcript';
ELSE
    INSERT INTO Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@LeadLessonId, 'transcript', '/media/video-bank/influence-by-doing/transcript.txt');

-- Professional Video Bank: Lead by Example. Keep this in sync with the transcript JSON file.
DELETE FROM Lesson_Sentences WHERE lesson_id = @LeadLessonId;

INSERT INTO Lesson_Sentences (lesson_id, sentence_order, [text], translation) VALUES
(@LeadLessonId, 1, N'The greatest way to influence someone is not to tell them to do something.', N'Cách tuyệt vời nhất để ảnh hưởng đến ai đó không phải là bảo họ phải làm gì.'),
(@LeadLessonId, 2, N'The greatest way to influence someone is to do it.', N'Cách tuyệt vời nhất để ảnh hưởng đến ai đó là tự mình thực hiện điều đó.'),
(@LeadLessonId, 3, N'And then when they see you do it and you get the results,', N'Và rồi khi họ thấy bạn làm và đạt được kết quả,'),
(@LeadLessonId, 4, N'they now want to do it because of that.', N'giờ đây họ cũng sẽ muốn làm theo vì chính lý do đó.');

-- Lesson 2: Job Interview
DECLARE @InterviewLessonId BIGINT;
IF NOT EXISTS (SELECT 1 FROM Lessons WHERE course_id = @ProCourseId AND title = 'Job Interview')
BEGIN
    INSERT INTO Lessons (course_id, title, lesson_order, duration) 
    VALUES (@ProCourseId, 'Job Interview', 2, 116);
    SET @InterviewLessonId = SCOPE_IDENTITY();

    INSERT INTO Lesson_Material (lesson_id, material_type, content_url) VALUES 
    (@InterviewLessonId, 'video', 'https://www.youtube.com/watch?v=w0YQwglgtTM'),
    (@InterviewLessonId, 'transcript', '/media/video-bank/job-interview-personnel/transcript.txt');
    PRINT 'Đã tạo Video Bank Pro: Lead by Example & Job Interview';
END
ELSE BEGIN
    SELECT @InterviewLessonId = lesson_id
    FROM Lessons
    WHERE course_id = @ProCourseId AND title = 'Job Interview';
END

UPDATE Lessons
SET duration = 116
WHERE lesson_id = @InterviewLessonId;

IF EXISTS (SELECT 1 FROM Lesson_Material WHERE lesson_id = @InterviewLessonId AND material_type = 'video')
    UPDATE Lesson_Material
    SET content_url = 'https://www.youtube.com/watch?v=w0YQwglgtTM'
    WHERE lesson_id = @InterviewLessonId AND material_type = 'video';
ELSE
    INSERT INTO Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@InterviewLessonId, 'video', 'https://www.youtube.com/watch?v=w0YQwglgtTM');

IF EXISTS (SELECT 1 FROM Lesson_Material WHERE lesson_id = @InterviewLessonId AND material_type = 'transcript')
    UPDATE Lesson_Material
    SET content_url = '/media/video-bank/job-interview-personnel/transcript.txt'
    WHERE lesson_id = @InterviewLessonId AND material_type = 'transcript';
ELSE
    INSERT INTO Lesson_Material (lesson_id, material_type, content_url)
    VALUES (@InterviewLessonId, 'transcript', '/media/video-bank/job-interview-personnel/transcript.txt');

-- Professional Video Bank: Job Interview. Keep this in sync with the transcript JSON file.
DELETE FROM Lesson_Sentences WHERE lesson_id = @InterviewLessonId;

INSERT INTO Lesson_Sentences (lesson_id, sentence_order, [text], translation) VALUES
(@InterviewLessonId, 1, N'Hello Mrs. Stevens, my name is Jane Phillips. I''m the Personnel Director.', N'Chào bà Stevens, tên tôi là Jane Phillips. Tôi là Giám đốc Nhân sự.'),
(@InterviewLessonId, 2, N'I''m pleased to meet you. Please have a seat.', N'Rất hân hạnh được gặp bà. Mời bà ngồi.'),
(@InterviewLessonId, 3, N'Thank you.', N'Cảm ơn bà.'),
(@InterviewLessonId, 4, N'According to your resume, you have several years of office experience.', N'Theo sơ yếu lý lịch của bà, bà có vài năm kinh nghiệm làm việc văn phòng.'),
(@InterviewLessonId, 5, N'Yes, I''ve had over 10 years experience.', N'Vâng, tôi đã có hơn 10 năm kinh nghiệm.'),
(@InterviewLessonId, 6, N'Tell me about your qualifications.', N'Hãy cho tôi biết về trình độ chuyên môn của bà.'),
(@InterviewLessonId, 7, N'I can type over 100 words per minute. I''m proficient in many computer programs.', N'Tôi có thể đánh máy hơn 100 từ mỗi phút. Tôi thành thạo nhiều chương trình máy tính.'),
(@InterviewLessonId, 8, N'I have excellent interpersonal skills. I am well organized, and I''m a very fast learner.', N'Tôi có kỹ năng giao tiếp cá nhân xuất sắc. Tôi có khả năng tổ chức tốt và học hỏi rất nhanh.'),
(@InterviewLessonId, 9, N'I see that you have excellent references.', N'Tôi thấy bà có những thư giới thiệu rất tuyệt vời.'),
(@InterviewLessonId, 10, N'Do you have any questions about the position?', N'Bà có câu hỏi nào về vị trí này không?'),
(@InterviewLessonId, 11, N'Yes, what are the responsibilities in this position?', N'Vâng, các trách nhiệm ở vị trí này là gì?'),
(@InterviewLessonId, 12, N'We''re looking for someone to supervise two Office Clerks,', N'Chúng tôi đang tìm kiếm một người để giám sát hai nhân viên văn phòng,'),
(@InterviewLessonId, 13, N'handle all the correspondence, arrange meetings, and manage the front office.', N'xử lý tất cả thư từ, sắp xếp các cuộc họp và quản lý bộ phận lễ tân.'),
(@InterviewLessonId, 14, N'Have you had any supervisory experience?', N'Bà đã có kinh nghiệm quản lý giám sát nào chưa?'),
(@InterviewLessonId, 15, N'Yes, I supervised three administrators in my last position.', N'Vâng, tôi đã giám sát ba nhân viên hành chính ở vị trí trước đây.'),
(@InterviewLessonId, 16, N'What are the office hours, Mrs. Phillips?', N'Giờ làm việc văn phòng là thế nào, thưa bà Phillips?'),
(@InterviewLessonId, 17, N'8:30 to 4:30, with an hour off for lunch.', N'Từ 8:30 sáng đến 4:30 chiều, với một giờ nghỉ trưa.'),
(@InterviewLessonId, 18, N'What are your salary expectations, Mrs. Stevens?', N'Mức lương mong muốn của bà là bao nhiêu, bà Stevens?'),
(@InterviewLessonId, 19, N'I expect to be paid the going rate for this type of position.', N'Tôi mong đợi được trả mức lương chung trên thị trường cho loại vị trí này.'),
(@InterviewLessonId, 20, N'Can you tell me about the benefits you offer?', N'Bà có thể cho tôi biết về các phúc lợi mà công ty cung cấp không?'),
(@InterviewLessonId, 21, N'Yes, we provide full medical and dental coverage, a pension plan, and a three-week holiday per year.', N'Vâng, chúng tôi cung cấp bảo hiểm y tế và nha khoa toàn diện, chế độ lương hưu và ba tuần nghỉ phép mỗi năm.'),
(@InterviewLessonId, 22, N'That''s very generous.', N'Các chế độ đó thật hào phóng.'),
(@InterviewLessonId, 23, N'When is the position available?', N'Khi nào vị trí này có thể bắt đầu làm việc?'),
(@InterviewLessonId, 24, N'We''re hoping the successful applicant can start at the beginning of next month.', N'Chúng tôi hy vọng ứng viên trúng tuyển có thể bắt đầu vào đầu tháng sau.'),
(@InterviewLessonId, 25, N'We''ll finish our interviews tomorrow and make a decision by the weekend.', N'Chúng tôi sẽ hoàn tất các cuộc phỏng vấn vào ngày mai và đưa ra quyết định trước cuối tuần.'),
(@InterviewLessonId, 26, N'We''ll contact you next week.', N'Chúng tôi sẽ liên hệ với bà vào tuần sau.'),
(@InterviewLessonId, 27, N'Thank you very much.', N'Cảm ơn bà rất nhiều.'),
(@InterviewLessonId, 28, N'It''s been a pleasure meeting you. I hope to hear from you soon.', N'Rất hân hạnh được gặp bà. Tôi hy vọng sẽ sớm nhận được tin từ công ty.'),
(@InterviewLessonId, 29, N'Thanks for coming in to see us, Mrs. Stevens.', N'Cảm ơn bà đã đến tham gia phỏng vấn với chúng tôi, bà Stevens.');

-- ---------------------------------------------------------
-- 5. SEED VIDEO BANK - HỌC THUẬT (Khung trống)
-- ---------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM Courses WHERE title = N'Video Bank - Học Thuật')
BEGIN
    INSERT INTO Courses (title, level, learning_mode, course_type)
    VALUES (N'Video Bank - Học Thuật', 'Advanced', 'academic', 'video_bank');
    PRINT 'Đã tạo Video Bank Academic (Khung trống)';
END

UPDATE Courses SET course_type = 'video_bank' WHERE title = N'Video Bank - Học Thuật';

COMMIT TRANSACTION;
