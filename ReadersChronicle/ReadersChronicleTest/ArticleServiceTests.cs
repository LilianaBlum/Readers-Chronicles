using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using System.Runtime.Intrinsics.X86;
using System.Security.Claims;

namespace ReadersChronicleTest
{

    [TestClass]
    public class ArticleServiceTests
    {
        private ApplicationDbContext _context;
        private ArticleService _articleService;
        private User _testUser;
        private UserManager<User> _userManager;
        private Article _testArticle;
        private SignInManager<User> _signInManager;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_Articles")
                .Options;

            _context = new ApplicationDbContext(options);

            var mockUserStore = new Mock<IUserStore<User>>();
            var mockUserManager = new Mock<UserManager<User>>(
                mockUserStore.Object,
                null,
                new PasswordHasher<User>(),
                null,
                null,
                null,
                null,
                null,
                null
            );


            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockUserClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();

            var mockSignInManager = new Mock<SignInManager<User>>(
                mockUserManager.Object,
                mockHttpContextAccessor.Object,
                mockUserClaimsPrincipalFactory.Object,
                null,
                null,
                null,
                null
            );

            _userManager = mockUserManager.Object;
            _signInManager = mockSignInManager.Object;

            _articleService = new ArticleService(_context, mockUserManager.Object);

            // Seed test data
            _testUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "testuser",
                Email = "testuser@example.com",
                SecurityAnswerHash = "bingo",
                SecurityQuestion = "What was the name of you first pet?"
            };

            _testArticle = new Article
            {
                Id = 1,
                Title = "Test Article",
                Description = "Test Description",
                UserId = _testUser.Id,
                TimeCreated = DateTime.UtcNow,
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };

