using Microsoft.EntityFrameworkCore;
using JonyBalls3.Data;
using JonyBalls3.Models;

namespace JonyBalls3.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Создать уведомление для пользователя
        /// </summary>
        public async Task<Notification> CreateAsync(string userId, string title, string message,
            NotificationType type = NotificationType.Info, string? link = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания уведомления для {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Уведомление: подрядчик добавлен / приглашение принято
        /// </summary>
        public async Task NotifyContractorAssignedAsync(string projectOwnerId, string contractorName, string projectName, int projectId)
        {
            await CreateAsync(
                projectOwnerId,
                "Подрядчик принял приглашение",
                $"Подрядчик {contractorName} принял ваше приглашение на проект «{projectName}».",
                NotificationType.Success,
                $"/Projects/Details/{projectId}"
            );
        }

        /// <summary>
        /// Уведомление: приглашение отклонено
        /// </summary>
        public async Task NotifyInvitationRejectedAsync(string projectOwnerId, string contractorName, string projectName, int projectId)
        {
            await CreateAsync(
                projectOwnerId,
                "Приглашение отклонено",
                $"Подрядчик {contractorName} отклонил ваше приглашение на проект «{projectName}».",
                NotificationType.Warning,
                $"/Projects/Details/{projectId}"
            );
        }

        /// <summary>
        /// Уведомление: новое приглашение для подрядчика
        /// </summary>
        public async Task NotifyNewInvitationAsync(string contractorUserId, string projectName, string ownerName, int invitationId)
        {
            await CreateAsync(
                contractorUserId,
                "Новое приглашение на проект",
                $"{ownerName} приглашает вас на проект «{projectName}».",
                NotificationType.Invitation,
                $"/Invitations/Index"
            );
        }

        /// <summary>
        /// Уведомление: новое сообщение в чате
        /// </summary>
        public async Task NotifyNewMessageAsync(string receiverId, string senderName, string projectName, int projectId)
        {
            // Не создаём уведомление если у пользователя уже есть непрочитанное уведомление о чате по этому проекту
            var existingUnread = await _context.Notifications
                .Where(n => n.UserId == receiverId && !n.IsRead && n.Type == NotificationType.Chat
                    && n.Link == $"/Chat/Project/{projectId}")
                .AnyAsync();

            if (!existingUnread)
            {
                await CreateAsync(
                    receiverId,
                    "Новое сообщение",
                    $"{senderName} написал вам в проекте «{projectName}».",
                    NotificationType.Chat,
                    $"/Chat/Project/{projectId}"
                );
            }
        }

        /// <summary>
        /// Уведомление: изменение статуса проекта
        /// </summary>
        public async Task NotifyProjectStatusChangedAsync(string userId, string projectName, string newStatus, int projectId)
        {
            await CreateAsync(
                userId,
                "Статус проекта изменён",
                $"Проект «{projectName}» теперь имеет статус: {newStatus}.",
                NotificationType.Project,
                $"/Projects/Details/{projectId}"
            );
        }

        /// <summary>
        /// Уведомление: этап проекта завершён
        /// </summary>
        public async Task NotifyStageCompletedAsync(string userId, string stageName, string projectName, int projectId)
        {
            await CreateAsync(
                userId,
                "Этап завершён",
                $"Этап «{stageName}» в проекте «{projectName}» отмечен как завершённый.",
                NotificationType.Success,
                $"/Projects/Details/{projectId}"
            );
        }

        /// <summary>
        /// Получить все уведомления пользователя (последние 50)
        /// </summary>
        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int take = 50)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Получить непрочитанные уведомления
        /// </summary>
        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        /// <summary>
        /// Количество непрочитанных уведомлений
        /// </summary>
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        /// <summary>
        /// Отметить уведомление как прочитанное
        /// </summary>
        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Отметить все уведомления пользователя как прочитанные
        /// </summary>
        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            foreach (var n in notifications)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Удалить уведомление
        /// </summary>
        public async Task DeleteAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Удалить все прочитанные уведомления пользователя
        /// </summary>
        public async Task DeleteAllReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead)
                .ToListAsync();
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();
        }
    }
}
