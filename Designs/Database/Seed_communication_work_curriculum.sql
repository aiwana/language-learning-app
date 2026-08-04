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

DECLARE @CourseId1 BIGINT;
SELECT @CourseId1 = course_id FROM Courses
WHERE title = N'Real Easy English - Basic Communication'
  AND learning_mode = 'casual' AND course_type = 'curriculum';
IF @CourseId1 IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'Real Easy English - Basic Communication', N'Easy-paced everyday English conversations for beginner listening and shadowing practice.', 'Beginner', 'casual', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @CourseId1 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'Easy-paced everyday English conversations for beginner listening and shadowing practice.', level = 'Beginner',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @CourseId1;
END;

-- Free the managed lesson-order range before applying the current source ordering.
UPDATE Lessons
SET lesson_order = lesson_order + 10000
WHERE course_id = @CourseId1
  AND lesson_order < 10000
  AND EXISTS (
      SELECT 1 FROM Lesson_Material AS managed_material
      WHERE managed_material.lesson_id = Lessons.lesson_id
        AND managed_material.source_provider = 'BBC Learning English'
  );

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId1 AND title = N'Books';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId1, N'Books', N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', 1, 370);
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
WHERE course_id = @CourseId1 AND title = N'Moving house';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId1, N'Moving house', N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', 2, 383);
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
WHERE course_id = @CourseId1 AND title = N'Phone habits';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId1, N'Phone habits', N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', 3, 301);
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
WHERE course_id = @CourseId1 AND title = N'Hair';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId1, N'Hair', N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', 4, 353);
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
WHERE course_id = @CourseId1 AND title = N'Family tree';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId1, N'Family tree', N'BBC Learning English - Real Easy English. Listening and shadowing practice from the original conversation.', 5, 350);
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

DECLARE @CourseId2 BIGINT;
SELECT @CourseId2 = course_id FROM Courses
WHERE title = N'6 Minute English - Intermediate Communication'
  AND learning_mode = 'casual' AND course_type = 'curriculum';
IF @CourseId2 IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'6 Minute English - Intermediate Communication', N'Topic-based discussions for intermediate listening, speaking, and vocabulary practice.', 'Intermediate', 'casual', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @CourseId2 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'Topic-based discussions for intermediate listening, speaking, and vocabulary practice.', level = 'Intermediate',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @CourseId2;
END;

-- Free the managed lesson-order range before applying the current source ordering.
UPDATE Lessons
SET lesson_order = lesson_order + 10000
WHERE course_id = @CourseId2
  AND lesson_order < 10000
  AND EXISTS (
      SELECT 1 FROM Lesson_Material AS managed_material
      WHERE managed_material.lesson_id = Lessons.lesson_id
        AND managed_material.source_provider = 'BBC Learning English'
  );

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId2 AND title = N'Love the foods you hate';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId2, N'Love the foods you hate', N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', 1, 370);
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
WHERE course_id = @CourseId2 AND title = N'Should we eat ultra-processed food?';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId2, N'Should we eat ultra-processed food?', N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', 2, 374);
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
WHERE course_id = @CourseId2 AND title = N'Living with debt';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId2, N'Living with debt', N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', 3, 375);
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
WHERE course_id = @CourseId2 AND title = N'Rude emails';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId2, N'Rude emails', N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', 4, 375);
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
WHERE course_id = @CourseId2 AND title = N'Children in warzones';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId2, N'Children in warzones', N'BBC Learning English - 6 Minute English. Listening and shadowing practice from the original conversation.', 5, 386);
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

DECLARE @CourseId3 BIGINT;
SELECT @CourseId3 = course_id FROM Courses
WHERE title = N'Feelings and Emotions'
  AND learning_mode = 'casual' AND course_type = 'curriculum';
IF @CourseId3 IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'Feelings and Emotions', N'Everyday conversations and expressions for talking about feelings and emotions.', 'Intermediate', 'casual', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @CourseId3 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'Everyday conversations and expressions for talking about feelings and emotions.', level = 'Intermediate',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @CourseId3;
END;

