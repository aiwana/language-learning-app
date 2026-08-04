using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.UnitTests;

public sealed class AdminUserServiceTests
{
    [Fact]
    public async Task SetActiveAsync_RejectsSelfDisable()
    {
        await using var db = CreateDbContext();
        var admin = await SeedUserAsync(db, "admin@test.com", UserRoles.Admin);
        var service = CreateService(db);

        var result = await service.SetActiveAsync(admin.UserId, admin.UserId, active: false, reason: "nope");

        Assert.False(result.Succeeded);
        Assert.Contains("chính mình", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(await db.Users.Where(u => u.UserId == admin.UserId).Select(u => u.IsActive).SingleAsync());
    }

    [Fact]
    public async Task SetActiveAsync_DisablesTargetAndWritesAudit()
    {
        await using var db = CreateDbContext();
        var admin = await SeedUserAsync(db, "admin@test.com", UserRoles.Admin);
        var learner = await SeedUserAsync(db, "learner@test.com", UserRoles.User);
        var service = CreateService(db);

        var result = await service.SetActiveAsync(admin.UserId, learner.UserId, active: false, reason: "spam");

        Assert.True(result.Succeeded);
        var updated = await db.Users.SingleAsync(u => u.UserId == learner.UserId);
        Assert.False(updated.IsActive);
        Assert.Equal("spam", updated.DisabledReason);
        Assert.Equal(admin.UserId, updated.DisabledByUserId);
        Assert.Equal(1, await db.AdminAuditLogs.CountAsync(a =>
            a.TargetUserId == learner.UserId && a.Action == AdminAuditActions.DisableUser));
    }

    [Fact]
    public async Task GrantVipAsync_SetsIsVipAndCreatesSubscription()
    {
        await using var db = CreateDbContext();
        var admin = await SeedUserAsync(db, "admin@test.com", UserRoles.Admin);
        var learner = await SeedUserAsync(db, "learner@test.com", UserRoles.User);
        var service = CreateService(db);

        var result = await service.GrantVipAsync(admin.UserId, learner.UserId, BillingPeriods.Monthly, customDays: null);

        Assert.True(result.Succeeded);
        var updated = await db.Users.SingleAsync(u => u.UserId == learner.UserId);
        Assert.True(updated.IsVip);
        Assert.Equal(1, await db.VipSubscriptions.CountAsync(s =>
            s.UserId == learner.UserId && s.Status == SubscriptionStatuses.Active && s.Provider == "admin"));
        Assert.Equal(1, await db.AdminAuditLogs.CountAsync(a =>
            a.TargetUserId == learner.UserId && a.Action == AdminAuditActions.GrantVip));
    }

    [Fact]
    public async Task LoginAsync_RejectsInactiveUser()
    {
        await using var db = CreateDbContext();
        var user = await SeedUserAsync(db, "inactive@test.com", UserRoles.User, isActive: false, password: "Password123!");
        var auth = CreateAuthService(db);

        var result = await auth.LoginAsync(new LoginViewModel
        {
            Email = user.Email,
            Password = "Password123!"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Tài khoản không khả dụng.", result.Message);
    }

    private static AdminUserService CreateService(AppDbContext db)
    {
        return new AdminUserService(db, new SubscriptionService(db));
    }

    private static AuthService CreateAuthService(AppDbContext db)
    {
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        return new AuthService(
            db,
            http,
            Options.Create(new VipStubOptions { Enabled = false }),
            Options.Create(new GamificationOptions
            {
                MaxHearts = 5,
                HeartExchangeAmount = 1,
                HeartExchangeExpCost = 10
            }));
    }

    private static async Task<User> SeedUserAsync(
        AppDbContext db,
        string email,
        string role,
        bool isActive = true,
        string password = "Password123!")
    {
        var user = new User
        {
            Username = email.Split('@')[0] + Guid.NewGuid().ToString("N")[..6],
            Email = email,
            FullName = "Test User",
            Role = role,
            IsActive = isActive,
            LearningMode = LearningModes.Casual,
            Accent = Accents.EnUs,
            PronunciationTarget = PronunciationTargets.Comprehension70,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Statistics = new UserStatistic { Hearts = 5, Exp = 0 }
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }
}
