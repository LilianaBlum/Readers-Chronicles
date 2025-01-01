using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Models;

/// <summary>
/// Provides services for managing articles, including retrieving, creating, deleting, and viewing detailed information about articles.
/// </summary>
public class ArticleService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    /// <summary>
    /// Initializes a new instance of the ArticleService class.
    /// </summary>
    /// <param name="context">The database context used to interact with the application's data.</param>
    /// <param name="userManager">The user manager used for handling user-related operations.</param>
    public ArticleService(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

     /// <summary>
    /// Retrieves a list of articles for a user, filtered by the specified view type (e.g., "My" for the user's own articles).
    /// </summary>
    /// <param name="user">The user whose articles are being retrieved.</param>
    /// <param name="viewType">The type of articles to view ("All" or "My" articles).</param>
    /// <returns>A task that represents the asynchronous operation, containing the articles and user books in the form of an <see cref="ArticlesIndexViewModel"/>.</returns>
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

    /// <summary>
    /// Creates a new article based on the provided data and user ID.
    /// </summary>
    /// <param name="model">The data model for creating a new article.</param>
    /// <param name="userId">The ID of the user creating the article.</param>
    /// <returns>A task that represents the asynchronous operation, indicating success or failure of the article creation.</returns>
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

    /// <summary>
    /// Deletes an article identified by its ID, if the article belongs to the specified user.
    /// </summary>
    /// <param name="articleId">The ID of the article to be deleted.</param>
    /// <param name="userId">The ID of the user who owns the article.</param>
    /// <returns>A task that represents the asynchronous operation, indicating success or failure of the deletion.</returns>
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

    /// <summary>
    /// Retrieves detailed information about a specific article, including likes, comments, and whether the current user liked it.
    /// </summary>
    /// <param name="articleId">The ID of the article whose details are to be fetched.</param>
    /// <param name="userId">The ID of the user viewing the article's details.</param>
    /// <returns>A task that represents the asynchronous operation, containing an <see cref="ArticleDetailsViewModel"/> with the article details.</returns>
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

        comments.Reverse();

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

    /// <summary>
    /// Retrieves the article details for editing based on the article ID and the user ID.
    /// </summary>
    /// <param name="articleId">The ID of the article to be edited.</param>
    /// <param name="userId">The ID of the user requesting to edit the article.</param>
    /// <returns>
    /// An instance of <see cref="EditArticleViewModel"/> containing article details for editing, or null if the article doesn't exist or doesn't belong to the user.
    /// </returns>
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

    /// <summary>
    /// Updates the details of an existing article based on the provided model and user ID.
    /// </summary>
    /// <param name="model">The model containing the updated article information.</param>
    /// <param name="userId">The ID of the user who owns the article.</param>
    /// <returns>
    /// True if the article was successfully updated, otherwise false.
    /// </returns>
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

    /// <summary>
    /// Toggles the like status for an article based on the provided article ID and user ID.
    /// </summary>
    /// <param name="articleId">The ID of the article being liked or unliked.</param>
    /// <param name="userId">The ID of the user liking or unliking the article.</param>
    /// <returns>
    /// True if the like status was successfully toggled, otherwise false.
    /// </returns>
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

    /// <summary>
    /// Adds a comment to the specified article based on the provided article ID, description, and user ID.
    /// </summary>
    /// <param name="articleId">The ID of the article being commented on.</param>
    /// <param name="description">The content of the comment being added.</param>
    /// <param name="userId">The ID of the user adding the comment.</param>
    /// <returns>
    /// True if the comment was successfully added, otherwise false.
    /// </returns>
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

    /// <summary>
    /// Toggles the like status for a comment based on the provided comment ID and user ID.
    /// </summary>
    /// <param name="commentId">The ID of the comment being liked or unliked.</param>
    /// <param name="userId">The ID of the user liking or unliking the comment.</param>
    /// <returns>
    /// True if the like status for the comment was successfully toggled, otherwise false.
    /// </returns>
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
