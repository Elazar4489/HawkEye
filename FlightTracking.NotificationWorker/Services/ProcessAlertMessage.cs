using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using FlightTracking.NotificationWorker.Models;
using FlightTracking.NotificationWorker.Settings;
using Google.Apis.Auth.OAuth2;
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
        private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

        private readonly ILogger<ProcessAlertMessage> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ApiSettings _apiSettings;
        private readonly bool _firebaseEnabled;

        public ProcessAlertMessage(
            ILogger<ProcessAlertMessage> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<ApiSettings> apiSettings,
            IOptions<FirebaseSettings> firebaseSettings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _apiSettings = apiSettings.Value;
            _firebaseEnabled = TryInitializeFirebase(firebaseSettings.Value.CredentialsPath);
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
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UzerId == alertData.UserId, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found in database.", alertData.UserId);
                    return;
                }

                var channels = new List<string>();

                // 1. Push (FCM) - when the user registered a device token and Firebase is configured
                if (_firebaseEnabled && !string.IsNullOrEmpty(user.PushToken))
                {
                    if (await SendPushNotificationAsync(user.PushToken, alertData))
                    {
                        channels.Add("Push");
                        _logger.LogInformation("Push notification successfully sent to device token for User ID {UserId}", user.UzerId);
                    }
                }

                // 2. In-app real time (SignalR via the API) - when the user has the app open
                var isUserConnectedRealtime = await CheckIfUserIsConnectedActiveAsync(dbContext, user.UzerId, cancellationToken);
                if (isUserConnectedRealtime)
                {
                    if (await SendInAppWebSocketMessageAsync(alertData))
                    {
                        channels.Add("SignalR");
                        _logger.LogInformation("In-app real-time message sent to active connection for User ID {UserId}", user.UzerId);
                    }
                }

                if (channels.Count == 0)
                {
                    _logger.LogInformation("User ID {UserId} is offline and has no push token. Fallback to DB history only.", user.UzerId);
                }

                // 3. Always keep the history
                var historyItem = new NotificationHistoryItem
                {
                    UserId = user.UzerId,
                    Message = alertData.Message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    Icao24 = alertData.Icao24,
                    Callsign = Truncate(alertData.Callsign?.Trim(), 16),
                    ZoneName = Truncate(alertData.ZoneName, 100),
                    Priority = alertData.Priority,
                    Latitude = alertData.Latitude,
                    Longitude = alertData.Longitude,
                    BaroAltitude = alertData.BaroAltitude,
                    Channel = channels.Count == 0 ? "DB" : string.Join("+", channels)
                };

                dbContext.NotificationHistories.Add(historyItem);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Alert successfully saved to NotificationHistories for User ID {UserId}", user.UzerId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing alert message payload.");
            }
        }

        private bool TryInitializeFirebase(string? credentialsPath)
        {
            if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
            {
                _logger.LogWarning("Firebase credentials not configured - FCM push is disabled (SignalR + history still work).");
                return false;
            }

            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(credentialsPath)
                    });
                }

                _logger.LogInformation("Firebase initialized - FCM push is enabled.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase - FCM push is disabled.");
                return false;
            }
        }

        private async Task<bool> SendPushNotificationAsync(string pushToken, FlightAlertMessage alert)
        {
            try
            {
                var firebaseMessage = new Message()
                {
                    // FCM registration token of the browser / device
                    Token = pushToken,
                    Notification = new Notification()
                    {
                        Title = "HawkEye flight alert",
                        Body = alert.Message
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["icao24"] = alert.Icao24 ?? string.Empty,
                        ["zoneName"] = alert.ZoneName ?? string.Empty
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

        private static async Task<bool> CheckIfUserIsConnectedActiveAsync(
            NotificationDbContext dbContext, int userId, CancellationToken cancellationToken)
        {
            return await dbContext.UserConnections
                .AsNoTracking()
                .AnyAsync(c => c.UserId == userId && c.IsConnected, cancellationToken);
        }

        private async Task<bool> SendInAppWebSocketMessageAsync(FlightAlertMessage alert)
        {
            if (string.IsNullOrWhiteSpace(_apiSettings.BaseUrl))
            {
                _logger.LogWarning("ApiSettings:BaseUrl is not configured - cannot send SignalR message.");
                return false;
            }

            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{_apiSettings.BaseUrl.TrimEnd('/')}/api/notifications/send-signalr")
                {
                    Content = JsonContent.Create(new
                    {
                        alert.UserId,
                        alert.Message,
                        alert.Icao24,
                        alert.Callsign,
                        alert.FlightNumber,
                        alert.ZoneName,
                        alert.Priority,
                        alert.Latitude,
                        alert.Longitude,
                        alert.BaroAltitude,
                        alert.MatchedAtUtc
                    })
                };
                request.Headers.Add("X-Internal-Key", _apiSettings.InternalApiKey);

                using var response = await HttpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SignalR real-time message successfully triggered via API for User ID {UserId}", alert.UserId);
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

        private static string? Truncate(string? value, int maxLength) =>
            value == null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
