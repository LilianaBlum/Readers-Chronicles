using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReadersChronicle.Data;
using ReadersChronicle.Services;
using System.Security.Claims;

namespace ReadersChronicle.Controllers
{
    public class CommentController : Controller
    {
        private readonly CommentService _commentService;
        private readonly UserManager<User> _userManager;

        public CommentController(CommentService commentService, UserManager<User> userManager)
        {
            _commentService = commentService;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }


            var articleID = await _commentService.GetArticleID(commentId);

            var success = await _commentService.DeleteCommentAsync(commentId, userId);

            if (!success)
            {
                return Unauthorized();
            }


            return RedirectToAction("Details", "Articles", new { id = articleID });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var comment = await _commentService.GetCommentForEditAsync(commentId, userId);

            if (comment == null)
            {
                return Unauthorized();
            }

            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int commentId, string description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var success = await _commentService.UpdateCommentAsync(commentId, description, userId);

            if (!success)
            {
                return Unauthorized();
            }

            return RedirectToAction("Details", "Articles", new { id = commentId });
        }
    }
}
