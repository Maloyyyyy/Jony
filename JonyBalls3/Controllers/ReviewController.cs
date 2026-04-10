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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int contractorId, int rating, string comment, int? projectId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Нельзя оставить отзыв самому себе
                var contractor = await _context.ContractorProfiles
                    .Include(c => c.Reviews)
                    .FirstOrDefaultAsync(c => c.Id == contractorId);

                if (contractor == null)
                    return Json(new { success = false, message = "Подрядчик не найден" });

                if (contractor.UserId == userId)
                    return Json(new { success = false, message = "Нельзя оставить отзыв самому себе" });

                // Один отзыв на одного пользователя
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ContractorId == contractorId && r.UserId == userId);

                if (existingReview != null)
                    return Json(new { success = false, message = "Вы уже оставляли отзыв этому подрядчику" });

                if (rating < 1 || rating > 5)
                    return Json(new { success = false, message = "Оценка должна быть от 1 до 5" });

                if (string.IsNullOrWhiteSpace(comment))
                    return Json(new { success = false, message = "Комментарий не может быть пустым" });

                var review = new Review
                {
                    ContractorId = contractorId,
                    UserId = userId!,
                    ProjectId = projectId,
                    Rating = rating,
                    Comment = comment.Trim(),
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);

                // Пересчитываем рейтинг
                var allRatings = contractor.Reviews.Select(r => r.Rating).ToList();
                allRatings.Add(rating);
                contractor.Rating = allRatings.Average();
                contractor.ReviewsCount = allRatings.Count;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Отзыв успешно добавлен!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании отзыва");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (review == null)
                    return Json(new { success = false, message = "Отзыв не найден" });

                // Только автор может удалить свой отзыв
                if (review.UserId != userId)
                    return Json(new { success = false, message = "Нет доступа к удалению этого отзыва" });

                _context.Reviews.Remove(review);

                // Пересчитываем рейтинг подрядчика
                var contractor = await _context.ContractorProfiles
                    .Include(c => c.Reviews)
                    .FirstOrDefaultAsync(c => c.Id == review.ContractorId);

                if (contractor != null)
                {
                    var remainingReviews = contractor.Reviews.Where(r => r.Id != id).ToList();
                    contractor.Rating = remainingReviews.Any() ? remainingReviews.Average(r => r.Rating) : 0;
                    contractor.ReviewsCount = remainingReviews.Count;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Отзыв удалён" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении отзыва");
                return Json(new { success = false, message = "Ошибка сервера" });
            }
        }
    }
}
