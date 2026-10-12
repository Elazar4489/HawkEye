using Microsoft.AspNetCore.SignalR;
using FlightTracking.Api.Settings;
using FlightTracking.Api.Models;
using Microsoft.EntityFrameworkCore;

public class NotificationHub : Hub
{
    private readonly AppDbContext _context;

    public NotificationHub(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        // כאן הלקוח מתחבר - מעדכנים ב-DB שהמשתמש מחובר
        // (ניתן לשלוף את ה-UserId מתוך ה-Context.User אם יש אימות)
        var userIdString = Context.User?.FindFirst("sub")?.Value;
        if (int.TryParse(userIdString, out int userId))
        {
            var connection = await _context.UserConnections.FirstOrDefaultAsync(c => c.UserId == userId);
            if (connection == null)
            {
                _context.UserConnections.Add(new UserConnection { UserId = userId, IsConnected = true });
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
        var userIdString = Context.User?.FindFirst("sub")?.Value;
        if (int.TryParse(userIdString, out int userId))
        {
            var connection = await _context.UserConnections.FirstOrDefaultAsync(c => c.UserId == userId);
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