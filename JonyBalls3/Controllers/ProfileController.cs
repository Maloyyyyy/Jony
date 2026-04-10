using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JonyBalls3.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace JonyBalls3.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            UserManager<User> userManager,
            IWebHostEnvironment environment,
            ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null) return NotFound();

            return View(user);
        }

        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model, IFormFile? avatarFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Убираем поля Identity из валидации
            ModelState.Remove("PasswordHash");
            ModelState.Remove("SecurityStamp");
            ModelState.Remove("ConcurrencyStamp");
            ModelState.Remove("NormalizedEmail");
            ModelState.Remove("NormalizedUserName");
            ModelState.Remove("UserName");
            ModelState.Remove("Email");

            // Валидация файла
            if (avatarFile != null && avatarFile.Length > 0)
            {
                if (avatarFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("avatarFile", "Размер файла не должен превышать 5 МБ");
                    return View(user);
                }
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("avatarFile", "Допустимые форматы: JPG, PNG, GIF, WEBP");
                    return View(user);
                }
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/avatars");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await avatarFile.CopyToAsync(stream);
                user.AvatarUrl = "/uploads/avatars/" + fileName;
            }

            user.FirstName = model.FirstName ?? "";
            user.LastName = model.LastName ?? "";
            user.PhoneNumber = model.PhoneNumber;
            user.Bio = model.Bio ?? "";
            user.Location = model.Location ?? "";
            user.BirthDate = model.BirthDate;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Профиль обновлён";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(user);
        }
    }
}
