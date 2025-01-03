using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Moq;
using ReadersChronicle.Data;
using ReadersChronicle.Services;

namespace ReadersChronicleTest
{
    [TestClass]
    public class CommentServiceTests
    {
        private CommentService _commentService;
        private Mock<ApplicationDbContext> _contextMock;
        private Mock<UserManager<User>> _userManagerMock;

        // In-memory db for testing
        private DbContextOptions<ApplicationDbContext> _dbContextOptions;
        private ApplicationDbContext _context;

        [TestInitialize]
        public void Setup()
        {
            // Set up in-memory database options
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            // Initialize the in-memory context
            _context = new ApplicationDbContext(_dbContextOptions);

            // Initialize UserManager mock
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Initialize CommentService
            _commentService = new CommentService(_context, _userManagerMock.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up in-memory database after each test
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task DeleteCommentAsync_ShouldReturnTrue_WhenCommentExistsAndBelongsToUser()
        {
            // Arrange
            var userId = "test-user-id";
            var comment = new Comment
            {
                CommentId = 1,
                UserId = userId,
                ArticleId = 1,
                Description = "This is a comment"
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _commentService.DeleteCommentAsync(comment.CommentId, userId);

            // Assert
            Assert.IsTrue(result);
            var deletedComment = await _context.Comments.FindAsync(comment.CommentId);
            Assert.IsNull(deletedComment); // Comment should be deleted
        }

        [TestMethod]
        public async Task DeleteCommentAsync_ShouldReturnFalse_WhenCommentDoesNotExist()
        {
            // Act
            var result = await _commentService.DeleteCommentAsync(999, "test-user-id");

            // Assert
            Assert.IsFalse(result); // Comment does not exist
        }

        [TestMethod]
        public async Task DeleteCommentAsync_ShouldReturnFalse_WhenCommentDoesNotBelongToUser()
        {
            // Arrange
            var userId = "test-user-id";
            var anotherUserId = "another-user-id";
            var comment = new Comment
            {
                CommentId = 1,
                UserId = anotherUserId,
                ArticleId = 1,
                Description = "This is a comment"
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _commentService.DeleteCommentAsync(comment.CommentId, userId);

            // Assert
            Assert.IsFalse(result); // Comment belongs to another user
        }

        [TestMethod]
        public async Task GetArticleID_ShouldReturnArticleId_WhenCommentExists()
        {
            // Arrange
            var comment = new Comment
            {
                CommentId = 1,
                UserId = "test-user-id",
                ArticleId = 10,
                Description = "Test Comment"
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Act
            var articleId = await _commentService.GetArticleID(comment.CommentId);

            // Assert
            Assert.AreEqual(10, articleId); // The correct article ID should be returned
        }

        [TestMethod]
        public async Task GetCommentForEditAsync_ShouldReturnComment_WhenCommentExistsAndBelongsToUser()
        {
            // Arrange
            var userId = "test-user-id";
            var comment = new Comment
            {
                CommentId = 1,
                UserId = userId,
                ArticleId = 1,
                Description = "This is a comment"
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _commentService.GetCommentForEditAsync(comment.CommentId, userId);

            // Assert
            Assert.IsNotNull(result); // The comment should be returned for editing
            Assert.AreEqual(comment.CommentId, result.CommentId); // The returned comment should match the one created
        }

        [TestMethod]
        public async Task GetCommentForEditAsync_ShouldReturnNull_WhenCommentDoesNotExist()
        {
            // Act
            var result = await _commentService.GetCommentForEditAsync(999, "test-user-id");

            // Assert
            Assert.IsNull(result); // Comment does not exist
        }

        [TestMethod]
        public async Task GetCommentForEditAsync_ShouldReturnNull_WhenCommentDoesNotBelongToUser()
        {
            // Arrange
            var comment = new Comment
            {
                CommentId = 1,
                UserId = "another-user-id",
                ArticleId = 1,
                Description = "This is a comment"
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _commentService.GetCommentForEditAsync(comment.CommentId, "test-user-id");

            // Assert
            Assert.IsNull(result); // The comment belongs to another user, so null should be returned
        }

        [TestMethod]
        public async Task UpdateCommentAsync_ShouldReturnTrue_WhenCommentExistsAndBelongsToUser()
        {
            // Arrange
            var userId = "test-user-id";
            var comment = new Comment
            {
                CommentId = 1,
                UserId = userId,
                ArticleId = 1,
                Description = "This is a comment"
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var newDescription = "Updated description";

            // Act
            var result = await _commentService.UpdateCommentAsync(comment.CommentId, newDescription, userId);

            // Assert
            Assert.IsTrue(result); // The update should succeed
            var updatedComment = await _context.Comments.FindAsync(comment.CommentId);
            Assert.AreEqual(newDescription, updatedComment.Description); // The description should be updated
        }

        [TestMethod]
        public async Task UpdateCommentAsync_ShouldReturnFalse_WhenCommentDoesNotExist()
        {
            // Act
            var result = await _commentService.UpdateCommentAsync(999, "Updated description", "test-user-id");

            // Assert
            Assert.IsFalse(result); // The comment does not exist
        }

        [TestMethod]
        public async Task UpdateCommentAsync_ShouldReturnFalse_WhenCommentDoesNotBelongToUser()
        {
            // Arrange
            var comment = new Comment
            {
                CommentId = 1,
                UserId = "another-user-id",
                ArticleId = 1,
                Description = "This is a comment"
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _commentService.UpdateCommentAsync(comment.CommentId, "Updated description", "test-user-id");

            // Assert
            Assert.IsFalse(result); // The comment belongs to another user, so update should fail
        }
    }
}
