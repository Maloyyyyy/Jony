using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JonyBalls3.Services;
using System.Security.Claims;

namespace JonyBalls3.Controllers
{
    [Authorize]
    public class InvitationsController : Controller
    {
        private readonly InvitationService _invitationService;
        private readonly ContractorService _contractorService;
        private readonly ProjectService _projectService;
        private readonly ILogger<InvitationsController> _logger;

        public InvitationsController(
            InvitationService invitationService,
            ContractorService contractorService,
            ProjectService projectService,
            ILogger<InvitationsController> logger)
        {
            _invitationService = invitationService;
            _contractorService = contractorService;
            _projectService = projectService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var contractor = await _contractorService.GetContractorByUserIdAsync(userId);
                if (contractor == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var invitations = await _invitationService.GetInvitationsForContractorAsync(contractor.Id);
                return View(invitations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка приглашений");
                return View(new List<JonyBalls3.Models.Invitation>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int projectId, int contractorId, string message)
        {
            try
            {
                _logger.LogInformation($"Попытка создания приглашения: projectId={projectId}, contractorId={contractorId}");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Необходимо авторизоваться" });
                }

                var project = await _projectService.GetProjectByIdAsync(projectId);
                if (project == null)
                {
                    return Json(new { success = false, message = "Проект не найден" });
                }

                if (project.UserId != userId)
                {
                    return Json(new { success = false, message = "У вас нет прав для этого действия" });
                }

                var contractor = await _contractorService.GetContractorByIdAsync(contractorId);
                if (contractor == null)
                {
                    return Json(new { success = false, message = "Подрядчик не найден" });
                }

                var exists = await _invitationService.HasExistingInvitationAsync(projectId, contractorId);
                if (exists)
                {
                    return Json(new { success = false, message = "Приглашение уже отправлено" });
                }

                var invitation = await _invitationService.CreateInvitationAsync(projectId, contractorId, message, userId);
                
                return Json(new { 
                    success = true, 
                    message = "Приглашение отправлено",
                    invitationId = invitation.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании приглашения");
                return Json(new { success = false, message = "Ошибка при отправке приглашения" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            try
            {
                _logger.LogInformation($"Попытка принятия приглашения {id}");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Необходимо авторизоваться" });
                }

                var result = await _invitationService.AcceptInvitationAsync(id, userId);
                if (result)
                {
                    return Json(new { success = true, message = "Приглашение принято" });
                }

                return Json(new { success = false, message = "Не удалось принять приглашение" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при принятии приглашения {Id}", id);
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                _logger.LogInformation($"Попытка отклонения приглашения {id}");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Необходимо авторизоваться" });
                }

                var result = await _invitationService.RejectInvitationAsync(id, userId);
                if (result)
                {
                    return Json(new { success = true, message = "Приглашение отклонено" });
                }

                return Json(new { success = false, message = "Не удалось отклонить приглашение" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отклонении приглашения {Id}", id);
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                _logger.LogInformation($"Попытка отмены приглашения {id}");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Необходимо авторизоваться" });
                }

                var result = await _invitationService.CancelInvitationAsync(id, userId);
                if (result)
                {
                    return Json(new { success = true, message = "Приглашение отменено" });
                }

                return Json(new { success = false, message = "Не удалось отменить приглашение" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отмене приглашения {Id}", id);
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingCount()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Ok(0);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(0);
                }

                var contractor = await _contractorService.GetContractorByUserIdAsync(userId);
                if (contractor == null)
                {
                    return Ok(0);
                }

                var count = await _invitationService.GetPendingCountAsync(contractor.Id);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении количества приглашений");
                return Ok(0);
            }
        }
    }
}
