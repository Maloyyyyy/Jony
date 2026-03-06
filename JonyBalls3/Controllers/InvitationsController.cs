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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var contractor = await _contractorService.GetContractorByUserIdAsync(userId);

            if (contractor == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var invitations = await _invitationService.GetInvitationsForContractorAsync(contractor.Id);
            return View(invitations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int projectId, int contractorId, string message)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var project = await _projectService.GetProjectByIdAsync(projectId);

                if (project == null)
                {
                    return Json(new { success = false, message = "Проект не найден" });
                }

                if (project.UserId != userId)
                {
                    return Json(new { success = false, message = "У вас нет прав для этого действия" });
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _invitationService.AcceptInvitationAsync(id, userId);

                if (result)
                {
                    return Json(new { success = true, message = "Приглашение принято" });
                }

                return Json(new { success = false, message = "Не удалось принять приглашение" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при принятии приглашения");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _invitationService.RejectInvitationAsync(id, userId);

                if (result)
                {
                    return Json(new { success = true, message = "Приглашение отклонено" });
                }

                return Json(new { success = false, message = "Не удалось отклонить приглашение" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отклонении приглашения");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _invitationService.CancelInvitationAsync(id, userId);

                if (result)
                {
                    return Json(new { success = true, message = "Приглашение отменено" });
                }

                return Json(new { success = false, message = "Не удалось отменить приглашение" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отмене приглашения");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingCount()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Ok(0);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var contractor = await _contractorService.GetContractorByUserIdAsync(userId);

            if (contractor == null)
            {
                return Ok(0);
            }

            var count = await _invitationService.GetPendingCountAsync(contractor.Id);
            return Ok(count);
        }
    }
}