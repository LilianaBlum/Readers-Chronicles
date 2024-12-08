using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using System.Security.Claims;

[Authorize]
public class ArticlesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public ArticlesController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string viewType = "All")
    {
        // Get the current user
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account"); // Redirect to login if user is not authenticated
        }

        // Get all articles from the database
        var allArticles = await _context.Articles.Include(a => a.User)
                                                 .OrderByDescending(a => a.TimeCreated)
                                                 .ToListAsync();

        // Filter articles based on the viewType
        var filteredArticles = viewType == "My"
            ? allArticles.Where(a => a.UserId == user.Id).ToList()
            : allArticles;

        // Build the ViewModel
        var viewModel = new ArticlesIndexViewModel
        {
            Articles = filteredArticles,
            UserBooks = await _context.UserBooks.Where(ub => ub.UserID == user.Id).ToListAsync()
        };

        ViewData["ViewType"] = viewType; // To highlight the active navigation item

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
        var userBook = await _context.UserBooks.FirstOrDefaultAsync(ub => ub.UserBookID == model.UserBookID && ub.UserID == userId);

        if (userBook == null)
        {
            return NotFound("Book not found in user's library.");
        }

        var article = new Article
        {
            UserId = userId,
            Title = model.Title,
            Description = model.Description,
            Picture = userBook.Picture,
            PictureMimeType = userBook.PictureMimeType,
            TimeCreated = DateTime.UtcNow
        };

        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // Get the current user
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account"); // Redirect to login if user is not authenticated
        }

        // Find the article by ID
        var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

        if (article == null)
        {
            return NotFound("Article not found or you don't have permission to delete it.");
        }

        // Remove the article
        _context.Articles.Remove(article);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { viewType = "My" });
    }

    public async Task<IActionResult> Details(int id)
    {
        var article = await _context.Articles
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var userLiked = await _context.ArticleRatings
            .AnyAsync(r => r.ArticleId == id && r.UserId == userId);

        var totalLikes = await _context.ArticleRatings
            .CountAsync(r => r.ArticleId == id);

        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.ArticleId == id)
            .ToListAsync();

        var userLikedComments = await _context.CommentRatings
            .Where(cr => cr.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            .ToListAsync();

        var viewModel = new ArticleDetailsViewModel
        {
            Id = article.Id,
            Title = article.Title,
            Description = article.Description,
            Picture = article.Picture,
            PictureMimeType = article.PictureMimeType,
            UserName = article.User.UserName,
            TimeCreated = article.TimeCreated,
            TotalLikes = totalLikes,
            UserLiked = userLiked,
            Article = article,
            Comments = comments,
            UserLikedComments = userLikedComments
        };

        return View(viewModel);
    }


    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (article == null)
        {
            return NotFound("Article not found or you do not have permission to edit this article.");
        }

        var viewModel = new EditArticleViewModel
        {
            Id = article.Id,
            Description = article.Description
        };

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
        var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);

        if (article == null)
        {
            return NotFound("Article not found or you do not have permission to edit this article.");
        }

        article.Description = model.Description;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { viewType = "My" });
    }

    [HttpPost]
    public async Task<IActionResult> Like(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var existingLike = await _context.ArticleRatings
            .FirstOrDefaultAsync(r => r.ArticleId == id && r.UserId == userId);

        if (existingLike != null)
        {
            _context.ArticleRatings.Remove(existingLike);
        }
        else
        {
            var like = new ArticleRating
            {
                ArticleId = id,
                UserId = userId
            };
            _context.ArticleRatings.Add(like);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(int articleId, string description)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var comment = new Comment
        {
            ArticleId = articleId,
            UserId = user.Id,
            Description = description,
            CreatedAt = DateTime.Now
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = articleId });
    }

    [HttpPost]
    public async Task<IActionResult> LikeComment(int commentId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
        {
            return NotFound();
        }

        var existingRating = await _context.CommentRatings
            .FirstOrDefaultAsync(cr => cr.CommentId == commentId && cr.UserId == user.Id);

        if (existingRating == null)
        {
            var commentRating = new CommentRating
            {
                CommentId = commentId,
                UserId = user.Id
            };

            _context.CommentRatings.Add(commentRating);
        }
        else
        {
            _context.CommentRatings.Remove(existingRating);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = comment.ArticleId });
    }
}
