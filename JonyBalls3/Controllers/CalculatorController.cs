using Microsoft.AspNetCore.Mvc;
using JonyBalls3.Services;
using JonyBalls3.Models.Calculator;
using Microsoft.AspNetCore.Authorization;

namespace JonyBalls3.Controllers
{
    public class CalculatorController : Controller
    {
        private readonly CalculatorService _calculatorService;
        private readonly ILogger<CalculatorController> _logger;

        public CalculatorController(
            CalculatorService calculatorService,
            ILogger<CalculatorController> logger)
        {
            _calculatorService = calculatorService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(new CalculationRequest());
        }

        [HttpPost]
        public IActionResult Calculate(CalculationRequest request)
        {
            if (ModelState.IsValid)
            {
                var result = _calculatorService.Calculate(request);
                return View("Result", result);
            }
            return View("Index", request);
        }

        public IActionResult SaveToProject(CalculationResult result)
        {
            // Сохраняем расчет в сессию для создания проекта
            HttpContext.Session.SetString("SavedCalculation", System.Text.Json.JsonSerializer.Serialize(result));
            return RedirectToAction("Create", "Projects", new { fromCalculator = true });
        }
    }
}