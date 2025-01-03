using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using ReadersChronicle.Services;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace ReadersChronicleTest
{
    [TestClass]
    public class BookServiceTests
    {
        private ApplicationDbContext _context;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<UserManager<User>> _userManagerMock;
        private BookService _bookService;

        [TestInitialize]
        public void Setup()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_BookService")
                .Options;

            _context = new ApplicationDbContext(options);

            // Mock HttpContextAccessor
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var mockHttpContext = new Mock<HttpContext>();
            _httpContextAccessorMock.Setup(_ => _.HttpContext).Returns(mockHttpContext.Object);

            // Mock UserManager
            var mockUserStore = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                mockUserStore.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

            // Mock HttpMessageHandler
            var mockHttpHandler = new MockHttpMessageHandler(request =>
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}")
                };
            });

            var httpClient = new HttpClient(mockHttpHandler);

            // Instantiate BookService
            _bookService = new BookService(httpClient, _context, _userManagerMock.Object, _httpContextAccessorMock.Object);


            var passwordHasher = new PasswordHasher<User>();
            // Add test user
            var testUser = new User
            {
                Id = "test-user-id",
                UserName = "testuser",
                UserType = "user",
                PasswordHash = passwordHasher.HashPassword(null, "Password123"),
                isBlocked = false,
                SecurityQuestion = "MotherMaidenName",
                SecurityAnswerHash = passwordHasher.HashPassword(null, "MyMaidenName")
            };

            var defaultImagePath = Path.Combine("wwwroot", "Common", "grey-image.jpg");

            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("test-user-id");
            _userManagerMock.Setup(um => um.FindByIdAsync("test-user-id")).ReturnsAsync(testUser);

            _context.Users.Add(testUser);
            _context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();

            var defaultImagePath = Path.Combine("wwwroot", "Common", "grey-image.jpg");
            if (File.Exists(defaultImagePath))
            {
                File.Delete(defaultImagePath);
            }
        }

        [TestMethod]
        public async Task IsUserAdmin_ShouldReturnFalse_WhenUserIsNotAdmin()
        {
            // Act
            var result = await _bookService.isUserAdmin();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsUserAdmin_ShouldReturnTrue_WhenUserIsAdmin()
        {
            // Arrange

            var passwordHasher = new PasswordHasher<User>();
            var adminUser = new User
            {
                Id = "admin-user-id",
                UserName = "adminuser",
                UserType = "admin",
                PasswordHash = passwordHasher.HashPassword(null, "Password123"),
                isBlocked = false,
                SecurityQuestion = "MotherMaidenName",
                SecurityAnswerHash = passwordHasher.HashPassword(null, "MyMaidenName")

            };

            _userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin-user-id");
            _userManagerMock.Setup(um => um.FindByIdAsync("admin-user-id")).ReturnsAsync(adminUser);

            _context.Users.Add(adminUser);
            _context.SaveChanges();

            // Act
            var result = await _bookService.isUserAdmin();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task AddToLibrary_ShouldAddBookToLibrary_WhenBookDoesNotExist()
        {
            // Arrange
            var bookApiID = "/works/OL654321W";
            var title = "New Book";
            var author = "New Author";
            var coverUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQgsg2ITrnfv-bbODlXzNBUuDrJ4hpljaupEQ&s";
            var pageCount = 200;

            // Mock HttpMessageHandler to return valid image bytes
            var mockHttpHandler = new MockHttpMessageHandler(request =>
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 }) // Simulated image data
                };
            });

            var httpClient = new HttpClient(mockHttpHandler);
            _bookService = new BookService(httpClient, _context, _userManagerMock.Object, _httpContextAccessorMock.Object);

            // Act
            var result = await _bookService.AddToLibrary(bookApiID, title, author, coverUrl, pageCount);

            // Assert
            Assert.IsTrue(result);

            var userBook = _context.UserBooks.FirstOrDefault(b => b.BookApiID == bookApiID);
            Assert.IsNotNull(userBook);
            Assert.AreEqual(bookApiID, userBook.BookApiID);
            Assert.AreEqual(title, userBook.Title);
            Assert.AreEqual(author, userBook.Author);
            Assert.AreEqual(pageCount, userBook.Length);
        }

        [TestMethod]
        public async Task GetBookCoverImage_ShouldReturnDefaultImage_WhenCoverUrlIsNull()
        {
            // Arrange
            var defaultImagePath = Path.Combine("wwwroot", "Common", "grey-image.jpg");
            var defaultImageBytes = new byte[] { 10, 20, 30, 40 };

            Directory.CreateDirectory(Path.GetDirectoryName(defaultImagePath));
            await File.WriteAllBytesAsync(defaultImagePath, defaultImageBytes);

            // Act
            var result = await _bookService.GetBookCoverImage(null);

            // Assert
            CollectionAssert.AreEqual(defaultImageBytes, result);
        }

        [TestMethod]
        public async Task GetBookCoverImage_ShouldReturnImage_WhenCoverUrlIsValid()
        {
            // Arrange
            var mockHttpHandler = new MockHttpMessageHandler(request =>
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(new byte[] { 50, 60, 70 })
                };
            });

            var httpClient = new HttpClient(mockHttpHandler);
            _bookService = new BookService(httpClient, _context, _userManagerMock.Object, _httpContextAccessorMock.Object);

            // Act
            var result = await _bookService.GetBookCoverImage("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQgsg2ITrnfv-bbODlXzNBUuDrJ4hpljaupEQ&s");

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetUserLibraryBooksAsync_ShouldReturnBooksWithSpecifiedStatus()
        {
            // Arrange
            var userId = "test-user-id";
            var status = "WantToRead";

            var userBook1 = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Book 1",
                Author = "Author 1",
                Length = 300,
                CurrentPage = 0,
                Status = status,
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            var userBook2 = new UserBook
            {
                UserID = userId,
                BookApiID = "book-2",
                Title = "Book 2",
                Author = "Author 2",
                Length = 200,
                CurrentPage = 50,
                Status = status,
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };

            _context.UserBooks.AddRange(userBook1, userBook2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bookService.GetUserLibraryBooksAsync(userId, status);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(b => b.Title == "Book 1" && b.Status == status));
            Assert.IsTrue(result.Any(b => b.Title == "Book 2" && b.Status == status));
            Assert.IsTrue(result.First(b => b.Title == "Book 1").CoverImageBase64 != null); // Picture exists
        }

        [TestMethod]
        public async Task GetUserJournalAsync_ShouldReturnJournalEntriesForUser()
        {
            // Arrange
            var userId = "test-user-id";

            var userBook = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Book 1",
                Author = "Author 1",
                Length = 300,
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            _context.UserBooks.Add(userBook);

            var journal1 = new BookJournal
            {
                UserBook = userBook,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-5),
                OverallRating = 5,
                OverallImpression = "Great book",
                Insights = "Learned a lot",
                AuthorsAim = "To inspire",
                Recommendation = "Highly recommended",
                AdditionalNotes = "Must read"
            };
            var journal2 = new BookJournal
            {
                UserBook = userBook,
                StartDate = DateTime.UtcNow.AddDays(-20),
                EndDate = DateTime.UtcNow.AddDays(-15),
                OverallRating = 4,
                OverallImpression = "Good book",
                Insights = "Interesting concepts",
                AuthorsAim = "To inform",
                Recommendation = "Recommended",
                AdditionalNotes = "Worth a try"
            };
            _context.BookJournals.AddRange(journal1, journal2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bookService.GetUserJournalAsync(userId);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(j => j.OverallImpression == "Great book" && j.Rating == 5));
            Assert.IsTrue(result.Any(j => j.OverallImpression == "Good book" && j.Rating == 4));
        }

        [TestMethod]
        public async Task SearchBookJournalAsync_ShouldReturnJournalEntriesMatchingQuery()
        {
            // Arrange
            var userId = "test-user-id";

            var userBook1 = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Book 1",
                Author = "Author 1",
                Length = 300,
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            var userBook2 = new UserBook
            {
                UserID = userId,
                BookApiID = "book-2",
                Title = "Another Book",
                Author = "Author 2",
                Length = 200,
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            _context.UserBooks.AddRange(userBook1, userBook2);

            var journal1 = new BookJournal
            {
                UserBook = userBook1,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-5),
                OverallImpression = "Great book",
                AdditionalNotes = "Must read",
                Insights = "Interesting concepts",
                AuthorsAim = "To inform",
                Recommendation = "Recommended",
            };
            var journal2 = new BookJournal
            {
                UserBook = userBook2,
                StartDate = DateTime.UtcNow.AddDays(-20),
                EndDate = DateTime.UtcNow.AddDays(-15),
                OverallImpression = "Good book",
                AdditionalNotes = "Worth a try",
                Insights = "Interesting concepts",
                AuthorsAim = "To inform",
                Recommendation = "Recommended",
            };
            _context.BookJournals.AddRange(journal1, journal2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bookService.SearchBookJournalAsync(userId, "Another");

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Good book", result.First().OverallImpression);
        }

        [TestMethod]
        public async Task ChangeStatus_ShouldUpdateStatus_WhenUserBookExists()
        {
            // Arrange
            var userId = "test-user-id";
            var userBook = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Test Book",
                Author = "Test Author",
                Status = "WantToRead",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bookService.ChangeStatus(userId, userBook.UserBookID, "CurrentlyReading");

            // Assert
            Assert.IsTrue(result);
            var updatedBook = _context.UserBooks.FirstOrDefault(b => b.UserBookID == userBook.UserBookID);
            Assert.AreEqual("CurrentlyReading", updatedBook.Status);
            Assert.IsNotNull(updatedBook.StartDate); // Ensure StartDate is set for "CurrentlyReading"
        }

        [TestMethod]
        public async Task ChangeStatus_ShouldReturnFalse_WhenUserBookDoesNotExist()
        {
            // Act
            var result = await _bookService.ChangeStatus("non-existent-user", 999, "Read");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateProgressAsync_ShouldUpdateCurrentPage_WhenUserBookExists()
        {
            // Arrange
            var userId = "test-user-id";
            var userBook = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Test Book",
                Author = "Test Author",
                CurrentPage = 10,
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            // Act
            await _bookService.UpdateProgressAsync(userId, userBook.UserBookID, 50);

            // Assert
            var updatedBook = _context.UserBooks.FirstOrDefault(b => b.UserBookID == userBook.UserBookID);
            Assert.IsNotNull(updatedBook);
            Assert.AreEqual(50, updatedBook.CurrentPage);
        }

        [TestMethod]
        public async Task UpdateProgressAsync_ShouldDoNothing_WhenUserBookDoesNotExist()
        {
            // Act
            await _bookService.UpdateProgressAsync("non-existent-user", 999, 50);

            // Assert
            Assert.AreEqual(0, _context.UserBooks.Count()); // No updates should occur
        }

        [TestMethod]
        public async Task RemoveBookAsync_ShouldRemoveBookAndJournal_WhenBookExists()
        {
            // Arrange
            var userId = "test-user-id";
            var userBook = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Test Book",
                Author = "Test Author",
                Status = "CurrentlyReading",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            _context.UserBooks.Add(userBook);

            var bookJournal = new BookJournal
            {
                UserBookID = userBook.UserBookID,
                UserBook = userBook,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now,
                OverallRating = 5,
                OverallImpression = "Great book",
                Insights = "Very insightful",
                AuthorsAim = "To entertain",
                Recommendation = "Highly recommend",
                AdditionalNotes = "Keep reading"
            };
            _context.BookJournals.Add(bookJournal);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bookService.RemoveBookAsync(userId, userBook.UserBookID);

            // Assert
            Assert.IsTrue(result);
            var removedBook = _context.UserBooks.FirstOrDefault(b => b.UserBookID == userBook.UserBookID);
            var removedJournal = _context.BookJournals.FirstOrDefault(b => b.UserBookID == userBook.UserBookID);

            Assert.IsNull(removedBook); // Book should be removed
            Assert.IsNull(removedJournal); // Journal entry should be removed
        }

        [TestMethod]
        public async Task FinishBookAsync_ShouldMarkBookAsFinished_WhenBookExists()
        {
            // Arrange
            var userId = "test-user-id";
            var userBook = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Test Book",
                Author = "Test Author",
                Status = "CurrentlyReading",
                Length = 300,
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            // Act
            await _bookService.FinishBookAsync(userId, userBook.UserBookID);

            // Assert
            var updatedBook = _context.UserBooks.FirstOrDefault(b => b.UserBookID == userBook.UserBookID);
            Assert.IsNotNull(updatedBook);
            Assert.AreEqual("Finished", updatedBook.Status); // Status should be updated
            Assert.AreEqual(300, updatedBook.CurrentPage); // CurrentPage should be set to the book's length
            Assert.IsNotNull(updatedBook.EndDate); // EndDate should be set
        }

        [TestMethod]
        public async Task FinishBookAsync_ShouldDoNothing_WhenBookDoesNotExist()
        {
            // Act
            await _bookService.FinishBookAsync("non-existent-user", 999);

            // Assert
            var book = _context.UserBooks.FirstOrDefault(b => b.UserBookID == 999);
            Assert.IsNull(book); // Ensure no book was updated
        }

        [TestMethod]
        public async Task MarkAsDNFAsync_ShouldMarkBookAsDNF_WhenBookExists()
        {
            // Arrange
            var userId = "test-user-id";
            var userBook = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Test Book",
                Author = "Test Author",
                Status = "CurrentlyReading",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            // Act
            await _bookService.MarkAsDNFAsync(userId, userBook.UserBookID);

            // Assert
            var updatedBook = _context.UserBooks.FirstOrDefault(b => b.UserBookID == userBook.UserBookID);
            Assert.IsNotNull(updatedBook);
            Assert.AreEqual("Dnf", updatedBook.Status); // Status should be updated to "Dnf"
            Assert.IsNotNull(updatedBook.EndDate); // EndDate should be set
        }

        [TestMethod]
        public async Task MarkAsDNFAsync_ShouldDoNothing_WhenBookDoesNotExist()
        {
            // Act
            await _bookService.MarkAsDNFAsync("non-existent-user", 999);

            // Assert
            var book = _context.UserBooks.FirstOrDefault(b => b.UserBookID == 999);
            Assert.IsNull(book); // Ensure no book was updated
        }

        [TestMethod]
        public async Task SaveEditedJournalAsync_ShouldUpdateJournal_WhenValidModelProvided()
        {
            // Arrange
            var userId = "test-user-id";
            var userBook = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Test Book",
                Author = "Test Author",
                Status = "Finished",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            var journal = new BookJournal
            {
                UserBookID = userBook.UserBookID,
                UserBook = userBook,
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(-5),
                OverallRating = 4,
                OverallImpression = "Great book",
                Insights = "Very insightful",
                AuthorsAim = "To entertain",
                Recommendation = "Recommend",
                AdditionalNotes = "Keep reading"
            };
            _context.BookJournals.Add(journal);
            await _context.SaveChangesAsync();

            var model = new EditJournalViewModel
            {
                StartDate = DateTime.Now.AddDays(-15),
                EndDate = DateTime.Now.AddDays(-10),
                OverallRating = 5,
                OverallImpression = "Excellent book",
                Insights = "Mind-opening",
                AuthorsAim = "To enlighten",
                Recommendation = "Highly recommend",
                AdditionalNotes = "Must read"
            };

            // Act
            var updatedJournals = await _bookService.SaveEditedJournalAsync(journal, model);

            // Assert
            var updatedJournal = _context.BookJournals.FirstOrDefault(j => j.JournalID == journal.JournalID);
            Assert.IsNotNull(updatedJournal);
            Assert.AreEqual(model.StartDate, updatedJournal.StartDate); // StartDate should be updated
            Assert.AreEqual(model.EndDate, updatedJournal.EndDate); // EndDate should be updated
            Assert.AreEqual(model.OverallRating, updatedJournal.OverallRating); // Rating should be updated
            Assert.AreEqual(model.OverallImpression, updatedJournal.OverallImpression); // OverallImpression should be updated
            Assert.AreEqual(model.Insights, updatedJournal.Insights); // Insights should be updated
            Assert.AreEqual(model.AuthorsAim, updatedJournal.AuthorsAim); // AuthorsAim should be updated
            Assert.AreEqual(model.Recommendation, updatedJournal.Recommendation); // Recommendation should be updated
            Assert.AreEqual(model.AdditionalNotes, updatedJournal.AdditionalNotes); // AdditionalNotes should be updated
        }

        [TestMethod]
        public async Task SaveEditedJournalAsync_ShouldReturnUpdatedJournalsList_WhenJournalUpdated()
        {
            // Arrange
            var userId = "test-user-id";
            var userBook = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Test Book",
                Author = "Test Author",
                Status = "Finished",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            var journal = new BookJournal
            {
                UserBookID = userBook.UserBookID,
                UserBook = userBook,
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(-5),
                OverallRating = 4,
                OverallImpression = "Great book",
                Insights = "Very insightful",
                AuthorsAim = "To entertain",
                Recommendation = "Recommend",
                AdditionalNotes = "Keep reading"
            };
            _context.BookJournals.Add(journal);
            await _context.SaveChangesAsync();

            var model = new EditJournalViewModel
            {
                StartDate = DateTime.Now.AddDays(-15),
                EndDate = DateTime.Now.AddDays(-10),
                OverallRating = 5,
                OverallImpression = "Excellent book",
                Insights = "Mind-opening",
                AuthorsAim = "To enlighten",
                Recommendation = "Highly recommend",
                AdditionalNotes = "Must read"
            };

            // Act
            var updatedJournals = await _bookService.SaveEditedJournalAsync(journal, model);

            // Assert
            Assert.IsTrue(updatedJournals.Count > 0); // Should return updated journals
        }

        [TestMethod]
        public async Task AddToJournalAsync_ShouldAddNewJournalEntry_WhenValidUserBookProvided()
        {
            // Arrange
            var userId = "test-user-id";
            var userBook = new UserBook
            {
                UserID = userId,
                BookApiID = "book-1",
                Title = "Test Book",
                Author = "Test Author",
                Status = "CurrentlyReading",
                Picture = new byte[3] { 1, 2, 3 },
                PictureMimeType = "image/png"
            };
            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            // Act
            await _bookService.AddToJournalAsync(userBook.UserBookID, userBook);

            // Assert
            var journalEntry = _context.BookJournals.FirstOrDefault(j => j.UserBookID == userBook.UserBookID);
            Assert.IsNotNull(journalEntry); // Journal entry should be created
            Assert.AreEqual(userBook.UserBookID, journalEntry.UserBookID); // Ensure the journal entry is linked to the correct userBook
            Assert.AreEqual(userBook.StartDate, journalEntry.StartDate); // Ensure the journal entry has the same StartDate
            Assert.AreEqual(userBook.EndDate, journalEntry.EndDate); // Ensure the journal entry has the same EndDate
        }

        [TestMethod]
        public async Task AddToJournalAsync_ShouldDoNothing_WhenUserBookDoesNotExist()
        {
            // Act
            await _bookService.AddToJournalAsync(999, new UserBook());

            // Assert
            var journalEntry = _context.BookJournals.FirstOrDefault(j => j.UserBookID == 999);
            Assert.IsNull(journalEntry); // Ensure no journal entry was added
        }
    }


    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _sendFunc;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> sendFunc)
        {
            _sendFunc = sendFunc;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_sendFunc(request));
        }
    }
}
