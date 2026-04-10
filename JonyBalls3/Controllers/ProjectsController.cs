using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JonyBalls3.Services;
using JonyBalls3.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using JonyBalls3.Data;

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
            TempData["Success"] = "Проект удалён";
            return RedirectToAction(nameof(Index));
        }

        // POST: Projects/AddStage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStage(ProjectStage stage)
        {
            try
            {
                // Ручная валидация вместо ModelState
                if (string.IsNullOrWhiteSpace(stage.Name))
                    return Json(new { success = false, message = "Название этапа обязательно" });
                if (stage.ProjectId <= 0)
                    return Json(new { success = false, message = "Некорректный ID проекта" });
                if (stage.Budget < 0)
                    return Json(new { success = false, message = "Бюджет не может быть отрицательным" });

                // Устанавливаем порядок автоматически
                var existingCount = await _context.ProjectStages
                    .CountAsync(s => s.ProjectId == stage.ProjectId);
                stage.Order = existingCount + 1;
                stage.Status = StageStatus.NotStarted;
                stage.Progress = 0;
                stage.Spent = 0;

                await _projectService.AddStageAsync(stage);
                await _projectService.UpdateProjectProgressAsync(stage.ProjectId);
                return Json(new { success = true, message = "Этап добавлен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении этапа");
                return Json(new { success = false, message = "Ошибка сервера: " + ex.Message });
            }
        }

        // GET: Projects/FindContractor/5
        public async Task<IActionResult> FindContractor(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
                return NotFound();

            // Ищем всех подрядчиков, сортируем по рейтингу
            var contractors = await _contractorService.SearchContractorsAsync(null, null);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            contractors = contractors
                .Where(c => c.UserId != userId)
                .OrderByDescending(c => c.Rating)
                .ToList();

            ViewBag.ProjectId = id;
            return View(contractors);
        }

        // POST: Projects/InviteContractor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteContractor(int projectId, int contractorId, string message)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var project = await _projectService.GetProjectByIdAsync(projectId);
                if (project == null)
                    return Json(new { success = false, message = "Проект не найден" });

                if (project.UserId != userId)
                    return Json(new { success = false, message = "Нет доступа" });

                // Запрет приглашать самого себя
                var contractorProfile = await _contractorService.GetContractorByIdAsync(contractorId);
                if (contractorProfile != null && contractorProfile.UserId == userId)
                    return Json(new { success = false, message = "Нельзя пригласить самого себя в проект" });

                // Проверяем нет ли уже приглашения
                var hasExisting = await _invitationService.HasExistingInvitationAsync(projectId, contractorId);
                if (hasExisting)
                    return Json(new { success = false, message = "Приглашение уже отправлено" });

                await _invitationService.CreateInvitationAsync(projectId, contractorId, message ?? "", userId);
                return Json(new { success = true, message = "Приглашение отправлено подрядчику" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки приглашения");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

        // POST: Projects/UpdateStageProgress
        [HttpPost]
        public async Task<IActionResult> UpdateStageProgress(int stageId, int progress, decimal spent)
        {
            var stage = await _projectService.GetStageByIdAsync(stageId);
            if (stage == null)
                return NotFound();

            var wasCompleted = stage.Status == StageStatus.Completed;
            stage.Progress = progress;
            stage.Spent = spent;

            if (progress == 100)
            {
                stage.Status = StageStatus.Completed;
                stage.ActualEndDate = DateTime.Now;
            }
            else if (progress > 0)
            {
                stage.Status = StageStatus.InProgress;
                if (!stage.ActualStartDate.HasValue)
                    stage.ActualStartDate = DateTime.Now;
            }

            await _projectService.UpdateStageAsync(stage);
            await _projectService.UpdateProjectProgressAsync(stage.ProjectId);

            // Уведомление при завершении этапа
            if (progress == 100 && !wasCompleted && stage.Project != null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                try
                {
                    await _notificationService.NotifyStageCompletedAsync(
                        stage.Project.UserId, stage.Name, stage.Project.Name, stage.ProjectId);
                }
                catch { }
            }

            return Json(new { success = true });
        }

        // POST: Projects/AddExpense
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExpense(AddExpenseViewModel model)
        {
            // Убираем необязательные поля из валидации
            ModelState.Remove("ReceiptImage");

            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Название расхода обязательно" });
            if (model.Amount <= 0)
                return Json(new { success = false, message = "Сумма должна быть больше 0" });

            try
            {
                ExpenseCategory category;
                if (!Enum.TryParse<ExpenseCategory>(model.Category, out category))
                    category = ExpenseCategory.Other;

                var expense = new Expense
                {
                    ProjectId = model.ProjectId,
                    StageId = model.StageId,
                    Name = model.Name,
                    Description = model.Description ?? "",
                    Amount = model.Amount,
                    Category = category,
                    Date = model.Date == default ? DateTime.Today : model.Date
                };

                if (model.ReceiptImage != null && model.ReceiptImage.Length > 0)
                {
                    if (model.ReceiptImage.Length > 5 * 1024 * 1024)
                        return Json(new { success = false, message = "Размер файла не должен превышать 5 МБ" });

                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/expenses");
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ReceiptImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await model.ReceiptImage.CopyToAsync(stream);
                    expense.ReceiptUrl = "/uploads/expenses/" + fileName;
                }

                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();

                // Пересчитываем Spent из базы
                var project = await _context.Projects.FindAsync(model.ProjectId);
                if (project != null)
                {
                    project.Spent = await _context.Expenses
                        .Where(e => e.ProjectId == model.ProjectId)
                        .SumAsync(e => e.Amount);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Расход добавлен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка добавления расхода");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

        // POST: Projects/UploadStagePhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadStagePhoto(StagePhotoViewModel model)
        {
            if (model.Image == null || model.Image.Length == 0)
                return Json(new { success = false, message = "Не выбрано фото" });

            if (model.Image.Length > 5 * 1024 * 1024)
                return Json(new { success = false, message = "Размер файла не должен превышать 5 МБ" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(model.Image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return Json(new { success = false, message = "Допустимые форматы: JPG, PNG, GIF, WEBP" });

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/stages");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await model.Image.CopyToAsync(stream);

                var photo = new StagePhoto
                {
                    StageId = model.StageId,
                    ImageUrl = "/uploads/stages/" + fileName,
                    Description = model.Description ?? "",
                    UploadedAt = DateTime.Now,
                    UploadedById = userId
                };
                _context.StagePhotos.Add(photo);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Фото загружено", imageUrl = photo.ImageUrl, description = photo.Description });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки фото");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        } }

        // GET: Projects/GetUserProjects
        [HttpGet]
        public async Task<IActionResult> GetUserProjects()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var projects = await _projectService.GetUserProjectsAsync(userId);

            var result = projects.Select(p => new {
                id = p.Id,
                name = p.Name,
                status = p.Status.ToString()
            });

            return Json(result);
        }
    }
}
