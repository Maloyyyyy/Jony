using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using JonyBalls3.Models;
using JonyBalls3.Services;
using JonyBalls3.Data;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace JonyBalls3.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ContractorService _contractorService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ContractorService contractorService,
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _contractorService = contractorService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Пользователь {Email} вошел в систему.", model.Email);
                return RedirectToAction("Index", "Projects");
            }
            
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            
            ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("Пользователь {Email} зарегистрирован.", model.Email);
                return RedirectToAction("Index", "Projects");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> BecomeContractor()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Challenge();
            }

            var existingProfile = await _contractorService.GetContractorByUserIdAsync(userId);
            
            if (existingProfile != null)
            {
                return RedirectToAction("MyProfile", "Contractors");
            }
            
            // ЯВНО УКАЗЫВАЕМ ПУТЬ К МОДЕЛИ
            var model = new JonyBalls3.Models.ContractorProfileViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BecomeContractor(JonyBalls3.Models.ContractorProfileViewModel model, List<IFormFile> portfolioFiles)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Challenge();
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }
                
                var existingProfile = await _contractorService.GetContractorByUserIdAsync(userId);
                if (existingProfile != null)
                {
                    return RedirectToAction("MyProfile", "Contractors");
                }
                
                var profile = new ContractorProfile
                {
                    UserId = userId,
                    CompanyName = model.CompanyName,
                    Specialization = model.Specialization,
                    Description = model.Description ?? "",
                    ExperienceYears = model.ExperienceYears,
                    Phone = model.Phone ?? user.PhoneNumber ?? "",
                    Website = model.Website ?? "",
                    HourlyRate = model.HourlyRate,
                    Status = ContractorStatus.Available,
                    CreatedAt = DateTime.Now
                };

                var createdProfile = await _contractorService.CreateContractorProfileAsync(profile);

                if (!await _userManager.IsInRoleAsync(user, "Contractor"))
                {
                    await _userManager.AddToRoleAsync(user, "Contractor");
                }

                if (portfolioFiles != null && portfolioFiles.Any())
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/portfolio");
                    Directory.CreateDirectory(uploadsFolder);

                    foreach (var file in portfolioFiles)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(uploadsFolder, fileName);
                            
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var portfolioItem = new PortfolioItem
                            {
                                ContractorId = createdProfile.Id,
                                Title = model.CompanyName + " - работа",
                                Description = "",
                                ImageUrl = "/uploads/portfolio/" + fileName,
                                UploadedAt = DateTime.Now,
                                CompletedDate = DateTime.Now
                            };

                            _context.PortfolioItems.Add(portfolioItem);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("User {Email} became a contractor", user.Email);
                
                TempData["Success"] = "Вы успешно стали подрядчиком!";
                return RedirectToAction("MyProfile", "Contractors");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Пользователь вышел из системы.");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [Display(Name = "Имя")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 50 символов")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Фамилия обязательна")]
        [Display(Name = "Фамилия")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилия должна быть от 2 до 50 символов")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Подтвердите пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = "";
    }
}