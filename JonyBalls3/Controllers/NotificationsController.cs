using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using JonyBalls3.Services;

namespace JonyBalls3.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly NotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(NotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return View(notifications);
        }

        // GET: Notifications/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                if (!User.Identity!.IsAuthenticated)
                    return Ok(new { count = 0 });
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var count = await _notificationService.GetUnreadCountAsync(userId!);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения количества уведомлений");
                return Ok(new { count = 0 });
            }
        }

        // GET: Notifications/GetRecent
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var notifications = await _notificationService.GetUnreadNotificationsAsync(userId!);
                return Ok(notifications.Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    TypeName = n.Type.ToString().ToLower(),
                    n.Link,
                    n.IsRead,
                    n.CreatedAt,
                    TimeAgo = GetTimeAgo(n.CreatedAt)
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения уведомлений");
                return Ok(new List<object>());
            }
        }

        // POST: Notifications/MarkAsRead/5
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _notificationService.MarkAsReadAsync(id, userId!);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отметки уведомления");
                return Ok(new { success = false });
            }
        }

        // POST: Notifications/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _notificationService.MarkAllAsReadAsync(userId!);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отметки всех уведомлений");
                return Ok(new { success = false });
            }
        }

        // POST: Notifications/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _notificationService.DeleteAsync(id, userId!);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления уведомления");
                return Ok(new { success = false });
            }
        }

        // POST: Notifications/DeleteAllRead
        [HttpPost]
        public async Task<IActionResult> DeleteAllRead()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _notificationService.DeleteAllReadAsync(userId!);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления прочитанных уведомлений");
                return Ok(new { success = false });
            }
        }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var diff = DateTime.Now - dateTime;
            if (diff.TotalMinutes < 1) return "только что";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин. назад";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч. назад";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} дн. назад";
            return dateTime.ToString("dd.MM.yyyy");
        }
    }
}
