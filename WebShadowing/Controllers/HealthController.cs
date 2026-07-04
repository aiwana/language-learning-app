using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;

namespace WebShadowing.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            if (!await _db.Database.CanConnectAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "unhealthy",
                    database = "disconnected"
                });
            }

            var userCount = await _db.Users.AsNoTracking().CountAsync(cancellationToken);
            var courseCount = await _db.Courses.AsNoTracking().CountAsync(cancellationToken);

            return Ok(new
            {
                status = "healthy",
                database = "connected",
                users = userCount,
                courses = courseCount,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unhealthy",
                database = "error",
                message = ex.Message
            });
        }
    }
}
