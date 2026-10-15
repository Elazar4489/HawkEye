using FlightTracking.Api.DTOs;
using FlightTracking.Api.Services;
using FlightTracking.Api.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FlightTracking.Api.Controllers
{
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        public const string InternalKeyHeader = "X-Internal-Key";

        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            AppDbContext context,
            IHubContext<NotificationHub> hubContext,
            IConfiguration configuration,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _configuration = configuration;
            _logger = logger;
        }

        // Called by FlightTracking.NotificationWorker (service-to-service, shared key).
        [HttpPost("api/notifications/send-signalr")]
        [AllowAnonymous]
        public async Task<IActionResult> SendSignalR([FromBody] FlightAlertNotificationDto dto)
        {
            var expectedKey = _configuration["Internal:ApiKey"];
            if (!string.IsNullOrEmpty(expectedKey) &&
                Request.Headers[InternalKeyHeader] != expectedKey)
            {
                return Unauthorized();
            }

            await _hubContext.Clients
                .Group(NotificationHub.UserGroup(dto.UserId))
                .SendAsync("ReceiveFlightAlert", dto);

            _logger.LogInformation("SignalR alert pushed to user {UserId}: {Message}", dto.UserId, dto.Message);
            return Ok();
        }

        [HttpGet("api/users/{userId:int}/notifications")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<NotificationHistoryDto>>> GetHistory(int userId, [FromQuery] int take = 200)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            take = Math.Clamp(take, 1, 1000);

            var items = await _context.NotificationHistories
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();

            return Ok(items.Select(n => new NotificationHistoryDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Message = n.Message,
                Icao24 = n.Icao24,
                Callsign = n.Callsign,
                ZoneName = n.ZoneName,
                Priority = n.Priority,
                Severity = Severity.FromPriority(n.Priority),
                Channel = n.Channel,
                Latitude = n.Latitude,
                Longitude = n.Longitude,
                BaroAltitude = n.BaroAltitude,
                Timestamp = DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc),
                IsRead = n.IsRead
            }));
        }

        [HttpPut("api/users/{userId:int}/notifications/read-all")]
        [Authorize]
        public async Task<IActionResult> MarkAllRead(int userId)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            await _context.NotificationHistories
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));

            return NoContent();
        }
    }
}
