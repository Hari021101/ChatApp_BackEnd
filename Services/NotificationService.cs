using System.Net.Http.Json;

namespace ChatApp.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string pushToken, string title, string body, object? data = null);
    }

    public class ExpoNotificationService : INotificationService
    {
        private readonly HttpClient _httpClient;
        private const string ExpoPushUrl = "https://exp.host/--/api/v2/push/send";

        public ExpoNotificationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendNotificationAsync(string pushToken, string title, string body, object? data = null)
        {
            if (string.IsNullOrEmpty(pushToken) || !pushToken.StartsWith("ExponentPushToken"))
            {
                return;
            }

            var payload = new
            {
                to = pushToken,
                title = title,
                body = body,
                data = data ?? new {},
                sound = "default"
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(ExpoPushUrl, payload);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"DEBUG: Failed to send notification: {error}");
                }
                else
                {
                    Console.WriteLine($"DEBUG: Successfully sent notification to {pushToken}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error sending notification: {ex}");
            }
        }
    }
}
