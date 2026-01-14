using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizMonitor.DAL.Data;
using QuizMonitor.DAL.Interfaces;

namespace QuizMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly QuizMonitorDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HealthController> _logger;

    public HealthController(QuizMonitorDbContext context, IUnitOfWork unitOfWork, ILogger<HealthController> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Check API health
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "QuizMonitor API"
        });
    }

    /// <summary>
    /// Check database connection
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            // Test database connection
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    message = "Cannot connect to database",
                    timestamp = DateTime.UtcNow
                });
            }

            // Get database statistics
            var userCount = await _unitOfWork.Users.CountAsync();
            var examCount = await _unitOfWork.Exams.CountAsync();

            return Ok(new
            {
                status = "healthy",
                database = "connected",
                statistics = new
                {
                    users = userCount,
                    exams = examCount
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return StatusCode(503, new
            {
                status = "unhealthy",
                message = "Database error",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
