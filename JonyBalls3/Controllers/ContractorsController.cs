using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JonyBalls3.Services;
using JonyBalls3.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

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
        public async Task<IActionResult> Index(string specialization = null, decimal? maxRate = null)
        {
            var contractors = await _contractorService.SearchContractorsAsync(specialization, maxRate);
            ViewBag.Specialization = specialization;
            ViewBag.MaxRate = maxRate;
            return View(contractors);
        }

        // GET: Contractors/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var contractor = await _contractorService.GetContractorByIdAsync(id);
            if (contractor == null)
            {
                return NotFound();
            }
            return View(contractor);
        }

        [Authorize]
        public async Task<IActionResult> MyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await _contractorService.GetContractorByUserIdAsync(userId);
            
            if (profile == null)
            {
                return RedirectToAction("BecomeContractor", "Account");
            }
            
            return View(profile);
        }
[Authorize]
public async Task<IActionResult> EditProfile()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var profile = await _contractorService.GetContractorByUserIdAsync(userId);
    
    if (profile == null)
    {
        return RedirectToAction("BecomeContractor", "Account");
    }
    
    return View(profile);
}

[HttpPost]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditProfile(ContractorProfile profile)
{
    if (ModelState.IsValid)
    {
        await _contractorService.UpdateContractorProfileAsync(profile);
        TempData["Success"] = "Профиль обновлен";
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
    {
        return RedirectToAction("BecomeContractor", "Account");
    }
    
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await _contractorService.GetContractorByUserIdAsync(userId);
        
        if (profile == null || file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Ошибка загрузки" });
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/portfolio");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

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
        {
            return Json(new { success = false, message = "Ошибка доступа" });
        }

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