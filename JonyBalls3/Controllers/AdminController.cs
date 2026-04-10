using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JonyBalls3.Models;
using JonyBalls3.Data;
using System.Security.Claims;

namespace JonyBalls3.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        // ─── DASHBOARD ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalUsers       = await _userManager.Users.CountAsync(),
                TotalContractors = await _context.ContractorProfiles.CountAsync(),
                TotalProjects    = await _context.Projects.CountAsync(),
                ActiveProjects   = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Active),
                TotalExpenses    = await _context.Expenses.SumAsync(e => (decimal?)e.Amount) ?? 0,
                RecentUsers      = await _userManager.Users.OrderByDescending(u => u.CreatedAt).Take(5).ToListAsync(),
                RecentProjects   = await _context.Projects.Include(p => p.User).OrderByDescending(p => p.CreatedAt).Take(5).ToListAsync()
            };
            return View(vm);
        }

        // ─── ПОЛЬЗОВАТЕЛИ ────────────────────────────────────────────────────────
        public async Task<IActionResult> Users(string? search = null)
        {
            var query = _userManager.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.Email!.Contains(search) || u.FirstName.Contains(search) || u.LastName.Contains(search));
            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

            // Получаем роли для каждого пользователя
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);

            ViewBag.UserRoles = userRoles;
            ViewBag.Search = search;
            return View(users);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            var projects = await _context.Projects.Where(p => p.UserId == id).ToListAsync();
            var contractor = await _context.ContractorProfiles.FirstOrDefaultAsync(c => c.UserId == id);
            ViewBag.Roles = roles;
            ViewBag.Projects = projects;
            ViewBag.Contractor = contractor;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string firstName, string lastName, string email, string? phone, bool isAdmin, bool isContractor, bool isBlocked)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "Пользователь не найден" });

            user.FirstName = firstName ?? "";
            user.LastName  = lastName  ?? "";
            user.Email     = email;
            user.UserName  = email;
            user.NormalizedEmail    = email.ToUpperInvariant();
            user.NormalizedUserName = email.ToUpperInvariant();
            user.PhoneNumber = phone;
            user.LockoutEnabled = isBlocked;
            if (isBlocked)
                user.LockoutEnd = DateTimeOffset.MaxValue;
            else
                user.LockoutEnd = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            // Управление ролями
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (isAdmin && !currentRoles.Contains("Admin"))
            {
                await EnsureRoleExists("Admin");
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            else if (!isAdmin && currentRoles.Contains("Admin"))
                await _userManager.RemoveFromRoleAsync(user, "Admin");

            if (isContractor && !currentRoles.Contains("Contractor"))
            {
                await EnsureRoleExists("Contractor");
                await _userManager.AddToRoleAsync(user, "Contractor");
            }
            else if (!isContractor && currentRoles.Contains("Contractor"))
                await _userManager.RemoveFromRoleAsync(user, "Contractor");

            _logger.LogInformation("Admin edited user {Email}", user.Email);
            return Json(new { success = true, message = "Пользователь обновлён" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
                return Json(new { success = false, message = "Нельзя удалить собственный аккаунт" });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "Пользователь не найден" });

            // Удаляем связанные данные
            var contractor = await _context.ContractorProfiles.FirstOrDefaultAsync(c => c.UserId == id);
            if (contractor != null)
            {
                var portfolio = _context.PortfolioItems.Where(p => p.ContractorId == contractor.Id);
                _context.PortfolioItems.RemoveRange(portfolio);
                _context.ContractorProfiles.Remove(contractor);
            }
            var projects = _context.Projects.Where(p => p.UserId == id);
            _context.Projects.RemoveRange(projects);
            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            _logger.LogInformation("Admin deleted user {Email}", user.Email);
            return Json(new { success = true, message = "Пользователь удалён" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "Пользователь не найден" });

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                user.LockoutEnd = null;
                await _userManager.UpdateAsync(user);
                return Json(new { success = true, message = "Пользователь разблокирован", blocked = false });
            }
            else
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await _userManager.UpdateAsync(user);
                return Json(new { success = true, message = "Пользователь заблокирован", blocked = true });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "Пользователь не найден" });
            await EnsureRoleExists("Admin");
            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (result.Succeeded)
                return Json(new { success = true, message = $"{user.Email} назначен администратором" });
            return Json(new { success = false, message = "Ошибка назначения роли" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdmin(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
                return Json(new { success = false, message = "Нельзя снять роль с себя" });
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "Пользователь не найден" });
            await _userManager.RemoveFromRoleAsync(user, "Admin");
            return Json(new { success = true, message = "Роль администратора снята" });
        }

        // ─── ПОДРЯДЧИКИ ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Contractors(string? search = null)
        {
            var query = _context.ContractorProfiles.Include(c => c.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.CompanyName.Contains(search) || c.Specialization.Contains(search));
            var contractors = await query.OrderByDescending(c => c.Rating).ToListAsync();
            ViewBag.Search = search;
            return View(contractors);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditContractor(int id, string companyName, string specialization,
            string? description, int experienceYears, decimal hourlyRate, decimal rating, string status)
        {
            var contractor = await _context.ContractorProfiles.FindAsync(id);
            if (contractor == null) return Json(new { success = false, message = "Подрядчик не найден" });

            if (string.IsNullOrWhiteSpace(companyName))
                return Json(new { success = false, message = "Название компании обязательно" });
            if (string.IsNullOrWhiteSpace(specialization))
                return Json(new { success = false, message = "Специализация обязательна" });
            if (rating < 0 || rating > 5)
                return Json(new { success = false, message = "Рейтинг должен быть от 0 до 5" });

            contractor.CompanyName    = companyName;
            contractor.Specialization = specialization;
            contractor.Description    = description ?? "";
            contractor.ExperienceYears = Math.Max(0, experienceYears);
            contractor.HourlyRate     = Math.Max(0, hourlyRate);
            contractor.Rating         = rating;
            contractor.Status         = status switch {
                "Available" or "Свободен"  => ContractorStatus.Available,
                "Busy"      or "Занят"      => ContractorStatus.Busy,
                _                                => ContractorStatus.Inactive
            };

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Подрядчик обновлён" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContractor(int id)
        {
            var contractor = await _context.ContractorProfiles.FindAsync(id);
            if (contractor == null) return Json(new { success = false, message = "Подрядчик не найден" });

            // Снимаем роль Contractor с пользователя
            var user = await _userManager.FindByIdAsync(contractor.UserId);
            if (user != null)
                await _userManager.RemoveFromRoleAsync(user, "Contractor");

            var portfolio = _context.PortfolioItems.Where(p => p.ContractorId == id);
            _context.PortfolioItems.RemoveRange(portfolio);
            _context.ContractorProfiles.Remove(contractor);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Подрядчик удалён" });
        }

        // ─── ПРОЕКТЫ ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Projects(string? search = null, string? status = null)
        {
            var query = _context.Projects.Include(p => p.User).Include(p => p.Contractor).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search));
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProjectStatus>(status, out var ps))
                query = query.Where(p => p.Status == ps);
            var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            ViewBag.Search = search;
            ViewBag.Status = status;
            return View(projects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProject(int id, string name, string? description,
            decimal budget, string status, DateTime? startDate, DateTime? endDate)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return Json(new { success = false, message = "Проект не найден" });

            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Название проекта обязательно" });

            project.Name        = name;
            project.Description = description ?? "";
            project.Budget      = Math.Max(0, budget);
            project.StartDate   = startDate;
            project.EndDate     = endDate;
            project.UpdatedAt   = DateTime.Now;
            project.Status      = status switch {
                "Planning"  or "Планирование"  => ProjectStatus.Planning,
                "Active"    or "Активный"      => ProjectStatus.Active,
                "Paused"    or "Приостановлен" => ProjectStatus.Paused,
                "Completed" or "Завершён"     => ProjectStatus.Completed,
                "Cancelled" or "Отменён"      => ProjectStatus.Cancelled,
                _                                => project.Status
            };

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Проект обновлён" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return Json(new { success = false, message = "Проект не найден" });

            var stages = _context.ProjectStages.Where(s => s.ProjectId == id);
            _context.ProjectStages.RemoveRange(stages);
            var expenses = _context.Expenses.Where(e => e.ProjectId == id);
            _context.Expenses.RemoveRange(expenses);
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Проект удалён" });
        }

        // ─── СТАТИСТИКА ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Stats()
        {
            var vm = new AdminStatsViewModel
            {
                UsersByMonth     = await GetUsersByMonth(),
                ProjectsByStatus = await GetProjectsByStatus(),
                ExpensesByCategory = await GetExpensesByCategory(),
                TopContractors   = await _context.ContractorProfiles
                    .OrderByDescending(c => c.Rating)
                    .Take(10)
                    .ToListAsync()
            };
            return View(vm);
        }

        // ─── ВСПОМОГАТЕЛЬНЫЕ ─────────────────────────────────────────────────────
        private async Task EnsureRoleExists(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        private async Task<Dictionary<string, int>> GetUsersByMonth()
        {
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var users = await _userManager.Users
                .Where(u => u.CreatedAt >= sixMonthsAgo)
                .ToListAsync();
            return users
                .GroupBy(u => u.CreatedAt.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private async Task<Dictionary<string, int>> GetProjectsByStatus()
        {
            var projects = await _context.Projects.ToListAsync();
            return projects
                .GroupBy(p => p.Status switch {
                    ProjectStatus.Planning  => "Планирование",
                    ProjectStatus.Active    => "Активный",
                    ProjectStatus.Paused    => "Приостановлен",
                    ProjectStatus.Completed => "Завершён",
                    ProjectStatus.Cancelled => "Отменён",
                    _                       => p.Status.ToString()
                })
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private async Task<Dictionary<string, decimal>> GetExpensesByCategory()
        {
            var expenses = await _context.Expenses.ToListAsync();
            return expenses
                .GroupBy(e => e.Category switch {
                    ExpenseCategory.Materials => "Материалы",
                    ExpenseCategory.Labor     => "Работы",
                    ExpenseCategory.Tools     => "Инструменты",
                    ExpenseCategory.Delivery  => "Доставка",
                    ExpenseCategory.Other     => "Прочее",
                    _                         => e.Category.ToString()
                })
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
        }
    }

    // ─── ViewModels ──────────────────────────────────────────────────────────────
    public class AdminDashboardViewModel
    {
        public int TotalUsers       { get; set; }
        public int TotalContractors { get; set; }
        public int TotalProjects    { get; set; }
        public int ActiveProjects   { get; set; }
        public decimal TotalExpenses { get; set; }
        public List<User> RecentUsers       { get; set; } = new();
        public List<Project> RecentProjects { get; set; } = new();
    }

    public class AdminStatsViewModel
    {
        public Dictionary<string, int>     UsersByMonth       { get; set; } = new();
        public Dictionary<string, int>     ProjectsByStatus   { get; set; } = new();
        public Dictionary<string, decimal> ExpensesByCategory { get; set; } = new();
        public List<ContractorProfile>     TopContractors     { get; set; } = new();
    }
}
