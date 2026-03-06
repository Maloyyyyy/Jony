using Microsoft.EntityFrameworkCore;
using JonyBalls3.Data;
using JonyBalls3.Models;

namespace JonyBalls3.Services
{
    public class InvitationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InvitationService> _logger;

        public InvitationService(
            ApplicationDbContext context,
            ILogger<InvitationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Invitation> CreateInvitationAsync(int projectId, int contractorId, string message, string userId)
        {
            try
            {
                _logger.LogInformation($"Создание приглашения: projectId={projectId}, contractorId={contractorId}, userId={userId}");

                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                {
                    _logger.LogError($"Проект с ID {projectId} не найден");
                    throw new Exception("Проект не найден");
                }

                var contractor = await _context.ContractorProfiles
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == contractorId);

                if (contractor == null)
                {
                    _logger.LogError($"Подрядчик с ID {contractorId} не найден");
                    throw new Exception("Подрядчик не найден");
                }

                if (contractor.User == null)
                {
                    _logger.LogError($"У подрядчика {contractorId} нет связанного пользователя");
                    throw new Exception("Ошибка данных подрядчика");
                }

                var invitation = new Invitation
                {
                    ProjectId = projectId,
                    ContractorId = contractorId,
                    Message = message,
                    Status = InvitationStatus.Pending,
                    SentAt = DateTime.Now
                };

                _context.Invitations.Add(invitation);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Приглашение сохранено с ID {invitation.Id}");

                try
                {
                    var chatMessage = new ChatMessage
                    {
                        ProjectId = projectId,
                        SenderId = userId,
                        ReceiverId = contractor.UserId,
                        Message = $"📨 Приглашение отправлено: {message}",
                        SentAt = DateTime.Now
                    };
                    _context.ChatMessages.Add(chatMessage);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Системное сообщение создано");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при создании системного сообщения");
                }

                return invitation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в CreateInvitationAsync");
                throw;
            }
        }

        public async Task<List<Invitation>> GetInvitationsForContractorAsync(int contractorId)
        {
            try
            {
                return await _context.Invitations
                    .Include(i => i.Project)
                        .ThenInclude(p => p.User)
                    .Where(i => i.ContractorId == contractorId)
                    .OrderByDescending(i => i.SentAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении приглашений для подрядчика {ContractorId}", contractorId);
                return new List<Invitation>();
            }
        }

        public async Task<List<Invitation>> GetInvitationsForProjectAsync(int projectId)
        {
            try
            {
                return await _context.Invitations
                    .Include(i => i.Contractor)
                        .ThenInclude(c => c.User)
                    .Where(i => i.ProjectId == projectId)
                    .OrderByDescending(i => i.SentAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении приглашений для проекта {ProjectId}", projectId);
                return new List<Invitation>();
            }
        }

        public async Task<bool> AcceptInvitationAsync(int invitationId, string userId)
        {
            try
            {
                var invitation = await _context.Invitations
                    .Include(i => i.Project)
                    .Include(i => i.Contractor)
                    .FirstOrDefaultAsync(i => i.Id == invitationId);

                if (invitation == null)
                {
                    _logger.LogWarning($"Приглашение {invitationId} не найдено");
                    return false;
                }

                if (invitation.Status != InvitationStatus.Pending)
                {
                    _logger.LogWarning($"Приглашение {invitationId} уже не в статусе Pending");
                    return false;
                }

                if (invitation.Contractor.UserId != userId)
                {
                    _logger.LogWarning($"Пользователь {userId} не является получателем приглашения");
                    return false;
                }

                invitation.Status = InvitationStatus.Accepted;
                invitation.RespondedAt = DateTime.Now;

                var project = invitation.Project;
                project.ContractorId = invitation.ContractorId;
                project.UpdatedAt = DateTime.Now;

                var chatMessage = new ChatMessage
                {
                    ProjectId = project.Id,
                    SenderId = userId,
                    ReceiverId = project.UserId,
                    Message = "✅ Приглашение принято! Теперь можно обсуждать детали.",
                    SentAt = DateTime.Now
                };
                _context.ChatMessages.Add(chatMessage);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Приглашение {invitationId} принято");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при принятии приглашения {InvitationId}", invitationId);
                return false;
            }
        }

        public async Task<bool> RejectInvitationAsync(int invitationId, string userId)
        {
            try
            {
                var invitation = await _context.Invitations
                    .Include(i => i.Project)
                    .Include(i => i.Contractor)
                    .FirstOrDefaultAsync(i => i.Id == invitationId);

                if (invitation == null || invitation.Status != InvitationStatus.Pending)
                    return false;

                if (invitation.Contractor.UserId != userId)
                    return false;

                invitation.Status = InvitationStatus.Rejected;
                invitation.RespondedAt = DateTime.Now;

                var chatMessage = new ChatMessage
                {
                    ProjectId = invitation.ProjectId,
                    SenderId = userId,
                    ReceiverId = invitation.Project.UserId,
                    Message = "❌ Приглашение отклонено",
                    SentAt = DateTime.Now
                };
                _context.ChatMessages.Add(chatMessage);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отклонении приглашения {InvitationId}", invitationId);
                return false;
            }
        }

        public async Task<bool> CancelInvitationAsync(int invitationId, string userId)
        {
            try
            {
                var invitation = await _context.Invitations
                    .Include(i => i.Project)
                    .FirstOrDefaultAsync(i => i.Id == invitationId);

                if (invitation == null || invitation.Status != InvitationStatus.Pending)
                    return false;

                if (invitation.Project.UserId != userId)
                    return false;

                invitation.Status = InvitationStatus.Cancelled;
                invitation.RespondedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отмене приглашения {InvitationId}", invitationId);
                return false;
            }
        }

        public async Task<int> GetPendingCountAsync(int contractorId)
        {
            try
            {
                return await _context.Invitations
                    .Where(i => i.ContractorId == contractorId && i.Status == InvitationStatus.Pending)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подсчете приглашений для {ContractorId}", contractorId);
                return 0;
            }
        }

        public async Task<bool> HasExistingInvitationAsync(int projectId, int contractorId)
        {
            try
            {
                return await _context.Invitations
                    .AnyAsync(i => i.ProjectId == projectId &&
                                   i.ContractorId == contractorId &&
                                   i.Status == InvitationStatus.Pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке существующего приглашения");
                return false;
            }
        }
    }
}