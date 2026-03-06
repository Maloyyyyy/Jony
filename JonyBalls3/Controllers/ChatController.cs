using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JonyBalls3.Services;
using System.Security.Claims;
using JonyBalls3.Models;

namespace JonyBalls3.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;
        private readonly ProjectService _projectService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            ChatService chatService,
            ProjectService projectService,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _projectService = projectService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chats = await _chatService.GetUserChatsAsync(userId);

            var unreadCounts = new Dictionary<int, int>();
            foreach (var chat in chats)
            {
                unreadCounts[chat.Id] = await _chatService.GetUnreadCountForProjectAsync(userId, chat.Id);
            }

            ViewBag.UnreadCounts = unreadCounts;
            return View(chats);
        }

        public async Task<IActionResult> Project(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await _projectService.GetProjectByIdAsync(id);

            if (project == null)
                return NotFound();

            if (project.UserId != userId && (project.Contractor == null || project.Contractor.UserId != userId))
                return Forbid();

            var messages = await _chatService.GetMessagesAsync(id);
            await _chatService.MarkAllAsReadAsync(id, userId);

            ViewBag.Project = project;
            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageModel model)
        {
            try
            {
                var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var project = await _projectService.GetProjectByIdAsync(model.ProjectId);

                if (project == null)
                    return Json(new { success = false, message = "Проект не найден" });

                string receiverId;
                if (project.UserId == senderId)
                    receiverId = project.Contractor?.UserId;
                else if (project.Contractor != null && project.Contractor.UserId == senderId)
                    receiverId = project.UserId;
                else
                    return Json(new { success = false, message = "Вы не участник проекта" });

                if (string.IsNullOrEmpty(receiverId))
                    return Json(new { success = false, message = "Получатель не найден" });

                var message = await _chatService.SendMessageAsync(
                    senderId, receiverId, model.ProjectId, model.Message, model.AttachmentUrl ?? "");

                return Ok(new
                {
                    success = true,
                    message = new
                    {
                        id = message.Id,
                        text = message.Message,
                        senderId = message.SenderId,
                        sentAt = message.SentAt,
                        isRead = message.IsRead
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения");
                return StatusCode(500, new { success = false, message = "Ошибка сервера" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int projectId, int lastMessageId = 0)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var messages = await _chatService.GetMessagesAsync(projectId, lastMessageId);

                var unreadIds = messages
                    .Where(m => m.ReceiverId == userId && !m.IsRead)
                    .Select(m => m.Id).ToList();

                if (unreadIds.Any())
                    await _chatService.MarkMessagesAsReadAsync(unreadIds, userId);

                return Ok(messages.Select(m => new
                {
                    m.Id,
                    m.Message,
                    m.SenderId,
                    m.SentAt,
                    m.IsRead,
                    IsMine = m.SenderId == userId
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки сообщений");
                return StatusCode(500, new { success = false, message = "Ошибка загрузки" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _chatService.MarkMessagesAsReadAsync(model.MessageIds, userId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отметки прочитанных");
                return StatusCode(500, new { success = false, message = "Ошибка сервера" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                    return Ok(0);

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var count = await _chatService.GetTotalUnreadCountAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения непрочитанных");
                return Ok(0);
            }
        }
    }

    public class SendMessageModel
    {
        public int ProjectId { get; set; }   // ← Этого не хватало!
        public string Message { get; set; } = "";
        public string? AttachmentUrl { get; set; }
    }

    public class MarkAsReadModel
    {
        public List<int> MessageIds { get; set; } = new();
    }
}