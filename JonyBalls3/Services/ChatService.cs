using Microsoft.EntityFrameworkCore;
using JonyBalls3.Data;
using JonyBalls3.Models;

namespace JonyBalls3.Services
{
    public class ChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public ChatService(ApplicationDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<ChatMessage>> GetMessagesAsync(int projectId, int lastMessageId = 0)
        {
            var query = _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ProjectId == projectId);

            if (lastMessageId > 0)
                query = query.Where(m => m.Id > lastMessageId);

            return await query.OrderBy(m => m.SentAt).ToListAsync();
        }

        public async Task<ChatMessage> SendMessageAsync(string senderId, string receiverId, int projectId, string message, string attachmentUrl = "")
        {
            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                ProjectId = projectId,
                Message = message,
                SentAt = DateTime.Now,
                AttachmentUrl = attachmentUrl ?? ""
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Уведомление получателю о новом сообщении
            try
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId);
                var sender = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == senderId);

                if (project != null && sender != null && !string.IsNullOrEmpty(receiverId))
                {
                    var senderName = sender.FullName ?? sender.UserName ?? "Пользователь";
                    await _notificationService.NotifyNewMessageAsync(receiverId, senderName, project.Name, projectId);
                }
            }
            catch
            {
                // Не прерываем отправку сообщения если уведомление не создалось
            }

            return chatMessage;
        }

        public async Task MarkAsReadAsync(int messageId, string userId)
        {
            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId && m.ReadAt == null);
            if (message != null)
            {
                message.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int projectId, string userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ProjectId == projectId && m.ReceiverId == userId && m.ReadAt == null)
                .ToListAsync();
            foreach (var m in messages)
                m.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Отмечаем уведомления о чате как прочитанные
            try
            {
                var chatNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead && n.Type == NotificationType.Chat
                        && n.Link == $"/Chat/Project/{projectId}")
                    .ToListAsync();
                foreach (var n in chatNotifications)
                {
                    n.IsRead = true;
                    n.ReadAt = DateTime.Now;
                }
                if (chatNotifications.Any())
                    await _context.SaveChangesAsync();
            }
            catch { }
        }

        public async Task MarkMessagesAsReadAsync(List<int> messageIds, string userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => messageIds.Contains(m.Id) && m.ReceiverId == userId && m.ReadAt == null)
                .ToListAsync();
            foreach (var m in messages)
                m.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task<List<Project>> GetUserChatsAsync(string userId)
        {
            return await _context.Projects
                .Include(p => p.User)
                .Include(p => p.Contractor).ThenInclude(c => c.User)
                .Include(p => p.ChatMessages)
                .Where(p => p.UserId == userId || (p.Contractor != null && p.Contractor.UserId == userId))
                .Where(p => p.ContractorId != null)
                .OrderByDescending(p => p.ChatMessages.Any() ? p.ChatMessages.Max(m => m.SentAt) : p.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountForProjectAsync(string userId, int projectId)
        {
            return await _context.ChatMessages
                .Where(m => m.ProjectId == projectId && m.ReceiverId == userId && m.ReadAt == null)
                .CountAsync();
        }

        public async Task<int> GetTotalUnreadCountAsync(string userId)
        {
            return await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && m.ReadAt == null)
                .CountAsync();
        }
    }
}
