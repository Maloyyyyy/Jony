using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace JonyBalls3.Controllers
{
    [Authorize] // Только для авторизованных
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Projects");
        }
    }
}
