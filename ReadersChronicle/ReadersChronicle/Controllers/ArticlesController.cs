using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using System.Security.Claims;

[Authorize]
public class ArticlesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ArticleService _articleService;

    public ArticlesController(ApplicationDbContext context, UserManager<User> userManager, ArticleService articleService)
    {
        _context = context;
        _userManager = userManager;
        _articleService = articleService;
    }

    public async Task<IActionResult> Index(string viewType = "All")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var viewModel = await _articleService.GetArticlesAsync(user, viewType);

        ViewData["ViewType"] = viewType;

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateArticleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Index");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var success = await _articleService.CreateArticleAsync(model, userId);

        if (!success)
        {
            return NotFound("Book not found in user's library.");
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _articleService.DeleteArticleAsync(id, userId);

        if (!success)
        {
            return NotFound("Article not found or you don't have permission to delete it.");
        }

        return RedirectToAction(nameof(Index), new { viewType = "My" });
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var viewModel = await _articleService.GetArticleDetailsAsync(id, userId);

        if (viewModel == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var viewModel = await _articleService.GetArticleForEditAsync(id, userId);

        if (viewModel == null)
        {
            return NotFound("Article not found or you do not have permission to edit this article.");
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditArticleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _articleService.UpdateArticleAsync(model, userId);

        if (!success)
        {
            return NotFound("Article not found or you do not have permission to edit this article.");
        }

        return RedirectToAction(nameof(Index), new { viewType = "My" });
    }

    [HttpPost]
    public async Task<IActionResult> Like(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        await _articleService.ToggleArticleLikeAsync(id, userId);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(int articleId, string description)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _articleService.AddCommentAsync(articleId, description, userId);

        if (!success)
        {
            return NotFound("Unable to add comment. Please try again.");
        }

        return RedirectToAction(nameof(Details), new { id = articleId });
    }

    [HttpPost]
    public async Task<IActionResult> LikeComment(int commentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _articleService.ToggleCommentLikeAsync(commentId, userId);

        if (!success)
        {
            return NotFound("Comment not found.");
        }

        var comment = await _context.Comments.FindAsync(commentId);

        return RedirectToAction(nameof(Details), new { id = comment.ArticleId });
    }
}
