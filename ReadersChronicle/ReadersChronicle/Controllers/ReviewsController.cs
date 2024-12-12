using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using System.Security.Claims;

namespace ReadersChronicle.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public IActionResult WriteReview()
        {
            TempData["SuccessMessage"] = null;
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> WriteReview(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Review content cannot be empty." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var review = new Review
            {
                UserID = userId,
                Content = content,
                Timestamp = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thank you for your review!" });
        }

        [HttpGet]
        public async Task<IActionResult> ViewReviews()
        {
            var reviews = await _context.Reviews
       .Include(r => r.User)
       .OrderByDescending(r => r.Timestamp)
       .Select(r => new
       {
           r.ReviewID,
           r.Content,
           r.Timestamp,
           UserName = r.User.UserName
       })
       .ToListAsync();

            var reviewViewModels = reviews.Select(r => new ReviewViewModel
            {
                ReviewID = r.ReviewID,
                Content = r.Content,
                Timestamp = r.Timestamp,
                UserName = r.UserName
            }).ToList();

            return View(reviewViewModels);
        }
    }
}
