using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JonyBalls3.Services;
using JonyBalls3.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using JonyBalls3.Data;
using System.Linq;

namespace JonyBalls3.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ProjectService _projectService;
        private readonly ContractorService _contractorService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProjectsController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly InvitationService _invitationService;
        private readonly NotificationService _notificationService;

        public ProjectsController(
            ProjectService projectService,
            ContractorService contractorService,
            UserManager<User> userManager,
            ILogger<ProjectsController> logger,
            IWebHostEnvironment env,
            ApplicationDbContext context,
            InvitationService invitationService,
            NotificationService notificationService)
        {
            _projectService = projectService;
            _contractorService = contractorService;
            _userManager = userManager;
            _logger = logger;
            _env = env;
            _context = context;
            _invitationService = invitationService;
            _notificationService = notificationService;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var projects = await _projectService.GetUserProjectsAsync(userId);
            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create(bool fromCalculator = false, decimal? calculatedTotal = null)
        {
            ViewBag.FromCalculator = fromCalculator;
            ViewBag.CalculatedTotal = calculatedTotal ?? 0;

            var viewModel = new ProjectViewModel();
            if (fromCalculator && calculatedTotal.HasValue)
            {
                viewModel.Budget = calculatedTotal.Value;
                viewModel.FromCalculator = true;
                viewModel.CalculatedTotal = calculatedTotal.Value;
            }

            return View(viewModel);
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                RepairType repairType = (viewModel.RepairType ?? "") switch
                {
                    "Косметический" or "Cosmetic" => RepairType.Cosmetic,
                    "Капитальный"   or "Capital"  => RepairType.Capital,
                    "Дизайнерский"  or "Design"   => RepairType.Design,
                    _                              => RepairType.Cosmetic
                };
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Challenge();
                var project = new Project
                {
                    Name = viewModel.Name ?? "",
                    Description = viewModel.Description ?? "",
                    Area = viewModel.Area,
                    RepairType = repairType,
                    Budget = viewModel.Budget,
                    StartDate = viewModel.StartDate,
                    EndDate = viewModel.EndDate,
                    Status = ProjectStatus.Planning,
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    Progress = 0,
                    Spent = 0
                };

                await _projectService.CreateProjectAsync(project);

                // Создаем базовые этапы
                var stages = new List<ProjectStage>
                {
                    new ProjectStage { ProjectId = project.Id, Name = "Подготовка помещения", Order = 1, Budget = project.Budget * 0.1m, Status = StageStatus.NotStarted, Progress = 0 },
                    new ProjectStage { ProjectId = project.Id, Name = "Черновые работы", Order = 2, Budget = project.Budget * 0.3m, Status = StageStatus.NotStarted, Progress = 0 },
                    new ProjectStage { ProjectId = project.Id, Name = "Чистовая отделка", Order = 3, Budget = project.Budget * 0.4m, Status = StageStatus.NotStarted, Progress = 0 },
                    new ProjectStage { ProjectId = project.Id, Name = "Уборка и сдача", Order = 4, Budget = project.Budget * 0.2m, Status = StageStatus.NotStarted, Progress = 0 }
                };

                foreach (var stage in stages)
                    await _projectService.AddStageAsync(stage);

                TempData["Success"] = "Проект успешно создан";
                return RedirectToAction(nameof(Details), new { id = project.Id });
            }
            return View(viewModel);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
                return NotFound();

            var viewModel = new ProjectViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Area = project.Area,
                RepairType = project.RepairType.ToString(),
                Budget = project.Budget,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status switch {
                    ProjectStatus.Planning  => "Планирование",
                    ProjectStatus.Active    => "Активный",
                    ProjectStatus.Paused    => "Приостановлен",
                    ProjectStatus.Completed => "Завершён",
                    ProjectStatus.Cancelled => "Отменён",
                    _                       => project.Status.ToString()
                }
            };

            return View(viewModel);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectViewModel viewModel)
        {
            if (id != viewModel.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var project = await _projectService.GetProjectByIdAsync(id);
                if (project == null)
                    return NotFound();

                RepairType repairType = (viewModel.RepairType ?? "") switch
                {
                    "Косметический" or "Cosmetic" => RepairType.Cosmetic,
                    "Капитальный"   or "Capital"  => RepairType.Capital,
                    "Дизайнерский"  or "Design"   => RepairType.Design,
                    _                              => RepairType.Cosmetic
                };
                ProjectStatus status = (viewModel.Status ?? "") switch
                {
                    "Планирование"              => ProjectStatus.Planning,
                    "Активный"                  => ProjectStatus.Active,
                    "Приостановлен"             => ProjectStatus.Paused,
                    "Завершён" or "Завершен"    => ProjectStatus.Completed,
                    "Отменён"  or "Отменен"     => ProjectStatus.Cancelled,
                    _                            => ProjectStatus.Planning
                };

                var oldStatus = project.Status;

                project.Name = viewModel.Name ?? "";
                project.Description = viewModel.Description ?? "";
                project.Area = viewModel.Area;
                project.RepairType = repairType;
                project.Budget = viewModel.Budget;
                project.StartDate = viewModel.StartDate;
                project.EndDate = viewModel.EndDate;
                project.Status = status;
                project.UpdatedAt = DateTime.Now;

                await _projectService.UpdateProjectAsync(project);

                // Уведомление об изменении статуса
                if (oldStatus != status)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var statusName = status switch
                    {
                        ProjectStatus.Planning => "Планирование",
                        ProjectStatus.Active => "Активный",
                        ProjectStatus.Paused => "Приостановлен",
                        ProjectStatus.Completed => "Завершён",
                        ProjectStatus.Cancelled => "Отменён",
                        _ => status.ToString()
                    };

                    // Уведомляем владельца
                    try
                    {
                        await _notificationService.NotifyProjectStatusChangedAsync(userId!, project.Name, statusName, project.Id);
                    }
                    catch { }

                    // Уведомляем подрядчика если есть
                    if (project.Contractor?.UserId != null)
                    {
                        try
                        {
                            await _notificationService.NotifyProjectStatusChangedAsync(project.Contractor.UserId, project.Name, statusName, project.Id);
                        }
                        catch { }
                    }
                }

                TempData["Success"] = "Проект обновлён";
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(viewModel);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
                return NotFound();
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _projectService.DeleteProjectAsync(id);
            TempData["Success"] = "Проект удален";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddStage(int projectId, string name, string description, decimal budget)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Название этапа обязательно" });
            if (budget < 0)
                return Json(new { success = false, message = "Бюджет не может быть отрицательным" });

            var stage = new ProjectStage
            {
                ProjectId = projectId,
                Name = name,
                Description = description ?? "",
                Budget = budget,
                Status = StageStatus.NotStarted,
                Progress = 0,
                Spent = 0,
                Order = (await _context.ProjectStages.Where(s => s.ProjectId == projectId).CountAsync()) + 1
            };

            await _projectService.AddStageAsync(stage);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddExpense(AddExpenseViewModel model)
        {
            if (model.Amount <= 0)
                return Json(new { success = false, message = "Сумма должна быть больше 0" });

            var expense = new Expense
            {
                ProjectId = model.ProjectId,
                StageId = model.StageId,
                Name = model.Name ?? "Расход",
                Amount = model.Amount,
                Category = Enum.TryParse<ExpenseCategory>(model.Category, out var cat) ? cat : ExpenseCategory.Other,
                Date = model.Date,
                Description = model.Description ?? ""
            };

            if (model.ReceiptImage != null && model.ReceiptImage.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ReceiptImage.FileName);
                var filePath = Path.Combine(_env.WebRootPath, "uploads/receipts", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ReceiptImage.CopyToAsync(stream);
                }
                expense.ReceiptUrl = "/uploads/receipts/" + fileName;
            }

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            // Обновляем потраченную сумму этапа и проекта
            var stage = await _context.ProjectStages.FindAsync(model.StageId);
            if (stage != null)
            {
                stage.Spent += model.Amount;
                await _context.SaveChangesAsync();
            }

            var project = await _context.Projects.FindAsync(model.ProjectId);
            if (project != null)
            {
                project.Spent += model.Amount;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Расход успешно добавлен" });
        }

        [HttpPost]
        public async Task<IActionResult> UploadStagePhoto(int stageId, string description, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Файл не выбран" });

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(_env.WebRootPath, "uploads/stages", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photo = new StagePhoto
            {
                StageId = stageId,
                ImageUrl = "/uploads/stages/" + fileName,
                Description = description ?? "",
                UploadedAt = DateTime.Now
            };

            _context.StagePhotos.Add(photo);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Фото успешно загружено" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStageProgress(int stageId, int progress, decimal spent)
        {
            var stage = await _context.ProjectStages.Include(s => s.Project).FirstOrDefaultAsync(s => s.Id == stageId);
            if (stage == null) return Json(new { success = false, message = "Этап не найден" });

            stage.Progress = progress;
            if (progress >= 100) stage.Status = StageStatus.Completed;
            else if (progress > 0) stage.Status = StageStatus.InProgress;

            await _context.SaveChangesAsync();

            // Обновляем общий прогресс проекта
            var allStages = await _context.ProjectStages.Where(s => s.ProjectId == stage.ProjectId).ToListAsync();
            if (allStages.Any())
            {
                stage.Project.Progress = (int)allStages.Average(s => s.Progress);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserProjects()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var projects = await _context.Projects
                .Where(p => p.UserId == userId && p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.Cancelled)
                .Select(p => new { id = p.Id, name = p.Name })
                .ToListAsync();
            return Json(projects);
        }
    }
}
