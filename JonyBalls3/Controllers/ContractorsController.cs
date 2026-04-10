using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JonyBalls3.Services;
using JonyBalls3.Models;
using System.Security.Claims;

namespace JonyBalls3.Controllers
{
    public class ContractorsController : Controller
    {
        private readonly ContractorService _contractorService;
        private readonly ILogger<ContractorsController> _logger;

        public ContractorsController(
            ContractorService contractorService,
            ILogger<ContractorsController> logger)
        {
            _contractorService = contractorService;
            _logger = logger;
        }

        // GET: Contractors
        public async Task<IActionResult> Index(string? specialization = null, decimal? maxRate = null, string? sortBy = "rating")
        {
            var contractors = await _contractorService.SearchContractorsAsync(specialization, maxRate);

            // Скрываем текущего пользователя из каталога
            if (User.Identity!.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                contractors = contractors.Where(c => c.UserId != userId).ToList();
            }

            // Сортировка
            contractors = sortBy switch
            {
                "rate_asc"  => contractors.OrderBy(c => c.HourlyRate).ToList(),
                "rate_desc" => contractors.OrderByDescending(c => c.HourlyRate).ToList(),
                "exp"       => contractors.OrderByDescending(c => c.ExperienceYears).ToList(),
                _           => contractors.OrderByDescending(c => c.Rating).ToList()
            };

            ViewBag.Specialization = specialization;
            ViewBag.MaxRate = maxRate;
            ViewBag.SortBy = sortBy;
            return View(contractors);
        }

        // GET: Contractors/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var contractor = await _contractorService.GetContractorByIdAsync(id);
            if (contractor == null)
                return NotFound();
            return View(contractor);
        }

        [Authorize]
        public async Task<IActionResult> MyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await _contractorService.GetContractorByUserIdAsync(userId);

            if (profile == null)
                return RedirectToAction("BecomeContractor", "Account");

            return View(profile);
        }

        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await _contractorService.GetContractorByUserIdAsync(userId);

            if (profile == null)
                return RedirectToAction("BecomeContractor", "Account");

            return View(profile);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ContractorProfile profile, IFormFile? avatarFile)
        {
            // Убираем необязательные навигационные свойства из валидации
            ModelState.Remove("User");
            ModelState.Remove("PortfolioItems");
            ModelState.Remove("Reviews");

            if (string.IsNullOrWhiteSpace(profile.CompanyName))
                ModelState.AddModelError("CompanyName", "Название компании обязательно");

            if (string.IsNullOrWhiteSpace(profile.Specialization))
                ModelState.AddModelError("Specialization", "Специализация обязательна");

            if (profile.HourlyRate < 0)
                ModelState.AddModelError("HourlyRate", "Ставка не может быть отрицательной");

            if (profile.ExperienceYears < 0)
                ModelState.AddModelError("ExperienceYears", "Опыт не может быть отрицательным");

            if (ModelState.IsValid)
            {
                // Загрузка аватара
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    if (avatarFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "Размер файла не должен превышать 5 МБ");
                        return View(profile);
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("", "Допустимые форматы: JPG, PNG, GIF, WEBP");
                        return View(profile);
                    }

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/avatars");
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = Guid.NewGuid().ToString() + ext;
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await avatarFile.CopyToAsync(stream);
                    profile.AvatarUrl = "/uploads/avatars/" + fileName;
                }

                await _contractorService.UpdateContractorProfileAsync(profile);
                TempData["Success"] = "Профиль обновлён";
                return RedirectToAction("MyProfile");
            }
            return View(profile);
        }

        [Authorize]
        public async Task<IActionResult> ManagePortfolio()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await _contractorService.GetContractorByUserIdAsync(userId);

            if (profile == null)
                return RedirectToAction("BecomeContractor", "Account");

            var portfolio = await _contractorService.GetPortfolioAsync(profile.Id);
            return View(portfolio);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPortfolio(string title, string description, IFormFile file, DateTime completedDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                    return Json(new { success = false, message = "Название обязательно" });

                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "Файл не выбран" });

                if (file.Length > 5 * 1024 * 1024)
                    return Json(new { success = false, message = "Размер файла не должен превышать 5 МБ" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                    return Json(new { success = false, message = "Допустимые форматы: JPG, PNG, GIF, WEBP" });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var profile = await _contractorService.GetContractorByUserIdAsync(userId);

                if (profile == null)
                    return Json(new { success = false, message = "Профиль подрядчика не найден" });

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/portfolio");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var portfolioItem = new PortfolioItem
                {
                    ContractorId = profile.Id,
                    Title = title,
                    Description = description ?? "",
                    ImageUrl = "/uploads/portfolio/" + fileName,
                    UploadedAt = DateTime.Now,
                    CompletedDate = completedDate
                };
                await _contractorService.AddPortfolioItemAsync(portfolioItem);

                return Json(new { success = true, message = "Файл загружен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки портфолио");
                return Json(new { success = false, message = "Ошибка загрузки" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePortfolio(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var profile = await _contractorService.GetContractorByUserIdAsync(userId);

                if (profile == null)
                    return Json(new { success = false, message = "Ошибка доступа" });

                await _contractorService.DeletePortfolioItemAsync(id, profile.Id);
                return Json(new { success = true, message = "Удалено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления");
                return Json(new { success = false, message = "Ошибка удаления" });
            }
        }
    }
}
