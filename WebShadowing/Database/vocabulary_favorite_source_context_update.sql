/*
  Chuc nang: tach rieng cac thay doi bo sung source context cho vocabulary notebook
  va favorite sentence de co the chay sau cac script schema nen hien co.
  Thu tu chay: production_learning_schema_update.sql -> project_completion_schema_update.sql
  -> vocabulary_favorite_source_context_update.sql.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.Vocabulary_Items', 'source_type') IS NULL
    ALTER TABLE dbo.Vocabulary_Items ADD source_type VARCHAR(30) NOT NULL CONSTRAINT DF_VocabularyItems_SourceType DEFAULT ('lesson_sentence');
IF COL_LENGTH('dbo.Vocabulary_Items', 'source_lesson_id') IS NULL
    ALTER TABLE dbo.Vocabulary_Items ADD source_lesson_id BIGINT NULL;
IF COL_LENGTH('dbo.Vocabulary_Items', 'source_lesson_title') IS NULL
    ALTER TABLE dbo.Vocabulary_Items ADD source_lesson_title NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.Vocabulary_Items', 'source_sentence_text') IS NULL
    ALTER TABLE dbo.Vocabulary_Items ADD source_sentence_text NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.Vocabulary_Items', 'source_learning_mode') IS NULL
    ALTER TABLE dbo.Vocabulary_Items ADD source_learning_mode VARCHAR(20) NULL;
GO

UPDATE v
SET v.source_type = COALESCE(NULLIF(v.source_type, ''), 'lesson_sentence'),
    v.source_lesson_id = COALESCE(v.source_lesson_id, s.lesson_id),
    v.source_lesson_title = COALESCE(v.source_lesson_title, l.title),
    v.source_sentence_text = COALESCE(v.source_sentence_text, s.[text], v.example_sentence),
    v.source_learning_mode = COALESCE(v.source_learning_mode, c.learning_mode)
FROM dbo.Vocabulary_Items AS v
LEFT JOIN dbo.Lesson_Sentences AS s ON s.sentence_id = v.source_sentence_id
LEFT JOIN dbo.Lessons AS l ON l.lesson_id = s.lesson_id
LEFT JOIN dbo.Courses AS c ON c.course_id = l.course_id;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Vocabulary_Items') AND name = 'CK_VocabularyItems_SourceType')
    ALTER TABLE dbo.Vocabulary_Items WITH NOCHECK ADD CONSTRAINT CK_VocabularyItems_SourceType CHECK (source_type IN ('lesson_sentence','ai_snapshot'));
GO

IF COL_LENGTH('dbo.Favorite_Sentences', 'saved_segment_id') IS NULL
    ALTER TABLE dbo.Favorite_Sentences ADD saved_segment_id BIGINT NULL;
IF COL_LENGTH('dbo.Favorite_Sentences', 'source_type') IS NULL
    ALTER TABLE dbo.Favorite_Sentences ADD source_type VARCHAR(30) NOT NULL CONSTRAINT DF_FavoriteSentences_SourceType DEFAULT ('lesson_sentence');
IF COL_LENGTH('dbo.Favorite_Sentences', 'source_key') IS NULL
    ALTER TABLE dbo.Favorite_Sentences ADD source_key NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.Favorite_Sentences', 'lesson_id') IS NULL
    ALTER TABLE dbo.Favorite_Sentences ADD lesson_id BIGINT NULL;
IF COL_LENGTH('dbo.Favorite_Sentences', 'lesson_title') IS NULL
    ALTER TABLE dbo.Favorite_Sentences ADD lesson_title NVARCHAR(255) NULL;
IF COL_LENGTH('dbo.Favorite_Sentences', 'text_snapshot') IS NULL
    ALTER TABLE dbo.Favorite_Sentences ADD text_snapshot NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.Favorite_Sentences', 'translation_snapshot') IS NULL
    ALTER TABLE dbo.Favorite_Sentences ADD translation_snapshot NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.Favorite_Sentences', 'learning_mode') IS NULL
    ALTER TABLE dbo.Favorite_Sentences ADD learning_mode VARCHAR(20) NULL;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Favorite_Sentences')
      AND name = 'sentence_id'
      AND is_nullable = 0)
    ALTER TABLE dbo.Favorite_Sentences ALTER COLUMN sentence_id BIGINT NULL;
GO

UPDATE f
SET f.source_type = COALESCE(NULLIF(f.source_type, ''), 'lesson_sentence'),
    f.source_key = COALESCE(NULLIF(f.source_key, ''), CONCAT('sentence:', f.sentence_id)),
    f.lesson_id = COALESCE(f.lesson_id, s.lesson_id),
    f.lesson_title = COALESCE(f.lesson_title, l.title),
    f.text_snapshot = COALESCE(f.text_snapshot, s.[text]),
    f.translation_snapshot = COALESCE(f.translation_snapshot, s.translation),
    f.learning_mode = COALESCE(f.learning_mode, c.learning_mode)
FROM dbo.Favorite_Sentences AS f
LEFT JOIN dbo.Lesson_Sentences AS s ON s.sentence_id = f.sentence_id
LEFT JOIN dbo.Lessons AS l ON l.lesson_id = s.lesson_id
LEFT JOIN dbo.Courses AS c ON c.course_id = l.course_id;
GO

IF COL_LENGTH('dbo.Favorite_Sentences', 'source_key') IS NOT NULL
BEGIN
    UPDATE dbo.Favorite_Sentences
    SET source_key = CONCAT('favorite-', favorite_sentence_id)
    WHERE source_key IS NULL;

    ALTER TABLE dbo.Favorite_Sentences ALTER COLUMN source_key NVARCHAR(255) NOT NULL;
END
GO

IF COL_LENGTH('dbo.Favorite_Sentences', 'text_snapshot') IS NOT NULL
BEGIN
    UPDATE dbo.Favorite_Sentences
    SET text_snapshot = N''
    WHERE text_snapshot IS NULL;

    ALTER TABLE dbo.Favorite_Sentences ALTER COLUMN text_snapshot NVARCHAR(MAX) NOT NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('dbo.Favorite_Sentences') AND name = 'UQ_FavoriteSentences_User_Sentence')
    ALTER TABLE dbo.Favorite_Sentences DROP CONSTRAINT UQ_FavoriteSentences_User_Sentence;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.Favorite_Sentences') AND name = 'FK_FavoriteSentences_SavedSegment')
    ALTER TABLE dbo.Favorite_Sentences WITH NOCHECK ADD CONSTRAINT FK_FavoriteSentences_SavedSegment FOREIGN KEY (saved_segment_id) REFERENCES dbo.Saved_AI_Lesson_Segments(saved_segment_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Favorite_Sentences') AND name = 'UQ_FavoriteSentences_User_Source')
    ALTER TABLE dbo.Favorite_Sentences ADD CONSTRAINT UQ_FavoriteSentences_User_Source UNIQUE (user_id, source_type, source_key);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Favorite_Sentences') AND name = 'CK_FavoriteSentences_SourceType')
    ALTER TABLE dbo.Favorite_Sentences WITH NOCHECK ADD CONSTRAINT CK_FavoriteSentences_SourceType CHECK (source_type IN ('lesson_sentence','ai_snapshot'));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Favorite_Sentences') AND name = 'CK_FavoriteSentences_Source')
    ALTER TABLE dbo.Favorite_Sentences WITH NOCHECK ADD CONSTRAINT CK_FavoriteSentences_Source CHECK ((CASE WHEN sentence_id IS NULL THEN 0 ELSE 1 END + CASE WHEN saved_segment_id IS NULL THEN 0 ELSE 1 END) <= 1);
GO