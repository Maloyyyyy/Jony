using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JonyBalls3.Data;
using JonyBalls3.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace JonyBalls3.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            ApplicationDbContext context,
            ILogger<ReviewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ForContractor(int id)
        {
            var contractor = await _context.ContractorProfiles
                .Include(c => c.User)
                .Include(c => c.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contractor == null) return NotFound();

            return View(contractor);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int contractorId, int rating, string comment, int? projectId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ContractorId == contractorId && r.UserId == userId);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "Вы уже оставляли отзыв" });
                }

                var review = new Review
                {
                    ContractorId = contractorId,
                    UserId = userId,
                    ProjectId = projectId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);

                var contractor = await _context.ContractorProfiles
                    .Include(c => c.Reviews)
                    .FirstOrDefaultAsync(c => c.Id == contractorId);

                if (contractor != null)
                {
                    contractor.Rating = (contractor.Reviews.Sum(r => r.Rating) + rating) / (contractor.Reviews.Count + 1.0);
                    contractor.ReviewsCount = contractor.Reviews.Count + 1;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Отзыв добавлен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании отзыва");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }
    }
}