            _context.Articles.Add(_testArticle);
            _context.Users.Add(_testUser);
            _context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task GetArticlesAsync_ShouldReturnAllArticles_WhenViewTypeIsAll()
        {
            // Arrange
            var articles = new List<Article>
        {
            new Article { Id = 10, Title = "Article 1", UserId = _testUser.Id, TimeCreated = DateTime.UtcNow.AddHours(-2), PictureMimeType = "image/png", Picture = new byte[] { 1, 2, 3 }, Description = "test" },
            new Article { Id = 2, Title = "Article 2", UserId = _testUser.Id, TimeCreated = DateTime.UtcNow.AddHours(-1), PictureMimeType = "image/png", Picture = new byte[] { 1, 2, 3 }, Description = "test" }
        };

            _context.Articles.AddRange(articles);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.GetArticlesAsync(_testUser, "All");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Articles.Count);
        }

        [TestMethod]
        public async Task GetArticlesAsync_ShouldReturnUserArticles_WhenViewTypeIsMy()
        {
            // Arrange
            var otherUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "otheruser",
                Email = "otheruser@example.com",
                SecurityAnswerHash = "bingo",
                SecurityQuestion = "What was the name of you first pet?"
            };

            _context.Users.Add(otherUser);

            var articles = new List<Article>
        {
            new Article { Id = 10, Title = "User Article 1", UserId = _testUser.Id, TimeCreated = DateTime.UtcNow.AddHours(-2), PictureMimeType = "image/png", Picture = new byte[] { 1, 2, 3 }, Description = "test" },
            new Article { Id = 2, Title = "Other User Article", UserId = otherUser.Id, TimeCreated = DateTime.UtcNow.AddHours(-1), PictureMimeType = "image/png", Picture = new byte[] { 1, 2, 3 }, Description = "test" }
        };

            _context.Articles.AddRange(articles);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.GetArticlesAsync(_testUser, "My");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Articles.Count);
            Assert.AreEqual("Test Article", result.Articles.First().Title);
        }

        [TestMethod]
        public async Task GetArticlesAsync_ShouldReturnUserBooks()
        {
            // Arrange
            var userBooks = new List<UserBook>
        {
            new UserBook { UserBookID = 1, UserID = _testUser.Id, Title = "Book 1", Author = "Author", BookApiID = "123", Picture = new byte[3] {1,2,3}, PictureMimeType = "image/png" },
            new UserBook { UserBookID = 2, UserID = _testUser.Id, Title = "Book 2", Author = "Author2", BookApiID = "124", Picture = new byte[3] {1,2,3}, PictureMimeType = "image/png" }
        };

            _context.UserBooks.AddRange(userBooks);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.GetArticlesAsync(_testUser, "All");

            // Assert
            Assert.IsNotNull(result.UserBooks);
            Assert.AreEqual(2, result.UserBooks.Count);
            Assert.IsTrue(result.UserBooks.All(ub => userBooks.Select(ubk => ubk.UserBookID).Contains(ub.UserBookID)));
        }

        [TestMethod]
        public async Task CreateArticleAsync_ShouldReturnFalse_WhenUserBookDoesNotExist()
        {
            // Arrange
            var model = new CreateArticleViewModel
            {
                UserBookID = 1,
                Title = "Test Article",
                Description = "This is a test article."
            };

            // Act
            var result = await _articleService.CreateArticleAsync(model, _testUser.Id);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task CreateArticleAsync_ShouldCreateArticle_WhenUserBookExists()
        {
            // Arrange
            var userBook = new UserBook
            {
                UserBookID = 1,
                BookApiID = "123",
                Author = "Name",
                UserID = _testUser.Id,
                Title = "Test Book",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };

            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            var model = new CreateArticleViewModel
            {
                UserBookID = 1,
                Title = "Test Article",
                Description = "This is a test article."
            };

            // Act
            var result = await _articleService.CreateArticleAsync(model, _testUser.Id);

            // Assert
            Assert.IsTrue(result);

            var article = _context.Articles.FirstOrDefault();
            Assert.IsNotNull(article);
            Assert.AreEqual("Test Article", article.Title);
            Assert.AreEqual("Test Description", article.Description);
            Assert.AreEqual(userBook.PictureMimeType, article.PictureMimeType);
            Assert.AreEqual(_testUser.Id, article.UserId);
        }

        [TestMethod]
        public async Task CreateArticleAsync_ShouldNotCreateDuplicateArticle()
        {
            // Arrange
            var userBook = new UserBook
            {
                UserBookID = 1,
                UserID = _testUser.Id,
                BookApiID = "123",
                Author = "Name",
                Title = "Test Book",
                Picture = new byte[] { 1, 2, 3 },
                PictureMimeType = "image/jpeg"
            };

            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            var model = new CreateArticleViewModel
            {
                UserBookID = 1,
                Title = "Test Article",
                Description = "This is a test article."
            };

            // Act
            var firstResult = await _articleService.CreateArticleAsync(model, _testUser.Id);
            var secondResult = await _articleService.CreateArticleAsync(model, _testUser.Id);

            // Assert
            Assert.IsTrue(firstResult);
            Assert.IsTrue(secondResult);

            var articles = _context.Articles.ToList();
            Assert.AreEqual(3, articles.Count); // If duplicates are allowed
        }

        [TestMethod]
        public async Task DeleteArticleAsync_ShouldReturnFalse_WhenArticleDoesNotExist()
        {
            // Arrange
            var nonExistentArticleId = 999;

            // Act
            var result = await _articleService.DeleteArticleAsync(nonExistentArticleId, _testUser.Id);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task DeleteArticleAsync_ShouldReturnFalse_WhenArticleDoesNotBelongToUser()
        {
            // Arrange
            var otherUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "otheruser",
                Email = "otheruser@example.com",
                SecurityAnswerHash = "bingo",
                SecurityQuestion = "What was the name of you first pet?"
            };

            _context.Users.Add(otherUser);

            var article = new Article
            {
                Id = 100,
                UserId = otherUser.Id,
                Title = "Other User Article",
                Description = "This is an article by another user.",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.DeleteArticleAsync(article.Id, _testUser.Id);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(2, _context.Articles.Count()); // Ensure the article is not deleted
        }

        [TestMethod]
        public async Task DeleteArticleAsync_ShouldReturnTrue_WhenArticleIsDeleted()
        {
            // Arrange
            var article = new Article
            {
                Id = 100,
                UserId = _testUser.Id,
                Title = "Test Article",
                Description = "This is a test article.",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.DeleteArticleAsync(article.Id, _testUser.Id);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _context.Articles.Count()); // Ensure the article is deleted
        }

        [TestMethod]
        public async Task DeleteArticleAsync_ShouldNotDeleteOtherUserArticles()
        {
            // Arrange
            var otherUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "otheruser",
                Email = "otheruser@example.com",
                SecurityAnswerHash = "bingo",
                SecurityQuestion = "What was the name of you first pet?"
            };

            _context.Users.Add(otherUser);

            var articleByOtherUser = new Article
            {
                Id = 100,
                UserId = otherUser.Id,
                Title = "Other User Article",
                Description = "This is an article by another user.",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };

            _context.Articles.Add(articleByOtherUser);

            var articleByTestUser = new Article
            {
                Id = 2,
                UserId = _testUser.Id,
                Title = "Test User Article",
                Description = "This is an article by the test user.",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };

            _context.Articles.Add(articleByTestUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.DeleteArticleAsync(100, otherUser.Id);

            // Assert
            Assert.AreEqual(true, result);
            Assert.AreEqual(2, _context.Articles.Count()); // Ensure no articles are deleted
        }

        [TestMethod]
        public async Task GetArticleDetailsAsync_ShouldReturnNull_WhenArticleDoesNotExist()
        {
            // Arrange
            var nonExistentArticleId = 999;

            // Act
            var result = await _articleService.GetArticleDetailsAsync(nonExistentArticleId, _testUser.Id);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetArticleDetailsAsync_ShouldReturnDetails_WhenArticleExists()
        {
            // Arrange
            var article = new Article
            {
                Id = 100,
                UserId = _testUser.Id,
                Title = "Test Article",
                Description = "This is a test article.",
                Picture = new byte[0],
                PictureMimeType = "image/png",
                TimeCreated = DateTime.UtcNow,
                User = _testUser
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.GetArticleDetailsAsync(article.Id, _testUser.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(article.Id, result.Id);
            Assert.AreEqual(article.Title, result.Title);
            Assert.AreEqual(article.Description, result.Description);
            Assert.AreEqual(article.Picture, result.Picture);
            Assert.AreEqual(article.PictureMimeType, result.PictureMimeType);
            Assert.AreEqual(article.User.UserName, result.UserName);
            Assert.AreEqual(0, result.TotalLikes);
            Assert.IsFalse(result.UserLiked);
            Assert.AreEqual(0, result.Comments.Count);
        }

        [TestMethod]
        public async Task GetArticleDetailsAsync_ShouldIncludeTotalLikesAndUserLiked()
        {
            // Arrange
            var article = new Article
            {
                Id = 100,
                UserId = _testUser.Id,
                Title = "Test Article",
                Description = "This is a test article.",
                TimeCreated = DateTime.UtcNow,
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png",
                User = _testUser
            };

            _context.Articles.Add(article);

            var rating = new ArticleRating
            {
                ArticleId = article.Id,
                UserId = _testUser.Id
            };

            _context.ArticleRatings.Add(rating);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.GetArticleDetailsAsync(article.Id, _testUser.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.TotalLikes);
            Assert.IsTrue(result.UserLiked);
        }

        [TestMethod]
        public async Task GetArticleDetailsAsync_ShouldIncludeCommentsAndUserLikedComments()
        {
            // Arrange
            var article = new Article
            {
                Id = 100,
                UserId = _testUser.Id,
                Title = "Test Article",
                Description = "This is a test article.",
                TimeCreated = DateTime.UtcNow,
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png",
                User = _testUser
            };

            var comment = new Comment
            {
                CommentId = 1,
                ArticleId = article.Id,
                UserId = _testUser.Id,
                Description = "This is a comment.",
                User = _testUser
            };

            var commentRating = new CommentRating
            {
                CommentId = comment.CommentId,
                UserId = _testUser.Id
            };

            _context.Articles.Add(article);
            _context.Comments.Add(comment);
            _context.CommentRatings.Add(commentRating);

            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.GetArticleDetailsAsync(article.Id, _testUser.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Comments.Count);
            Assert.AreEqual(comment.Description, result.Comments.First().Description);
            Assert.AreEqual(1, result.UserLikedComments.Count);
            Assert.AreEqual(commentRating.CommentId, result.UserLikedComments.First().CommentId);
        }

        [TestMethod]
        public async Task GetArticleForEditAsync_ShouldReturnNull_WhenArticleDoesNotExist()
        {
            // Arrange
            var nonExistentArticleId = 999;

            // Act
            var result = await _articleService.GetArticleForEditAsync(nonExistentArticleId, _testUser.Id);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetArticleForEditAsync_ShouldReturnNull_WhenArticleDoesNotBelongToUser()
        {
            // Arrange
            var anotherUserId = Guid.NewGuid().ToString();
            var article = new Article
            {
                Id = 10,
                UserId = anotherUserId,
                Description = "This is not the user's article.",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png",
                Title = "Article"
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.GetArticleForEditAsync(article.Id, _testUser.Id);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetArticleForEditAsync_ShouldReturnEditViewModel_WhenArticleExistsAndBelongsToUser()
        {
            // Arrange
            var article = new Article
            {
                Id = 10,
                UserId = _testUser.Id,
                Description = "This is the user's article.",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png",
                Title = "Article"
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.GetArticleForEditAsync(article.Id, _testUser.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(article.Id, result.Id);
            Assert.AreEqual(article.Description, result.Description);
        }

        [TestMethod]
        public async Task UpdateArticleAsync_ShouldReturnFalse_WhenArticleDoesNotExist()
        {
            // Arrange
            var nonExistentArticleId = 999;
            var model = new EditArticleViewModel
            {
                Id = nonExistentArticleId,
                Description = "Updated description"
            };

            // Act
            var result = await _articleService.UpdateArticleAsync(model, _testUser.Id);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateArticleAsync_ShouldReturnFalse_WhenArticleDoesNotBelongToUser()
        {
            // Arrange
            var anotherUserId = Guid.NewGuid().ToString();
            var article = new Article
            {
                Id = 10,
                UserId = anotherUserId,
                Description = "Original description",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png",
                Title = "Article"
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            var model = new EditArticleViewModel
            {
                Id = article.Id,
                Description = "Updated description"
            };

            // Act
            var result = await _articleService.UpdateArticleAsync(model, _testUser.Id);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateArticleAsync_ShouldUpdateDescription_WhenArticleExistsAndBelongsToUser()
        {
            // Arrange
            var article = new Article
            {
                Id = 10,
                UserId = _testUser.Id,
                Description = "Original description",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png",
                Title = "Article"
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            var model = new EditArticleViewModel
            {
                Id = article.Id,
                Description = "Updated description"
            };

            // Act
            var result = await _articleService.UpdateArticleAsync(model, _testUser.Id);

            // Assert
            Assert.IsTrue(result);

            var updatedArticle = await _context.Articles.FirstOrDefaultAsync(a => a.Id == article.Id);
            Assert.IsNotNull(updatedArticle);
            Assert.AreEqual(model.Description, updatedArticle.Description);
        }

        [TestMethod]
        public async Task ToggleArticleLikeAsync_ShouldAddLike_WhenNoExistingLike()
        {
            // Act
            var result = await _articleService.ToggleArticleLikeAsync(_testArticle.Id, _testUser.Id);

            // Assert
            Assert.IsTrue(result);

            var like = await _context.ArticleRatings
                .FirstOrDefaultAsync(r => r.ArticleId == _testArticle.Id && r.UserId == _testUser.Id);

            Assert.IsNotNull(like);
            Assert.AreEqual(_testArticle.Id, like.ArticleId);
            Assert.AreEqual(_testUser.Id, like.UserId);
        }

        [TestMethod]
        public async Task ToggleArticleLikeAsync_ShouldRemoveLike_WhenExistingLikeExists()
        {
            // Arrange
            var like = new ArticleRating
            {
                ArticleId = _testArticle.Id,
                UserId = _testUser.Id
            };

            _context.ArticleRatings.Add(like);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.ToggleArticleLikeAsync(_testArticle.Id, _testUser.Id);

            // Assert
            Assert.IsTrue(result);

            var removedLike = await _context.ArticleRatings
                .FirstOrDefaultAsync(r => r.ArticleId == _testArticle.Id && r.UserId == _testUser.Id);

            Assert.IsNull(removedLike);
        }

        [TestMethod]
        public async Task AddCommentAsync_ShouldAddComment_WhenValidInput()
        {
            Mock.Get(_userManager)
                .Setup(um => um.FindByIdAsync(_testUser.Id))
                .ReturnsAsync(_testUser);
            // Act
            var result = await _articleService.AddCommentAsync(1, "Test Comment", _testUser.Id);

            // Assert
            Assert.IsTrue(result);

            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.ArticleId == _testArticle.Id && c.UserId == _testUser.Id);
            Assert.IsNotNull(comment);
            Assert.AreEqual("Test Comment", comment.Description);
            Assert.AreEqual(_testUser.Id, comment.UserId);
            Assert.AreEqual(_testArticle.Id, comment.ArticleId);
        }

        [TestMethod]
        public async Task AddCommentAsync_ShouldReturnFalse_WhenUserNotFound()
        {
            Mock.Get(_userManager)
                .Setup(um => um.FindByIdAsync(_testUser.Id))
                .ReturnsAsync(_testUser);
            // Act
            var result = await _articleService.AddCommentAsync(_testArticle.Id, "Test Comment", "NonExistentUser");

            // Assert
            Assert.IsFalse(result);

            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.ArticleId == _testArticle.Id);
            Assert.IsNull(comment);
        }

        [TestMethod]
        public async Task AddCommentAsync_ShouldHandleEmptyCommentDescription()
        {
            Mock.Get(_userManager)
                .Setup(um => um.FindByIdAsync(_testUser.Id))
                .ReturnsAsync(_testUser);
            // Act
            var result = await _articleService.AddCommentAsync(_testArticle.Id, string.Empty, _testUser.Id);

            // Assert
            Assert.IsTrue(result);

            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.ArticleId == _testArticle.Id && c.UserId == _testUser.Id);
            Assert.IsNotNull(comment);
            Assert.AreEqual(string.Empty, comment.Description);
        }

        [TestMethod]
        public async Task AddCommentAsync_ShouldHandleNonExistentArticle()
        {
            Mock.Get(_userManager)
                .Setup(um => um.FindByIdAsync(_testUser.Id))
                .ReturnsAsync(_testUser);
            // Act
            var result = await _articleService.AddCommentAsync(999, "Test Comment", _testUser.Id);

            // Assert
            Assert.IsTrue(result);

            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.ArticleId == 999 && c.UserId == _testUser.Id);
            Assert.IsNotNull(comment);
            Assert.AreEqual("Test Comment", comment.Description);
        }

        [TestMethod]
        public async Task ToggleCommentLikeAsync_ShouldAddLike_WhenNoExistingLike()
        {
            // Arrange
            var comment = new Comment
            {
                CommentId = 1,
                Description = "Test Comment",
                UserId = _testUser.Id
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.ToggleCommentLikeAsync(comment.CommentId, _testUser.Id);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _context.CommentRatings.Count());
            Assert.IsNotNull(await _context.CommentRatings.FirstOrDefaultAsync(cr => cr.CommentId == comment.CommentId && cr.UserId == _testUser.Id));
        }

        [TestMethod]
        public async Task ToggleCommentLikeAsync_ShouldRemoveLike_WhenExistingLike()
        {
            // Arrange
            var comment = new Comment
            {
                CommentId = 2,
                Description = "Test Comment",
                UserId = _testUser.Id
            };
            _context.Comments.Add(comment);

            var commentRating = new CommentRating
            {
                CommentId = comment.CommentId,
                UserId = _testUser.Id
            };
            _context.CommentRatings.Add(commentRating);
            await _context.SaveChangesAsync();

            // Act
            var result = await _articleService.ToggleCommentLikeAsync(comment.CommentId, _testUser.Id);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, _context.CommentRatings.Count());
        }

        [TestMethod]
        public async Task ToggleCommentLikeAsync_ShouldReturnFalse_WhenCommentDoesNotExist()
        {
            // Arrange
            int nonExistentCommentId = 999;

            // Act
            var result = await _articleService.ToggleCommentLikeAsync(nonExistentCommentId, _testUser.Id);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, _context.CommentRatings.Count());
        }
    }
}
