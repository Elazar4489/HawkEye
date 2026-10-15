using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using FlightTracking.Api.Settings;
using FlightTracking.Api.Models;
using FlightTracking.Api.Services;
using Microsoft.EntityFrameworkCore;

// The browser connects with its JWT (?access_token=...). Each user joins a private
// group, and NotificationsController pushes "ReceiveFlightAlert" into that group.
[Authorize]
public class NotificationHub : Hub
{
    private readonly AppDbContext _context;

    public NotificationHub(AppDbContext context)
    {
        _context = context;
    }

    public static string UserGroup(int userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        // כאן הלקוח מתחבר - מעדכנים ב-DB שהמשתמש מחובר
        var userId = Context.User?.GetUserId();
        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));

            var connection = await _context.UserConnections.FirstOrDefaultAsync(c => c.UserId == userId.Value);
            if (connection == null)
            {
                _context.UserConnections.Add(new UserConnection { UserId = userId.Value, IsConnected = true });
            }
            else
            {
                connection.IsConnected = true;
                connection.LastUpdated = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // כאן הלקוח מתנתק - מעדכנים ב-DB שהמשתמש התנתק
        var userId = Context.User?.GetUserId();
        if (userId.HasValue)
        {
            var connection = await _context.UserConnections.FirstOrDefaultAsync(c => c.UserId == userId.Value);
            if (connection != null)
            {
                connection.IsConnected = false;
                connection.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