-- Free the managed lesson-order range before applying the current source ordering.
UPDATE Lessons
SET lesson_order = lesson_order + 10000
WHERE course_id = @CourseId3
  AND lesson_order < 10000
  AND EXISTS (
      SELECT 1 FROM Lesson_Material AS managed_material
      WHERE managed_material.lesson_id = Lessons.lesson_id
        AND managed_material.source_provider = 'BBC Learning English'
  );

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId3 AND title = N'I''m scared of making mistakes';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId3, N'I''m scared of making mistakes', N'BBC Learning English - Feelings and Emotions. Listening and shadowing practice from the original conversation.', 1, 657);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Feelings and Emotions. Listening and shadowing practice from the original conversation.', lesson_order = 1, duration = 657
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/feelings-and-emotions/Im-scared-of-making-mistakes/1_BSA_making_mistakes_download.mp3', 'BBC Learning English', N'Im-scared-of-making-mistakes',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/feelings-and-emotions/Im-scared-of-making-mistakes/transcript.json', 'BBC Learning English', N'Im-scared-of-making-mistakes',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Do you get anxious when you speak English?', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'You''re not alone.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I guess I''d say that my experience speaking English is full of dread and regret.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'I don''t know why, but I cannot find the courage to speak to someone.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'For me, it was very important to be good in English.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'And I was like thinking what people will think about me when I''m speaking the wrong way or my pronunciation is not correct.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Imagine that if you are able to sound very intelligent, very wise, very smart in your first language, but then in the second language, you are not able to do that.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'It isn''t about perfection, and it isn''t about necessarily being very fluent.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'It''s about communicating well.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'In this special series from BBC Learning English, we''ll be helping you understand speaking anxiety and improve your confidence in English.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Hello and welcome to Beating Speaking Anxiety.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'I''m Georgie, an English teacher and presenter at BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'And I''m Hanan, a bilingual reporter for BBC Arabic and presenter of the Arabic educational series, Dars.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'So, as an English teacher, something my students used to ask me all the time was, "How can I get better at speaking?" And sometimes they mean they want to make fewer mistakes, but most often it''s about confidence and wanting to stop feeling so nervous.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'They''re worried about being judged for their mistakes.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'They''re scared they''ll forget their words, that people won''t understand their accent.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'There are so many fears when it comes to speaking a foreign language.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Yes, it''s something I struggled with too when I moved to the UK to work at the BBC.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'My English was actually pretty good, but having conversations with people, I found it really difficult.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'So when I first joined the BBC, the Learning English team made an assessment of my English level, which they used to do for all new joiners to see if they need any help or courses.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'My results were pretty good, and I was fluent, but on that very same day, leaving the building and going to get some coffee, I couldn''t really understand what the barista was saying, and I felt pretty nervous to order coffee and was trying to stress every single word, hoping that my grammar is correct and I am pronouncing the words right.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Yeah, I''m sure that''s a situation lots of people can relate to.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'So, in this series, we''re going to look at all the things that make us afraid of speaking in a new language.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'We''ll speak to experts to understand why speaking makes us so anxious, learn about what happens to our brain when we learn a new language, and explore some tips to help make speaking English less stressful.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Each week for the next eight weeks, we will focus on a different fear learners have when speaking English.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'And we start with one of the most common fears for learners: I''m scared of making mistakes.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Let''s hear more from some learners - Cindy from Colombia, David from Brazil and Elisa from Mexico.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'And I feel afraid when I speak English because I don''t have more vocabulary and I feel afraid for mistake and can''t communicate my idea.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'I felt very self-conscious.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'I felt really insecure sometimes because I was like, oh, am I saying the right things?', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'Do I say, do I know things well enough?', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'I don''t like making mistakes and knowing they know more than me.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'I put pressure on myself to avoid making mistakes and being foolish.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'All of those learners are worried about making mistakes.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'A time when I felt this fear the most was when I worked as an English teacher in Spain, and I had to have meetings with my students'' parents to discuss their progress, all in Spanish.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'I was so scared of making mistakes because in my head it was linked to my job and my professionalism.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'I didn''t want the parents to judge me and think I was a bad teacher.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'Totally.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'And you know, it''s not just new learners of English who are worried about making mistakes.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Even advanced learners talk about this.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'So what''s going on?', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'You know, usually the beliefs that cause anxiety, especially severe anxiety, are, we call it ''irrational beliefs''.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'And also like, some low self-perceptions, fear of negative evaluation.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'All those learner internal, you know, factors.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'This is Han Luo, associate professor of Chinese at Lafayette College in the United States.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'Han has done lots of research into the sources of anxiety, or where that fear comes from.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Han says irrational beliefs can make us anxious.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Irrational beliefs are beliefs that aren''t based on things that are true.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'And Han says that learners worry about mistakes because they''re scared of ''negative evaluation''.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'In other words, that people will judge them for their mistakes and think badly of them.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'Imagine that if you are able to sound very intelligent, very wise, very smart in your first language.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'You are, you know, admired by people, but then in the second language, you are not able to do that, right?', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'When people speak in another language, they worry about what other people might think about them.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'But Han says this judgement doesn''t come from other people.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'It comes from within.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'But in the moment when we try to speak, we''re often not aware of what''s causing the anxiety and stress.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'And so the first step to reducing the fear of making mistakes is to recognise that fear.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'We want to make those implicit beliefs into conscious beliefs.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'That is already like a very, very important step.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'Often the beliefs that are making us anxious are implicit - we don''t notice them, and we need to make them conscious so that we do notice them.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'You realise it now - ''Oh, nobody will laugh at me, uh, if I make a mistake'', because everyone is in the same boat, right?', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'So when you realise this, you know, now, I tell you, "You don''t have to worry about it".', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'So are you able to just remove your anxiety, you know, and then your your beliefs are changed?', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'I find what Han said about irrational beliefs really interesting.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'So I''m an English teacher, so I know that mistakes help us learn.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'And also that as long as you communicate your ideas effectively, it doesn''t really matter if you make a few mistakes.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'But I still have an irrational fear of making mistakes in Spanish.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'I need to make my feelings match my belief that making mistakes is fine.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'And you know what, Georgie?', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'Um, we already make mistakes in our own languages, so I feel like we should encourage ourselves and tell ourselves it''s OK to make mistakes.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'A hundred per cent, I totally agree.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Han says recognising that these fears of making mistakes are irrational is the first step.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'But is there anything we can do practically to help get rid of this fear?', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'It isn''t about perfection, and it isn''t about necessarily being very fluent.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'It''s about communicating well.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'This is Barnaby Griffiths.', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'He''s a speaking coach who works with the students who want to improve their speaking in English.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'Barnaby says if we think about speaking as communicating rather than like a test, then we can relax.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'So embrace your mistakes.', NULL, NULL, NULL, NULL),
    (@LessonId, 82, N'Above all, allow for mistakes.', NULL, NULL, NULL, NULL),
    (@LessonId, 83, N'So self-correct is fine.', NULL, NULL, NULL, NULL),
    (@LessonId, 84, N'Allow pauses and silence within your communication and learn to correct yourself and also introduce elements such as humour and smile and laugh when things go wrong and say, ''Oh, I didn''t mean to say that'' and have phrases like ''Let me rephrase that'', or ''I need to say that again'' and be vulnerable.', NULL, NULL, NULL, NULL),
    (@LessonId, 85, N'I love Barnaby''s advice to just smile and laugh when things go wrong, but I imagine it might be difficult to be vulnerable like Barnaby says.', NULL, NULL, NULL, NULL),
    (@LessonId, 86, N'Do you have any tips, Georgie?', NULL, NULL, NULL, NULL),
    (@LessonId, 87, N'Yes, I do.', NULL, NULL, NULL, NULL),
    (@LessonId, 88, N'I think the best way to get comfortable making mistakes is to start in situations where you feel safe.', NULL, NULL, NULL, NULL),
    (@LessonId, 89, N'So you could practise with someone you feel comfortable with.', NULL, NULL, NULL, NULL),
    (@LessonId, 90, N'And another idea is to do a language exchange.', NULL, NULL, NULL, NULL),
    (@LessonId, 91, N'So this is when you find someone who wants to practise your language and you want to practise their language.', NULL, NULL, NULL, NULL),
    (@LessonId, 92, N'This is really good because you''re both practising languages and you''re both making mistakes, and you''re kind of in the same situation.', NULL, NULL, NULL, NULL),
    (@LessonId, 93, N'That''s true.', NULL, NULL, NULL, NULL),
    (@LessonId, 94, N'I had a similar experience actually when I was learning Turkish, so I did an exchange with a Turkish friend.', NULL, NULL, NULL, NULL),
    (@LessonId, 95, N'She was teaching me Turkish and I was, funnily enough, I was actually giving her some English tips.', NULL, NULL, NULL, NULL),
    (@LessonId, 96, N'Um, and it was really good because it is with someone you know and you feel comfortable with and you don''t worry too much about mistakes.', NULL, NULL, NULL, NULL),
    (@LessonId, 97, N'I was making mistakes in both languages, and, uh, that felt OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 98, N'Yeah, it''s great because there''s no judgement, is there?', NULL, NULL, NULL, NULL),
    (@LessonId, 99, N'Exactly.', NULL, NULL, NULL, NULL),
    (@LessonId, 100, N'Thanks for listening to this episode of Beating Speaking Anxiety.', NULL, NULL, NULL, NULL),
    (@LessonId, 101, N'To learn more about speaking anxiety, head to our website where Georgie has made videos for each of the speaking fears we talk about in this series.', NULL, NULL, NULL, NULL),
    (@LessonId, 102, N'You will hear more advice and see some tips in action with real learners.', NULL, NULL, NULL, NULL),
    (@LessonId, 103, N'Use the link in the notes for this episode or visit bbclearningenglish.com', NULL, NULL, NULL, NULL),
    (@LessonId, 104, N'And we''d love to hear about your experience speaking English.', NULL, NULL, NULL, NULL),
    (@LessonId, 105, N'Please send us an email and tell us what scares you about speaking.', NULL, NULL, NULL, NULL),
    (@LessonId, 106, N'Our email address is learningenglish@bbc.co.uk.', NULL, NULL, NULL, NULL),
    (@LessonId, 107, N'And in the next episode, we''ll be talking about what to do when you''re speaking English and your mind suddenly goes blank.', NULL, NULL, NULL, NULL),
    (@LessonId, 108, N'See you then.', NULL, NULL, NULL, NULL),
    (@LessonId, 109, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId3 AND title = N'Tug at the heartstrings';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId3, N'Tug at the heartstrings', N'BBC Learning English - Feelings and Emotions. Listening and shadowing practice from the original conversation.', 2, 166);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Feelings and Emotions. Listening and shadowing practice from the original conversation.', lesson_order = 2, duration = 166
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/feelings-and-emotions/Tug-at-the-heartstrings/240909_tews_tug_at_the_heartstrings_download.mp3', 'BBC Learning English', N'Tug-at-the-heartstrings',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/feelings-and-emotions/Tug-at-the-heartstrings/transcript.json', 'BBC Learning English', N'Tug-at-the-heartstrings',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to The English We Speak, where we explain phrases used by fluent English speakers so that you can use them, too!', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Feifei, and I''m joined by Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'Hi Feifei!', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'How are you doing?', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'I''m well, thank you.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'I actually feel quite emotional, Feifei!', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'This morning, I watched a video about a man who was reunited with his pet dog after it had been missing for three weeks!', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'It was a beautiful story - it really tugged at my heartstrings!', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Ah, how sweet.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Now, you said the story ''tugged at your heartstrings'', and that''s the phrase we''re looking at in this programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'''Tug'' or ''pull'' at the heartstrings means that something brings out strong emotions.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'That''s right, but particularly emotions you might associate with the heart - things like love, compassion, sympathy or sadness.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'So, imagine something literally pulling on the nerves or strings attached to the heart.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Feifei, when was the last time something pulled at your heartstrings?', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Recently, I''ve been following this soap on TV, and the two main characters really want to be with each other, and they''ve overcome a lot of difficulties and we, the audience, really wanted them to be together as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'But in the end, they still couldn''t be with each other.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'It really tugged at my heartstrings.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Oh, I bet it did!', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Let''s hear more examples from our BBC Learning English colleagues.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'I saw my niece perform a song from a musical and it really tugged at my heartstrings.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'So, my friend has just got a puppy and he is so cute!', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'He was really tugging at my heartstrings.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'I was going through my stuff from high school, and I saw all the silly things I did with my friend and all the old memories started tugging at my heart strings.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'We often hear references to the heart when we talk about emotions, don''t we?', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Yes, in a similar way, if something affects you emotionally, you can say that it ''touched your heart'', like that video you watched about the man and his dog probably touched your heart.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'It did.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'And if something ''melts your heart'' it means you start to feel emotional about something that maybe you didn''t before.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'It''s the idea that your heart was icy and cold but now it''s melted.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'OK, let''s recap.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'We''ve learned ''tug at the heartstrings'', which means something makes you feel emotions like sympathy, love and compassion.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Remember to check out our website: bbclearningenglish.com for more resources to help you improve your English.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'Thanks for joining us.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Bye!', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId3 AND title = N'Yourself';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId3, N'Yourself', N'BBC Learning English - Feelings and Emotions. Listening and shadowing practice from the original conversation.', 3, 345);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Feelings and Emotions. Listening and shadowing practice from the original conversation.', lesson_order = 3, duration = 345
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/feelings-and-emotions/Talking-about-yourself/RealEasyEnglish_s2e7_yourself.mp3', 'BBC Learning English', N'Talking-about-yourself',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/feelings-and-emotions/Talking-about-yourself/transcript.json', 'BBC Learning English', N'Talking-about-yourself',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to Real Easy English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'In this podcast, we have a real conversation in easy English to help you learn.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'You can find the vocabulary from this episode and more to help you with your English at bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Hi, Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'How''re things?', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'I''m very well, thank you, Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'How about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'I''m very good, thank you.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Now, in today''s episode, we are going to talk all about us!', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Yes, that''s right.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'We''ll help you learn to talk about yourself, your feelings, and your personality.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'So, Neil, what do we mean by personality?', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Well, we use the word personality to talk about the type of person we are.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'So how we behave, how we feel, how we think.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'For example, we can have a warm personality or a friendly personality.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'So, Neil, how would you describe yourself?', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'What kind of personality do you have?', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Ah that''s difficult Beth!', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'I think I''m quite calm, laid back, maybe.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'I don''t get too upset or angry very easily.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'How about you, Beth?', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'What''s your personality?', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'I think that when I first meet people, I''m quite shy.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'I''m a bit quiet, and I don''t say much, and maybe feel a bit nervous.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'But as soon as I get to know somebody, I am much more confident and then I''m very chatty and talkative.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'So then I do talk a lot.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'OK, Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'So, can you tell me what your interests are?', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'What you like doing?', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Well, I really like reading.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'I read quite a lot and I listen to audio books as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'I find lots of different topics interesting, and I also love going to the theatre.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'I find that really exciting.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'I really like spending time with friends, chatting, having a laugh.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'And I really like live music, too.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'So, Neil, you said you like going to watch live music.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Why do you enjoy that?', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'Yes, I get really excited to see a new band that I like, that I''ve heard and I haven''t seen before, and I think it''s also really exciting to hear live music because it exists just then, that moment, and it''s never repeated.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Yeah, that''s kind of why I love going to the theatre.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'It''s special because it''s just one performance.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Anything could happen, and it''s really exciting to just be there in the moment.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'Now, when you''re with your friends, I know that you laugh quite a lot.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Would you say that you have a good sense of humour?', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Maybe, yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'I do, I like laughing, and I think I like to see the funny side of things in the world.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'So, usually, everybody has a sense of humour.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'It just means the kind of things that you find funny.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Yeah, and Beth, I remember once you told me you and your friend have a secret language.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'We do.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'It''s very silly.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'She made it up when she was a child, and she taught me it.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'So, sometimes we have secret conversations, and it''s very silly, and we find it very funny.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'I would say I have a silly sense of humour.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Well, Beth, I''ve learned a lot about you today.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'Shall we have a look at the vocabulary we used during our conversation?', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'So, we had personality.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'That is the type of person you are.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'We''ve had sense of humour, the types of things that make you laugh.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'And throughout the conversation, we have used adjectives that end in -ed and -ing.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'For example, Neil, you are excited to watch live music because you find the live music exciting.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'We use adjectives that end with -ed like excited and interested when we mean our feelings.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'You are interested in something.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'And we use adjectives that end in -ing to talk about the thing that gives you that feeling.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'For example, you find live music exciting.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'And I find reading books interesting.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'That''s it for this episode of Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'If you want to learn more about how to talk about yourself, try one of our free easy English courses.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'You can find them on our website bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'Next time on Real Easy English, we''ll talk about a very British topic, the weather.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'See you next time.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'Bye.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'Bye!', NULL, NULL, NULL, NULL);

DECLARE @CourseId4 BIGINT;
SELECT @CourseId4 = course_id FROM Courses
WHERE title = N'Friends and Family'
  AND learning_mode = 'casual' AND course_type = 'curriculum';
IF @CourseId4 IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'Friends and Family', N'Natural English for conversations about friends, family, and relationships.', 'Beginner', 'casual', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @CourseId4 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'Natural English for conversations about friends, family, and relationships.', level = 'Beginner',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @CourseId4;
END;

-- Free the managed lesson-order range before applying the current source ordering.
UPDATE Lessons
SET lesson_order = lesson_order + 10000
WHERE course_id = @CourseId4
  AND lesson_order < 10000
  AND EXISTS (
      SELECT 1 FROM Lesson_Material AS managed_material
      WHERE managed_material.lesson_id = Lessons.lesson_id
        AND managed_material.source_provider = 'BBC Learning English'
  );

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId4 AND title = N'Friends in high places';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId4, N'Friends in high places', N'BBC Learning English - Friends and Family. Listening and shadowing practice from the original conversation.', 1, 163);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Friends and Family. Listening and shadowing practice from the original conversation.', lesson_order = 1, duration = 163
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/friends-and-family/Friends-in-high-places/180618_tews_friends_in_high_places_download.mp3', 'BBC Learning English', N'Friends-in-high-places',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/friends-and-family/Friends-in-high-places/transcript.json', 'BBC Learning English', N'Friends-in-high-places',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to The English We Speak.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Feifei…', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'…you are, and I''m Rob.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'Rob, a question - is it right you have a friend who lives on top of a mountain?', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Errr no.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'What about a friend who lives in La Paz - one of the highest cities in the world?', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Strange question, but no.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'OK, how about a friend who lives at the top of a very tall tower block?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'Definitely not.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Why are you asking about my friends anyway?', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Well, someone in the office said you have friends in high places - and I just wondered why it was useful to know people who lived high up.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'OK, well if you have friends in high places it has nothing to do with their physical location - they are people you know who are powerful and in an important position and are able to help you.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'So these are useful people to know then?', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'They certainly are.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Let''s hear some examples of other friends in high places…', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Wang managed to get a promotion but I''m sure it''s only because he knows people in high places.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Thanks to his friends in high places, my boyfriend managed to get tickets for the sold-out rock concert.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Yeah!', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Despite failing her exams, Jane still managed to get a place at university - I''m sure she has friends in high places.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'This is The English We Speak from BBC Learning English and we''re talking about the phrase ''friends in high places''.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'These are powerful and important people we know and might be able to help us in some way.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'So Rob, you know some very important people then?', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Well, yes a few - although not the Queen.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Why do you want to know?', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Well I have to renew my passport and I need someone important to witness my application.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Oh come on Feifei, you know I could do that.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Err sorry Rob.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'You may be a friend but you''re not in a high enough position to do this!', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Look, it needs a doctor, lawyer or policeman to sign it.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Great!', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'So how high am I?', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'About this high.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'That low.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Oh dear.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'Time to make some new friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Bye bye.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId4 AND title = N'Ship';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId4, N'Ship', N'BBC Learning English - Friends and Family. Listening and shadowing practice from the original conversation.', 2, 162);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Friends and Family. Listening and shadowing practice from the original conversation.', lesson_order = 2, duration = 162
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/friends-and-family/Ship/251027_tews_ship_download.mp3', 'BBC Learning English', N'Ship',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/friends-and-family/Ship/transcript.json', 'BBC Learning English', N'Ship',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to The English We Speak, where we explain phrases used by fluent English speakers so that you can use them as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Feifei, and I''m joined by Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'Hi Feifei, how are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'I''m very well, thank you!', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'I''ve actually got a question for you, Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Have you been watching that new TV drama everyone''s talking about?', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Oh yes, I love it.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'There are two characters, Becky and Max, and I really want them to get together.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'I ship them so much.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'''Ship''.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Such a great word, isn''t it?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'And it''s what we''re looking at in this programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'In the world of fandoms, ''to ship'' means you want two people, usually fictional characters, to be in a relationship.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'Of course, ''ship'' is short for the word ''relationship''.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Exactly.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'It''s a popular word among fans of TV shows, movies and books.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'I''ve seen lots of people on social media talking about how they ship characters, meaning they want the characters to end up together.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Yes, and after the verb ''ship'' we add people''s names.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'For example, when I was reading Pride and Prejudice, I really shipped Mr Darcy and Elizabeth, and they got together in the end - I was so happy!', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Yeah, so nice!', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'But ''ship'' is not just used for fictional couples.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'You can even use it playfully to talk about real people.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'So, I have two friends who always hang out and they''re both single and I hope they get together.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'I really ship them.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Let''s hear some more examples of the word ''ship''.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'So, there''s these two people I see on my bus every day, and they don''t know each other, but I ship them.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'They''re made for each other.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'I watch the breakfast news every morning.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'There''s two presenters on there.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'I don''t think there''s anything going on, but they''re so cute together.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'I totally ship them.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'So, I''ve just read this book, and the two main characters got together at the end, but I shipped them from the beginning.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'I knew they were going to get together.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'So, to ship means you support the idea of two people being in a romantic relationship, and it''s very common in communities online.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'And fans even create ''ship names'' by combining character names.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'For example, Becky and Max, and their ship name could be…Bax!', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'So, it''s a way for fans to be enthusiastic and creative together.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'Indeed.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'And that''s all from us.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'We''ll be back next time with another useful English phrase.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Bye-bye.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId4 AND title = N'Family';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId4, N'Family', N'BBC Learning English - Friends and Family. Listening and shadowing practice from the original conversation.', 3, 347);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Friends and Family. Listening and shadowing practice from the original conversation.', lesson_order = 3, duration = 347
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/friends-and-family/about-family/RealEasyEnglish_ep1_family.mp3', 'BBC Learning English', N'about-family',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/friends-and-family/about-family/transcript.json', 'BBC Learning English', N'about-family',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Welcome to Real easy English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'In this programme, we have real conversations in easy English to help you practise listening and learn new words and phrases.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'In each episode, we talk in English about a different topic that you need for everyday speaking.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'If you want to read along, you can visit our website: bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Hello, Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'Hi, Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'How are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'I''m very well, thank you.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'What are we talking about today?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Well, today''s episode is all about family.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'We''ll be talking about who is in our families and comparing them.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'OK, great.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'So, what do we mean when we say comparing our families, Beth?', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Well, when we compare two things, we look at them and see if they are the same or different and we can compare things in different ways, but we often do it with adjectives.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'OK, let''s start the conversation.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'So, Neil, how big is your family?', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'It''s probably average, really.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'I have one sister, but she has three kids and I have two kids.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'So, when we get together with my parents, there… there are a few of us.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'It''s not...', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'It''s not tiny.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'It''s not huge.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'OK, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'I think my family is definitely smaller than yours because I don''t have any brothers or sisters.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'I am an only child.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'So, when I get together with my family it''s very small because I also only have one cousin and she''s an only child as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'So my family''s tiny!', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'And what is your family like?', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'It''s small, but what''s it like?', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'It is small, but we''re very close.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'So, we see each other quite often.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'We are a bit silly.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'We like to go out and have food and play games and we can be quite loud, even though there aren''t many of us.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'OK, it sounds like you get on well with your family.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'Is that right?', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'Yeah, definitely.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'I love spending time with my cousin and she has two children.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Do you get on well with your sister?', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'Yes, I get on well with my sister.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'We don''t see each other very often because we don''t live in the same place.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'But when we see each other, we have a nice time.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'We catch up and chat.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'So, Beth, we have used the expression get on well with someone.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'What does that mean?', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Well, that means that you have a good relationship with them.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'So, if you get on well with your sister, it means when you''re together you''re happy, you''re not fighting.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'It''s easy to have good conversation.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'You don''t really have any arguments.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Maybe when you were a child, you didn''t get on well with your sister.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'I don''t know.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Yes, I think now that we''re grown-ups it''s easier to get on well with your siblings.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'When… When…When I was a kid, maybe I was a bit mean to her.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Oh dear!', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'So, Neil, your sister has three children.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'What are the ages of them?', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Well, for a start, they''re all boys!', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'Oh, my gosh.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'That sounds very difficult!', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'And the oldest one is 17 and he has just done his driving test.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'It''s difficult to believe because, he''s the oldest, he''s always been the oldest, but now he''s almost an adult.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'And is he the oldest including your children?', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'So out of all the kids he''s the oldest?', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'He is, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'So, in my family, my cousin has two children and they are five and two.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Ah!', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'Little ones.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'They are the youngest in the family.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'But I was the youngest in my family until they came along and I am 32!', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'So, I was used to being the youngest, but I''m not the youngest anymore, now we''ve got little ones.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'Oh well!', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'Are you sad?', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'No, I''m OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'OK, let''s quickly recap the vocabulary we learned in this conversation.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'We learnt compare which means to look at two things to see if they are the same or different.', NULL, NULL, NULL, NULL),
    (@LessonId, 82, N'Bigger and smaller, which are ways to compare the size of something.', NULL, NULL, NULL, NULL),
    (@LessonId, 83, N'Youngest and oldest which are ways to talk about the age of someone or something.', NULL, NULL, NULL, NULL),
    (@LessonId, 84, N'And we looked at get on well with someone, which means to have a good relationship with them.', NULL, NULL, NULL, NULL),
    (@LessonId, 85, N'Thanks for listening to Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 86, N'Visit our website for more activities and courses to help you with your English: bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 87, N'Next time, we''ll talk about food and some of our favourite meals to eat.', NULL, NULL, NULL, NULL),
    (@LessonId, 88, N'Mmm, delicious!', NULL, NULL, NULL, NULL),
    (@LessonId, 89, N'See you, then.', NULL, NULL, NULL, NULL),
    (@LessonId, 90, N'Bye!', NULL, NULL, NULL, NULL),
    (@LessonId, 91, N'Goodbye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId4 AND title = N'Talking about friends';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId4, N'Talking about friends', N'BBC Learning English - Friends and Family. Listening and shadowing practice from the original conversation.', 4, 307);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Friends and Family. Listening and shadowing practice from the original conversation.', lesson_order = 4, duration = 307
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/friends-and-family/Talking-about-friends/RealEasyEnglish_ep3_friends.mp3', 'BBC Learning English', N'Talking-about-friends',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/friends-and-family/Talking-about-friends/transcript.json', 'BBC Learning English', N'Talking-about-friends',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello!', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'Welcome to Real Easy English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'We''re here to help you improve your English with a real conversation in easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'If you want to read along, you can find a text version of this podcast at bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Right, let''s start the show.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'How are you today, Neil?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'I''m very well, thank you, Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'What are we talking about in this episode?', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Today is all about friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'We''ll talk a bit about our friends and why they are important.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Great, let''s get started.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'So, Neil, do you have a lot of friends?', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Yes, I do have quite a few friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'How about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Yes, I also have quite a lot of friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'I''ve lived in different places, so I have made friends in lots of different areas and times of my life.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'So, Georgie, you said that you have lived in lots of places, and I know you have lived in Spain.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'So, do you have lots of friends in Spain?', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Yes, I have a few friends in Spain.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'But now that I live in London, it''s quite difficult to stay in touch with them.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'But, actually, one of them is coming to visit this weekend.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Ah!', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'So you try to keep in touch with some of your friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'That means to keep contact with them, to make sure that you stay friends by speaking to them.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Yes, exactly.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'And if you have a lot of friends, it can be difficult to stay in touch with so many.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'What do you like to do with your friends, Neil?', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Well, I like to talk to them, mainly.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'So, I like to meet them maybe in a pub or restaurant, or we go to a sports match sometimes and we... and we talk there and have a good time.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'That sounds nice.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'How about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Yes, I''m the same.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'During the week I think a nice plan is to go out for dinner with friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'At the weekends I like going for a walk or getting a coffee with friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'You can do both of those at the same time!', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'That''s right.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Sometimes I meet friends, we get a coffee to take away and then go for a walk around the park.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'And are you happy about the number of friends you have?', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Or would you like to have more or fewer?', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'I always like making new friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'If you start a new hobby or you move to a different place there are always more people and new people to meet.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'I like making friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'Yes, but I like to see my old friends and if I make new friends, I know it''s something that happens slowly and that''s OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Now we have used this word friends a lot, but there are other words for friends too aren''t there?', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'One is mate.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'I use mate a lot.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'We also use, pal.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'So sometimes I refer to my friends that are girls as gal pals.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'Gal pals.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'That''s a nice expression.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'So, Neil, why are friends important to you?', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Ah, yes, good question!', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Friends are important I think because you can be yourself with your friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'You don''t have to pretend if they''re good friends because they know you as you are.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'And that''s not the same everywhere you go in life.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Yep, that''s true!', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'Let''s recap the language we learned during the conversation.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'We learned mates and pals, which are other words for friends.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'We heard keep in touch with, which means see or speak to someone regularly.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'We also say stay in touch.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'And a few, which means more than two, but not many.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'So I have a few friends in Spain, but not many.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'Thanks for listening to Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'Visit our website for more activities and courses to help you with your English: bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Next time, we''ll talk about holidays and places we would like to go.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'See you then, goodbye.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'Bye!', NULL, NULL, NULL, NULL);

DECLARE @CourseId5 BIGINT;
SELECT @CourseId5 = course_id FROM Courses
WHERE title = N'Travel and Transport'
  AND learning_mode = 'casual' AND course_type = 'curriculum';
IF @CourseId5 IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'Travel and Transport', N'Practical listening and speaking topics for travel, places, and transport.', 'Intermediate', 'casual', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @CourseId5 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'Practical listening and speaking topics for travel, places, and transport.', level = 'Intermediate',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @CourseId5;
END;

-- Free the managed lesson-order range before applying the current source ordering.
UPDATE Lessons
SET lesson_order = lesson_order + 10000
WHERE course_id = @CourseId5
  AND lesson_order < 10000
  AND EXISTS (
      SELECT 1 FROM Lesson_Material AS managed_material
      WHERE managed_material.lesson_id = Lessons.lesson_id
        AND managed_material.source_provider = 'BBC Learning English'
  );

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId5 AND title = N'Ecotourism: good or bad?';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId5, N'Ecotourism: good or bad?', N'BBC Learning English - Travel and Transport. Listening and shadowing practice from the original conversation.', 1, 390);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Travel and Transport. Listening and shadowing practice from the original conversation.', lesson_order = 1, duration = 390
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/travel-and-transport/Ecotourism-good-or-bad/230601_6min_english_ecotourism_download.mp3', 'BBC Learning English', N'Ecotourism-good-or-bad',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/travel-and-transport/Ecotourism-good-or-bad/transcript.json', 'BBC Learning English', N'Ecotourism-good-or-bad',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'This is 6 Minute English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Nowadays, the word ''safari'' is often used negatively.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'For many people, the idea of killing animals for sport is unacceptable.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'As the popularity of hunting declines, safaris are swapping their guns for cameras, offering tourists the chance to photograph wild animals in their natural habitat.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'In recent years, nature and wildlife tourism, also called ecotourism, has grown massively.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'But the story is complex.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'While money from ecotourism is supposed to support threatened wildlife and traditional local cultures, the reality is sometimes different.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'In this programme, we''ll be asking: is ecotourism good or bad?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'And, as usual, we''ll be learning some useful new vocabulary as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'But first I have a question for you, Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'Most tourists on safari are looking for ''the big five'', the name given to Africa''s most iconic large animals.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'But which animals are ''the big five''?', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Is it: a) the lion, leopard, giraffe, baboon and buffalo b) the lion, leopard, tiger, elephant and buffalo or c) the lion, leopard, rhinoceros, elephant and buffalo?', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'I guess it''s a) the lion, leopard, giraffe, baboon and buffalo.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'I''ll reveal the answer at the end of the programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'The balance between the good and bad things ecotourism can bring is well understood by Vicky Smith, whose website, Earth Changes, matches ecotourists with environmentally-friendly travel companies.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Here is Vicky talking with BBC Radio 4 programme, Costing the Earth.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Just because tourism is nature-based, it doesn''t mean to say it''s necessarily responsible or sustainable.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'So, there''s a lot of animal activities in tourism that we know which are, you know, highly irresponsible and unsustainable, like a performing whale and dolphin shows, or swimming with dolphins, elephant-riding, tiger selfies where the tigers are drugged.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Genuine ecotourism is sustainable - designed to continue at a steady level which does not damage the environment.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Not every travel company which calls themselves eco-friendly acts sustainably, and may still advertise irresponsible tourist activities, including tiger selfies - having your photo taken with a captive wild tiger.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'There are two requirements travel companies should meet to qualify as genuine ecotourism.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'First, tourists'' main motivation should be to appreciate and observe the natural world without interfering, and second, the money they spend should support traditional communities.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Clearly, having your photograph taken with a chained and drugged tiger does not meet these requirements.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'But not all companies claiming to be ecotourism behave so irresponsibly.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'According to Antonia Bolingbroke-Kent, who runs small scale wildlife expeditions to some of the most remote places on Earth, it''s possible to put travel companies on a sliding scale from good to bad.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'On BBC Radio 4''s programme, Costing the Earth, Antonia discussed her work in Tajikistan, a country where ecotourism is making a positive impact on both animal and human communities.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'At the other end of the scale is Tajikistan, where I work a lot, which gets less than two dozen wildlife tourists a year, and the money these visitors bring is essential to the conservation work that grassroots NGOs are doing.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'So those few tourists… their money goes a very long way and the animals people are looking at… snow leopards, rare mountain ungulates like Bukharan markhor, they are being observed from a distance, their behaviour is not being affected in any way, and the local communities are genuinely benefiting.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'Antonia uses the phrase at the other end of the scale as a way of contrasting irresponsible tourist companies with what''s happening in Tajikistan.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'There, animals including snow leopards and mountain ungulates, are being protected by ecotourist projects run by non-governmental organisations, or NGOs - organizations trying to achieve environmental or social aims outside of government control.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'These NGOs are grassroots organisations meaning that they are run from the bottom up, by ordinary people rather than leaders.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Despite getting very few ecotourists a year, the money they spend in Tajikistan goes a long way, in other words, the money is an important factor in achieving their goals, which in Tajikistan at least, means protecting rare wild animals.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'OK, it''s time to reveal the answer to my question.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'You asked me about ''the big five'', the name for Africa''s iconic safari animals.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'I guessed they were: the lion, leopard, giraffe, baboon and buffalo.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'You guessed right about the lion, leopard, and buffalo, but the others were the rhinoceros and the elephant.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'OK, let''s recap the vocabulary we''ve learned from this programme about ecotourism - travel to places of natural beauty where the tourists'' motivation is to appreciate nature and support the local culture.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'The adjective sustainable describes actions designed to continue at a steady level so as not to damage the environment.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'A tiger selfie means having your photo taken with a captive wild tiger, not something to be advised!', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'The phrase at the other end of the scale is similar in meaning to the phrase, ''by contrast''.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'A grassroots NGO is a non-governmental organisation which tries to achieve its aims through the actions of local ordinary people rather than leaders.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId5 AND title = N'Public transport';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId5, N'Public transport', N'BBC Learning English - Travel and Transport. Listening and shadowing practice from the original conversation.', 2, 343);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Travel and Transport. Listening and shadowing practice from the original conversation.', lesson_order = 2, duration = 343
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/travel-and-transport/Talking-about-public-transport/RealEasyEnglish_public_transport_download.mp3', 'BBC Learning English', N'Talking-about-public-transport',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/travel-and-transport/Talking-about-public-transport/transcript.json', 'BBC Learning English', N'Talking-about-public-transport',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to Real Easy English, the podcast where we have real conversations in easy English to help you learn.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Neil, and with me is Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'Hello.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'Did you know that you can now watch a video of this podcast?', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'And you can read along with a transcript on our website.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Visit bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Hi, Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'How are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'I''m very well, thank you, Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'How are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'How was your journey into work?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'My journey into work was very good today, actually.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'And we''re actually talking about transport today.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'How was your journey to work?', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'My journey was quite easy this morning, actually.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'It isn''t always.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'And, Neil, we actually have a very similar commute, don''t we?', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'I get the Tube to work, which in the UK is the underground train.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'So I walk to the station around 15 to 20 minutes, and then I get the underground train, one train, and then I change and then get another one, and then I walk to work.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'I do that, but before, before your bit, I have to get a bus as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Are you that far away from the station?', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'A couple of miles.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Three kilometres.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'A bit too far to walk, then.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'A bit too far to walk.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'I have walked when the public transport has been unreliable.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'But normally I, I get the bus to the Tube stop, and then I get on the Tube into work.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Lovely.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'And do you like the Tube?', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'What do you think about it?', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'I think the Tube is very convenient, actually, because the trains go very frequently.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'So if you miss one, you can wait just a couple of minutes and get the next one.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'And it''s probably the quickest way to get into central London because there''s so much traffic.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Driving is a really bad idea.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Yeah, I agree with you.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'It is very reliable.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'It''s usually very easy.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'The thing I don''t like about it, especially in the morning at rush hour, is the number of people on the Tube.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'The, the underground trains in London are very old.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'They''re very small.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'And they are very crowded in the mornings.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'So crowded means there''s lots of people.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'We can also say packed.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'And I really don''t like being in a tight space with lots of people in the mornings, so early in the morning.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Yeah, I agree, but I''m lucky because I get on the Tube at the first stop, so I always get a seat, but then by the time you get on, it is usually really packed.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'And the closer you get into central London, the more packed it is.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'You''re squashed up against strangers.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'It''s not always fun.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'I hate it.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'What about buses?', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'Do you like buses?', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Buses are OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'They, in London again, they are quite frequent.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'And that''s convenient.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Not always.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'I mean, they are, they''re frequent, but they''re more unreliable than trains, aren''t they?', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'There always seems to be roadworks near where I live, and when there are roadworks, there are traffic lights and then there are delays on the buses.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'So in the city, public transport can be great.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'But what about when we go outside of the city?', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'What do you use to go to other parts of the UK?', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'If it''s quite far, so if I''m going really far north, maybe to Scotland or to, I don''t know, Manchester or a northern city, it''s probably better to get the train because it''s faster and I really like trains.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'You can relax.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'It''s a relaxing experience.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'Some of my best travel experiences have been on long distance trains.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'Yeah?', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Can you...', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Where have you been that''s been so fantastic on a train?', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'Well, I once went from Prague to Moscow on a train.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'Wow.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'What was that like?', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'Really exciting.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'Why?', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'It took about three days, and I was with some friends, and we could eat and drink and look out the window.', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'The landscape changed frequently.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'It was just exciting.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'Let''s recap the vocabulary we heard in this podcast, starting with some useful adjectives to describe public transport.', NULL, NULL, NULL, NULL),
    (@LessonId, 82, N'For example, we had crowded, which means very busy.', NULL, NULL, NULL, NULL),
    (@LessonId, 83, N'We also hear packed.', NULL, NULL, NULL, NULL),
    (@LessonId, 84, N'We had reliable, which describes something you can trust, and unreliable which describes something you can''t trust.', NULL, NULL, NULL, NULL),
    (@LessonId, 85, N'We heard frequent, which describes something that happens often.', NULL, NULL, NULL, NULL),
    (@LessonId, 86, N'And we also heard delayed, which means something, like public transport, comes later than expected.', NULL, NULL, NULL, NULL),
    (@LessonId, 87, N'That''s it for this episode of Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 88, N'Don''t forget to go to our website where you can get a free worksheet to download to test what you''ve learnt.', NULL, NULL, NULL, NULL),
    (@LessonId, 89, N'It''s at bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 90, N'Next time, we''re talking about different times of year.', NULL, NULL, NULL, NULL),
    (@LessonId, 91, N'Spring, summer, autumn and winter.', NULL, NULL, NULL, NULL),
    (@LessonId, 92, N'See you then.', NULL, NULL, NULL, NULL),
    (@LessonId, 93, N'Goodbye.', NULL, NULL, NULL, NULL),
    (@LessonId, 94, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId5 AND title = N'Visiting places';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId5, N'Visiting places', N'BBC Learning English - Travel and Transport. Listening and shadowing practice from the original conversation.', 3, 299);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Travel and Transport. Listening and shadowing practice from the original conversation.', lesson_order = 3, duration = 299
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/giao-tiep/travel-and-transport/Talking-about-visiting-places/RealEasyEnglish_visiting_places_download.mp3', 'BBC Learning English', N'Talking-about-visiting-places',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/giao-tiep/travel-and-transport/Talking-about-visiting-places/transcript.json', 'BBC Learning English', N'Talking-about-visiting-places',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'In this podcast, we have real conversations in easy English to help you learn.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'You can find a video of this podcast and a worksheet to help you practice what you''ve learnt on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'How are you today, Beth?', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'I''m very well, thank you.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'How are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'Yes, I''m pretty good, thank you.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Good.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'So, what are we talking about today?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Well, Beth, today we are talking about places.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'We''ll talk about the city and the countryside and which we prefer.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'Very nice.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'OK, let''s start then.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'So, Georgie, if you were going to visit somewhere, for example, would you prefer to visit the city or the countryside?', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Are we talking about the UK or going abroad?', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Let''s start with the UK.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'If I visit somewhere in the UK, I would usually want to go to the countryside.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'That''s because I live in a city.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'I live in London, but I''m from the countryside, so sometimes I crave going out into a more rural area where I can see more green space, there''s more wildlife.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'It''s almost like you need to escape the, the busy hustle and bustle of the city.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'D''you know, I, I completely agree with you.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Cities in the UK are great.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'There are some great cities, but I have visited quite a lot of them.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'And after a while the cities are all similar.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Whereas the countryside, you can do different activities.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'It''s really nice to see new places.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'I, a few years ago, I went to Kent and I had never been to Kent before because I''m from the north of England and Kent is in the south, and it was amazing.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'They had different kinds of buildings and like architecture on the farms.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'We don''t have those same kind of roofs in the farms in the, in the north.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Yeah, I also went to Kent last year.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'I did, but I went to the seaside because Kent also has a bit of coastline.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'And yeah, I really enjoyed visiting Kent.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'And the villages are so like cute and picturesque, we could say.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'What about when you go abroad?', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Do you, do you also visit the countryside in other countries?', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'I have done, but to be honest, I would normally go to a city if I go abroad, maybe the capital city or maybe a smaller city.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'But one of the reasons for that, I think, is the transport.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'It''s so much easier to get a train or a bus when you''re in a city.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'And you''d have to hire a car, I think, to visit a village or something like that.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'It''s very true.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Usually if you go abroad, you fly to an airport and the airports are usually closer to the city.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'And if you only have a few days in a place, it''s easier to just stay in the city.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'It''s also, I guess, easier to do lots of, like, activities in the city when you visit and try out lots of the local restaurants, everything is closer together so you can do more things.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Well, I''m just thinking if you''re in the city you can, like, search on your phone for really good restaurants really easily.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'But if I''m in Italy, for example, and I don''t speak Italian, it would be really difficult, I think, for me to find a good restaurant because I wouldn''t know where to go.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Sometimes it''s the locals in the countryside that are able to tell you this.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'It might not even be on the internet.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Yep.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'That''s very true.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Let''s recap the language we used in this conversation, starting with countryside land outside of towns and cities where there are not many buildings.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'We can also describe this with the adjective rural.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'And hustle and bustle, the busyness and noise that you find in the city.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'We had picturesque, which means pretty or beautiful.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'We often use this to describe a place.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'And visit, to go somewhere for a short time.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'That''s it for this episode of Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'Go to our website to find a worksheet to test what you''ve learned. bbclearningenglish.com', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'And next week we''ll be talking about gifts.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'So see you then.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'Goodbye.', NULL, NULL, NULL, NULL);

DECLARE @CourseId6 BIGINT;
SELECT @CourseId6 = course_id FROM Courses
WHERE title = N'Workplace English - Easy'
  AND learning_mode = 'professional' AND course_type = 'curriculum';
IF @CourseId6 IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'Workplace English - Easy', N'Easy English conversations about offices, jobs, and everyday workplace life.', 'Beginner', 'professional', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @CourseId6 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'Easy English conversations about offices, jobs, and everyday workplace life.', level = 'Beginner',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @CourseId6;
END;

-- Free the managed lesson-order range before applying the current source ordering.
UPDATE Lessons
SET lesson_order = lesson_order + 10000
WHERE course_id = @CourseId6
  AND lesson_order < 10000
  AND EXISTS (
      SELECT 1 FROM Lesson_Material AS managed_material
      WHERE managed_material.lesson_id = Lessons.lesson_id
        AND managed_material.source_provider = 'BBC Learning English'
  );

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId6 AND title = N'Work';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId6, N'Work', N'BBC Learning English - Workplace English. Listening and shadowing practice from the original conversation.', 1, 368);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Workplace English. Listening and shadowing practice from the original conversation.', lesson_order = 1, duration = 368
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/easy-level/Talking-about-work/260417_REE_work_download.mp3', 'BBC Learning English', N'Talking-about-work',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/easy-level/Talking-about-work/transcript.json', 'BBC Learning English', N'Talking-about-work',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to Real Easy English, where we have real conversations in easy English to help you learn.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Becca.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'You can find a video version of this podcast on our website and test yourself with a worksheet on bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Hello Becca.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'How are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'I''m really well, thank you, Georgie.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'How about yourself?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'Yes, I''m good, thank you.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Are you having a busy week?', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'It has been a little bit busy, but also quite fun.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'How about yours?', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Me too.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'I''ve had a busy week, but it''s going well.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'And Becca, today we''re talking about work.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'We''re going to talk about what we''re good at, at work, and also what we do when things go wrong.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Ooh.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'So, what do you like about work?', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'I like that we have different tasks to do in the day, and some of them are creative, and then other tasks you really need to, kind of, put more thought and focus into.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Becca, do you ever find work stressful?', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Of course.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'I think that you will find work stressful at some point during your job or your career.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Yeah, I think if I haven''t planned things well - if I haven''t done the preparation well - then I find things a bit stressful.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'How about yourself?', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Yeah, me too.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'When we have lots of projects as well, I feel like when we have a lot of things going on, it can be stressful to, kind of, juggle different tasks.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'And when we need to publish a new podcast or a new video series, it can be... there can be a lot of pressure to get things done.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'So, would you say that you''re someone who gets stressed easily at work?', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'Because you seem to be quite calm all the time.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'No, I don''t actually get too stressed very often.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Even when there''s a bit of pressure at work, I tend to stay quite calm.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'I always just think, there''s no point in worrying.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Worrying doesn''t actually fix the issues.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'And you can only do what you can do.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'So, yeah, I tend to stay quite relaxed.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Do you get quite stressed or...', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Yeah, as I previously mentioned, I think if...', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'I''m usually quite an organised person but, you know, there are some times where I feel a bit disorganised.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'And if I don''t prepare, I find myself in stressful situations and I might start feeling a bit nervous, especially as time gets closer to completing something.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'I really don''t like having things late.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Not just in work, but in my general life.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'So, you get nervous if you''re not feeling organised.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'But are you good at being organised?', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'If I write things down, then I tend to stay organised.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'It''s likely that I won''t feel very stressed.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'I can start to relax a bit when I know that, you know, I''ve ticked things off my list.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Do you write to-do lists?', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'I do.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'I do write to-do lists.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'And that can be really helpful to, kind of, keep calm because you have your whole plan on a page that you can see.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'And you can, kind of, know where you are in your week, which is really helpful.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'What happens when things go wrong at work?', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'What... how do you, kind of... how do you deal with these situations?', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Well, I''ve only been here for a little while in comparison to yourself and the rest of the team.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'And I feel really thankful that you''re all very supportive.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'So, if I do have something that I haven''t quite finished, or I''m not really sure what I''m doing with it, or it just goes completely wrong, then I feel that I can come to yourself or any of the team to ask for a little bit of help or guidance.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Oh, that''s good!', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'I don''t think anything''s gone completely wrong since you''ve been here.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'Not yet...', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'Audio listeners - fingers crossed!', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'Fingers crossed!', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Let''s recap the language we heard during the conversation, starting with stress.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Now, stress is a feeling of worry, usually caused by a particular situation.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'And you can describe a person as being stressed, and that is the feeling of having stress.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'And we can also say stressful, which describes the situation.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'We also had calm, and that''s when you don''t feel stressed or worried about something.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'For example, Georgie is always very calm at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'We also heard nervous, which describes someone who worries a lot.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'We had organised - someone who is organised plans very well and prepares very well.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'The opposite of organised is disorganised.', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'And we heard supportive, and if someone is supportive, they help other people.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'That''s it for this episode of Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'We''ll be back next week with another easy English conversation.', NULL, NULL, NULL, NULL),
    (@LessonId, 82, N'And if you want more easy English, head to our website to try some of our other easy series - for example, the London Letter Challenge.', NULL, NULL, NULL, NULL),
    (@LessonId, 83, N'Go to our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 84, N'Goodbye for now!', NULL, NULL, NULL, NULL),
    (@LessonId, 85, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId6 AND title = N'The office';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId6, N'The office', N'BBC Learning English - Workplace English. Listening and shadowing practice from the original conversation.', 2, 322);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Workplace English. Listening and shadowing practice from the original conversation.', lesson_order = 2, duration = 322
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/easy-level/Talking-about-the-office/RealEasyEnglish_office_download.mp3', 'BBC Learning English', N'Talking-about-the-office',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/easy-level/Talking-about-the-office/transcript.json', 'BBC Learning English', N'Talking-about-the-office',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'In this podcast, we have real conversations in easy English to help you understand.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'You can watch a video of this podcast and find a worksheet to help you learn on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Hi Beth, how are you today?', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'I''m very good, Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'How are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'I''m very well.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'What are we talking about?', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Today we are talking about work and the office.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'An office is a place with desks and computers where people do their jobs.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'We are in a studio in the office, but we''ll go back to the office after this.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Yes we will.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'So, Neil, where do you work?', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Do you work at home or do you work in the office?', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Well, I do both of those things.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'I work in the office usually three days a week, and I work from home two days a week.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'OK, I''m the same.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'We have the same schedule.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'So, yeah, at home twice, where I have, a sort of place that could be considered a home office.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Do you like working in the office?', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'I do like working in the office.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'I think it''s easy to get work done in the office because it''s an office environment.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'It''s a workplace.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'But I also quite like working from home sometimes because, commuting.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'London is a big city, you know, coming to work and going back is really tiring if you do it every day.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Yeah, I know what you mean.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'I, I like working from home occasionally, but I definitely prefer being in the office.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'I think being with colleagues around you and I need people, and then I can work better.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'So what''s wrong with working from home?', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'A lot!', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'I think, when you''re in the office, yeah, it''s louder than at home, but I like having the people around me.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'I like being able to have conversations.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'I find it''s easier to work in an office environment because everyone else is working.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'But I feel more lonely at home because it''s just me.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'So, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Yeah, I understand what you mean.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'I also have a bit of a problem with working from home, because the boundary or the line between work and home is not so clear.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'So when you finish your day, you close your computer and you think, oh, hang on, I''m still at home.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'This is where I''ve been working all day.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'And sometimes you feel like you should be doing things for home when you''re actually working.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Yeah, it''s a bit weirder.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Like, it''s, it''s strange being at home because it''s harder to know whether you should be working or doing a bit of housework.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'In the office, it''s easier to just sit and be with your laptop, be with your colleagues and just be, be working.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Have you ever worked somewhere that is not an office?', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'So I used to work at a farm.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'It was a farm park.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'So lots of children playing and there was a cafe.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'There was a play area, go carts, being with animals.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'So lots of different areas.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'And it was fun.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'Lots of variety.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'So that wasn''t an office.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'I''ve worked in all kinds of places, especially when I was a student.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'I worked in factories, farms.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'I''ve worked in restaurants, bars, all kinds of places that are not offices.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'You''ve done it all.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'I''ve done it all.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Let''s recap some of the vocabulary we heard in this podcast.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'We had office, a place with desks and computers where people go to work sometimes.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'At BBC Learning English, we work in the office.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'We had working remotely, which we often also call working from home, and that means working somewhere that isn''t the office, for example, your home.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'But it could also be a cafe or something.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'And we also had colleagues.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'These are the people that you work with.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'And we compared working from home with working from an office.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'And we used lots of comparatives.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'So it''s harder to concentrate at home or it''s more difficult to get your work done in the office.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'And it''s louder in the office, but sometimes that''s better.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'That''s it for this episode of Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'You can test what you''ve learned with a worksheet, which you can download for free from our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'And next time we''ll be talking all about games.', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'See you then.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'Goodbye.', NULL, NULL, NULL, NULL);

