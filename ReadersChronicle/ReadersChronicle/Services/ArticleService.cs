using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Models;

public class ArticleService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public ArticleService(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ArticlesIndexViewModel> GetArticlesAsync(User user, string viewType)
    {
        var allArticles = await _context.Articles
            .Include(a => a.User)
            .OrderByDescending(a => a.TimeCreated)
            .ToListAsync();

        var filteredArticles = viewType == "My"
            ? allArticles.Where(a => a.UserId == user.Id).ToList()
            : allArticles;

        var viewModel = new ArticlesIndexViewModel
        {
            Articles = filteredArticles,
            UserBooks = await _context.UserBooks.Where(ub => ub.UserID == user.Id).ToListAsync()
        };

        return viewModel;
    }

    public async Task<bool> CreateArticleAsync(CreateArticleViewModel model, string userId)
    {
        var userBook = await _context.UserBooks.FirstOrDefaultAsync(ub => ub.UserBookID == model.UserBookID && ub.UserID == userId);

        if (userBook == null)
        {
            return false;
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

        return true;
    }

    public async Task<bool> DeleteArticleAsync(int articleId, string userId)
    {
        var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == articleId && a.UserId == userId);

        if (article == null)
        {
            return false;
        }

        _context.Articles.Remove(article);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<ArticleDetailsViewModel?> GetArticleDetailsAsync(int articleId, string userId)
    {
        var article = await _context.Articles
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == articleId);

        if (article == null)
        {
            return null;
        }

        var userLiked = await _context.ArticleRatings
            .AnyAsync(r => r.ArticleId == articleId && r.UserId == userId);

        var totalLikes = await _context.ArticleRatings
            .CountAsync(r => r.ArticleId == articleId);

        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.ArticleId == articleId)
            .ToListAsync();

        var userLikedComments = await _context.CommentRatings
            .Where(cr => cr.UserId == userId)
            .ToListAsync();

        return new ArticleDetailsViewModel
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
    }

    public async Task<EditArticleViewModel?> GetArticleForEditAsync(int articleId, string userId)
    {
        var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == articleId && a.UserId == userId);

        if (article == null)
        {
            return null;
        }

        return new EditArticleViewModel
        {
            Id = article.Id,
            Description = article.Description
        };
    }

    public async Task<bool> UpdateArticleAsync(EditArticleViewModel model, string userId)
    {
        var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);

        if (article == null)
        {
            return false;
        }

        article.Description = model.Description;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleArticleLikeAsync(int articleId, string userId)
    {
        var existingLike = await _context.ArticleRatings
            .FirstOrDefaultAsync(r => r.ArticleId == articleId && r.UserId == userId);

        if (existingLike != null)
        {
            _context.ArticleRatings.Remove(existingLike);
        }
        else
        {
            var like = new ArticleRating
            {
                ArticleId = articleId,
                UserId = userId
            };
            _context.ArticleRatings.Add(like);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddCommentAsync(int articleId, string description, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var comment = new Comment
        {
            ArticleId = articleId,
            UserId = userId,
            Description = description,
            CreatedAt = DateTime.Now
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleCommentLikeAsync(int commentId, string userId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
        {
            return false;
        }

        var existingRating = await _context.CommentRatings
            .FirstOrDefaultAsync(cr => cr.CommentId == commentId && cr.UserId == userId);

        if (existingRating == null)
        {
            var commentRating = new CommentRating
            {
                CommentId = commentId,
                UserId = userId
            };

            _context.CommentRatings.Add(commentRating);
        }
        else
        {
            _context.CommentRatings.Remove(existingRating);
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
