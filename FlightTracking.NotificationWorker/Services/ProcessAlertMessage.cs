using FirebaseAdmin.Messaging;
using FlightTracking.NotificationWorker.Models;
using FlightTracking.NotificationWorker.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlightTracking.NotificationWorker.Services
{
    public class ProcessAlertMessage
    {
        private readonly ILogger<ProcessAlertMessage> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ApiSettings _apiSettings;

        public ProcessAlertMessage(
            ILogger<ProcessAlertMessage> logger,
            IServiceScopeFactory scopeFactory, 
            IOptions<ApiSettings> apiSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _apiSettings = apiSettings.Value;
        }

        public async Task ProcessAlertMessageAsync(string messagePayload, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            try
            {
                var alertData = JsonSerializer.Deserialize<FlightAlertMessage>(messagePayload);
                if (alertData == null)
                {
                    _logger.LogWarning("Failed to deserialize alert payload: {Payload}", messagePayload);
                    return;
                }

                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.UzerId == alertData.UserId, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found in database.", alertData.UserId);
                    return;
                }

                bool notificationSent = false;

                if (!string.IsNullOrEmpty(user.PushToken))
                {
                    notificationSent = await SendPushNotificationAsync(user.PushToken, alertData.Message);
                    if (notificationSent)
                    {
                        _logger.LogInformation("Push notification successfully sent to device token for User ID {UserId}", user.UzerId);
                    }
                }

                if (!notificationSent)
                {
                    bool isUserConnectedRealtime = CheckIfUserIsConnectedActive(dbContext, user.UzerId);

                    if (isUserConnectedRealtime)
                    {
                        notificationSent = await SendInAppWebSocketMessageAsync(user.UzerId, alertData.Message);
                        _logger.LogInformation("In-app real-time message sent to active connection for User ID {UserId}", user.UzerId);
                    }
                    else
                    {
                        _logger.LogInformation("User ID {UserId} is offline and has no push token. Fallback to DB history only.", user.UzerId);
                    }
                }

                var historyItem = new NotificationHistoryItem
                {
                    UserId = user.UzerId,
                    Message = alertData.Message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                dbContext.NotificationHistories.Add(historyItem);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Alert successfully saved to NotificationHistories for User ID {UserId}", user.UzerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alert message payload.");
            }
        }

        private async Task<bool> SendPushNotificationAsync(string pushToken, string message)
        {
            try
            {
                var firebaseMessage = new Message()
                {
                    Fid = pushToken,
                    Notification = new Notification()
                    {
                        Title = "Flight alert",
                        Body = message
                    }
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(firebaseMessage);
                _logger.LogInformation("FCM sent successfully: {Response}", response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM push notification.");
                return false;
            }
        }

        private bool CheckIfUserIsConnectedActive(NotificationDbContext dbContext, int userId)
        {
            var connection = dbContext.UserConnections
                .FirstOrDefault(c => c.UserId == userId);

            return connection != null && connection.IsConnected;
        }

        private async Task<bool> SendInAppWebSocketMessageAsync(int userId, string message)
        {
            try
            {
                using var client = new HttpClient();

                var payload = new
                {
                    UserId = userId,
                    Message = message
                };

                var response = await client.PostAsJsonAsync($"{_apiSettings.BaseUrl}/api/notifications/send-signalr", payload);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SignalR real-time message successfully triggered via API for User ID {UserId}", userId);
                    return true;
                }

                _logger.LogWarning("Failed to trigger SignalR message via API. Status code: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send in-app WebSocket message.");
                return false;
            }
        }
    }
}