using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;

namespace ReadersChronicle.Services
{
    /// <summary>
    /// Service class responsible for managing comment-related operations such as deleting, editing, and updating comments.
    /// </summary>
    public class CommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommentService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="userManager">The user manager to handle user-related operations.</param>
        public CommentService(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Deletes a comment if it belongs to the specified user.
        /// </summary>
        /// <param name="commentId">The ID of the comment to be deleted.</param>
        /// <param name="userId">The ID of the user attempting to delete the comment.</param>
        /// <returns>True if the comment was successfully deleted, otherwise false.</returns>
        public async Task<bool> DeleteCommentAsync(int commentId, string userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);

            if (comment == null || comment.UserId != userId)
            {
                return false;
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Retrieves the article ID associated with a specific comment.
        /// </summary>
        /// <param name="commentId">The ID of the comment whose article ID is to be fetched.</param>
        /// <returns>The article ID associated with the comment.</returns>
        public async Task<int> GetArticleID(int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);

            return comment.ArticleId;
        }

        /// <summary>
        /// Retrieves a comment for editing if the comment belongs to the specified user.
        /// </summary>
        /// <param name="commentId">The ID of the comment to be retrieved.</param>
        /// <param name="userId">The ID of the user requesting to edit the comment.</param>
        /// <returns>The comment to be edited, or null if the comment doesn't exist or doesn't belong to the user.</returns>
        public async Task<Comment?> GetCommentForEditAsync(int commentId, string userId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null || comment.UserId != userId)
            {
                return null;
            }

            return comment;
        }

        /// <summary>
        /// Updates the description of a comment if it belongs to the specified user.
        /// </summary>
        /// <param name="commentId">The ID of the comment to be updated.</param>
        /// <param name="description">The new description for the comment.</param>
        /// <param name="userId">The ID of the user updating the comment.</param>
        /// <returns>True if the comment was successfully updated, otherwise false.</returns>
        public async Task<bool> UpdateCommentAsync(int commentId, string description, string userId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null || comment.UserId != userId)
            {
                return false;
            }

            comment.Description = description;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