DECLARE @CourseId7 BIGINT;
SELECT @CourseId7 = course_id FROM Courses
WHERE title = N'Job Applications'
  AND learning_mode = 'professional' AND course_type = 'curriculum';
IF @CourseId7 IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'Job Applications', N'A practical series covering CVs, job descriptions, interviews, and job offers.', 'Intermediate', 'professional', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @CourseId7 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'A practical series covering CVs, job descriptions, interviews, and job offers.', level = 'Intermediate',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @CourseId7;
END;

-- Free the managed lesson-order range before applying the current source ordering.
UPDATE Lessons
SET lesson_order = lesson_order + 10000
WHERE course_id = @CourseId7
  AND lesson_order < 10000
  AND EXISTS (
      SELECT 1 FROM Lesson_Material AS managed_material
      WHERE managed_material.lesson_id = Lessons.lesson_id
        AND managed_material.source_provider = 'BBC Learning English'
  );

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId7 AND title = N'After the interview';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId7, N'After the interview', N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', 1, 343);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', lesson_order = 1, duration = 343
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/job-applications/After-the-interview/241007_after_the_interview.mp3', 'BBC Learning English', N'After-the-interview',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/job-applications/After-the-interview/transcript.json', 'BBC Learning English', N'After-the-interview',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to Learning English for Work from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'This is our special series all about getting a job in English.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'In each episode, we talk about a different step in the job application process and learn some useful vocabulary along the way.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Today, the interview''s over and we''ve done all we can.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'It''s time to talk about accepting a job offer or, if you''re unsuccessful, how to ask for feedback.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'You can find a transcript for this episode to read along on our website: bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'So today''s episode is about how to communicate with an employer after the interview is over.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Yeah and this can sometimes be a really bad part, can''t it because you… maybe the interview didn''t go that well and you''re worrying about, I don''t know, something you said or anything.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'But until you hear something, you don''t really know.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'It could be good.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'It could be bad.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'And sometimes it takes a lot longer than you think it''s going to take as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Yeah, so communication after an interview often is by email.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'So it can be quite slow and you might be really nervous or stressed during the waiting, but you need to keep your communication calm and professional, I guess, even if you''re feeling really frustrated, you haven''t head back, you can''t kind of show that in your communication.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'You''ve got to stay polite.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Yeah, definitely.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'In each episode of this series, we''ve been hearing from Amy Evans, who works in recruitment for the BBC World Service.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'And Amy says the most important thing, as we said, is to keep your communication polite.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Feel free to, you know, email them and just have a sort of polite, formal email that says, ''Can I just ask, following up, what the outcome of the interview is''.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'OK, let''s say we''ve got the job.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'What are the next steps?', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Obviously, if you''re happy to accept then you can just get straight back and say yes, I''m very happy to accept.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'If you feel you would like to negotiate salary or if you have any questions, you can say that you would like to negotiate the salary or the terms.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Ask who the best person to speak to is to negotiate that.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'Negotiations can be tricky and will depend on your country and context.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'We actually made an episode all about negotiations in our series Office English, so have a listen to get more on the specifics.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'But Amy says that the key thing with negotiating a salary is to be clear about what you''re asking for.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'If they''ve offered you one salary and you have another salary in mind, say what the other salary is and your reasoning for it.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'And then just allow them a bit of time to kind of go away because there''s probably, you know, a few people involved in having to make decisions about salaries or start dates.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'Of course, there''''s a possibility that even if the interview went well, that we don''t get the job.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'This can be tough, but Amy says it''s a good opportunity to try and improve.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'I would always encourage people to ask for feedback.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'So, if it''s not mentioned when you''re sent a sort of turn down or rejection letter, then absolutely feel free to reply and just say ''please can I have some feedback on the interview''.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'And as we have said over and over, make sure you stay polite in your communications, even if you''re disappointed, as the person you''re talking to might have useful feedback that you can use next time.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'Someone would hopefully be able to kind of go through and say these were your strong… your strengths and your strong points.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'But however, on this occasion, we did not feel as though you answered this bit, you know, with what we were looking for and then that helps you sort of prepare for the next interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'I suppose you might find yourself applying for another job in that company in a few months'' time.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'And you don''t want to have annoyed the person who''s in charge of the interviews.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'And especially at big companies, there''s usually quite a lot of people involved in the decision about who to hire and your salary and start date.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'So it can take time to decide and it requires lots of people and the person that you''re speaking to might not be the person making the decision.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'So you''ve got to be polite to them.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Just try and speak to the person you''ve been in contact with from the start to get your answers.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'Yeah, and I really like that thing where Amy said that if you don''t get the job, do ask for feedback.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'It can be so difficult to know why you didn''t get a job or what you''re doing wrong, particularly if you haven''t had many interviews.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'So that feedback is so… it''s so valuable if you apply for a similar job again in the future.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'Yeah and sometimes the reason you weren''t hired it might just be beyond your control, but they might be able to give you feedback on your interview performance and then you can use that to improve next time.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'They might say, ''Oh, we didn''t think you had enough examples of a certain skill''.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'So you can then think about do you have more that you could talk about next time.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Often it might just be because the other person was better.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'But that can be useful because it means if you see the same job come up again in a few months'' time, you know it''s worth applying because if they like you, but they just chose someone else, that means they''ll probably like you again.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'OK, that''s it for this episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'Next time, we''ll be talking about your first day in a new job and how to make a good impression.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Remember, there are more programmes to help you with your English at work on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Bye for now!', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId7 AND title = N'CVs';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId7, N'CVs', N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', 2, 314);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', lesson_order = 2, duration = 314
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/job-applications/Write-CVs/240826_writing_cvs.mp3', 'BBC Learning English', N'Write-CVs',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/job-applications/Write-CVs/transcript.json', 'BBC Learning English', N'Write-CVs',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'From BBC Learning English, this is Learning English for Work and welcome to a special series all about job applications.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'In this series, we''re going to take you through the process of getting a job from search, to interview, to your first day, with helpful vocabulary and tips along the way.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Today, we''ll be talking about the first thing an employer will read about you.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Your CV, or resume.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Find a transcript for this episode to read along on our website: bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'So, Phil, can you explain?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'What is a CV?', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'It''s a document that has a list of your qualifications and experience that you use to try and get a job.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Right and in American English we call a CV a resume.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'So CV, resume, they''re all that same list of qualifications and experience.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Now, it''s the first thing that an employer usually reads about you.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'So you need to make sure that you include all your key information, so that they want to find out more.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Yeah and you hear lots of statistics about how long an employer will look at your CV for, but it''s not really that long.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'So you need to make sure that you stand out and that the information is clear.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Try and keep it concise.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'So two pages or two sides of A4.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'And make sure you have sort of all your relevance career experience on there.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'This is Amy Evans.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Amy works in recruitment for the BBC World Service and so she''s dealing with the process of hiring new people all the time.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Throughout this series, Amy''s going to help us understand each stage of getting a new job and give us some top tips too.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'So, Amy said that you need to keep your CV concise, that means short and focused.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'And she also says that thinking about the layout can help, too.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Make sure it''s in an easy-to-read font with headers so that you can add distinction between the job titles and dates.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'I would say, put your most recent or most relevant experience at the top.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'So if you''re changing careers, it''s always good to highlight any transferable skills or any sort of projects that you''ve worked on right at the top of your CV.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'Should a CV be written in paragraphs and full sentences or just as bullet points?', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'For me, personally, I think I would use bullet points because it makes it very clear and easy to read right at the top of the CV.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'So, for example, if they''re looking for certain types of software that they need, or if they want someone that''s got a certain language.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'If it''s right at the top and it''s in a bullet point, it is very easy to read and you can get all that information quickly.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Remember, people look at a lot of CVs, so the most important thing is it needs to be easy to read.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'This can mean no big paragraphs or long sentences, but it also means to make sure that your grammar is accurate and your information is clear.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Now, is there anything that we should leave out of a CV?', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'You don''t need any pictures on there.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'You don''t need to put your date of birth or… or any personal details really such as gender or if your married, your family.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Just keep it sort of professional, relevant to sort of your work and what you''re applying for.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'So remember that here Amy is talking about applying for jobs in the UK.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'In some countries it may be normal to put pictures on your CV.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'So it does depend on the country and the sector that you''re working in.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'So think of a CV like a short summary of the most important things about you.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'So that''s why it''s important not to put lots of personal details, your whole life story.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'This is just the key facts.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'There''ll be lots of opportunities to talk more about yourself in an application and then later in interview, but you don''t need to put it all on that document.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Now, here''s a top tip.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'It''s a good idea to make a different version of your CV for every job you apply for, then you can move the most important skills to the top.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'So if a particular job asked for a specific skill and you can do that, make sure it''s really clear on your CV, maybe move it towards the top of it.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'And nowadays, some companies will ask you to put your qualifications and experience into a form, so they don''t want to see your CV, that you''ve lovingly formatted for them.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'But it''s useful to have it ready because then you have all of the information to hand.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'And making a CV helps you think about your strengths, what you''re good at.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'That''s it for this episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'We have lots more programmes to help you with your English at work on our website.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Like Office English, a series, all about the everyday situations we face at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'Visit bbclearningenglish.com to listen.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Next time, we''ll be looking at job descriptions and how to know whether to apply for a role.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'See you then, goodbye.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId7 AND title = N'Understanding job descriptions';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId7, N'Understanding job descriptions', N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', 3, 341);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', lesson_order = 3, duration = 341
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/job-applications/Understanding-job-descriptions/240902_understanding_job_applications.mp3', 'BBC Learning English', N'Understanding-job-descriptions',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/job-applications/Understanding-job-descriptions/transcript.json', 'BBC Learning English', N'Understanding-job-descriptions',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'From BBC Learning English, this is Learning English for Work and welcome to our special series all about job applications.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'In this series, we''re talking about every step in the process of getting a job, from search, to interview, to your first day, with helpful vocabulary and tips along the way.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'And today, we''re talking about how to decide whether a job is right for you.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'You can find a transcript of this episode to read along with on our website: bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'OK, so today we''re starting with the job search, and specifically job descriptions.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'So, what''s a job description, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'So, a job description is the information that you get from a company about a job that they''re advertising.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Right.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'And this can be quite different depending on the job.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'So, sometimes it''s really detailed and sometimes it is not so detailed.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'So that makes it a little bit difficult, doesn''t it when you''re reading a job description?', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'So, job descriptions are often written in quite formal language.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'They can use a lot of jargon so it can be difficult to understand exactly what they''re trying to say.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'And because of that, it can be difficult to know from a job description whether you should apply for the job.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'So, we''re going to try and help you with that today.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Now, throughout this series, we''re speaking to Amy Evans, who works in recruitment for the BBC World Service, about how to approach each stage of a job application.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'And Amy says that job descriptions are usually broken down into two parts.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Part of it is telling you what the job is, what the sort of overview of that job is.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'And then daily tasks, overall responsibilities of that job, what you can expect to be doing if you were working in that role.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'So, part one tells you what the job is so the detail of what a role is like or what you might be expected to do every day.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'And then the other half of it is the company and the hiring manager''s opportunity to show what they''re looking for in a candidate.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'And what somebody needs to have in order to do that job.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'So, in this part of the job description, we would expect to see either a list or a paragraph describing what skills they expect you to be able to do or what qualifications they expect you to have.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'If you''re interested in the job, this is really important to pay attention to.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'If you meet most of the listed criteria needed for the role, and you''re able to demonstrate sort of, on an application how you meet that criteria, then I think that''s how you know that, you know, you''re suitable to apply for the role.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Think of it like a checklist.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'You get a tick if you can do something that the job description is asking for.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'If the job asks for you to speak Spanish for example, and you can speak Spanish, that''s a tick.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'But it might not always be straightforward.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'You might not be able to do everything on the list.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Ideally, obviously, if you can tick off everything that they''re looking for, but certainly if you can tick off most of them, and you know that you''ve got experience in most aspects of what they''re looking for, and either you''ve got that you''re currently working in a very similar job or you have a job that might not be the same, but you still do very similar responsibilities, then I think that''s how you know that you can sort of put together a strong application as to why you should apply for the one you''re looking at.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'And if you do decide to apply for the job, the job description is your most important tool.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'I think it''s important the whole way through the process, I would say.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Because you can then use that job description to format your application and make sure that you''re demonstrating each of the points that they''re looking for either in an application form, but also when it then comes to interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'Sometimes, job description language is confusing.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'And that''s especially true when it comes to softer skills.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Those are the things that you do as part of your job but you don''t have a formal qualification for.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Yeah so, Phil, shall we have a go at looking at some common soft skills and what they mean?', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'OK, yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'So, one of them you might have is thrive in a fast-paced environment.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Yeah, so if you thrive in a fast-paced environment, that means you don''t mind being busy, and you don''t get too stressed if there''s lots to do and lots going on at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'And what about if it says details-orientated?', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Yeah so if you''re details-orientated, that means that you pay attention to the details.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'And they''re looking for somebody who can show that they don''t make silly mistakes.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'And then if it says you need strong problem-solving skills?', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'So, someone with strong problem-solving skills would usually be someone who can think on their own.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'If something goes wrong at work, they don''t panic, they just try and find a solution.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Now, here''s a top tip.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'Like Amy said, use the job description throughout your application.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'This is what the company have asked for, so you need to prove you fit into their description.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'Yeah, don''t just read the job description and then throw it away, never look at it again.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'It''s really useful when you''re preparing your application and for an interview if you get one.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'That''s it for this episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'You can find more business English help on our website: bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Next time, we''ll be talking about writing an application and the language you need to stand out to employers.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'See you, then.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'Bye.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId7 AND title = N'Preparing for an interview';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId7, N'Preparing for an interview', N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', 4, 335);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', lesson_order = 4, duration = 335
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/job-applications/Preparing-for-an-interview/240916_preparing_for_an_interview.mp3', 'BBC Learning English', N'Preparing-for-an-interview',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/job-applications/Preparing-for-an-interview/transcript.json', 'BBC Learning English', N'Preparing-for-an-interview',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'From BBC Learning English, this is learning English for work and welcome to our special series all about job applications.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'In this series, we''re talking about applying for a new job in English.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Listen to the series so far to learn about every step of the journey.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Today we''re talking about researching and preparing for a job interview and what you can do to get ready.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'You can find a transcript for this episode to read along with on our website.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'That''s bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'So, it''s a great feeling to be invited for a job interview, but it can also be really stressful, especially if you''re going to have to do the interview in your second language.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'But it''s also stressful for native speakers right, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Oh, yes, definitely.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'There''s always this feeling of pressure that you might say the wrong thing and ruin your chances.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'It''s probably very unlikely that that will happen, but it can be in the back of your mind as you''re getting ready.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'Yeah, it''s an intense situation and people usually aren''t used to that kind of situation.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'So, I think everybody gets nervous.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'There''s lots of focus on the interview itself, but there''s lots that you can do to prepare and we''re gonna be talking about preparing for an interview today.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Do you have any rituals for preparing for an interview, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'I always try to work out what questions they could ask me and try and come up with like answers that I can have ready in my mind to come up with.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'So I do a lot of practise with that.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'And then the other thing is, I just get there really, really early, but that probably doesn''t help with nerves.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Yes, it''s sitting there for an hour waiting, getting more and more stressed.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'But yeah, getting there early is a good idea.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Throughout this series, we''re speaking to Amy Evans, who works in recruitment for the BBC World Service about how to approach each stage of a job application.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'And Amy says you should start your preparations by getting as much information as possible about the interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'It''s always helpful to look up who''s on the panel so you can have an idea of what they do in the company and just so that you feel familiar with who you''re going to be meeting on the day.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'I think you can ask about the format of the interview as well and check what sort of interview they''re gonna be doing, if there''s anything that you need to prepare in advance.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'It can be reassuring to know a bit more about who you''ll be speaking to and what kind of questions might be asked.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'And if you can''t find out this information as you said, Phil, you can think about what questions are likely to come up.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'And then you can think ahead of the interview about some examples to speak about.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'And Amy says, don''t just practise on paper or in your head, practise speaking.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Either, you know, if you''ve got a friend or family member or colleague that could, you could practise and roleplay with.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'Or if you don''t have that even just, sort of, standing in front of a mirror and just practise saying them.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'So that when you come to the interview and you''re asked a question, even if it''s not the exact question you were thinking of, if it''s something quite similar, in your head, you''re gonna know with confidence that you can answer that.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'And you can go, yep I''ve got an example for that, and you can sort of go into it with ease and not having to panic too much in the situation.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Now, Phil, I''m often quite worried about practising too much for an interview or sounding like a robot, like I''ve memorised an answer.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'What do you think about that?', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'Well, yeah, that is important.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'It''s a tricky balance.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'You need to sound natural, I think.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'You want to practise enough so that you feel confident knowing your skills and experience to talk about in the interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'You don''t want to, sort of, practise so much that you almost fill your head with too many examples, so that you get a bit confused on the day.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'And you want to, as much as possible, kind of try and come across as sort of relaxed and confident rather than giving quite sort of rigid over- rehearsed answers.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'OK, so practise is important, but you shouldn''t write a script.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Yeah, and perhaps a good tip here is to talk about the examples, so that you''re confident and then you can pick the right example in the interview and then it should sound more natural.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'So, I guess, don''t practise a full answer with exact sentences, but practise talking about a time that you did something.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'And then you''ll be ready to kind of fit that to whatever question they ask.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Yeah, and you can bring notes to an interview, but just don''t read off them like a script.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'Yeah, I guess the most important thing is to feel comfortable and confident and that could also just mean researching the journey and logistics.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'I sometimes actually go to the place just to test that I know the way and I''m not going to get confused on the day.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'So, a little bit like your turning up early.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'I like to kind of practise to make sure that I''m not stressed about the journey as well as the interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'OK, that''s it for this episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Remember, you can find more programmes to improve your business English on our website, like Office English where we talk about using English in everyday situations at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'Next time, it''s interview time and we''ll talk about the best way to talk about yourself and your experience.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'See you then.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Bye.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId7 AND title = N'Interviews part 1';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId7, N'Interviews part 1', N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', 5, 421);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', lesson_order = 5, duration = 421
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/job-applications/Interviews-part-1/240923_interviews_part_1.mp3', 'BBC Learning English', N'Interviews-part-1',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/job-applications/Interviews-part-1/transcript.json', 'BBC Learning English', N'Interviews-part-1',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'From BBC Learning English, this is Learning English for Work, our podcast to help you with your business English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'In this special series, we''re talking you through the process of getting a new job in English.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'And today, we''re talking about a big step, the job interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Remember, you can find a transcript for this episode at bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'So, today''s episode is all about job interviews.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'Yes, we''ve had so many questions about job interviews and they''re something lots of people find difficult, even native speakers of English.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'So, we are going to break down the interview into two episodes.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Today we''re focusing on how to talk about yourself in an interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'And Pippa, how do you find talking about yourself?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'I find it quite difficult.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'I think it''s really hard to talk about your strengths and to always kind of talk positively about yourself.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'It doesn''t come naturally to me.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'What about you, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Yeah, I used to find this really difficult.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'I think as I''ve done more interviews, I''ve got better at just preparing things that I can use to talk about myself.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'As ever, we have our job applications expert, Amy Evans, to help us.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Amy works in recruitment for the BBC World Service, and Amy says that while it''s normal to not like talking about your strengths, it is an important part of job interviews.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Don''t be afraid to highlight your strengths and make sure the panel know why you think you''re suitable for that job.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'We talked in the previous episode about preparing for an interview and having lots of examples ready to talk about, but it can be difficult to know how to talk about these naturally in an interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Yes, but Amy has a technique to help.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'A good method for, sort of, answers is the STAR method which is situation, task, action and result.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Amy recommends the STAR method, which is a common business template for giving examples.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'It''s kind of a structure for your answers.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'S, T, A, R.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'So situation, task, action, result.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'When you''re thinking of your answers and thinking of specific examples, you can think of what the situation was, what the task was that you were asked to do, what you did, what was your action, what was your part in that situation and then what the result was.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'So that''s a good way of formatting and having concise and clear answers.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'OK, so maybe let''s try an example of a STAR answer.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'OK, so let''s imagine that I was asked to talk about how I deal with a problem at work in an interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'So this is a fictional job interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'OK, so first we''ve got S, haven''t we?', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'So that''s situation.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'What situation could you talk about?', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Right so, let''s imagine that I work in an office and I''m responsible for ordering the office supplies.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'So there''s a problem with the order and the delivery didn''t come through in time.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'And now the office needs more pens.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'That''s my situation.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'So that''s the S from star.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Situation.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Next, we''ve got the T, which is task.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'What would be your task in this situation?', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'So, in this situation, the task is that I needed to make sure that the office had the right supplies, so I needed to sort out this problem.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'OK, and so we''ve got S and T.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'So next is A in STAR and that A is for action.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'So what action would you take?', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'So, in this example, I could say that I called another supplier to get the pens for the week for the office and then I contacted the original company to make sure that the usual order would come as soon as possible and to sort out any problems with delivery.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'So that is my action.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'OK, so we''ve had situation, task and action.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'The last letter of STAR is R and that''s for result.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'So what happened, and why does that make you a really good candidate for this job?', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Yep, so here I might say that my actions meant that the office had the supplies that they needed, they had enough pens.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'And I was able to negotiate a better delivery system with the main supplier.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'So I could then say it showed that I had quick thinking and good problem-solving skills.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'So always bring the answer back to what it shows about me and that''s the result.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Well I''d definitely give you the job, Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Oh, thanks, Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'And of course, if you were in a job interview you need to use a real example.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'I talked about an imagined example there, but you need to think of a real thing that happened to you and then apply the STAR method.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'So situation, task, action, result.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'OK, let''s hear what other advice Amy has got.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'Make sure your answers, you''re bringing all the points back to the question that they''ve asked.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'So try not to kind of go off on tangents.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'And if you need a recap of what the question is, you know, don''t be afraid to say ''sorry, can you repeat the question'' to make sure that you''re kind of keeping on track.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'So, remember the interview is not just a chat, it''s about knowing whether you''re right for the job.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'So you need to make sure you''re answering the question and talking about relevant things.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'So, as we were saying earlier that job interviews can make you quite nervous, and of course the interview panel will expect you to be a bit nervous, but do you think there''s things that we can do that will make us less nervous when we get into that interview room?', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Well, I think it''s practising that structure to your answers, so that even if they ask a question that you weren''t expecting you have some examples and you''ve thought about the structure of how you will talk about them.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Yeah, and that STAR structure is really useful ''cause you''ll often find that different languages might organise ideas in different ways, but it''s a really common way in English that we sort things out.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'That you kind of start with the situation and you follow through those different steps.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'I always find that when I''ve thought of a few examples like this from things that I''ve done in jobs, it''s like I''ve had a bit of practice and it''s much easier for me to come up with something.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'If I get asked in the interview about something that I hadn''t prepared, I can usually remember something.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'And then I could put it into that structure and give an answer for the interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'So that''s it for this episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'Next time, we''ll be talking more about job interviews and how to show your interest in the company.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'In the meantime, if you want more tips for talking about your strengths, we have an episode with some helpful phrases and examples from our series Office English.', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'And you can find the link in the notes for this programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'Bye for now.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId7 AND title = N'Interviews part 2';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId7, N'Interviews part 2', N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', 6, 338);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Job Applications. Listening and shadowing practice from the original conversation.', lesson_order = 6, duration = 338
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/job-applications/Interviews-part-2/240930_interviews_part_2.mp3', 'BBC Learning English', N'Interviews-part-2',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/job-applications/Interviews-part-2/transcript.json', 'BBC Learning English', N'Interviews-part-2',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'From BBC Learning English, this is Learning English for Work, our podcast that helps you improve your English in the workplace.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'In this series, we''re talking about job applications and each step in the journey of getting a job in English.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'We talked in the last episode about the all-important job interview and talking about yourself in an interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'So if you missed that make sure you listen.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Today we''re talking more about interviews and how to show not just that you can do the job, but that you''re excited about working for the company.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'To get a full transcript for this episode to read along, visit our website: bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'So, Pippa, this episode is about job interviews and other ways you can make a good impression, right?', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Yeah, it can be really competitive when you''re trying to get a new job and there might be lots of people who are interviewing for the job who have very similar experience and qualifications to you.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Yeah and so while knowing how to sell yourself, that''s talk about your strengths, is really important, there are other parts to an interview that can help make you stand out and look better than the other candidates.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'And a really important aspect of this is showing that you''re interested in the job and excited about the company.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Make sure you research the company that you''re applying for and their values, the work that they do, any sort of specifics to that company.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'This is Amy Evans.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Amy works in recruitment at the BBC World Service and she''s sharing her experience with us as part of this series.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'And her advice to research the company might be obvious, but it can be forgotten.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Yeah, we can be really nervous about an interview and having to explain our own experience.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'But interviewers will usually want to see that you understand what the company does and are interested in it.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'They might even ask you why you''re interested in working for the company.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'It''s really good to be prepared to talk on why you''ve applied for the job, what''s your knowledge of the company because most places are going to ask you that.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'You obviously don''t need to know everything.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'But if there''s a new product or initiative the company are introducing that would be a useful thing to talk about.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Or Amy mentioned values.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'This is usually a list of guidelines for how a company works and can often be found easily online.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'And they might be useful things to know and reference in your answers.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Of course, you might not be asked about this stuff.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'But you can still mention it.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'You can mention things in your answers.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'So you can say ''from my research I know this about the company and therefore I think I would be a good fit'' or things like that.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Usually, at the end of interviews, or certainly in the UK, you''ll be asked if you have any questions for the panel.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'And I sometimes panic about this because I don''t know what to ask.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Yeah and you do see some business coaches advising people to prepare a really intelligent question for this part.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'But Amy says people shouldn''t be too stressed about this.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'My advice is always don''t feel as though you have to ask a question.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'You''re not being marked at all on what questions you ask once the interview has finished.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'If you have a genuine interest in something, then feel free to ask it.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Equally, you know, if you''ve done your research on the company, you know, and there''s something that quite interests you, you could ask about it.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'So, Amy says the questions part at the end of the interview is a good time to use your research, if you haven''t already.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Or you can ask more about something your interviewer mentioned in the interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Maybe a new project that will be part of your role.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'You just want to show that you''re enthusiastic about the job and you''ll be a nice person to work with.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'Now, some interviews will be very formal, but others will be much more conversational, so it can be difficult to know what kind of language to use when you''re talking to interviewers, particularly at the end of the interview when the main questions are over.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Try and, yeah, read how the panellists are speaking to you.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'If they are being quite formal, then obviously you can keep your answers quite formal, but if they are being a bit more sort of personable and conversational than you can react to that and base your answers around it.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'I think it''s just about making sure, yeah, you''re sort of reading them on a human level and trying to connect to them whilst also getting across the important information you want them to know out of the interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'Picking up on how formal or informal a conversation is can be really difficult.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'So, if in doubt, I''d say always stay formal and always make sure you''re using polite, friendly language.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Now, as a top tip to remember, make sure you research the basics about the company and the team you''ll be working with.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'You want to have things to say if they ask and if they don''t ask you can show you''re excited by the role by mentioning them at the end of the interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Yeah, again, it''s all about your research and preparation for the interview.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'And if you missed our episode about preparing for your interview, you can listen back now on your podcast app or on our website: bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'That''s it for this episode of Learning English for Work.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'Next time, we''ll be talking about how to communicate with the company after your job interview and hopefully accept your offer.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Bye for now.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'Bye.', NULL, NULL, NULL, NULL);

DECLARE @CourseId8 BIGINT;
SELECT @CourseId8 = course_id FROM Courses
WHERE title = N'Technology and Digital Life'
  AND learning_mode = 'professional' AND course_type = 'curriculum';
IF @CourseId8 IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'Technology and Digital Life', N'Technology topics and useful digital vocabulary for modern work and life.', 'Intermediate', 'professional', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @CourseId8 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'Technology topics and useful digital vocabulary for modern work and life.', level = 'Intermediate',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @CourseId8;
END;

-- Free the managed lesson-order range before applying the current source ordering.
UPDATE Lessons
SET lesson_order = lesson_order + 10000
WHERE course_id = @CourseId8
  AND lesson_order < 10000
  AND EXISTS (
      SELECT 1 FROM Lesson_Material AS managed_material
      WHERE managed_material.lesson_id = Lessons.lesson_id
        AND managed_material.source_provider = 'BBC Learning English'
  );

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId8 AND title = N'Smartphone addiction';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId8, N'Smartphone addiction', N'BBC Learning English - Technology and Digital Life. Listening and shadowing practice from the original conversation.', 1, 379);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Technology and Digital Life. Listening and shadowing practice from the original conversation.', lesson_order = 1, duration = 379
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/technology/Smartphone-addiction/180712_6min_english_smartphone_addiction_download.mp3', 'BBC Learning English', N'Smartphone-addiction',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/technology/Smartphone-addiction/transcript.json', 'BBC Learning English', N'Smartphone-addiction',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello, welcome to 6 Minute English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Rob.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'And I''m Catherine.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'So, Catherine, how long do you spend on your smartphone?', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'My smartphone?', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Not that long really, only about 18 or 19 hours.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'No, sorry, I meant in a day, not in a week.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'Er, that''s what I meant too, Rob - a day.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'Oh wow, so you''ve even got it right here…', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'…yep, got it now, Rob.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Yes, I should tell you that I suffer from FOMO.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'FOMO?', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'FOMO - Fear of Missing Out.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'Something cool or interesting might be happening somewhere, Rob, and I want to be sure I catch it, so I have to keep checking my phone, to make sure, you know, I don''t miss out on anything.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'So we could call you a phubber… Hello… I said, so you''re a phubber?', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Someone who ignores other people because you''d rather look at your phone.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Oh, yeah, that''s right.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'It sounds like you have a bit of a problem there, Catherine.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'But you''re not the only one.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'According to one recent survey, half of teenagers in the USA feel like they are addicted to their mobile phones.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'If you are addicted to something, you have a physical or mental need to keep on doing it.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'You can''t stop doing it.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'You often hear about people being addicted to drugs or alcohol, but you can be addicted to other things too, like mobile phones.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'So, Catherine, do you think you''re addicted to your phone?', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'How long could you go without it?', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Catherine?', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Catherine!', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'Sorry, Rob, yes, well I think if I went more than a minute, I''d probably get sort of sweaty palms and I think I''d start feeling a bit panicky.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Oh dear!', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Well, if I can distract you for a few minutes, can we look at this topic in more detail please?', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'Let''s start with a quiz question first though.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'In what year did the term ''smartphone'' first appear in print?', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'Was it: a) 1995 b) 2000 c) 2005 What do you think?', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'OK, you''ve got my full attention now, Rob, and I think it''s 2000, but actually can I just have a quick look on my phone to check the answer?', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'No, no, that would be cheating - for you - maybe not for the listeners.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Spoilsport.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Right, Jean Twenge is a psychologist who has written about the damage she feels smartphones are doing to society.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'She has written that smartphones have probably led to an increase in mental health problems for teenagers.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'We''re going to hear from her now, speaking to the BBC.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'What does she say is one of the dangers of using our phones?', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Jean Twenge, psychologist and author I think everybody''s had that experience of reading their news feed too much, compulsively checking your phone if you''re waiting for a text or getting really into social media then kind of, looking up and realising that an hour has passed.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'So what danger does she mention?', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'Well, she said that we can get so involved in our phones that we don''t notice the time passing and when we finally look up, we realise that maybe an hour has gone.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'And I must say, I find that to be true for me, especially when I''m watching videos online.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'They pull you in with more and more videos and I''ve spent ages just getting lost in video after video.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Well that''s not a problem if you''re looking at our YouTube site, of course - there''s lots to see there.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'Yes, BBC Learning English, no problem.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'You can watch as many as you like.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Well, she talks about checking our phones compulsively.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'If you do something compulsively you can''t really control it - it''s a feature of being addicted to something, you feel you have to do it again and again.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Some tech companies, though, are now looking at building in timers to apps which will warn us when we have spent too long on them.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'Does Jean Twenge think this will be a good idea?', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Jean Twenge, psychologist and author It might mean that people look at social media less frequently and that they do what it really should be used for, which is to keep in touch with people but then put it away and go see some of those people in person or give them a phone call.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'So, does she think it''s a good idea?', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Well, she doesn''t say so directly, but we can guess from her answer that she does, because she says these timers will make people spend more time in face-to-face interaction, which a lot of people think would be a good thing.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'Yes, she said we should be using it for keeping in touch with people - which means contacting people, communicating with them and also encouraging us to do that communication in person.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'If you do something in person then you physically do it - you go somewhere yourself or see someone yourself, you don''t do it online or through your smartphone, which nicely brings us back to our quiz question.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'When was the term smartphone first used in print - 1995, 2000 or 2005?', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'What did you say, Catherine?', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'I think I said 2005, without looking it up on my phone, Rob!', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'That''s good to know, but maybe looking at your phone would have helped because the answer was 1995.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'But well done to anybody who did know that.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'Or well done to anyone who looked it up on their phone and got the right answer.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Mmm, right, before logging off let''s review today''s vocabulary.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'OK, we had FOMO, an acronym that means ''Fear of Missing Out''.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'Something that I get quite a lot.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'And that makes you also a phubber - people who ignore the real people around them because they are concentrating on their phones.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'Yes, I do think I''m probably addicted to my phone.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'I have a psychological and physical need to have it.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'My smartphone is my drug.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Wow, and you look at it compulsively.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'You can''t stop looking at it, you do it again and again, don''t you?', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'It''s sadly true, Rob.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'To keep in touch with someone is to contact them and share your news regularly.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'And if you do that yourself by actually meeting them, then you are doing it in person.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'And that brings us to the end of today''s programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'Don''t forget you can find us on the usual social media platforms - Facebook, Twitter, Instagram and YouTube - and on our website at bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'Bye for now.', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId8 AND title = N'Doomscrolling';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId8, N'Doomscrolling', N'BBC Learning English - Technology and Digital Life. Listening and shadowing practice from the original conversation.', 2, 154);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Technology and Digital Life. Listening and shadowing practice from the original conversation.', lesson_order = 2, duration = 154
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/technology/Doomscrolling/201026_tews_doomscrolling_download.mp3', 'BBC Learning English', N'Doomscrolling',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/technology/Doomscrolling/transcript.json', 'BBC Learning English', N'Doomscrolling',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to The English We Speak.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I''m Feifei… err Rob, could we have your attention please?', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'… oh sorry, Feifei, I was just looking at the news on my smartphone.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'Now is not the time to be looking at the news - we are presenting a programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'I know, but there''s so much news to look at and it''s all very very…', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'…depressing?', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Yes, there has been a lot of depressing news recently, but you seem addicted to it.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'Look at this!', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'Did you know we are all going to die… some day?', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Enough doomscrolling, Rob!', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'What''s that?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'''Doomscrolling'' describes continuously scrolling through endless bad news stories on your smartphone app, on social media or on the internet.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'It happens a lot during the coronavirus pandemic.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'And you''re obsessed, Rob - you just can''t stop reading information that depresses you.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'OK, OK - I''ll try to find some more positive news while we hear some examples…', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'I''ve been doomscrolling too much and read so much information about coronavirus that I can''t sleep at night.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Stop doomscrolling!', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'If you read too much bad news you''ll get depressed.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'My brother does too much doomsurfing - he loves to tell us the latest gloom and doom in the world, so we''ve stopped listening to him!', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'This is The English We Speak from BBC Learning English and we''re talking about doomscrolling - also called doomsurfing.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'That''s endlessly looking at depressing news stories on your smartphone app, on social media or the internet.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'I think it''s time we had some good news stories, Rob.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Yes - and I think I have one.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Look, a kitten that went missing has been found…', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Sweet!', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'… and look at this - new research says biscuits don''t make you fat… and this story…', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'OK, Rob.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'Is there a word for endlessly looking at good news stories?', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Joyscrolling?', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Happyscrolling?', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'Well, it''s good to see you smiling again.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Well we all need something to smile about after the events of this year.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'I agree.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Bye.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId8 AND title = N'Can AI have a mind of its own?';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId8, N'Can AI have a mind of its own?', N'BBC Learning English - Technology and Digital Life. Listening and shadowing practice from the original conversation.', 3, 380);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Technology and Digital Life. Listening and shadowing practice from the original conversation.', lesson_order = 3, duration = 380
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/technology/Can-AI-have-a mind-of-its-own/230126_6min_english_AI_conscious_download.mp3', 'BBC Learning English', N'Can-AI-have-a mind-of-its-own',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/technology/Can-AI-have-a mind-of-its-own/transcript.json', 'BBC Learning English', N'Can-AI-have-a mind-of-its-own',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'This is 6 Minute English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Sam.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'In the autumn of 2021, something strange happened at the Google headquarters in California''s Silicon Valley.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'A software engineer called, Blake Lemoine, was working on the artificial intelligence project, ''Language Models for Dialogue Applications'', or LaMDA for short.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'LaMDA is a chatbot - a computer programme designed to have conversations with humans over the internet.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'After months talking with LaMDA on topics ranging from movies to the meaning of life, Blake came to a surprising conclusion: the chatbot was an intelligent person with wishes and rights that should be respected.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'For Blake, LaMDA was a Google employee, not a machine.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'He also called it his ''friend''.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Google quickly reassigned Blake from the project, announcing that his ideas were not supported by the evidence.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'But what exactly was going on?', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'In this programme, we''ll be discussing whether artificial intelligence is capable of consciousness.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'We''ll hear from one expert who thinks AI is not as intelligent as we sometimes think, and as usual, we''ll be learning some new vocabulary as well.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'But before that, I have a question for you, Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'What happened to Blake Lemoine is strangely similar to the 2013 Hollywood movie, Her, starring Joaquin Phoenix as a lonely writer who talks with his computer, voiced by Scarlett Johansson.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'But what happens at the end of the movie?', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Is it: a) the computer comes to life? b) the computer dreams about the writer? or, c) the writer falls in love with the computer?', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'… c) the writer falls in love with the computer.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'OK, Neil, I''ll reveal the answer at the end of the programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Although Hollywood is full of movies about robots coming to life, Emily Bender, professor of linguistics and computing at the University of Washington, thinks AI isn''t that smart.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'She thinks the words we use to talk about technology, phrases like ''machine learning'', give a false impression about what computers can and can''t do.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Here is Professor Bender discussing another misleading phrase, ''speech recognition'', with BBC World Service programme, The Inquiry:', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'If you talk about ''automatic speech recognition'', the term ''recognition'' suggests that there''s something cognitive going on, where I think a better term would be automatic transcription.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'That just describes the input-output relation, and not any theory or wishful thinking about what the computer is doing to be able to achieve that.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Using words like ''recognition'' in relation to computers gives the idea that something cognitive is happening - something related to the mental processes of thinking, knowing, learning and understanding.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'But thinking and knowing are human, not machine, activities.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'Professor Benders says that talking about them in connection with computers is wishful thinking - something which is unlikely to happen.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'The problem with using words in this way is that it reinforces what Professor Bender calls, technical bias - the assumption that the computer is always right.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'When we encounter language that sounds natural, but is coming from a computer, humans can''t help but imagine a mind behind the language, even when there isn''t one.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'In other words, we anthropomorphise computers - we treat them as if they were human.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Here''s Professor Bender again, discussing this idea with Charmaine Cozier, presenter of BBC World Service''s, the Inquiry.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'So ''ism'' means system, ''anthro'' or ''anthropo'' means human, and ''morph'' means shape… And so this is a system that puts the shape of a human on something, and in this case the something is a computer.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'We anthropomorphise animals all the time, but we also anthropomorphise action figures, or dolls, or companies when we talk about companies having intentions and so on.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'We very much are in the habit of seeing ourselves in the world around us.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'And while we''re busy seeing ourselves by assigning human traits to things that are not, we risk being blindsided.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'The more fluent that text is, the more different topics it can converse on, the more chances there are to get taken in.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'If we treat computers as if they could think, we might get blindsided, or unpleasantly surprised.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'Artificial intelligence works by finding patterns in massive amounts of data, so it can seem like we''re talking with a human, instead of a machine doing data analysis.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'As a result, we get taken in - we''re tricked or deceived into thinking we''re dealing with a human, or with something intelligent.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Powerful AI can make machines appear conscious, but even tech giants like Google are years away from building computers that can dream or fall in love.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Speaking of which, Sam, what was the answer to your question?', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'I asked what happened in the 2013 movie, Her.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Neil thought that the main character falls in love with his computer, which was the correct answer!', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Right, it''s time to recap the vocabulary we''ve learned from this programme about AI, including chatbots - computer programmes designed to interact with humans over the internet.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'The adjective cognitive describes anything connected with the mental processes of knowing, learning and understanding.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Wishful thinking means thinking that something which is very unlikely to happen might happen one day in the future.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'To anthropomorphise an object means to treat it as if it were human, even though it''s not.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'When you''re blindsided, you''re surprised in a negative way.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'And finally, to get taken in by someone means to be deceived or tricked by them.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'My computer tells me that our six minutes are up!', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Join us again soon, for now it''s goodbye from us.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId8 AND title = N'Technology';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId8, N'Technology', N'BBC Learning English - Technology and Digital Life. Listening and shadowing practice from the original conversation.', 4, 287);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Technology and Digital Life. Listening and shadowing practice from the original conversation.', lesson_order = 4, duration = 287
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/technology/Talking-about-technology/RealEasyEnglish_s3e6_technology.mp3', 'BBC Learning English', N'Talking-about-technology',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/technology/Talking-about-technology/transcript.json', 'BBC Learning English', N'Talking-about-technology',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Hello and welcome to Real Easy English.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'In this podcast, we have real conversations in easy English to help you learn.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'And I''m Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Remember, you can read along with this podcast on our website: bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Hi, Neil.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'How are you?', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'Neil are you checking your phone?', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'We''re recording!', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Sorry, Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Yes, I''m just looking at my phone.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'I''m just checking the football scores.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'Finished now.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'OK, good.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Well, today we''re talking all about technology.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Yes, and technology is what we call new things that we use to make life easier like phones, computers and the internet.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'We''ll talk about new technology and how much we use it.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Great.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'Well, let''s get started.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Now, Neil, you just said, "I''m checking my phone".', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'That is the present continuous and you said it because you were checking the scores or checking your phone right then.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Right at that moment.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'But I have a question for you in the present simple, which is how often do you use technology?', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'So I use technology every day, many times every day.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'I check my phone a lot.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'I use a Bluetooth speaker.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'I sometimes play games with my son on his video console.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'So you use some technology.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'I do, yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'How about you, Beth?', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'I read quite a lot on a tablet, so I do read some books, but I tend to use technology to read.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'And I have been learning Spanish, or I''m learning Spanish at the moment using an app on my phone.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'So I''m using technology for that a little bit.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'And what''s your favourite piece of technology that makes your life easy?', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Well, I use a smoothie maker all the time.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'So, especially when I''m working from home, I chuck any old fruit and vegetables in there and make a smoothie.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'What about you?', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'So, I''ve got a bread maker.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Ooh, that''s cool.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Yeah and so I put all the ingredients together and in the morning we have lovely fresh bread.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'Nice.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'Would you say that you are tech-savvy?', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Are you good at using new technology?', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'No, not at all!', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'Eventually I learn the technology, but I never really get excited about new technology until lots of other people are already using it.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Yeah, I feel the same.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'I only really feel comfortable using technology when I have practised and got used to it.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'So, Beth, is there any new piece of technology that you would like to get that you don''t have at the moment?', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'Well, I used to have a really good camera, but it''s now about 10 years old.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'So, I think that technology has moved on quite a bit since then.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'So, I''d quite like to get a really good new camera.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'But the camera''s on phones are quite good these days, but it''s always nice to have something really good for when you go on holiday and that sort of thing.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'Let''s recap some of the language we heard during the conversation.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'We had technology, new scientific things that make our life easier, such as smartphones and computers.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Tech-savvy, good at using technology.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'We learned to use the present simple to talk about our technology habits, things we always do.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'For example, I use my phone way too much.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'And we learnt to use the present continuous to talk about things we''re doing at this moment in time.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'For example, Neil, you are still checking the football scores!', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'I am.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Sorry, Beth.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'But only because my home team is beating yours at football.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'Yeah, yeah, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'Whatever!', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'That''s it for this episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'If you want more easy English, try our video series, The London to Edinburgh Challenge.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'All episodes are available now on our website:', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Next time on Real Easy English, we''ll talk about our hobbies.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'Bye for now.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'Bye!', NULL, NULL, NULL, NULL);

