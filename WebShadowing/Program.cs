using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.RateLimiting;
using WebShadowing.Data;
using WebShadowing.Services;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

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

    public partial class Program
    {
    }
