using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;

namespace ReadersChronicle.Services
{
    public class CommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CommentService(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

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
