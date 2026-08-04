using Microsoft.EntityFrameworkCore;
// Chức năng: quản lý session/turn Đối thoại AI, transcription voice, chat reply và TTS.
// Phụ trách chính: Minh Anh. Minh review giới hạn VIP, auth và retention dữ liệu.
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class AiDialogueService : IAiDialogueService
{
    private readonly AppDbContext _db;
    private readonly IOpenAiApiClient _openAi;
    private readonly ITtsAudioService _tts;
    private readonly AiDialogueOptions _options;
    public AiDialogueService(AppDbContext db, IOpenAiApiClient openAi, ITtsAudioService tts, IOptions<AiDialogueOptions> options)
    {
        _db = db; _openAi = openAi; _tts = tts; _options = options.Value;
    }

    public async Task<DialogueSessionDto> StartAsync(long userId, long? lessonId, CancellationToken cancellationToken = default)
    {
        var user = await RequireVipAsync(userId, cancellationToken);

        Lesson? lesson = null;
        if (lessonId is not null)
        {
            lesson = await _db.Lessons.Include(item => item.Sentences)
                .SingleOrDefaultAsync(item => item.LessonId == lessonId, cancellationToken);
            if (lesson is null) lessonId = null;
        }

        var now = DateTime.UtcNow;
        var session = new AiDialogueSession
        {
            UserId = userId, LessonId = lessonId, LearningMode = user.LearningMode,
            Status = "active", CreatedAt = now, LastActivityAt = now,
            Lesson = lesson
        };
        _db.AiDialogueSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        // Generate the first greeting / starter message from AI
        var messages = new List<OpenAiChatMessage>
        {
            new("system", BuildSystemPrompt(session)),
            new("user", $"Start the conversation by greeting me and asking an engaging starter question related to the lesson topic: \"{lesson?.Title ?? "General English"}\". Make it friendly and simple.")
        };

        var firstReply = await _openAi.GenerateTextAsync(_options.Model, messages, cancellationToken);
        var audioUrl = await _tts.CreateAsync(firstReply, user.Accent, $"dialogue-{session.DialogueSessionId}", cancellationToken);

        var firstTurn = new AiDialogueTurn
        {
            DialogueSessionId = session.DialogueSessionId,
            Role = "assistant",
            Text = firstReply,
            AudioUrl = audioUrl,
            CreatedAt = now.AddMilliseconds(1)
        };
        _db.AiDialogueTurns.Add(firstTurn);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(session, [firstTurn]);
    }

    public async Task<DialogueSessionDto?> GetAsync(long userId, long sessionId, CancellationToken cancellationToken = default)
    {
        await RequireVipAsync(userId, cancellationToken);
        var session = await _db.AiDialogueSessions.Include(item => item.Turns)
            .SingleOrDefaultAsync(item => item.DialogueSessionId == sessionId && item.UserId == userId, cancellationToken);
        if (session is null) return null;
        if (ExpireIfNeeded(session)) await _db.SaveChangesAsync(cancellationToken);
        return ToDto(session, session.Turns);
    }

    public async Task<DialogueReplyDto?> SendTextAsync(long userId, long sessionId, string message, CancellationToken cancellationToken = default)
    {
        var user = await RequireVipAsync(userId, cancellationToken);
        var session = await _db.AiDialogueSessions.Include(item => item.Lesson).ThenInclude(item => item!.Sentences)
            .Include(item => item.Turns)
            .SingleOrDefaultAsync(item => item.DialogueSessionId == sessionId && item.UserId == userId, cancellationToken);
        if (session is null) return null;
        if (ExpireIfNeeded(session))
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        if (session.Status != "active") throw new InvalidOperationException("Phiên đối thoại đã kết thúc.");
        if (session.TurnCount >= _options.MaxTurnsPerSession) throw new InvalidOperationException("Phiên đối thoại đã đạt giới hạn lượt.");

        var trimmed = message.Trim();
        var messages = new List<OpenAiChatMessage> { new("system", BuildSystemPrompt(session)) };
        messages.AddRange(session.Turns.OrderBy(item => item.CreatedAt).TakeLast(20).Select(item => new OpenAiChatMessage(item.Role, item.Text)));
        messages.Add(new("user", trimmed));
        var reply = await _openAi.GenerateTextAsync(_options.Model, messages, cancellationToken);
        var audioUrl = await _tts.CreateAsync(reply, user.Accent, $"dialogue-{sessionId}", cancellationToken);
        var now = DateTime.UtcNow;
        _db.AiDialogueTurns.AddRange(
            new AiDialogueTurn { DialogueSessionId = sessionId, Role = "user", Text = trimmed, CreatedAt = now },
            new AiDialogueTurn { DialogueSessionId = sessionId, Role = "assistant", Text = reply, AudioUrl = audioUrl, CreatedAt = now.AddMilliseconds(1) });
        session.TurnCount++;
        session.LastActivityAt = now;
        if (session.TurnCount >= _options.MaxTurnsPerSession)
        {
            session.Status = "completed";
            session.EndedAt = now;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return new(sessionId, session.TurnCount, trimmed, reply, audioUrl, session.Status != "active");
    }

    public async Task<DialogueReplyDto?> SendAudioAsync(long userId, long sessionId, byte[] audio, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        await RequireVipAsync(userId, cancellationToken);
        var transcript = await _openAi.TranscribeAsync(_options.TranscriptionModel, audio, fileName, contentType, cancellationToken);
        if (string.IsNullOrWhiteSpace(transcript)) throw new InvalidOperationException("Không nhận được giọng nói trong bản thu.");
        return await SendTextAsync(userId, sessionId, transcript, cancellationToken);
    }

    private async Task<User> RequireVipAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");
        if (!user.IsVip) throw new UnauthorizedAccessException("Đối thoại AI chỉ dành cho tài khoản VIP.");
        return user;
    }

    private string BuildSystemPrompt(AiDialogueSession session)
    {
        var style = session.LearningMode switch
        {
            LearningModes.Academic => "academic discussion partner",
            LearningModes.Professional => "workplace role-play partner",
            _ => "friendly everyday conversation partner"
        };
        var lessonTitle = session.Lesson?.Title ?? "General English";
        var context = session.Lesson is null ? string.Empty : $" Lesson topic: {session.Lesson.Title}. Useful lines: {string.Join(" | ", session.Lesson.Sentences.OrderBy(item => item.SentenceOrder).Take(6).Select(item => item.Text))}.";
        return $"You are an English {style} for a Vietnamese learner. " +
               $"IMPORTANT: You must keep the conversation STRICTLY focused on the lesson topic: \"{lessonTitle}\". " +
               "Do not drift away from this topic under any circumstances. If the user tries to change the topic, politely steer them back. " +
               $"Reply in 1-3 natural English sentences, keep the conversation moving, and gently reformulate important mistakes without lecturing.{context}";
    }

    private bool ExpireIfNeeded(AiDialogueSession session)
    {
        if (session.Status == "active" && session.LastActivityAt < DateTime.UtcNow.AddMinutes(-_options.SessionTimeoutMinutes))
        {
            session.Status = "expired";
            session.EndedAt = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    private DialogueSessionDto ToDto(AiDialogueSession session, IEnumerable<AiDialogueTurn> turns) => new(
        session.DialogueSessionId, session.Status, session.TurnCount, _options.MaxTurnsPerSession,
        turns.OrderBy(item => item.CreatedAt).Select(item => new DialogueTurnDto(item.Role, item.Text, item.AudioUrl, item.CreatedAt)).ToList());
}
