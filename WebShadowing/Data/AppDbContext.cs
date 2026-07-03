using Microsoft.EntityFrameworkCore;
using WebShadowing.Models;

namespace WebShadowing.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<UserCourse> UserCourses => Set<UserCourse>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonMaterial> LessonMaterials => Set<LessonMaterial>();
    public DbSet<LessonSentence> LessonSentences => Set<LessonSentence>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<UserRecording> UserRecordings => Set<UserRecording>();
    public DbSet<Transcript> Transcripts => Set<Transcript>();
    public DbSet<AiFeedback> AiFeedbacks => Set<AiFeedback>();
    public DbSet<UserStatistic> UserStatistics => Set<UserStatistic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserCourse>(entity =>
        {
            entity.HasKey(uc => new { uc.UserId, uc.CourseId });

            entity.HasOne(uc => uc.User)
                .WithMany(u => u.UserCourses)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uc => uc.Course)
                .WithMany(c => c.UserCourses)
                .HasForeignKey(uc => uc.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserStatistic>(entity =>
        {
            entity.HasIndex(us => us.UserId).IsUnique();

            entity.HasOne(us => us.User)
                .WithOne(u => u.Statistics)
                .HasForeignKey<UserStatistic>(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(us => us.Hearts).HasDefaultValue(5);
            entity.Property(us => us.Exp).HasDefaultValue(0);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(c => c.Level).HasMaxLength(20);
            entity.Property(c => c.LearningMode).HasDefaultValue(LearningModes.Casual);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasIndex(l => new { l.CourseId, l.LessonOrder }).IsUnique();

            entity.HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonMaterial>(entity =>
        {
            entity.HasOne(m => m.Lesson)
                .WithMany(l => l.Materials)
                .HasForeignKey(m => m.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonSentence>(entity =>
        {
            entity.HasIndex(s => new { s.LessonId, s.SentenceOrder }).IsUnique();

            entity.HasOne(s => s.Lesson)
                .WithMany(l => l.Sentences)
                .HasForeignKey(s => s.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PracticeSession>(entity =>
        {
            entity.HasOne(ps => ps.User)
                .WithMany(u => u.PracticeSessions)
                .HasForeignKey(ps => ps.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ps => ps.Lesson)
                .WithMany(l => l.PracticeSessions)
                .HasForeignKey(ps => ps.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRecording>(entity =>
        {
            entity.HasOne(r => r.Session)
                .WithMany(ps => ps.Recordings)
                .HasForeignKey(r => r.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.HasOne(t => t.Recording)
                .WithMany(r => r.Transcripts)
                .HasForeignKey(t => t.RecordingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiFeedback>(entity =>
        {
            entity.HasOne(f => f.Session)
                .WithMany(ps => ps.AiFeedbacks)
                .HasForeignKey(f => f.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.LearningMode).HasDefaultValue(LearningModes.Casual);
            entity.Property(u => u.PronunciationTarget).HasDefaultValue(PronunciationTargets.Comprehension70);
            entity.Property(u => u.Accent).HasDefaultValue(Accents.EnUs);
            entity.Property(u => u.IsVip).HasDefaultValue(false);
        });
    }
}
