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

        public ProjectsController(
            ProjectService projectService,
            ContractorService contractorService,
            UserManager<User> userManager,
            ILogger<ProjectsController> logger,
            IWebHostEnvironment env,
            ApplicationDbContext context)
        {
            _projectService = projectService;
            _contractorService = contractorService;
            _userManager = userManager;
            _logger = logger;
            _env = env;
            _context = context;
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
                // Преобразуем строку в Enum безопасно
                RepairType repairType;
                switch (viewModel.RepairType)
                {
                    case "Косметический":
                        repairType = RepairType.Cosmetic;
                        break;
                    case "Капитальный":
                        repairType = RepairType.Capital;
                        break;
                    case "Дизайнерский":
                        repairType = RepairType.Design;
                        break;
                    default:
                        repairType = RepairType.Cosmetic;
                        break;
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Challenge();
                }

                var project = new Project
                {
                    Name = viewModel.Name,
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
                    new ProjectStage {
                        ProjectId = project.Id,
                        Name = "Подготовка помещения",
                        Order = 1,
                        Budget = project.Budget * 0.1m,
                        Status = StageStatus.NotStarted,
                        Progress = 0
                    },
                    new ProjectStage {
                        ProjectId = project.Id,
                        Name = "Черновые работы",
                        Order = 2,
                        Budget = project.Budget * 0.3m,
                        Status = StageStatus.NotStarted,
                        Progress = 0
                    },
                    new ProjectStage {
                        ProjectId = project.Id,
                        Name = "Чистовая отделка",
                        Order = 3,
                        Budget = project.Budget * 0.4m,
                        Status = StageStatus.NotStarted,
                        Progress = 0
                    },
                    new ProjectStage {
                        ProjectId = project.Id,
                        Name = "Уборка и сдача",
                        Order = 4,
                        Budget = project.Budget * 0.2m,
                        Status = StageStatus.NotStarted,
                        Progress = 0
                    }
                };

                foreach (var stage in stages)
                {
                    await _projectService.AddStageAsync(stage);
                }

                return RedirectToAction(nameof(Details), new { id = project.Id });
            }
            return View(viewModel);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }

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
                Status = project.Status.ToString()
            };

            return View(viewModel);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var project = await _projectService.GetProjectByIdAsync(id);
                if (project == null)
                {
                    return NotFound();
                }

                // Преобразуем строку в Enum безопасно
                RepairType repairType;
                switch (viewModel.RepairType)
                {
                    case "Косметический":
                        repairType = RepairType.Cosmetic;
                        break;
                    case "Капитальный":
                        repairType = RepairType.Capital;
                        break;
                    case "Дизайнерский":
                        repairType = RepairType.Design;
                        break;
                    default:
                        repairType = RepairType.Cosmetic;
                        break;
                }

                ProjectStatus status;
                switch (viewModel.Status)
                {
                    case "Планирование":
                        status = ProjectStatus.Planning;
                        break;
                    case "Активный":
                        status = ProjectStatus.Active;
                        break;
                    case "Приостановлен":
                        status = ProjectStatus.Paused;
                        break;
                    case "Завершен":
                        status = ProjectStatus.Completed;
                        break;
                    case "Отменен":
                        status = ProjectStatus.Cancelled;
                        break;
                    default:
                        status = ProjectStatus.Planning;
                        break;
                }

                project.Name = viewModel.Name;
                project.Description = viewModel.Description ?? "";
                project.Area = viewModel.Area;
                project.RepairType = repairType;
                project.Budget = viewModel.Budget;
                project.StartDate = viewModel.StartDate;
                project.EndDate = viewModel.EndDate;
                project.Status = status;
                project.UpdatedAt = DateTime.Now;

                await _projectService.UpdateProjectAsync(project);
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(viewModel);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _projectService.DeleteProjectAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // POST: Projects/AddStage (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStage(ProjectStage stage)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                await _projectService.AddStageAsync(stage);
                // Обновляем общий прогресс проекта
                await _projectService.UpdateProjectProgressAsync(stage.ProjectId);
                return Json(new { success = true, message = "Этап добавлен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении этапа");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

        // GET: Projects/FindContractor/5
        public async Task<IActionResult> FindContractor(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            var contractors = await _contractorService.SearchContractorsAsync(project.RepairType.ToString(), null);
            ViewBag.ProjectId = id;
            return View(contractors);
        }

        // POST: Projects/InviteContractor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteContractor(int projectId, int contractorId, string message)
        {
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project == null)
            {
                return Json(new { success = false, message = "Проект не найден" });
            }

            // Здесь будет логика создания приглашения
            // TODO: Добавить InvitationService

            return Json(new { success = true, message = "Приглашение отправлено" });
        }

        // POST: Projects/UpdateStageProgress
        [HttpPost]
        public async Task<IActionResult> UpdateStageProgress(int stageId, int progress, decimal spent)
        {
            var stage = await _projectService.GetStageByIdAsync(stageId);
            if (stage == null)
            {
                return NotFound();
            }

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
                {
                    stage.ActualStartDate = DateTime.Now;
                }
            }

            await _projectService.UpdateStageAsync(stage);

            // Обновляем общий прогресс проекта
            await _projectService.UpdateProjectProgressAsync(stage.ProjectId);

            return Json(new { success = true });
        }

        // POST: Projects/AddExpense
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExpense(AddExpenseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            try
            {
                var expense = new Expense
                {
                    ProjectId = model.ProjectId,
                    StageId = model.StageId,
                    Name = model.Name,
                    Description = model.Description,
                    Amount = model.Amount,
                    Category = Enum.Parse<ExpenseCategory>(model.Category),
                    Date = model.Date
                };

                if (model.ReceiptImage != null && model.ReceiptImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/expenses");
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ReceiptImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ReceiptImage.CopyToAsync(stream);
                    }
                    expense.ReceiptUrl = "/uploads/expenses/" + fileName;
                }

                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();

                // Обновляем потраченную сумму проекта
                var project = await _projectService.GetProjectByIdAsync(model.ProjectId);
                project.Spent += expense.Amount;
                await _projectService.UpdateProjectAsync(project);

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
            if (!ModelState.IsValid || model.Image == null)
            {
                return Json(new { success = false, message = "Не выбрано фото" });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/stages");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Image.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }

                var photo = new StagePhoto
                {
                    StageId = model.StageId,
                    ImageUrl = "/uploads/stages/" + fileName,
                    Description = model.Description,
                    UploadedAt = DateTime.Now,
                    UploadedById = userId
                };

                _context.StagePhotos.Add(photo);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Фото загружено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки фото");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

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
