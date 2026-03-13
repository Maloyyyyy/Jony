using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JonyBalls3.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JonyBalls3.Controllers
{
    [Authorize]
    public class ProjectStageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectStageController> _logger;

        public ProjectStageController(ApplicationDbContext context, ILogger<ProjectStageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var stage = await _context.ProjectStages
                .Include(s => s.Photos)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stage == null)
                return NotFound();

            // Проверка доступа: пользователь должен быть владельцем проекта или подрядчиком
            var project = await _context.Projects.FindAsync(stage.ProjectId);
            if (project == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isOwner = project.UserId == userId;
            var isContractor = project.ContractorId != null && (await _context.ContractorProfiles.FirstOrDefaultAsync(c => c.Id == project.ContractorId))?.UserId == userId;
            if (!isOwner && !isContractor)
                return Forbid();

            return PartialView("_StageDetails", stage);
        }
    }
}