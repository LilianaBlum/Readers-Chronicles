using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using System.Security.Claims;

namespace ReadersChronicle.Controllers
{
    public class CommentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CommentController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int commentId)
        {
            var user = await _userManager.GetUserAsync(User);
            var comment = await _context.Comments.FindAsync(commentId);

            if (comment == null || comment.UserId != user.Id)
            {
                return Unauthorized(); // Prevent unauthorized users from deleting other users' comments
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Articles", new { id = comment.ArticleId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int commentId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
            {
                return NotFound();
            }

            // Check if the logged-in user is the one who wrote the comment
            if (comment.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }

            // Pass the comment to the view
            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int commentId, string description)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
            {
                return NotFound();
            }

            // Check if the logged-in user is the one who wrote the comment
            if (comment.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Unauthorized();
            }

            // Update the comment
            comment.Description = description;

            await _context.SaveChangesAsync();

            // Redirect back to the article details page
            return RedirectToAction("Details", "Articles", new { id = comment.ArticleId });
        }
    }
}
