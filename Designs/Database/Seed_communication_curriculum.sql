USE EnglishShadowingDB;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Generated from user-provided BBC Learning English audio and transcript PDFs.
-- Safe to run repeatedly: courses/lessons are updated and sentence rows are replaced.

DECLARE @LessonId BIGINT;

-- Remove duplicate rows produced by the legacy VARCHAR schema replacing Vietnamese letters with '?'.
DELETE FROM Courses
WHERE learning_mode = 'casual' AND course_type = 'curriculum'
  AND title IN ('Real Easy English - Giao ti?p co b?n', '6 Minute English - Giao ti?p trung c?p')
  AND EXISTS (
      SELECT 1 FROM Lessons AS legacy_lesson
      INNER JOIN Lesson_Material AS legacy_material
          ON legacy_material.lesson_id = legacy_lesson.lesson_id
      WHERE legacy_lesson.course_id = Courses.course_id
        AND legacy_material.source_provider = 'BBC Learning English'
  );

DECLARE @BasicCourseId BIGINT;
SELECT @BasicCourseId = course_id FROM Courses
WHERE title = N'Real Easy English - Basic Communication'
  AND learning_mode = 'casual' AND course_type = 'curriculum';
IF @BasicCourseId IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'Real Easy English - Basic Communication', N'Easy-paced everyday English conversations for beginner listening and shadowing practice.', 'Beginner', 'casual', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @BasicCourseId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'Easy-paced everyday English conversations for beginner listening and shadowing practice.', level = 'Beginner',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @BasicCourseId;
END;

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @BasicCourseId AND title = N'Books';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@BasicCourseId, N'Books', N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', 1, 370);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', lesson_order = 1, duration = 370
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/basic-level/Talking-about-books/260403_REE_books_download.mp3', 'BBC Learning English', N'Talking-about-books',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/basic-level/Talking-about-books/transcript.json', 'BBC Learning English', N'Talking-about-books',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome back to Real Easy English, the podcast where we have real conversations in easy English to help you learn.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Becca.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'You can find a video version with subtitles of this podcast on our website bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Hi Becca.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'How are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Yeah, I''m well, thank you Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'How about yourself?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'I''m very good.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'I''m excited because we''re talking about books and reading today, and I really love reading.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'How about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Yeah, I like reading as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'I do like reading.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'How often do you read, though?', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Well, I read every day, but different types of things.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'So, I do...', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'I sit down and read for the longest period of time and the most serious kind of book when I''m travelling to and from work.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Oh!', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'And when I''m at home, I read stuff that''s not so difficult, because usually at home I just fall asleep if I read.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'So, I see a lot of people reading on the tube and I wish I could be that kind of person, but for me, I only read when I''m in a relaxed environment.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'So, like, sometimes I''ll go to the park in summer or take a book to the beach.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'So, I do like reading, but I don''t read as often as I feel like I should.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'So, Becca, what kind of books do you like to read?', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'I like short books.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'I sometimes feel a bit stressed when I see a long, thick book.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'A lot of the books that I have on my shelf at home are poems.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Poetry is a form of literature that...', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'It''s usually quite emotional and a lot shorter than a short story or, like, a novel, for example.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Yeah, poems - they also sometimes have a lovely kind of sound quality to them, if you read them loud.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'So, some of the words might rhyme, and they use a lot of metaphor.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'So, I really enjoy short stories or books that have lots of poems in them.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'How about yourself, Neil?', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'What stories or what books do you enjoy?', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'I like fiction.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'So, fiction is when the story is not true.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'It''s made up by the writer.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'And I like novels.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'And novels are fiction books.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'That''s just one quite long story.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'So, you know, probably more than 100 or 150 pages.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Otherwise, it''s a short story.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Short stories are...', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'...shorter!', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Have you ever read a non-fiction book?', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'So, maybe, something that''s based on, like, a real event or like a biography from someone''s perspective that you really enjoyed?', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'Yeah, I like reading biographies as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'So, a biography is somebody''s life story.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'I like biographies of musicians and people like that.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Yeah, I know you play guitar, so music seems to be quite a hobby that you enjoy.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'Yeah, definitely.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'And I like reading about the lives of people who''ve worked in music.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Mmm.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Do you like non-fiction?', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'I prefer to watch non-fiction.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'I prefer to watch documentaries.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'I haven''t read many non-fiction books.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'Do you have a favourite author?', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Author - an author is a person who writes books.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'Mmm yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'So, actually a lot of my favourite authors - some of them have written novels.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'I prefer poets, so authors that have written poems.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'And I prefer them because they''re quite moving and usually quite short as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'So, I find that the authors that I enjoy the most write short things, like poems or short stories.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Let''s recap some of the vocabulary we''ve heard in this podcast, starting with fiction.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'Fiction is a type of writing where the story is not true - it''s made up.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'And the opposite is non-fiction.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Non-fiction is a true story.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'And our next word - novel.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'A novel is a fiction, and it''s usually a longer story.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'We also heard the word biography.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'A biography is a type of book about a person''s life, and if that person wrote the story of their own life, it''s called an autobiography.', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'We also mentioned poems.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'Now, poems are a shorter piece of writing, even shorter than a short story or a novel.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'And the sounds of the words and the ideas are very important in poetry.', NULL, NULL, NULL, NULL),
    (@LessonId, 82, N'That''s it for this episode of Real Easy English, but don''t forget to go to our website.', NULL, NULL, NULL, NULL),
    (@LessonId, 83, N'That''s bbclearningenglish.com, where you can find a free worksheet.', NULL, NULL, NULL, NULL),
    (@LessonId, 84, N'And we''ll be back next week with another conversation in easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 85, N'Goodbye for now!', NULL, NULL, NULL, NULL),
    (@LessonId, 86, N'Goodbye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @BasicCourseId AND title = N'Moving house';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@BasicCourseId, N'Moving house', N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', 2, 383);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', lesson_order = 2, duration = 383
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/basic-level/Talking-about-moving-house/260626_REE_moving_house_download.mp3', 'BBC Learning English', N'Talking-about-moving-house',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/basic-level/Talking-about-moving-house/transcript.json', 'BBC Learning English', N'Talking-about-moving-house',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'We''re back with another conversation in easy English to help you learn.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Don''t forget that there''s a video version of this podcast on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'You can read along with the subtitles and download a free worksheet.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Hello Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'How are you doing?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'Hi Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Yeah, I''m fine.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'How are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Yes, I''m very well too, thank you.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Today, Neil, in this conversation, we''re talking about moving house.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'So, when was the last time you moved house?', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Oh, I moved house probably 12 years ago.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Oh wow!', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'And why did you move to where you live now?', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Because my family was getting bigger.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'More children need more space, so we moved to somewhere bigger and further out of London.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'How about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'I rent my flat.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'And the place I was living before - the owners of the place wanted to sell the house, so I needed to move because I couldn''t live there anymore.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'And also, my friend had this flat and he was offering me very cheap rent, so that''s why I live there.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'But I''m very happy there because it''s between two really nice parks.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'It has good transport links and, yeah, it was a good decision.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'And how was the move itself?', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'The move was actually very easy because I don''t have many possessions.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'I''m a bit of a minimalist.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'The reason is because I moved country, so I just didn''t really have many things to bring back to the UK, so I didn''t need a big van or anything.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'I just ordered a taxi and put all of my bags in the taxi and it was very easy.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'And what about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'It was the opposite of that.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'Oh no!', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'We had a removal van and some removal men, who came and packed everything up in boxes to make sure that things were easy to carry into the van, and also that they didn''t get damaged.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'And then they drove all of our stuff to my new house.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Did you have... did you have any fragile bits?', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'So, fragile means that it''s easy to break, not very strong.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'Plates and things - did you have...?', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Those things had to be wrapped and protected, otherwise they would break.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'And did you have to move furniture as well?', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Yeah, we had to move sofas and cupboards and wardrobes and heavy things like that, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'It does sound different to my experience, doesn''t it?', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'I didn''t have any furniture to take.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'Do you find moving house exciting?', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Scary?', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'How do you feel about moving house?', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'I think it''s both.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'It''s exciting and scary.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'It''s exciting because this is going to be your new life, or where your new life will happen.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'But it''s also scary because you don''t know what that''s going to be like.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'You don''t know what your neighbours are going to be like.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'That''s very true.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'You don''t know what your new friends are going to be like.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'So, there''s lots of things that you don''t know.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'And that''s scary, but also exciting.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'Yeah, a lot of uncertainty for sure.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Yeah, I think it can depend.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'It depends if you''re moving somewhere better or worse.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'But yeah, I was quite excited when I moved because I knew that my new flat had a nice outdoor terrace.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'As I said, we were close to parks and stuff, so I was excited.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'So, Georgie, if you are planning a move, what do you need to do to prepare?', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'Well, you need to do things like setting up WiFi in the new house.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'Other bills, like electricity and heating, gas - you need to put your name on the accounts.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'You need to plan what you''re going to take.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'So, when you move, it''s a really good time to look at all of your possessions and think, what do I need?', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Maybe I can get rid of stuff, so throw stuff away.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Have you got any other ideas?', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'What else can you do to prepare for a move?', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'You can hire a removal company or van if you''re doing it yourself, if you''re... if you need to transport your stuff to your new house and you can''t just use a taxi.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'Sounds like a lot of work.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'I don''t want to move anytime soon.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'No.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'You?', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'Me neither.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'No.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 82, N'Let''s recap the language we heard during the conversation.', NULL, NULL, NULL, NULL),
    (@LessonId, 83, N'We heard possessions, which are the things that you own or have.', NULL, NULL, NULL, NULL),
    (@LessonId, 84, N'For example, my phone and my clothes are my possessions.', NULL, NULL, NULL, NULL),
    (@LessonId, 85, N'We talked about removal vans.', NULL, NULL, NULL, NULL),
    (@LessonId, 86, N'A removal van is a large vehicle that people put your possessions into to go from one home to another, and the company is called a removals company or removals firm.', NULL, NULL, NULL, NULL),
    (@LessonId, 87, N'We heard packing, and packing is where you put your own things into boxes, so that a removal firm can take them to your new home.', NULL, NULL, NULL, NULL),
    (@LessonId, 88, N'And we heard fragile.', NULL, NULL, NULL, NULL),
    (@LessonId, 89, N'Fragile is an adjective which describes something that can break easily.', NULL, NULL, NULL, NULL),
    (@LessonId, 90, N'For example, glass - glass is fragile.', NULL, NULL, NULL, NULL),
    (@LessonId, 91, N'That''s it for this episode of Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 92, N'Don''t forget to test what you''ve learned using the free worksheet on our website.', NULL, NULL, NULL, NULL),
    (@LessonId, 93, N'Go to bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 94, N'We''ll be back next week with another conversation in easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 95, N'Bye for now.', NULL, NULL, NULL, NULL),
    (@LessonId, 96, N'Goodbye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @BasicCourseId AND title = N'Phone habits';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@BasicCourseId, N'Phone habits', N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', 3, 301);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', lesson_order = 3, duration = 301
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/basic-level/Talking-about-phone-habits/260703_REE_phone_habits_download.mp3', 'BBC Learning English', N'Talking-about-phone-habits',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/basic-level/Talking-about-phone-habits/transcript.json', 'BBC Learning English', N'Talking-about-phone-habits',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'In this podcast, we have real conversations in easy English to help you learn.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Becca.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'You can find a video version of this podcast along with a transcript to help you learn on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Hi Becca, how are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'I''m well, thanks, Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'How about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'I''m pretty good.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Today, we''re talking about phones.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Now, Becca, how much time do you spend on your phone every day?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Ooh, I don''t know the number.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'I haven''t checked my screen time in a while.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'So, your screen time is the time in which you spend looking on your phone screen.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'I probably would guess that I spend quite a few hours looking at my phone.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'How about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Yeah, I spend probably a lot of time on my phone every day, and I''m not really happy about that because I think it''s a waste of time.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'But I do it, like millions of other people.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Mmm, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'I mean, phones can be very useful.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'You can get information very quickly.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Yes, this is true.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'So, they are also really handy of course for finding out information quickly, and you can do loads of things that used to be quite hard to do.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'One of those things that we can probably do better with phones is to connect with people and to have contact with people.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Do you get lots of notifications from your friends and family on your phone?', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Because I don''t like spending too much time on my phone, I turned off my notifications.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'Now, notifications are the little messages that you get from apps when people or the app wants to tell you something.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'So, that''s a good tip really, if you want to reduce your screen time, is to turn off your notifications.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'I also try not to doomscroll.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'What''s doomscrolling?', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'That''s probably how I spend a lot of time on my phone.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'So, when you doomscroll, you look at negative news stories or things that make you feel maybe a bit upset, a bit sad.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'And you just keep scrolling and scrolling and scrolling and it really doesn''t make you feel good.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'And so if you do, like you Becca, a lot of doomscrolling, that could be a sign of an addiction.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'Now, an addiction is something that you can''t stop doing.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'So, for example, an obvious example is smoking.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'People who smoke - they have an addiction.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'It''s something they have to do.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'And we hear a lot about phone addiction.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'And it''s really hard to stop an addiction.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Even though I know doomscrolling is very bad for me and it doesn''t make me feel good, I can''t stop doing it.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'I don''t think you''re alone, Becca, but do you think most people look at their phone too much?', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'I do, and I think that it doesn''t matter how old you are.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'I see phone addictions with people my age, people younger and people older.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'I think that a lot of people are addicted to their phones.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'If you just look around anywhere.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Most people have their head down at that angle, looking at their phone, missing what''s going on in the real world.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Let''s recap the vocabulary we learnt in this episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'First screen time, and that''s the time that you spend looking at an electronic screen.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'We usually use it to talk about looking at our phones.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'We heard doomscrolling.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Doomscrolling is when you look at one story after another, after another, after another, and they''re usually bad news stories.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Notifications are the messages that apps will send you to make you check your phone.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'If something is an addiction, it means that you can''t stop doing it.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'That''s it for this week''s episode of Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'You can head to our website for a free worksheet to test what you''ve learnt.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'That''s at bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'And we''ll be back next week with another conversation in easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Goodbye for now.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @BasicCourseId AND title = N'Hair';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@BasicCourseId, N'Hair', N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', 4, 353);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', lesson_order = 4, duration = 353
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/basic-level/Talking-about-hair/260717_REE_hair__download.mp3', 'BBC Learning English', N'Talking-about-hair',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/basic-level/Talking-about-hair/transcript.json', 'BBC Learning English', N'Talking-about-hair',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello, I''m Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'And I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'Welcome to Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'Find all the vocabulary and a worksheet to test what you''ve learned over on our website.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Go to bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Hi Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'How are you doing?', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'I''m doing really well, Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'I must say, your hair is looking fantastic today.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Thank you very much, Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Today we are talking about hair.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Now, I can see your hair, but for our listeners maybe you can start by describing your hair.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'Well, my hair - it''s short, but it''s not very short.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'I think it''s fairly straight.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'And then the colour - it''s kind of a light brown colour.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'The bits that aren''t grey, that is!', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'You said your hair used to be ginger.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Yes, it used to be ginger - bright orange.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Wow!', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Well, my hair is naturally dark brown.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'It''s quite short and it''s wavy, so sometimes it can be curly, but mostly it''s wavy.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'So, you''ve told us about your hair, Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Do you like it?', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Yes, I do now.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'I actually...', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'When I was younger, I didn''t really like my hair because I wanted it to be straight, so I used to straighten it - take the curls and the waves out.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'But as I''ve got older, I''ve started to really like my hair.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'It looks a bit wild and I enjoy it.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'How do you...', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'Do you like your hair?', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Yeah, usually I do.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'I think similar to you.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'When I was a teenager, I just wanted my hair to look different, so I used to try and stick hair gel in it and try and make it do things that it didn''t want to do and it wasn''t going to do.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'And I used to get very frustrated about that.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'And then as you get older and wiser, you realise actually it looks fine.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'And I quite...', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'I quite like it.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'I don''t get my hair cut that often, so it often gets a bit too long and messy for me.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'And then when I can''t put up with it any longer, I go and get it cut.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'And how do you feel about the hairdresser?', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Do you like the experience?', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'I don''t mind it.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'It''s not... it''s not something I really like doing but I don''t really mind it.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'I don''t have a problem with it and it does feel good to come out and it''s a lot neater than it was when you went in.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'How long does a usual appointment take when you go to the hairdresser?', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'It depends how busy it is, but if it''s quiet, I can probably go in 20-30 minutes and it''s done.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Wow.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Well, I think my experience of the hairdresser is a little bit different.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'If I get a haircut and a hair dye - so, a hair dye is when you change the colour - it can take about three hours.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'But I actually enjoy it.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'I probably go to the hairdresser once every six months, and when I go, it''s like a treat.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'You told us about the hairdresser''s, Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Do you often change your hairstyle?', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'Well, when I was at university, I once dyed my hair - not all of it, but half of it pink and green.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'But the dye, the hair dye, wasn''t permanent.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'It was temporary, so the colour didn''t stay, which was probably good for me.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'I don''t think I suited it.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'But recently I actually have thought about growing my hair, so I want to grow it, make it a bit longer and I want to stop dyeing it as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'So, in the last few years I have been dyeing my hair a bit blonder, but I was looking at some old photos of me when I had brown, natural hair, and I think I''d like to go back to it.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'I don''t think I''m going to change my hair.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'Do you think green would suit?', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Phil, I think you would look great with green hair.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Maybe.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'Maybe we should try that, because my hair is changing its colour itself.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'It''s going greyer and greyer and greyer.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'And every time I get my hair cut, more grey comes off.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'Ah, well, maybe green would be...', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'...the way forward!', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'The other thing is there''s less of it.', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'When I get my hair cut, I can see it''s higher up, and there''s a bit going a little bit bald there, but hopefully no one notices.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'Let''s recap the language we heard during the conversation.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'If you dye your hair, and that''s spelt ''D-Y-E'', then you use chemicals to change the colour.', NULL, NULL, NULL, NULL),
    (@LessonId, 82, N'Blonde hair is a light colour, a bit like yellow.', NULL, NULL, NULL, NULL),
    (@LessonId, 83, N'Ginger hair is a bit like orange.', NULL, NULL, NULL, NULL),
    (@LessonId, 84, N'If your hair is curly or wavy, it''s not straight.', NULL, NULL, NULL, NULL),
    (@LessonId, 85, N'It has curves.', NULL, NULL, NULL, NULL),
    (@LessonId, 86, N'And if you''re bald, you don''t have any hair on your head.', NULL, NULL, NULL, NULL),
    (@LessonId, 87, N'That''s it for this episode of Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 88, N'We''ll be back next week with another conversation in easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 89, N'And don''t forget to test what you''ve learned with the free worksheet on our website.', NULL, NULL, NULL, NULL),
    (@LessonId, 90, N'Go to bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 91, N'Bye for now.', NULL, NULL, NULL, NULL),
    (@LessonId, 92, N'Goodbye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @BasicCourseId AND title = N'Family tree';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@BasicCourseId, N'Family tree', N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', 5, 350);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', lesson_order = 5, duration = 350
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/basic-level/Talking-about-family-trees/260724_REE_family_tree_download.mp3', 'BBC Learning English', N'Talking-about-family-trees',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/basic-level/Talking-about-family-trees/transcript.json', 'BBC Learning English', N'Talking-about-family-trees',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello, I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'And I''m Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'Welcome to Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'Remember, you can find the vocabulary from this episode and test yourself with a free worksheet on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Hi Georgie, how are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'I''m very well, thank you, Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'How are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'I''m really good.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'Today we''re going to talk about our families and particularly our extended families.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Georgie, do you have a big family?', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'I don''t actually.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'I have my two parents, a sister and then outside of that, my family is quite small.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'I''ve got one aunt, two uncles, and only one of my uncles has children, so I don''t have many cousins.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Do you have a big family?', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Actually, my family is probably similar.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'I''ve got...', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'I''ve got a brother, and then I''ve got two aunts, three uncles.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'I''ve got...', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Well, I''ve got three cousins that I know and a cousin somewhere lost in Australia.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Oh wow!', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Interesting.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'So, we both have quite small extended families.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Do you see yours much?', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Usually only at times, like at Christmas I''ll often see especially my uncles and my aunts.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'I see them fairly regularly.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'My cousins - yeah, often Christmas or New Year or something like that.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'Same with me, really.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Growing up, my uncle, who has children who are my cousins, they lived quite far away, so it was similar.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'We only really saw them at Christmas and times like that.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'And nowadays I only really see my extended family at events like weddings and, sadly, funerals.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'I''d probably like to see them more.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'But this is life.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'Georgie, what about your grandparents?', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Have you been close with your grandparents?', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'So, sadly, I don''t have any grandparents left.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'They''ve all died, but I was really close with particularly my grandmothers.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Both of them lived quite close to me, so that was really nice.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'I could spend a lot of time with them.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'They were big personalities - really funny, really loving, caring.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'And yeah, I was really close with them.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Yeah, I think for me it''s kind of similar to your story, really.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'I don''t have any grandparents now, but when I was younger, the two grandparents that were still alive then, I was quite close to.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'I used to see my granddad every week.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'He used to come and do the shopping for my mum, and I''d see my grandma quite regularly as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Yeah, I was quite close with them when I was younger.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'To talk about our extended families, we often look at family trees.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'That''s like a diagram that shows your family history.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'Have you researched your family tree?', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'I personally haven''t researched my family tree, but other people in my family have researched it quite a lot, so I can see the bits of my family going back lots of generations - going back to Scotland, to Ireland, to Norway, to Armenia and places like this.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'Wow.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'So, your ancestors are from all over the world, it sounds like?', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'Quite a few places, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Yeah, I also have not researched my family tree, but I really would like to because I do know that my great grandmother was Italian and my great grandfather was from the US, so I would love to, kind of, find some family members from those countries and maybe become close with them.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'I think that would be really interesting to, kind of, understand more about my history.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'If you want to know more about your family history, I think you can often look it up.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'You can... you can look it up in records in government offices, but also I think there''s a lot of websites where you can look things up now and find... find out your family history - find out about your ancestors.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'Well, actually, there is a story that my family tells that I''m not sure is true, but one of my ancestors apparently has a connection to royalty - a big event in history.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'But again, I don''t know if it''s true, so I''d love to look it up and find out the facts.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'I think that would be good.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'Let''s recap the vocabulary that we''ve looked at in this episode, starting with close.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'If you''re close with or close to a family member, then it''s someone whom you see a lot and you get on well with.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'Your family tree is a diagram or image which shows your family history.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'Your ancestors are your relatives from a long time ago.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Lots of people like to look up their ancestors.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'If you look something up, you try to find out information - for example, by searching online or reading books.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'That''s it for this episode of Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'We''ll be back next week with another episode in easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'And if you want to learn to talk more about your family, we have a collection of episodes all about this topic.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'Go to bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'Bye for now!', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'Goodbye!', NULL, NULL, NULL, NULL);

DECLARE @IntermediateCourseId BIGINT;
SELECT @IntermediateCourseId = course_id FROM Courses
WHERE title = N'6 Minute English - Intermediate Communication'
  AND learning_mode = 'casual' AND course_type = 'curriculum';
IF @IntermediateCourseId IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'6 Minute English - Intermediate Communication', N'Topic-based discussions for intermediate listening, speaking, and vocabulary practice.', 'Intermediate', 'casual', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @IntermediateCourseId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'Topic-based discussions for intermediate listening, speaking, and vocabulary practice.', level = 'Intermediate',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @IntermediateCourseId;
END;

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @IntermediateCourseId AND title = N'Love the foods you hate';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@IntermediateCourseId, N'Love the foods you hate', N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', 1, 370);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', lesson_order = 1, duration = 370
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/intermediate-level/Love-the-foods-you-hate/260416_love_the_foods_you_hate_download.mp3', 'BBC Learning English', N'Love-the-foods-you-hate',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/intermediate-level/Love-the-foods-you-hate/transcript.json', 'BBC Learning English', N'Love-the-foods-you-hate',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'This is 6 Minute English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Becca.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Neil, are there any foods that you used to really hate in the past but now don''t mind?', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Yes, actually.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'There''s a Japanese food called umeboshi which, when I first tried it, I really didn''t like.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'But after a while, I got used to it and actually, now, I really love it.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'How about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'A similar story to me with olives.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'I used to really hate olives but, as I''ve grown older, I''ve also grown to love them.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Well, in this episode, we''ll be learning from food experts about why there are some foods we just hate, and whether it''s possible to learn to love them.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Yes, and as usual, you can find a transcript for this episode, along with all the vocabulary and a worksheet, on our website bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'OK, the question for you, Becca.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'What word means to have a fear of new things, such as trying new foods?', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Is it: a) aerophobia, b) claustrophobia, or c) neophobia?', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Well, Neil, I''ll go for c) neophobia, because neo sounds like ''new''.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'That''s clever thinking but let''s see.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Now though, back to food.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Some experts have said that we can teach ourselves to like new things.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Ruth Alexander asks Dr Dana Small of McGill University where our dislike of certain foods comes from in this BBC World Service programme, The Food Chain.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Are food dislikes learned or genetic, hard-wired in some way?', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Ah, both!', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'So, there''s many reasons why you can dislike a food.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'For example, you could, via genetics, smell coriander or taste coriander differently.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Uh so, that''s genetic.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'But there''s also a really strong learning component.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'Dana explains that how we taste or smell something can be different depending on our genetics.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'However, how we taste or smell something can also be learnt.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Ruth asked if a dislike for certain food is hard-wired, and Dana confirmed that this is sometimes the case.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'The adjective hard-wired describes automatically thinking or behaving in a particular way, for instance, because it''s genetic.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'And Dana says that you could taste food differently to others via genetics.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'The preposition via means by the way of or by the use of.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'For example, I get to work via a train.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'So, there are lots of different reasons why we dislike some foods, but can we change that?', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Let''s listen to Dietitian Claire Thornton Wood explaining on the BBC World Service programme, The Food Chain.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'One of the really good techniques that we might use is something called masking, where you dip a food that you don''t like into something that you do like.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'For those parents who really say they like everything, we actually get chocolate-covered insects and we offer those and actually people do eat them and try them.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'And I think it''s the concept that it''s an insect, but usually they find that once they eat it, there isn''t anything inherently unpleasant about it.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'It''s a little bit like eating just a bit of crunchy chocolate.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Claire uses masking.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Masking is the act of stopping something from being seen.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'In Claire''s clinic, she masks the disliked foods with something that is liked.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Yes, and what parents usually find out is that the food they dislike isn''t inherently bad.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'The adverb inherently describes something that exists in a way which is natural or essential.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'So, insects aren''t inherently unpleasant to eat, some of us think they are because the concept of eating them could be strange to us culturally.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'We talked about how people sometimes fear trying new food.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Claire talks about where these fears might come from.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'For instance, just say that you had eaten prawns in the past and you had become unwell from eating them, you know, you had what you call a dodgy prawn.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'There''s a good chance that you might actually associate that with eating the prawn and think, oh, I don''t want to eat the prawn again because it''s going to make me unwell.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'So, that''s a sort of fear-based avoidance.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'Claire said that you''re likely to have a fear of a food if you''ve had a bad experience with it.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'She uses the example of eating a dodgy prawn, which would make you unwell.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'If something is dodgy, it''s generally bad or has a bad reputation.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'But when we talk about food, it could mean that it''s undercooked, old or has been left out, therefore making you sick.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'In fact, we may avoid dodgy things.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'To avoid is to keep away from something.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'So, avoidance is the act of keeping away.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Now, Neil, that reminds me of the question you asked earlier.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'Ah yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'I asked you what word means to have a fear of new things, and you answered c) neophobia.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'And, Becca, I''m pleased to say your answer was correct!', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'So, my thinking was right!', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'That''s great.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'Now let''s have a recap of the language we''ve learnt in this episode, starting with hard-wired, which describes automatically thinking or behaving in a particular way, because of genetics, for example.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'Via is a preposition that means by way of or by use of.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'We had masking, that''s hiding or stopping something from being seen.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'The adverb inherently describes something that exists in a way which is natural or essential.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'We also had avoidance, that is the act of keeping away from something.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'And finally, when we talk about food, dodgy means something that can make you unwell.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Once again, our six minutes are up, but head over to our website, bbclearningenglish.com, for a quiz and worksheet for this episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'See you there soon.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'But for now, it''s goodbye.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Goodbye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @IntermediateCourseId AND title = N'Should we eat ultra-processed food?';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@IntermediateCourseId, N'Should we eat ultra-processed food?', N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', 2, 374);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', lesson_order = 2, duration = 374
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/intermediate-level/Should we eat ultra-processed-food/260430_6_minute_english_should_we_eat_ultra_processed_food_download.mp3', 'BBC Learning English', N'Should we eat ultra-processed-food',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/intermediate-level/Should we eat ultra-processed-food/transcript.json', 'BBC Learning English', N'Should we eat ultra-processed-food',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'This is 6 Minute English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'If you''ve eaten anything today, then it''s likely that some of your food was ultra- processed - food containing artificial ingredients like additives and sweeteners.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Ultra-processed foods are everywhere, from sliced bread to chocolate biscuits and crisps.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'They might taste good, but the bad news is that ultra-processed foods have been linked to poor health.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'They often contain lots of sugar and salt and have been linked to problems like obesity and diabetes.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'So, how can we tell what food is ultra- processed and what''s not?', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Here''s health reporter Annabel Rackham on BBC World Service programme What in the World?', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Ultra-processed foods are things that contain five or more ingredients, and things that you wouldn''t find in your average kitchen.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'It''s going to have things on there, like emulsifiers, preservatives, additives, dyes and sweeteners.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Annabel describes ultra-processed foods as things containing ingredients you wouldn''t find in your kitchen.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'Do you eat much ultra-processed food, Pippa, or do you try to avoid it?', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'I used to eat a lot of ultra-processed foods, and now I try to cook everything myself and not eat things like chocolate and snacks all day.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'How about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'I think the same.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'I try to cook things using just normal ingredients, just so you know what''s gone into it.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'In this episode, we''ll be discussing ultra-processed food as well as learning some useful new vocabulary.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'And remember, there''s also a quiz and worksheet available on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'But now I have a question for you, Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Fizzy drinks, like cola and lemonade, are another example of popular ultra-processed foods, but when were fizzy drinks invented?', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Was it: a) 1772, b) 1872, or c) 1972?', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Well, I think it was before 1972, but 1772 sounds like too early, so I''m going to say b) 1872.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'We''ll find out the answer at the end of the programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'One reason for the popularity of ultra-processed food is convenience.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Let''s hear more from health reporter Annabel, who talks here with Hannah Gelbart, presenter of BBC World Service''s What in the World.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'So, I do think convenience - it is the main issue there.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'And again, with a ready meal, you put it in the microwave for a couple of minutes.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'It''s done.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'It''s hot.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'It serves you.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'Whereas, you know, sometimes cooking a fresh meal from scratch - that can take a really long time.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'What''s your ultra-processed guilty food?', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'I''m a chocolate girl.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'A packet of biscuits - something like that.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'A cake.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'That is my... that''s my guilty pleasure.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'Ultra-processed foods like ready meals are convenient.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'A ready meal is a meal from a supermarket that has already been prepared and can be heated up quickly in a microwave.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'That''s a lot quicker and easier than cooking from scratch - an idiom meaning to do something from the very beginning without using anything that''s already been made.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Even though ultra-processed foods are often unhealthy, they taste good.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'That''s why Annabel calls chocolate her guilty pleasure.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'A guilty pleasure is something you enjoy but think you shouldn''t and feel a little embarrassed about.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Ultra-processed food is a tricky topic.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'We know these foods have been linked to poor health, but at the same time they''re cheap, convenient and taste good.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'So, what should we do?', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Here''s Hannah and Annabel discussing this for BBC programme What in the World:', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Is it OK for me to have a packet of crisps once in a while?', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'Should we be cutting ultra-processed foods out of our diets completely, or is there a way for us to still enjoy them from time to time?', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'I think the best thing to do is just not to panic.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'Everything is fine in moderation.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Hannah asks if it''s OK to eat ultra-processed foods once in a while, or from time to time.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'The phrases once in a while and from time to time mean occasionally - sometimes, but not very often.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Annabel replies using the phrase everything in moderation, which advises us that it''s best to avoid too much of anything.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'That sounds sensible to me, and it also means I won''t feel bad about eating chocolate now and then.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Pippa, it''s time to reveal the answer to my question.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Now, I asked you when fizzy drinks were invented.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'You said 1872.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'I''m afraid the correct answer was 1772.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'Apparently carbonated water was used to try to prevent scurvy on sea voyages.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'Wow, that is amazing.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'I wouldn''t have thought it was that long ago.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'Right.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'Let''s recap the vocabulary we''ve learned, starting with ready meal - a meal from a supermarket that has already been prepared so you can heat it up quickly.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'If you do something from scratch, you do it from the very beginning, without using anything that''s already been made.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'A guilty pleasure is something you enjoy but feel guilty or embarrassed about because you think you shouldn''t do it.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'The phrases once in a while and from time to time mean occasionally, not very often.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'And finally, the phrase everything in moderation is used to advise someone that it''s best to avoid too much of anything.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Once again, our six minutes are up, but if you''re hungry for more, head over to our website, bbclearningenglish.com, for more tasty topics and useful vocabulary.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'See you again soon.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'But for now, it''s goodbye.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @IntermediateCourseId AND title = N'Living with debt';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@IntermediateCourseId, N'Living with debt', N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', 3, 375);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', lesson_order = 3, duration = 375
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/intermediate-level/Living-with-debt/260528_6_minute_english_living_with_debt_download.mp3', 'BBC Learning English', N'Living-with-debt',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/intermediate-level/Living-with-debt/transcript.json', 'BBC Learning English', N'Living-with-debt',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello, this is 6 Minute English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'Are you good at saving money, Neil?', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Or do you like to spend it?', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Well, actually, a bit of both.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'I like to spend money on nice things, but I also try to save, mainly because I''ve got children.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'How about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'Yeah, I''m the same.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'I like to save money, or I try to, for the future.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'But I also do spend it.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'I don''t count every penny that I spend and save every single penny that I earn.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Mmm.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'Well, whether you''re a saver or a spender, being in debt is common in the UK.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Debt refers to money a person has borrowed to buy something and which they have to pay back, usually to a bank, a credit card company, or another person.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Many people avoid talking about debt, but it affects us all.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'In 2025, over 1000 people contacted the UK Citizens Advice Bureau every single day of the year for help with their debt.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'And when debt gets out of control, it causes stress and worry.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'BBC Radio 4 programme Thinking Allowed interviewed one young man, Jason, about his debt.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Whatever jobs there are aren''t enough to provide for a family.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Sometimes you need to take out loans.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'I''ve done it a few times, but you can''t ever pay it back.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'You can''t see a way out, other than winning the lottery or something.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'I''d like to think I could clear them all one day, even if it means, like, five years paying them off.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Jason uses three phrases, pay back, pay off, and clear debt, all of which mean the same thing - to give back the money you''ve borrowed.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'In this episode, we''ll hear more about living with debt by learning some useful new words and phrases.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'And remember, you''ll find all the vocabulary used, plus a quiz and worksheet, on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'But first, I have a question for you, Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'According to debt support group The Money Charity, roughly how much is the average British adult in debt through credit cards, overdrafts and personal loans?', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Is it: a) £2,200, b) £4,200, or c) £6,200?', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'Hmm.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'I''m not sure.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'I''ll say £2,200.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'Well, we will find out the answer later in the programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Jason''s story features in a new book by sociologist Ryan Davey.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Ryan spent months living in a low-income housing estate, which he gave the fictional name Woldham.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'He talked with residents and listened to their money worries.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'Here, Ryan explains more to BBC Radio 4''s Thinking Allowed:', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'My approach was to let people know that I was interested in learning about their lives and how they were making ends meet, so I did some interviews with residents, and I paid attention to where debt came up in everyday conversations.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'For many people in Woldham, Jason included, debt was an ordinary feature of daily life.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'And beyond that, being in arrears - so, being behind with one or more monthly payment commitments - was part of daily life.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'The people of Woldham were making ends meet.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'To make ends meet means having just enough money to pay for basic living expenses like food, bills and rent.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Ryan found that many residents were in arrears, a phrase meaning to still owe money that should have already been repaid.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'For most residents, debt was a normal part of daily life.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'With a regular income, debt can be managed, but for those who are unemployed or on low incomes, it can cause serious distress.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Here, Ryan discusses how the residents he met felt about their debt with BBC Radio 4''s Thinking Allowed:', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Jason actually fluctuated between wanting to clear all of his debts on the one hand, and on the other what he described as living on the never-never and actually questioning the supposed moral obligation to pay his debts.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'Over the months that I knew him, the financial strain on him and his partner increased.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'They missed some of their bills.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'Their internet was disconnected.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Jason was living on the never-never, an informal phrase for buying the things you need by making regular small payments over a long time.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'It''s called the never- never because it seems the debt will never be repaid.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Jason experienced financial strain - emotional stress caused by a lack of money to meet his basic needs or to repay his debt.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'Debt is a serious issue.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'It affects many people and there are support groups who can help if you need it.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Neil, what was the answer to your question?', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'I asked what the average amount of debt a British adult has through credit cards, overdrafts and personal loans.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'I said it was around £2,200.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'I''m afraid that''s not the right answer.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'In fact, it''s £4,232.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'Let''s recap the vocabulary we''ve learned, starting with debt - money a person has borrowed and needs to give back.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'The phrases to pay back, to pay off, and to clear a debt all mean to give back money you have borrowed.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'If someone is making ends meet, they have just enough money to pay for basic living expenses.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'And if they are in arrears, they still owe money that should have been repaid already.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'If you buy something on the never-never, you buy it by making small regular payments over a long period.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'And finally, financial strain is emotional stress caused by a lack of money to meet your basic needs.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Once again, our six minutes are up, but if you''d like to know how debt is spelled and all the other words from this episode, visit our website, bbclearningenglish.com, to find a full vocabulary list.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'See you again soon.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'But for now, it''s goodbye.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @IntermediateCourseId AND title = N'Rude emails';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@IntermediateCourseId, N'Rude emails', N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', 4, 375);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', lesson_order = 4, duration = 375
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/intermediate-level/Rude-emails/260702_6_minute_english_rude_emails_download.mp3', 'BBC Learning English', N'Rude-emails',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/intermediate-level/Rude-emails/transcript.json', 'BBC Learning English', N'Rude-emails',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello, this is 6 Minute English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Becca.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'Sending emails is a big part of modern work, and most people try to write emails politely.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Most, but not all!', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Recently, BBC Radio 4 programme All in the Mind asked listeners to tell them what they find rude in emails.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Here, presenter Claudia Hammond and guest Pete Olusoga discuss what listeners had to say:', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'Rachel in Manchester says what annoys her is people who start their emails with just your name and without a simple greeting like ''hi''.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'She says the unnecessary formality always has the effect of putting her immediately on the defensive.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'We''ve got an anonymous one here, who recently received a single emoji in response to a carefully considered and worded email.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'One listener doesn''t like emails which start with just her name because they put her on the defensive.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'The phrase to put someone on the defensive means to do or say something which makes them feel threatened or unsure.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Another listener found it rude when their email was answered with a single emoji, and I think that would annoy me too.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'These emails are examples of people being uncivil - an adjective meaning rude or impolite.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'And just to note, the noun that goes with this is incivility.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'In this episode, we''ll be hearing more about rude emails at work and, as usual, we''ll be learning some useful new vocabulary, all of which you can find on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'But first, I have a question for you, Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Last year, thousands of US government workers received an email requiring them to justify their job by listing five things they had accomplished that week… or resign.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'But who sent this email?', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Was it: a) Donald Trump, b) JD Vance, or c) Elon Musk?', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Ooh, I don''t know...', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Maybe JD Vance?', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'We''ll find out later in the programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Now, according to some psychologists, rudeness in emails is based on a fight-or-flight response humans feel when stressed.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Here, psychologist Dr Emma Russell explains these responses for BBC Radio 4''s All in the Mind.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'So, a fight response is usually when we try to dominate or belittle other people.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Emails that are hostile in tone and language, or even cc''ing senior personnel in on the message in order to elevate an issue and put someone in their place.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'Then the flight response is when people try to protect themselves by withdrawing or avoiding.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'They won''t reply, or if they do reply, maybe they don''t answer all of the points in the email.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'They''re just trying to get it off their plate.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'The fight response can make us belittle someone - make them feel inferior or unimportant.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'For example, a boss sends a group email in which they give someone else the credit for the work you did.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'The boss wants to put you in your place - an idiom meaning to show someone that they are less important than they think they are.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'The flight response, meanwhile, can be seen when co-workers ignore your message or give a quick, unhelpful reply.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'Such colleagues want to get work off their plate - another idiom, meaning to remove tasks from your own workload and give them to someone else to deal with.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Unfortunately, when stressed and working to a deadline it''s hard to make sure all your emails are polite.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'That''s what Emma Russell said when she spoke to BBC Radio 4''s All in the Mind.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'Because we''re operating in these environments where we are all quite frazzled, we are more at risk of engaging in incivility.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'We''re more likely to be uncivil when we''re frazzled - an informal adjective describing feeling tired or anxious because you''re doing too many things at the same time.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Luckily, there are ways to help.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'If you feel angry when writing an email, pause before you hit send.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'One day it might save your friendship or even your job!', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Becca, I''m going to politely ask you to please reveal the answer to your question.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Of course, Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'I asked you who sent the email requiring workers to justify their employment or resign.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'I said that I thought it might have been JD Vance.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Well, we''re not going to fire you, Phil, but you are wrong.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'The correct answer was Elon Musk.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Let''s recap the vocabulary we''ve learnt, starting with the idiom put someone on the defensive, meaning to make someone feel threatened or unsure.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'The adjective uncivil means impolite, and we have a similar noun that''s incivility.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'To belittle someone means to make them feel inferior or unimportant.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'If you put someone in their place, you show them that they''re not as important as they think they are.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'To get something off your plate means to give someone else a task or problem instead of dealing with it yourself.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'And finally, if you''re frazzled, you feel tired or anxious because you''re doing too much.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Once again, our six minutes are up.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'If you want to know more about communication at work, check out our series Office English.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'It''s on our website, along with a quiz and a worksheet for this episode, and that''s bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'But now it''s goodbye.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @IntermediateCourseId AND title = N'Children in warzones';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@IntermediateCourseId, N'Children in warzones', N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', 5, 386);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', lesson_order = 5, duration = 386
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/intermediate-level/Children-in-warzones/260723_6_minute_english_children_in_warzones.mp3', 'BBC Learning English', N'Children-in-warzones',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/intermediate-level/Children-in-warzones/transcript.json', 'BBC Learning English', N'Children-in-warzones',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello, this is 6 Minute English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Becca.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'Now, today, we''re going to be talking about the very serious topic of war and how it affects children.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Some of the discussions you hear will mention death and trauma.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'We won''t go into detail, but if you think you feel may uncomfortable, you can check the transcript for this episode before listening to it on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'And remember, you''ll find all this episode''s vocabulary, along with a free worksheet, and many other 6 Minute English episodes on our website.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'Again, that''s bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'The most recent figure from 2024 says that 520 million children are living in conflict zones.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'That''s about one in every five children worldwide.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'In this episode, we''ll hear from Fergal Keane on the BBC World Service Programme The Global Story.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Fergal is a BBC reporter known best for reporting from war zones around the world and, as a result, developed PTSD, a type of stress disorder.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'We''ll be finding out what PTSD stands for at the end of the programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Now, in the clips we''ll hear journalist Asma Khalid asks Fergal Keane about his own experience with childhood trauma.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Fergal grew up in a home with an alcoholic father.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Asma wanted to know how his own trauma affected his reporting on the experiences of children in warzones:', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'I was hyper-vigilant from the very earliest age, obviously watching out, you know, am I under threat, is there a danger to me here?', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Fergal mentions that he was hyper-vigilant because was in dangerous situations as a child.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'The adjective vigilant means to always be careful to notice things, particularly when there is a possibility of danger.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'And the prefix hyper- expresses that there is a lot of or even too much of something.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Though neutral, it is often used in negative or critical contexts.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'So, hyper-vigilant means very, very careful and thoughtful.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Hypervigilance, the noun, is a state of extreme alertness and sensitivity to surroundings, possibly of extreme anxiety.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Now let''s hear from Fergal as he talks about research on the experience of children in warzones:', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'I mean, they disagree about a few things, but on this they''re absolutely of one mind.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'And that is that you can''t recover from war trauma if you''re living with continuing trauma.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Fergal mentions that ''they'', the researchers, disagree about their findings.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'But they''re absolutely of one mind when considering the recovery of war trauma.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'If you are of one mind, you are of a group of people that share the same opinion, desire, or viewpoint on a matter.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Researchers may disagree on whether the experiences of children in warzones is improving, but on the point of recovery, they are of one mind on that you can''t recover from war trauma if you continue living with it.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'Fergal goes on to talk about some of the ways in which these children might be helped:', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'And so there''s a new field of theory, well I mean relatively new field of theory which encourages children to slowly and carefully try to confront obviously not death and massacre again, but things like learning to sleep alone, not having to sleep with a parent because that''s what they did during the war because they were so consistently terrified.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'Now, it sounds like a simple thing, but as anybody who''s tried to put a child to sleep, who''s had a nightmare, a child who hasn''t been traumatised will know it''s very difficult.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Fergal says that children should be encouraged to slowly and carefully try to confront their experiences.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'To confront something is to meet with it or deal with it.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'For the children to confront their terrifying experiences, they must start with more simple things, like learning to sleep alone.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'Sleeping alone seems simple enough, but Fergal compares the experiences of a children of in a warzone to that of children who have nightmares.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Yes, and a nightmare is a very scary or upsetting dream.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'It can also be used metaphorically to describe unpleasant events or experiences.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Now, let''s hear Fergal talk about what he has learned from reporting on children in warzones:', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'The big lesson is that, given a chance, the human spirit, and especially children, are phenomenally resilient.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Fergal describes the children as being phenomenally resilient.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'The noun phenomenon, or phenomena in its plural form, refers to an observable fact or event that can be experienced through the senses.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'The adverb phenomenally describes something being done or felt in an extreme or surprising way.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'To be resilient is to be able to recover from and come back from something difficult or bad that''s happened.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'So, Fergal says that children can be surprisingly positive despite the negative things happening around them.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'They are phenomenally resilient.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'A result of these negative things could be PTSD.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Earlier we mentioned PTSD and it stands for post-traumatic stress disorder.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'Yes, and the P in PTSD stands for post-, as we''ve just heard.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'And the prefix post- expresses something that happens after or as a result of the event it''s attached to.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'Now, let''s recap the vocabulary.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'We first had hyper-vigilant, that is to be very careful or maybe almost too careful to notice things, particularly when there is a possibility of danger.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'To be of one mind is to be part of a group of people that share the same opinion, desire, or viewpoint on a matter.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'We had confront something.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'That is to meet with it or deal with it.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'A nightmare is a scary or upsetting dream.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'To do something phenomenally is to do it in an extreme or surprising way.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'And to be resilient is to be able to recover and come back from something difficult or bad.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'That''s happened once again.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'Our six minutes are up.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'You''ll find a quiz and a worksheet to practice the vocabulary we''ve learnt from this episode on our website BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'Goodbye for now.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'Bye.', NULL, NULL, NULL, NULL);

COMMIT TRANSACTION;
GO

SELECT c.title AS course_title, l.lesson_order, l.title AS lesson_title,
       l.duration, COUNT(s.sentence_id) AS sentence_count
FROM Courses AS c
INNER JOIN Lessons AS l ON l.course_id = c.course_id
LEFT JOIN Lesson_Sentences AS s ON s.lesson_id = l.lesson_id
WHERE EXISTS (
    SELECT 1 FROM Lesson_Material AS m
    WHERE m.lesson_id = l.lesson_id AND m.source_provider = 'BBC Learning English'
)
GROUP BY c.course_id, c.title, l.lesson_order, l.title, l.duration
ORDER BY c.title, l.lesson_order;
GO
