using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;

namespace WebShadowing.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public HealthController(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
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

            return Ok(new
            {
                status = "healthy",
                database = "connected",
                timestamp = _timeProvider.GetUtcNow()
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unhealthy",
                database = "error"
            });
        }
    }
}
