using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.RateLimiting;
using WebShadowing.Data;
using WebShadowing.Configuration;
using WebShadowing.Middleware;
using WebShadowing.Models;
using WebShadowing.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddDevelopmentEnvironmentFile();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ILessonContentService, LessonContentService>();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache(options => options.SizeLimit = 2_000);
builder.Services.Configure<PronunciationAssessmentOptions>(
    builder.Configuration.GetSection(PronunciationAssessmentOptions.SectionName));
builder.Services.AddScoped<PronunciationScoreProfileService>();
builder.Services.AddScoped<AzurePronunciationAssessmentService>();
builder.Services.AddScoped<OpenAiPronunciationAssessmentService>();
builder.Services.AddScoped<IPronunciationAssessmentService, HybridPronunciationAssessmentService>();
builder.Services.AddScoped<IPracticeEvaluationService, PracticeEvaluationService>();
builder.Services.AddSingleton<ILanguageReferenceService, OpenAiLanguageReferenceService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("pronunciation-ai", context => BuildAiRateLimiter(context, permitLimit: 10));
    options.AddPolicy("language-reference-ai", context => BuildAiRateLimiter(context, permitLimit: 30));
    options.AddPolicy("ai-generation", context => BuildAiRateLimiter(context, permitLimit: 5));
    options.AddPolicy("ai-dialogue", context => BuildAiRateLimiter(context, permitLimit: 12));
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")))
    .SetApplicationName("WebShadowing");

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Authen";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/Authen";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = ValidateAdminPrincipalAsync;
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IUserStatsService, UserStatsService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<IVocabularyService, VocabularyService>();
builder.Services.AddScoped<IFavoriteSentenceService, FavoriteSentenceService>();
builder.Services.AddScoped<IWordErrorTracker, WordErrorTracker>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IModeChangeService, ModeChangeService>();
builder.Services.AddScoped<IOpenAiApiClient, OpenAiApiClient>();
builder.Services.AddScoped<ITtsAudioService, TtsAudioService>();
builder.Services.AddScoped<IAiLessonGenerationService, AiLessonGenerationService>();
builder.Services.AddScoped<IAiDialogueService, AiDialogueService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddHostedService<SubscriptionExpiryService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<GamificationOptions>()
    .Bind(builder.Configuration.GetSection(GamificationOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => options.MaxHearts >= options.HeartExchangeAmount,
        "Gamification:MaxHearts must be at least HeartExchangeAmount.")
    .ValidateOnStart();
builder.Services.Configure<VipStubOptions>(options =>
{
    options.Enabled = builder.Configuration.GetValue<bool?>("VipStub:Enabled")
        ?? builder.Environment.IsDevelopment();
});
builder.Services.AddOptions<VocabularyOptions>()
    .Bind(builder.Configuration.GetSection(VocabularyOptions.SectionName))
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<ModeChangeOptions>()
    .Bind(builder.Configuration.GetSection(ModeChangeOptions.SectionName))
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<AiLessonOptions>()
    .Bind(builder.Configuration.GetSection(AiLessonOptions.SectionName))
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<AiDialogueOptions>()
    .Bind(builder.Configuration.GetSection(AiDialogueOptions.SectionName))
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<PaymentOptions>()
    .Bind(builder.Configuration.GetSection(PaymentOptions.SectionName))
    .ValidateDataAnnotations().ValidateOnStart();

var app = builder.Build();

app.UseResponseCompression();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    var request = context.HttpContext.Request;
    if (response.StatusCode == StatusCodes.Status404NotFound
        && !request.Path.StartsWithSegments("/api")
        && !request.Path.StartsWithSegments("/Home/NotFoundPage"))
    {
        response.Redirect("/Home/NotFoundPage");
    }
    await Task.CompletedTask;
});
app.UseAuthentication();
app.UseMiddleware<OnboardingGuardMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Users}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static async Task ValidateAdminPrincipalAsync(CookieValidatePrincipalContext context)
{
    var userIdValue = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!long.TryParse(userIdValue, out var userId))
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return;
    }

    var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
    var user = await db.Users.AsNoTracking()
        .Where(u => u.UserId == userId)
        .Select(u => new { u.IsActive, u.Role, u.FullName, u.Email, u.Username })
        .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

    if (user is null || !user.IsActive)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return;
    }

    var currentRole = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;
    var currentName = context.Principal?.FindFirst(ClaimTypes.Name)?.Value;
    if (currentRole == user.Role && currentName == user.FullName)
    {
        return;
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, userId.ToString()),
        new(ClaimTypes.Name, user.FullName),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.Role),
        new("username", user.Username)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    context.ReplacePrincipal(new ClaimsPrincipal(identity));
    context.ShouldRenew = true;
}

static RateLimitPartition<string> BuildAiRateLimiter(HttpContext context, int permitLimit)
{
    var userKey = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? context.User.Identity?.Name
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: userKey,
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
    });
}

public partial class Program;
