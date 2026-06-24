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
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<UserRecording> UserRecordings => Set<UserRecording>();
    public DbSet<Transcript> Transcripts => Set<Transcript>();
    public DbSet<AiFeedback> AiFeedbacks => Set<AiFeedback>();
    public DbSet<UserStatistic> UserStatistics => Set<UserStatistic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserCourse>()
            .HasKey(uc => new { uc.UserId, uc.CourseId });

        modelBuilder.Entity<UserStatistic>()
            .HasIndex(us => us.UserId)
            .IsUnique();

        modelBuilder.Entity<UserStatistic>()
            .HasOne(us => us.User)
            .WithOne(u => u.Statistics)
            .HasForeignKey<UserStatistic>(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Lesson>()
            .HasIndex(l => new { l.CourseId, l.LessonOrder })
            .IsUnique();

        modelBuilder.Entity<Course>()
            .Property(c => c.CourseType)
            .HasDefaultValue(CourseTypes.Curriculum);

        modelBuilder.Entity<Lesson>()
            .Property(l => l.Source)
            .HasDefaultValue(LessonSources.Curated);

        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.CreatedByUser)
            .WithMany()
            .HasForeignKey(l => l.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
