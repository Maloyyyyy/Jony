using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using JonyBalls3.Models;
using JonyBalls3.Services;
using System.Security.Claims;

namespace JonyBalls3.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ContractorService _contractorService;

        public HomeController(
            ILogger<HomeController> logger,
            ContractorService contractorService)
        {
            _logger = logger;
            _contractorService = contractorService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var contractor = await _contractorService.GetContractorByUserIdAsync(userId);
                ViewBag.IsContractor = contractor != null;
            }
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}