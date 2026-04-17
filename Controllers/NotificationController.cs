using ChatApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("test-send")]
        public async Task<IActionResult> TestSendNotification([FromBody] PushNotificationRequest request)
        {
            if (string.IsNullOrEmpty(request.PushToken))
            {
                return BadRequest("PushToken is required");
            }

            await _notificationService.SendNotificationAsync(
                request.PushToken,
                request.Title ?? "Test Notification",
                request.Body ?? "This is a test notification from the API",
                request.Data
            );

            return Ok(new { message = "Notification sent successfully." });
        }
    }

    public class PushNotificationRequest
    {
        public string? PushToken { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public object? Data { get; set; }
    }
}