DECLARE @CourseId9 BIGINT;
SELECT @CourseId9 = course_id FROM Courses
WHERE title = N'Office English - Upper Intermediate'
  AND learning_mode = 'professional' AND course_type = 'curriculum';
IF @CourseId9 IS NULL
BEGIN
    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)
    VALUES (N'Office English - Upper Intermediate', N'Advanced workplace communication for negotiation, deadlines, disagreement, and career growth.', 'Advanced', 'professional', 'curriculum', SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @CourseId9 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Courses
    SET [description] = N'Advanced workplace communication for negotiation, deadlines, disagreement, and career growth.', level = 'Advanced',
        updated_at = SYSUTCDATETIME()
    WHERE course_id = @CourseId9;
END;

-- Free the managed lesson-order range before applying the current source ordering.
UPDATE Lessons
SET lesson_order = lesson_order + 10000
WHERE course_id = @CourseId9
  AND lesson_order < 10000
  AND EXISTS (
      SELECT 1 FROM Lesson_Material AS managed_material
      WHERE managed_material.lesson_id = Lessons.lesson_id
        AND managed_material.source_provider = 'BBC Learning English'
  );

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId9 AND title = N'Negotiating';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId9, N'Negotiating', N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', 1, 589);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', lesson_order = 1, duration = 589
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/upper-intermediate-level/Negotiating/240318_OfficeEnglish_Negotiating.mp3', 'BBC Learning English', N'Negotiating',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/upper-intermediate-level/Negotiating/transcript.json', 'BBC Learning English', N'Negotiating',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Sometimes at work, we need to be able to negotiate.', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I guess the important thing is to be sure of what you actually want.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'So you don''t wanna come away feeling that you''ve negotiated badly.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'I find negotiating very awkward because my main instinct is to be polite and kind.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'So I find it quite difficult to be direct in what I want.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Today on Office English, we''re talking about the language of negotiating.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Hello and welcome to Office English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'And I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'And in this podcast we discuss the business English that you can use to do well at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Today, we''re talking about negotiations.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'This means discussions which we use to get what we want.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'So, for example, if we wanted to buy a car, we might negotiate with the sales person to get the best price.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'OK, so do you ever negotiate at work, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Sometimes, yes, particularly when there''s someone who we might need to do some work for us and we have to make sure that we get a good price for the department.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Yeah, so we tend to negotiate at work even if it''s not a big part of our job.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'So, if you''re a salesperson or you''re dealing with customers a lot, you might have to negotiate all the time at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'But even if you don''t, you might need to negotiate now and then when you''re asking for a price for something.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Or even in your own role, so if you''re asking for something from your boss or if you''re starting a new job, you need to talk about how much you''re going to get paid, what your hours would be, you''d been negotiating with them about that.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'And the way that people negotiate, especially in business deals between different companies, differs around the world and depending on the situation.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'But today, we''ll talk about some phrases for negotiating that are familiar in the UK context.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'So, first up, how do we start a negotiation, Pippa?', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'So, in Britain, there''s usually some politeness or small talk, and we talked about small talk in a previous episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'But once you''ve kind of said hello, had a chat with the person, then you might say something like Right! let''s talk about the price or let''s get down to business is a nice phrase.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'It''s a nice focusing expression that isn''t it Right! sort of you''re saying, now we''re getting started.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Now we ''re doing what we really mean to do.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Yeah, and in some contexts you wouldn''t need to have the chit chat part at the beginning.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'You might just go straight in and say ''OK, we''re talking about this product and we need to talk about the price of it'' and you don''t need to have the small talk.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'But in the UK we tend to do that.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'And then probably what you want to do is make your opening offer.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'So you might say I''m looking for... ten pounds for this or I think my work is worth... four hundred pounds, for instance.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Yes and of course, because it''s a negotiation, you''re probably going to ask for more than what you''d actually accept.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'That''s a tactic that people often use in negotiations.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'They start with a higher price in the hope that they might get more than they... than they wanted and then there''s sort of a a lower limit that they''re willing to take for something.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'You could open a negotiation by asking the other person for their first offer.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'So that sort of changes the dynamics a little bit.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'So you could say What sort of price would you be willing to pay for this?', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'OK, so we''ve started the negotiation.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'But how do we try and persuade the other person to give us what we want?', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Are there any phrases that we can use, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Yes, well this is all about something we call haggling, which is basically arguing, but professionally and persistently, about the price of something.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'So you might say what your first offer is and someone will say, ''No, no, no, that''s too expensive, but we can pay this''.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'And you go ''Ooh, no, no, no, that''s too low, but I might accept this''.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'And you go backwards and forwards until you get to the right price.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'So it''s about a compromise between the two and there''s often a lot of different tactics that people use.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Now, I''m not a very good negotiator so I wouldn''t be very good at the haggling part of things, I usually just kind of accept what someone offers because I''m scared.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'But yeah, people have different ways of trying to persuade the other person to kind of meet their price rather than dropping the price.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'Yeah so, if you want somebody to increase the amount they''ll pay you for something, then you could say something like we''ve got to cover our costs and cover your costs means that you need to earn enough to pay for what it would cost to do something.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Yeah and this is good because you''re sort of saying we have to be realistic, we''d love to give it to you for less money, but we''ve got to cover our costs.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'And if we''re buying something from somebody else, and we wanted them to lower the price that they were asking what could we say then?', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Yeah, I mean, you could say something like Oh, I''d love to offer that.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'But we have to be realistic about our budget.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'And this is another one where you kind of maybe making it slightly less personal, you''re saying, ''Oh, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'That''s fine.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'That''d be great, but we don''t have that money.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'And we need to think about this.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'So, while I want to pay you more, I can''t, it''s not down to me.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'It''s just the situation''.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Yes, you''re using ''we'' so you''re sort of negotiating on behalf of the company rather than on behalf of yourself.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'And both of these phrases we''ve got to cover our costs and we have to be realistic about the budget are still kind of friendly.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'So it''s not actually an argument when you''re negotiating, it''s more of a discussion.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Ok and we''ve talked a lot about negotiating with someone from another company, but actually sometimes you have to negotiate with people in your company and in fact, sometimes you have to negotiate with your boss, particularly about how much you get paid.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'So you might want to use phrases like Well, other people in my position earn... this much or I''ve taken on lots of responsibility without more pay.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'Yes, and it''s probably useful to say that it will depend on your company as to whether there is an opportunity to talk about your pay and to negotiate it.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'Some companies don''t like that, some do, it really depends.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'But yes, giving evidence for why you want more money would be a useful thing to do.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'Right, so we have some ways to try and persuade the other person in a negotiation.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Let''s imagine that after haggling for a while, we''re ready to accept the price or offer.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'How do we end a negotiation?', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'Well, we could be quite informal and we could say OK, we can go with five pounds.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'So we can go with means we''ll accept that amount.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Or you could be more formal, you could say something like I''m happy to accept five pounds, thank you very much.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'And then one thing that I think is useful is to try and sort of maintain the business relationship.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'So you''ve not just gone there to get the best price possible, but you also want to kind of continue a working relationship with the person.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'So you could say something like I look forward to working with you or it was great doing business with you.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'What do you think about that, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'Yes, I think this is quite nice because negotiations can sometimes get a little bit tense.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'So it''s quite good to bring everything back to a kind of friendly tone at the end.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'Negotiations can be difficult, especially if we''re not used to persuading other people to do something.', NULL, NULL, NULL, NULL),
    (@LessonId, 82, N'Let''s hear again from our BBC Learning English colleagues.', NULL, NULL, NULL, NULL),
    (@LessonId, 83, N'I guess the important thing is to be sure of what you actually want.', NULL, NULL, NULL, NULL),
    (@LessonId, 84, N'So you don''t want to come away feeling that you''ve negotiated badly.', NULL, NULL, NULL, NULL),
    (@LessonId, 85, N'I find negotiating very awkward because my main instinct is to be polite and kind.', NULL, NULL, NULL, NULL),
    (@LessonId, 86, N'So I find it quite difficult to be direct in what I want.', NULL, NULL, NULL, NULL),
    (@LessonId, 87, N'Yeah, I think it''s different, isn''t it.', NULL, NULL, NULL, NULL),
    (@LessonId, 88, N'I mean, if you do it all the time as part of your job, if you''re a salesperson or you''re involved in things like that then I guess it''s a lot easier, you get used to it.', NULL, NULL, NULL, NULL),
    (@LessonId, 89, N'But in a lot of jobs we don''t do a lot of negotiating.', NULL, NULL, NULL, NULL),
    (@LessonId, 90, N'Yeah and that''s why I think we''re nervous to do it.', NULL, NULL, NULL, NULL),
    (@LessonId, 91, N'But as we said, if you try and remain friendly and try and kind of make the conversation less of an argument and more of a discussion then that''s a good way to kind of try and get your, your opinion across.', NULL, NULL, NULL, NULL),
    (@LessonId, 92, N'And one thing that is really important that we haven''t talked about is it''s good to be clear with yourself about what you want before you start the negotiation, what you would be willing to accept as a high or low point.', NULL, NULL, NULL, NULL),
    (@LessonId, 93, N'Otherwise, you could get carried away and pay far too much or accept far too little for something.', NULL, NULL, NULL, NULL),
    (@LessonId, 94, N'So yeah, think before about what are you wanting to get out of a negotiation, so that you don''t kind of go in and end up with something you didn''t want.', NULL, NULL, NULL, NULL),
    (@LessonId, 95, N'That''s it for this episode of Office English.', NULL, NULL, NULL, NULL),
    (@LessonId, 96, N'Remember, you can find courses and activities to help you with your English at work on our website,', NULL, NULL, NULL, NULL),
    (@LessonId, 97, N'Next time, we''re talking about how to talk about your achievements at work and sell yourself.', NULL, NULL, NULL, NULL),
    (@LessonId, 98, N'See you then!', NULL, NULL, NULL, NULL),
    (@LessonId, 99, N'Bye!', NULL, NULL, NULL, NULL),
    (@LessonId, 100, N'Bye!', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId9 AND title = N'Deadlines and logistics';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId9, N'Deadlines and logistics', N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', 2, 486);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', lesson_order = 2, duration = 486
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/upper-intermediate-level/Deadlines-and-logistics/250526_OfficeEnglish_deadlines_and_logistics_download.mp3', 'BBC Learning English', N'Deadlines-and-logistics',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/upper-intermediate-level/Deadlines-and-logistics/transcript.json', 'BBC Learning English', N'Deadlines-and-logistics',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Do you find it hard to stay organised at work?', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I love a to-do list, and then I tick off when I''ve done that task and I get a great feeling of satisfaction.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I''m not as organised as I should be about it.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'I think probably at first I think it''s too much.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'And then I think about it and I realise it''s OK, and then I don''t do the planning I should have done in the first place.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'In this episode of Office English, we''ll be talking about deadlines and logistics.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Hello and welcome to Office English, your podcast guide to the world of work.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'And I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'Visit our website to find a transcript of this episode to read along, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'We heard from our colleagues Neil and Beth at the start of the episode about some of the ways they try to stay organised at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Are you an organised person, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'We''ve just been talking about this, haven''t we?', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'I''m organised enough for myself.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'But I''m not always organised enough to work well with other people.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'OK, so it''s all in your head and you haven''t told anyone else what''s going on?', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Is that what you''re saying?', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Basically, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'I think I''m similar.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'I''ve found recently, I don''t know if it''s an age thing, I''m still quite young, but I have to write things down a lot more.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'I have to have lists.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Otherwise there''s too much going on and I forget something.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'So, I like to try and stay organised.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'I don''t like to feel panicked at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Today we''re going to talk about some of the language of organisation at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'So, Phil, let''s start with deadlines.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Sometimes at work you rely on the work of other people, and you need to set clear deadlines to make sure that you can deliver everything on time.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'How can you make deadlines clear at work?', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Yeah, you can say stuff like, ''if we''re going to meet that client''s deadline, I need this work from you by the end of the day tomorrow'' or something like that.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Yeah, I might then check that deadline with them so I could say something like, ''does that sound feasible?'' That means does that sound doable to you?', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'You want to make sure the deadline you''re giving to someone is something that they can actually realistically do.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'A really useful expression here is you can say that there is a ''firm deadline'' for something or a ''hard deadline'', and what that means is we can''t move it.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'If we miss the deadline, it''s not going to work.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'Another way I might communicate it is I might just talk someone through my process.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'I might say ''I''m mapping out our plan for this project.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'If you could finish your part by this date, that would be great''.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'It might be a good idea to look at the deadline and then work backwards and think of when the other stages need to be done by.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'Yeah, and you definitely want to add in contingency.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'And that means a little bit of time in case something goes wrong.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'And I especially like to do this when you''re working with someone who you think might miss the deadline that you give them.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'So, you''re just allowing for problems, or just for someone to be ill or unable to do the work for another reason.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'I have a golden rule with deadlines.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'What''s that?', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Never tell anyone your real deadlines.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'Yes, that''s something a lot of people do.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'They''ll give them a fake deadline that''s maybe a week earlier than the real deadline.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Yeah, so that''s why I would always sort of start with the real deadline and then map backwards.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'So, I''m giving myself enough time based off what I''m telling other people.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Sometimes at work, you''re given a deadline.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'In other situations, you''re asked to give an idea of how long something will take and set your own deadlines.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'What do we need to consider here?', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'Well, Phil, the temptation in this situation is to overpromise, so to say, oh, yeah, I can do that really easily.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'But it''s better to give yourself more time, I guess, and deliver something early, than have to move a deadline because you were too optimistic about how much time you had.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'What''s that saying?', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'It''s best to under promise and overdeliver.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'Exactly.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'And it can be stressful when you''re not sure what an acceptable answer is, for how long something will take.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'And a lot of us actually have time blindness and really struggle to judge how long things will take.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'So you should probably think about how you can discuss it with the person who''s asked you and what language you can use.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'Yeah, so you could say ''if we delivered the report by next week, does that sound reasonable?'' And you might want to check your ideas with your colleagues to see if it actually sounds realistic.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Another way you could phrase it is saying something like ''realistically, we need a while to do this properly.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'I would suggest...'' and then give your suggestion.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'''Does that fit in with your timeline?'' So you''re showing that you''ve thought about how long you think it will take, but you want to check still that it fits in with the expectations because yeah, it''s really hard when someone asks you because you don''t know what their kind of ballpark figure was for something - and a ballpark figure is just sort of a rough, a rough estimation of what the time would be.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'Another thing that might be good to check here, we talked earlier about hard deadlines, is like, do we have a hard deadline on this?', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'It''s trying to find out if there is flexibility or if actually, no, it has to be done by then and that''s it.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'And if someone''s asked you to estimate how long it will take to do something and there could be several answers, it depends how much detail you go into, for example.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'So, you could just throw the question back to them and say ''when would you like it finished ideally?'' and then kind of see if you''ve got similar ideas in mind.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'So, we''ve talked a lot about schedules and deadlines.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Another aspect of work organisation is something called logistics.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'This is what we call the coordination of lots of different things to make sure something runs smoothly.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'And this can mean that you need to be in communication with lots of people at the same time, and update them about changing details.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'How can you do this professionally and without annoying someone?', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'Well, I would always keep my tone friendly, but be clear.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'So, one thing you can do if you''re emailing people but you''re not sure about a day is ask, ''can I pencil in this date and come back to confirm later?'' So if you pencil something in, it means it''s not absolutely certain, but you''ve sort of maybe left them a reminder so they can keep the date free, for example.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'Yeah, you could always say ''I''ll follow up with more details closer to the time''.', NULL, NULL, NULL, NULL),
    (@LessonId, 82, N'Sometimes as well, you''ll have to move things around and you don''t want to annoy someone doing this, so I would always apologise.', NULL, NULL, NULL, NULL),
    (@LessonId, 83, N'Just say, ''I''m so sorry.', NULL, NULL, NULL, NULL),
    (@LessonId, 84, N'Is it possible to move the appointment?', NULL, NULL, NULL, NULL),
    (@LessonId, 85, N'We''ve had some unavoidable delays''.', NULL, NULL, NULL, NULL),
    (@LessonId, 86, N'So you want to kind of, you know, show that you''re being considerate of someone else''s time.', NULL, NULL, NULL, NULL),
    (@LessonId, 87, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 88, N'And when you''re communicating that, make sure that you''re clear with your email subjects and any calendar invites that you''ve sent.', NULL, NULL, NULL, NULL),
    (@LessonId, 89, N'Always reply to confirm so that there''s, there''s no confusion.', NULL, NULL, NULL, NULL),
    (@LessonId, 90, N'It''s kind of, you''re closing the loop of the communication.', NULL, NULL, NULL, NULL),
    (@LessonId, 91, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 92, N'So if you ask someone, can they do a date and they come back and say yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 93, N'Then you email them back and say, ''that''s great, I''ll book in this date''.', NULL, NULL, NULL, NULL),
    (@LessonId, 94, N'So everybody is sure that it''s definitely happening.', NULL, NULL, NULL, NULL),
    (@LessonId, 95, N'That''s it for this episode of Office English.', NULL, NULL, NULL, NULL),
    (@LessonId, 96, N'What parts of work do you find difficult?', NULL, NULL, NULL, NULL),
    (@LessonId, 97, N'Send us an email to learningenglish@bbc.co.uk.', NULL, NULL, NULL, NULL),
    (@LessonId, 98, N'And next time we''ll talk about managing clients at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 99, N'See you then.', NULL, NULL, NULL, NULL),
    (@LessonId, 100, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId9 AND title = N'Disagreements';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId9, N'Disagreements', N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', 3, 354);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', lesson_order = 3, duration = 354
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/upper-intermediate-level/Disagreements/260316_OfficeEnglish_disagreements_download.mp3', 'BBC Learning English', N'Disagreements',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/upper-intermediate-level/Disagreements/transcript.json', 'BBC Learning English', N'Disagreements',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'What do you do when there''s a disagreement at work?', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I don''t mind it too much when people disagree.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I think it can be really healthy, and especially if everyone feels able to disagree in a kind of calm way.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'When people get angry, that''s when it gets a bit a bit stressful.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'I don''t enjoy confrontation anyway.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Having said that, I do think it''s important to listen to different perspectives.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Today on Office English, we''ll talk about agreeing, disagreeing, and compromising.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'Hello and welcome to Office English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'And I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'This is your podcast guide to the world of work.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'And you can find subtitles and a transcript to read along with this podcast on our website bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'So, Pippa, we''ve just heard some of our Learning English colleagues talking about keeping people happy at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'We often call this being diplomatic.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'Yes, being diplomatic means acting in a way that doesn''t upset or offend someone.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Yes, it can be difficult to keep everyone happy all the time at work, but there are some steps that you can take to make sure you''re not being rude and to help disagreements be resolved more easily.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'And it''s important to note that it will depend on your company culture and the country that you work in, how diplomatic people are.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'There are some workplaces where people will just say what they think.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'They won''t kind of try to, um, not offend people with their opinion.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'But it''s very common in UK workplaces and in lots of international companies that you need to be diplomatic.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'So in this episode, we''ll talk about understanding disagreements, taking a side and compromising.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'OK, so let''s imagine you''re at work and there''s a disagreement.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Two colleagues have different ideas about how to do a task and want your opinion.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'What should you do, Pippa?', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Well, you need to understand the disagreement before you wade in, and wade in just means to get involved quite quickly.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Um, so you need to ask about, kind of, what the context is of the thing that you''re being asked to give an opinion on.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'A nice expression to do this could be, ''could you fill me in on what the problem is?'' And fill me in means give me the details.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Mhm.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'And you can sort of explain why you want to know this.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'''I want to be sure I understand both sides'' would be a nice expression.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'It''s just showing respect to both perspectives and making sure that you don''t make a very quick decision if this is quite a big disagreement.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'And then another question that could be important to ask is, is my opinion going to be helpful?', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'Is it something you can actually help with, or is it just going to make things even more complicated if you wade in with your opinion?', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'OK, so now we understand the disagreement, we need to decide which idea is best.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'How do we do this diplomatically without upsetting anyone, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'It is good to show that you understand both sides, both perspectives.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'So maybe start off by saying something like, ''I can see why you want to do it this way.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'But in this case I agree with...'' the other side.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Another way you could approach this is if there''s maybe a complex problem where actually both options might work, there''s not a particular right or wrong, you can say that.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'You can say, ''I''m not sure there are any right answers, but I''m going to go with this option because...'' and then state your reasons.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'So you''re just acknowledging that it''s not actually that the other person is wrong.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Sometimes you just need to make a decision.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'So you might say ''in this instance I''m siding with...'' this person.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'''But it''s not that I don''t respect your opinion''.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'Um, so you''re showing that you are thinking about what the other person has said, and you''re not just, it''s not just you don''t like them or something like that.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'Mhm.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'You don''t want it to be personal really.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'So you want to show that in the way that you''re talking to people.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'So we''ve covered how to disagree politely.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'But another option is to reach a compromise.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'And this is where both people change their opinion or demands in order to agree.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'So how can we talk about compromise at work?', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'Well we have lots of really nice expressions for this in English.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'So we could ask, ''can you meet me halfway?'' And that just means can you compromise with me?', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'You might want to try and find common ground.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'And common ground are the things that you agree on.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Mhm.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'A similar expression is a happy medium.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'And this just means to reach a point where everybody is happy.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'So you have a solution that makes both sides happy, that''s in the middle.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'It''s a happy medium.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'Now in order to reach a compromise it''s often good to use slightly more indirect language or softer language.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'So things like possibly or maybe, could you, would you, modal verbs like that, which, they just make it a bit less confrontational.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'That''s it for this episode of Office English.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'Find more business English programmes on our website, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'Now, do you want to improve your speaking confidence at work?', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'Make sure you try our Beating Speaking Anxiety series with videos and podcasts to help you fight your fears of speaking English, and it''s all available at bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'Until next week, goodbye.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId9 AND title = N'Describing your job';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId9, N'Describing your job', N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', 4, 407);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', lesson_order = 4, duration = 407
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/upper-intermediate-level/Describing-you- job/260330_OfficeEnglish_describing_your_job_download.mp3', 'BBC Learning English', N'Describing-you- job',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/upper-intermediate-level/Describing-you- job/transcript.json', 'BBC Learning English', N'Describing-you- job',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'How would you describe your job?', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'It''s really difficult to explain what my job is to people.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'I think my parents still, after many years, don''t really understand what I do.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'When I have to describe what a friend does to another friend, sometimes I feel like I sound really stupid because I''m, I don''t exactly know how to explain it.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'Today on Office English, we''re talking about how to make what you do clear to other people.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Hello and welcome to Office English, your podcast guide to the world of work.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'And I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'You can find a transcript for this episode to read along while you listen at bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'So, Pippa, we heard from some colleagues at the start of the programme about their experience trying to tell other people what they do.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Do you ever have difficulty understanding people''s job titles and descriptions?', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Yes, I do have difficulty with that sometimes, and there''s often a lot of jargon or generic words used for people''s job titles.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'So some examples might be producer, project manager, consultant.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'And that could mean a lot of different things.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'So I find that a bit confusing sometimes.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'I''ll tell you what, I actually find it quite difficult to explain my own job to people who don''t, who don''t work in the same industry.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'The job title itself says very little, so I tend to find I just describe the things I do every day and hope that people work it out from there.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Hmm.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'And we can also find it difficult to see similarities with other jobs, other industries and recognise the skills that we use.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'So in this episode, we''re going to talk about describing your job to people within your organisation, outside your organisation, and also how to describe your job when you want to apply for a new job or change career.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'OK, Pippa, let''s start by talking about describing your job to people within your company.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Um, what do you need to consider when you do this?', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'Well, think about what you do rather than just your job title.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'For example, you could say, ''I look after sales'', for example, or ''I handle client complaints'', and those two phrases just basically mean I''m responsible for, this is the area that I work in.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'And it can be helpful to give some context and talk about how your job relates to other people and other departments in your organisation.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'Sometimes we say where you sit within the organisation.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'So you might say, ''I work a lot with'' a particular person or a particular department.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'And then you also might say you report to and you''re talking about your manager, because it may be that people know the same manager.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'And so that helps them understand what you''re working on.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'Another thing that might be useful to do, for example, in a meeting, is to tell people when they should contact you or in what situations they might encounter you at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'So you could say something like, ''if you ever have any questions about this element, you should email me''.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'And then that makes it really clear when the people that you''re meeting are going to actually interact with you at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'Of course, organisations have their own internal jargon, the language they use within the organisation.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'But of course people who are new to your organisation might not know that jargon yet.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Yeah, and sometimes people who''ve been there a long time, still don''t really understand it.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'They just use it every day.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'So, we''ve talked about how to describe your job to people you work with.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'But what about people outside of your workplace, perhaps friends and family or maybe clients and customers?', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Do you need to approach this differently, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'I think it does depend on the context.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'So if you''re, if you''re meeting someone as part of your job, maybe you''re at a conference or you''re meeting a client, you''ve got to keep things formal and professional.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'So you might say, ''I represent'' and then the name of your company, and you''d probably say something like, ''I''m responsible for'' and then say what you''re responsible for.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'Um, but yeah, you probably keep it on quite a formal level there, which would be different if you''re talking to your friends, wouldn''t it?', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'If you were talking to your friends, you might just talk more generally.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'You might say ''I mostly work on'' or you might find a relatable element.', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'So you could say something like, oh, ''do you know this product or this company?', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'My work is similar to that''.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Now, one common time we need to talk about our current job is when we apply for a new one.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'Um, it can be difficult to clearly explain your jobs and skills, and especially if you''re planning a career change, which means to work in a different area to the one that you''ve worked in before.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'So in this situation, you want to say what you do and then give more detail.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'For example, you could say ''I have a background in'' a particular area, ''which in practice means'' and that phrase in practice means allows you to introduce what you do day to day, what your job actually involves.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'And a really useful expression we have here is transferable skills.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'And what we mean by that are the skills that you have, that you use in your current job, or a job you''ve done in the past, which you can transfer to a different job and you can use them.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'So you might say something like, ''I have a lot of experience with sales.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'I''m very good at persuading clients, so I think I''d be useful in this role''.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'That''s an example of a transferable skill.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'Mhm.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'And it might be good to in your application for a job for example, talk about why you''d like to change.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'So acknowledge that you''ve worked in a different area and that you''re maybe changing to a slightly different one.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'So you could say something like ''I''d like to apply my skills to a new challenge'', and then you''re not just trying to hide that you haven''t worked in this area before.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'That''s it for this episode of Office English.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'But if you''d like more tips for applying for jobs in English, try the Job Applications series on our website.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'There''s a link in the notes below this episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'We''ll be back next week with another episode to help you with your business.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'English.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'Bye for now.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId9 AND title = N'Extra work';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId9, N'Extra work', N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', 5, 557);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', lesson_order = 5, duration = 557
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/upper-intermediate-level/Extra-work/260427_OfficeEnglish_extra_work_download.mp3', 'BBC Learning English', N'Extra-work',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/upper-intermediate-level/Extra-work/transcript.json', 'BBC Learning English', N'Extra-work',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Do you go above and beyond at work?', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I think you have to be careful that you don''t end up spending a lot of your own free time working on something that you''re not supposed to be working on, or that you''re not, in fact, being paid for.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'If it''s something that you actually really enjoy, then I think it''s totally OK to do things outside of your job description, so long as you''re looking after yourself and you''re not putting too much pressure on yourself to get it done.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'Because at the end of the day, you''re not being paid for it.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'In today''s episode of Office English, we''re talking about doing work outside your job description.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Hello and welcome to Office English, your podcast guide to the world of work.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'And I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'On our website we have subtitles and a transcript so you can read along with this podcast, head to bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'So Pippa, we''re talking about what work you do and how much you do, and this can be a difficult subject at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'So let''s start by looking at some common terms we hear when we talk about this.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'So we''ve got our job description.', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'And that''s what we''re expected to do every day, day to day.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'So your usual tasks, your usual role, what is expected of you at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'Yeah, we have this expression which we started the programme with today: going above and beyond.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'And that means putting in extra effort or extra hours on something that you''re doing at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Mhm.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'And then we also have this idea of taking initiative.', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'And if you take initiative that means you do things without being asked to.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'And we''ll talk later in the programme about whether you should take initiative at work or when you can.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'Um, and in this episode we''ll discuss language and approaches to how much work you do.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'Let''s start with a scenario.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'There''s a project coming up at work that you''re really passionate about.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'You''d like to work on the project, but it will involve doing things that aren''t technically part of your job and possibly some extra work.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'How would you approach this?', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'Well, it will depend on your workplace and your manager.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'In some workplaces, managers like you to kind of be passionate.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'They like you to ask to work on things.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'They would be quite excited if you had a request like this.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'In other places it might be a bit more hierarchical or structured and your job is very set.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'You have to do certain things.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'There''s not spare time or flexibility for you to do other things.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'So, that will be kind of something you have to think about.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'But generally it''s a good thing to be passionate about work.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'And you could demonstrate this if you want to talk to your line manager about working on a project, you could say something like, ''I think I can bring a lot to this project, and I''d like to take it on alongside my day-to-day work''.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'So if you can bring a lot to something, that means you have lots of skills that could be useful.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'So you could say something like, ''this would be a good chance for me to stretch and challenge myself and learn new skills''.', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'We use that expression stretch and challenge to talk about doing something, doing something difficult, but often with the idea that you''re going to make yourself better by doing something difficult.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'And this might be a scenario where you want to go above and beyond.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'Or a similar phrase, go the extra mile.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'That means just doing a bit more than your normal day-to-day job.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'And you could use those phrases when you''re talking to your line manager.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'OK, let''s think about another situation.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'You go to a networking event outside of work, and you''re inspired to start some new relationships and explore some collaborations with other people in the industry.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'Should you take the initiative, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'I mean, that is an interesting question because it really will depend on your workplace, your manager, the kind of culture that you have, because it can be good to show initiative, to take things forward if that''s what you''re expected to do.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'Um, but it is possible that you might go too far.', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Um, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'So, you don''t want to overstep.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'And overstepping means basically doing things that you shouldn''t do and taking on responsibilities that you shouldn''t do.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'So perhaps your manager doesn''t want you to start new relationships with other people in the industry and organise collaborations.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'They want you to just focus on your job.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'And also you don''t want to spend time working on something that isn''t a priority for your team, and then take that time away from your actual job, from the things that you need to do.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'Yeah, there''s a nice expression here.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'You can talk about looking at the big picture, which is how everything fits together.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'And so it might be that when you look at the big picture, that actually that collaboration that you thought was a really good idea around one small thing just doesn''t fit in to like the bigger, the bigger picture.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'So if you''re talking to people outside your work, you want to be careful about how much you share first of all.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'So if you are taking initiative and you don''t really know how much permission you have to start these relationships, don''t share lots of company secrets would probably be good advice, but also don''t overpromise.', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'Um, so don''t kind of tell people that you absolutely can collaborate with them, you absolutely can take this idea forward, if you don''t know that that''s true.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'It could be that you need to say something when you''re talking to people: ''Yeah, this is a really exciting opportunity, but I need to check that this aligns with our priorities'', um, that this fits with what we''re doing.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'Another way to phrase that would be to say something like, ''it would be great to work together - I''ll feed back everything we''ve discussed to my team to see what''s possible''.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'And this idea of feeding back, that means you''ll tell your team and your manager what you''ve been talking about, and it just shows the other person that you don''t really have the authority to make a decision right there and then.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'Yeah, and then, of course, when you go back and you talk to your team or your manager, you might want to say something like, yes, ''I took the opportunity to speak with some colleagues across the industry.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'I think there''s some potential to work with others''.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'You could also say something like, ''should we take this forward?'' or ''should we pursue this?'' And both of these mean, should we do this?', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Should we explore the possibility of it?', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'OK.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'We''ve talked about when you want to do things outside your job description.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'But let''s talk about a different scenario.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'What do you do if you''re being asked to do things outside your job description all the time at work?', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'This is a really difficult situation and it can be difficult to have these conversations at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'So this is where you''re working extra hours all the time.', NULL, NULL, NULL, NULL),
    (@LessonId, 78, N'You''re doing lots of tasks that are actually maybe above your pay grade, we might say, which means that they''re taking on responsibility that is not normally done by somebody who''s paid what you are, who has your job.', NULL, NULL, NULL, NULL),
    (@LessonId, 79, N'And depending on your workplace, a conversation with your line manager is probably the best way to deal with this.', NULL, NULL, NULL, NULL),
    (@LessonId, 80, N'So you could say something like, ''I''ve recently been taking on extra responsibilities outside of my job description.', NULL, NULL, NULL, NULL),
    (@LessonId, 81, N'Will this become a permanent part of my role?'' And that question is basically asking, are we able to make this more official?', NULL, NULL, NULL, NULL),
    (@LessonId, 82, N'I''ve been doing these things sort of unofficially in addition to my normal job.', NULL, NULL, NULL, NULL),
    (@LessonId, 83, N'And you''re kind of saying, I want to be recognised for doing that.', NULL, NULL, NULL, NULL),
    (@LessonId, 84, N'Yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 85, N'Or you might say, ''look, my role is supposed to be...'' this, ''but I''m increasingly spending lots of time on...'' something else, the other task that you''re doing, ''is there something we can do to make sure that I have time for my day job?'' And when we say day job, we mean what your normal job description is.', NULL, NULL, NULL, NULL),
    (@LessonId, 86, N'Another way of phrasing this: you could say ''requests to work on other projects are impacting my ability to get my job done.', NULL, NULL, NULL, NULL),
    (@LessonId, 87, N'Can we find a solution?'' That''s a slightly stronger way of putting it because you''re basically saying, I can''t get all the work done, and your line manager then can hopefully take some action to make sure that you can get your work done.', NULL, NULL, NULL, NULL),
    (@LessonId, 88, N'Yes, of course, sometimes it might be that you''re in a situation where you are just being asked to do too much, and perhaps your manager doesn''t see the issue with it, and then you might need to question it and say whether, whether something is acceptable or not acceptable in the situation you''re in.', NULL, NULL, NULL, NULL),
    (@LessonId, 89, N'But that will depend a lot about the industry you''re working in and the culture of the company that you''re in.', NULL, NULL, NULL, NULL),
    (@LessonId, 90, N'That''s it for this episode of Office English.', NULL, NULL, NULL, NULL),
    (@LessonId, 91, N'We''ll be back soon with another episode.', NULL, NULL, NULL, NULL),
    (@LessonId, 92, N'In the meantime, practise your English skills on our website.', NULL, NULL, NULL, NULL),
    (@LessonId, 93, N'Why not try The Reading Room where you can read graded articles on interesting topics and test your understanding?', NULL, NULL, NULL, NULL),
    (@LessonId, 94, N'Find a link in the notes below this programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 95, N'Bye for now.', NULL, NULL, NULL, NULL),
    (@LessonId, 96, N'Bye.', NULL, NULL, NULL, NULL);

SET @LessonId = NULL;
SELECT @LessonId = lesson_id FROM Lessons
WHERE course_id = @CourseId9 AND title = N'Career development';
IF @LessonId IS NULL
BEGIN
    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)
    VALUES (@CourseId9, N'Career development', N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', 6, 458);
    SET @LessonId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Lessons
    SET [description] = N'BBC Learning English - Office English. Listening and shadowing practice from the original conversation.', lesson_order = 6, duration = 458
    WHERE lesson_id = @LessonId;
END;

DELETE FROM Lesson_Material
WHERE lesson_id = @LessonId AND material_type IN ('audio', 'transcript');
INSERT INTO Lesson_Material
    (lesson_id, material_type, content_url, source_provider, source_id, license_note,
     source_review_status, source_reviewed_at)
VALUES
    (@LessonId, 'audio', N'/media/curriculum/cong-viec/upper-intermediate-level/Career-developmen/OE_career_development_download.mp3', 'BBC Learning English', N'Career-developmen',
     N'User-provided educational media; verify distribution rights before publishing.',
     'pending', NULL),
    (@LessonId, 'transcript', N'/media/curriculum/cong-viec/upper-intermediate-level/Career-developmen/transcript.json', 'BBC Learning English', N'Career-developmen',
     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);

DELETE FROM Lesson_Sentences WHERE lesson_id = @LessonId;
INSERT INTO Lesson_Sentences
    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)
VALUES
    (@LessonId, 1, N'Do you have goals for your career?', NULL, NULL, NULL, NULL),
    (@LessonId, 2, N'I think it''s really important that you enjoy the kind of work that you do.', NULL, NULL, NULL, NULL),
    (@LessonId, 3, N'So I think you have to really understand your own personality and do something that suits you.', NULL, NULL, NULL, NULL),
    (@LessonId, 4, N'I''m the kind of person that''s happy with what they have.', NULL, NULL, NULL, NULL),
    (@LessonId, 5, N'So, unless I have a really big or good idea about what I want to do next, I usually just carry on with what I''m doing and don''t think about the next thing.', NULL, NULL, NULL, NULL),
    (@LessonId, 6, N'Today we''re talking about how to develop and grow at work.', NULL, NULL, NULL, NULL),
    (@LessonId, 7, N'Hello and welcome to Office English from BBC Learning English.', NULL, NULL, NULL, NULL),
    (@LessonId, 8, N'Your podcast guide to the world of work.', NULL, NULL, NULL, NULL),
    (@LessonId, 9, N'I''m Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 10, N'And I''m Pippa.', NULL, NULL, NULL, NULL),
    (@LessonId, 11, N'Head to our website for a full transcript and subtitles for this podcast, bbclearningenglish.com.', NULL, NULL, NULL, NULL),
    (@LessonId, 12, N'OK, so, Pippa, today we''re talking about career development.', NULL, NULL, NULL, NULL),
    (@LessonId, 13, N'What do we mean by this?', NULL, NULL, NULL, NULL),
    (@LessonId, 14, N'Well, career development is kind of how you get new experience, how you learn new things.', NULL, NULL, NULL, NULL),
    (@LessonId, 15, N'So this can be the different jobs that you have across your career, getting promoted or changing jobs.', NULL, NULL, NULL, NULL),
    (@LessonId, 16, N'But it can just be learning new skills or doing new things within your existing job.', NULL, NULL, NULL, NULL),
    (@LessonId, 17, N'Today we''ll talk about a few different aspects of career development - that''s improving your skills, applying for jobs, and building a network.', NULL, NULL, NULL, NULL),
    (@LessonId, 18, N'Let''s start with improving your skills.', NULL, NULL, NULL, NULL),
    (@LessonId, 19, N'Let''s imagine you want to get better at a certain aspect of your job, or learn a new skill, such as leadership to prepare for the future.', NULL, NULL, NULL, NULL),
    (@LessonId, 20, N'How could you discuss this with your manager?', NULL, NULL, NULL, NULL),
    (@LessonId, 21, N'Well, I think it''s useful to think about why you want to learn a new skill, why you want to improve a certain thing.', NULL, NULL, NULL, NULL),
    (@LessonId, 22, N'Maybe it''s that you don''t think anyone in your team knows how to do something.', NULL, NULL, NULL, NULL),
    (@LessonId, 23, N'If so, you can kind of say this to your line manager.', NULL, NULL, NULL, NULL),
    (@LessonId, 24, N'So you could say, ''I think our team would benefit from more experience in...'' Or if you have another reason why you want to do it, you can say that to your line manager.', NULL, NULL, NULL, NULL),
    (@LessonId, 25, N'I think you need to think about what the benefit is for the workplace, for the team, as well as what the benefit is for you personally.', NULL, NULL, NULL, NULL),
    (@LessonId, 26, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 27, N'Um, and then you probably want to talk about specifics or things that you''ve planned to do.', NULL, NULL, NULL, NULL),
    (@LessonId, 28, N'So it might be that you''ve been researching a course that will help you improve certain skills.', NULL, NULL, NULL, NULL),
    (@LessonId, 29, N'It might be there are certain tasks or roles in your team that you don''t do currently, but you might want to have a try at to see if you can develop your skills in them.', NULL, NULL, NULL, NULL),
    (@LessonId, 30, N'Yeah, and then what you want to do is basically see how your manager feels about these ideas.', NULL, NULL, NULL, NULL),
    (@LessonId, 31, N'Um, we have this really nice expression in English, ''sound somebody out''.', NULL, NULL, NULL, NULL),
    (@LessonId, 32, N'And if you sound somebody out, you basically just try to gently get them to say what they think or feel about something.', NULL, NULL, NULL, NULL),
    (@LessonId, 33, N'So you might ask, ''how would you feel about me building these skills?'' So that''s not saying I must do this course or please can I do this course.', NULL, NULL, NULL, NULL),
    (@LessonId, 34, N'It''s just kind of getting their opinion.', NULL, NULL, NULL, NULL),
    (@LessonId, 35, N'And they might say, oh, that''s a great idea.', NULL, NULL, NULL, NULL),
    (@LessonId, 36, N'You should do it.', NULL, NULL, NULL, NULL),
    (@LessonId, 37, N'Or they may say, at the moment, we just don''t have the time for you to be doing a course like this.', NULL, NULL, NULL, NULL),
    (@LessonId, 38, N'So you''ll get a better idea of things.', NULL, NULL, NULL, NULL),
    (@LessonId, 39, N'OK, one aspect of career development is applying for jobs and promotions.', NULL, NULL, NULL, NULL),
    (@LessonId, 40, N'How should you talk about this with your manager?', NULL, NULL, NULL, NULL),
    (@LessonId, 41, N'Maybe let''s start with promotions, Phil.', NULL, NULL, NULL, NULL),
    (@LessonId, 42, N'Yes.', NULL, NULL, NULL, NULL),
    (@LessonId, 43, N'So if you''re looking for a promotion, you''re looking at something within your organisation.', NULL, NULL, NULL, NULL),
    (@LessonId, 44, N'If it''s in your team, it might actually be your line manager who decides who they''re going to pick for the promotion.', NULL, NULL, NULL, NULL),
    (@LessonId, 45, N'So you might want to go to them and say, ''look, are there any skills or experiences that you''re particularly looking for in this role?'' Because that''s the kind of information that will then help you put a strong application together.', NULL, NULL, NULL, NULL),
    (@LessonId, 46, N'You can also say quite clearly, ''I''d like to apply for this promotion.', NULL, NULL, NULL, NULL),
    (@LessonId, 47, N'What advice do you have for submitting a strong application?'' Definitely talk to your manager before you apply for a promotion that they are deciding on.', NULL, NULL, NULL, NULL),
    (@LessonId, 48, N'What about kind of a job within the organisation, but not in your team?', NULL, NULL, NULL, NULL),
    (@LessonId, 49, N'So maybe an opportunity to get some more experience in a different department or a promotion elsewhere.', NULL, NULL, NULL, NULL),
    (@LessonId, 50, N'Should someone tell their line manager about that, about that application, Phil?', NULL, NULL, NULL, NULL),
    (@LessonId, 51, N'Well, like so many things in the world of work, this is all about the relationship that you have with your manager and whether you feel that it''s safe to talk about that, or whether you feel that it might not be in your best interest to let your manager know that you''re thinking about that.', NULL, NULL, NULL, NULL),
    (@LessonId, 52, N'That''s going to vary from person to person, I think.', NULL, NULL, NULL, NULL),
    (@LessonId, 53, N'Um, but if you do have that kind of relationship, then your manager is probably a really good person to give you an idea on what your strengths and weaknesses are and things that you could mention in an application for a promotion.', NULL, NULL, NULL, NULL),
    (@LessonId, 54, N'So I think they are exactly the right person to talk to usually, but it will depend on how, how the relationship is where you work.', NULL, NULL, NULL, NULL),
    (@LessonId, 55, N'Mmm, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 56, N'They can help you prepare.', NULL, NULL, NULL, NULL),
    (@LessonId, 57, N'They''ve probably been through the process of being promoted if they''re your manager.', NULL, NULL, NULL, NULL),
    (@LessonId, 58, N'So yeah, they might have a bit of, uh, what we''d call ''insider knowledge'' on those kinds of things, but it will depend, of course.', NULL, NULL, NULL, NULL),
    (@LessonId, 59, N'And finally, a big part of career development is networking.', NULL, NULL, NULL, NULL),
    (@LessonId, 60, N'And this is speaking to other people in your industry to build relationships that you might use in the future.', NULL, NULL, NULL, NULL),
    (@LessonId, 61, N'We''ve talked about networking at conferences and events before on the podcast, but is there a way you can do this as part of your job?', NULL, NULL, NULL, NULL),
    (@LessonId, 62, N'Well, this will depend on the context of your workplace, but as you meet people as part of your job, you can try to keep a professional relationship with them and ask for advice.', NULL, NULL, NULL, NULL),
    (@LessonId, 63, N'So, for example, if you work with someone on a particular project and that project is ending, you could say something like, ''I''d be really interested to work more with you in the future.', NULL, NULL, NULL, NULL),
    (@LessonId, 64, N'Would there be any opportunities?'' And that''s a good way to keep the relationship going.', NULL, NULL, NULL, NULL),
    (@LessonId, 65, N'Yeah, what''s really important is just try to remain professional, friendly and to, you know, keep up your good reputation.', NULL, NULL, NULL, NULL),
    (@LessonId, 66, N'And let people know, you know, ''it''s been great working with you.', NULL, NULL, NULL, NULL),
    (@LessonId, 67, N'Let''s keep in touch about future prospects.'' And then you might want to send fairly regular emails, you know.', NULL, NULL, NULL, NULL),
    (@LessonId, 68, N'''Just wanted to check in with you.', NULL, NULL, NULL, NULL),
    (@LessonId, 69, N'Wonder if you had any opportunities coming up.'' Um, you know, remind people that you''re there, I guess.', NULL, NULL, NULL, NULL),
    (@LessonId, 70, N'Mmm, yeah.', NULL, NULL, NULL, NULL),
    (@LessonId, 71, N'You can also, if you''ve built a good relationship with someone who maybe is a bit more senior than you in your company, you can ask them for advice sort of more generally instead of a specific opportunity.', NULL, NULL, NULL, NULL),
    (@LessonId, 72, N'So you could say something like, ''could I pick your brains about career options?'' And if you pick someone''s brains, you basically ask for their advice or for them to share some of their knowledge.', NULL, NULL, NULL, NULL),
    (@LessonId, 73, N'And that is it for this episode of Office English.', NULL, NULL, NULL, NULL),
    (@LessonId, 74, N'If you''re interested in leadership, try the Leaders series.', NULL, NULL, NULL, NULL),
    (@LessonId, 75, N'There''s a link to it in the notes below this programme.', NULL, NULL, NULL, NULL),
    (@LessonId, 76, N'Thanks for joining us and goodbye.', NULL, NULL, NULL, NULL),
    (@LessonId, 77, N'Bye.', NULL, NULL, NULL, NULL);

COMMIT TRANSACTION;
GO

SELECT c.learning_mode, c.title AS course_title, l.lesson_order, l.title AS lesson_title,
       l.duration, COUNT(s.sentence_id) AS sentence_count
FROM Courses AS c
INNER JOIN Lessons AS l ON l.course_id = c.course_id
LEFT JOIN Lesson_Sentences AS s ON s.lesson_id = l.lesson_id
WHERE EXISTS (
    SELECT 1 FROM Lesson_Material AS m
    WHERE m.lesson_id = l.lesson_id AND m.source_provider = 'BBC Learning English'
)
  AND c.course_type = 'curriculum'
GROUP BY c.course_id, c.learning_mode, c.title, l.lesson_order, l.title, l.duration
ORDER BY c.learning_mode, c.title, l.lesson_order;
GO
