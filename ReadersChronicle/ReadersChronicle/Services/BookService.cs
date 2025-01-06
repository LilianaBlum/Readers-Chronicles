using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using ReadersChronicle.Data;
using ReadersChronicle.Models;

namespace ReadersChronicle.Services
{
    /// <summary>
    /// Provides services related to books, including searching for books, adding books to the user's library, managing book statuses, and handling book journals.
    /// </summary>
    public class BookService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        /// <summary>
        /// Initializes a new instance of the BookService class with dependencies for HTTP client, context, user manager, and HTTP context accessor.
        /// </summary>
        public BookService(HttpClient httpClient, ApplicationDbContext context, UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Checks whether the current user is an admin.
        /// </summary>
        /// <returns>True if the user is an admin; otherwise, false.</returns>
        public async Task<bool> isUserAdmin()
        {
            var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
            if(userId == null)
            {
                return false;
            }
            else
            {
                var user = await _userManager.FindByIdAsync(userId);
                return user.UserType == "admin" ? true : false;
            }
        }

        /// <summary>
        /// Searches for books based on the specified query using the Open Library API.
        /// </summary>
        /// <param name="query">The search query to use for finding books.</param>
        /// <returns>A list of "BookViewModel" representing the books found.</returns>
        public async Task<List<BookViewModel>> SearchBooksAsync(string query)
        {
            var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
            
            // IMPORTANT! - DO NOT REMOVE COMMENTED CODE - in case open library does not work - use googleapis instead.
            
            //    var requestUri = $"https://www.googleapis.com/books/v1/volumes?q={query}";

            //    var response = await _httpClient.GetAsync(requestUri);
            //    response.EnsureSuccessStatusCode();

            //    var content = await response.Content.ReadAsStringAsync();
            //    var json = JObject.Parse(content);

            //    var bookIds = json["items"]
            //.Select(item => (string)item["id"])
            //.ToList();

            //    var books = json["items"]
            //        .Select(item => new BookViewModel
            //        {
            //            Title = (string)item["volumeInfo"]["title"],
            //            Author = string.Join(", ", item["volumeInfo"]["authors"] ?? new JArray()),
            //            BookId = (string)item["id"],
            //            PageCount = (int?)item["volumeInfo"]["pageCount"] ?? 0,
            //            CoverUrl = (string)item["volumeInfo"]["imageLinks"]?["thumbnail"],
            //        })
            //    .ToList();

            // Construct the Open Library API URL
            var requestUri = $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(query)}";

            // Fetch data from Open Library
            var response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            // Extract book data
            var bookDocs = json["docs"]?.ToList() ?? new List<JToken>();

            var books = bookDocs
                .Select(doc => new BookViewModel
                {
                    Title = (string)doc["title"] ?? "Unknown Title",
                    Author = string.Join(", ", doc["author_name"]?.Select(a => a.ToString()) ?? new List<string>()),
                    BookId = (string)doc["key"], // Open Library book ID (e.g., "/works/OL123456W")
                    PageCount = (int?)doc["number_of_pages_median"] ?? 0,
                    CoverUrl = doc["cover_i"] != null
                        ? $"https://covers.openlibrary.org/b/id/{doc["cover_i"]}-L.jpg"
                        : null,
                })
                .ToList();

            // Get books already in the user's library
            var bookIds = books.Select(b => b.BookId).ToList();

            var userBookIds = await _context.UserBooks
       .Where(ub => ub.UserID == userId && bookIds.Contains(ub.BookApiID))
       .Select(ub => ub.BookApiID)
       .ToListAsync();

            foreach (var book in books)
            {
                book.IsInLibrary = userBookIds.Contains(book.BookId);
            }

            return books;
        }

        /// <summary>
        /// Adds a book to the user's library.
        /// </summary>
        /// <param name="bookApiID">The API ID of the book.</param>
        /// <param name="title">The title of the book.</param>
        /// <param name="author">The author of the book.</param>
        /// <param name="coverUrl">The cover image URL of the book.</param>
        /// <param name="pageCount">The page count of the book.</param>
        /// <returns>True if the book was added successfully; otherwise, false.</returns>
        public async Task<bool> AddToLibrary(string bookApiID, string title, string author, string coverUrl, int pageCount)
        {
            var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);

            var existingBook = await _context.UserBooks
            .FirstOrDefaultAsync(b => b.UserID == userId && b.BookApiID == bookApiID);

            if (existingBook != null)
            {
                return false;
            }

            var userBook = new UserBook
            {
                UserID = userId,
                BookApiID = bookApiID,
                Title = title,
                Author = author,
                Length = pageCount,
                CurrentPage = 0,
                Status = "WantToRead",
                Picture = await GetBookCoverImage(coverUrl),
                PictureMimeType = "image/jpeg"
            };

            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Retrieves the book cover image as a byte array from the given URL.
        /// </summary>
        /// <param name="coverUrl">The URL of the book's cover image.</param>
        /// <returns>A byte array representing the cover image.</returns>
        public async Task<byte[]> GetBookCoverImage(string coverUrl)
        {
            if (string.IsNullOrEmpty(coverUrl))
            {
                // Use the default image if no cover URL is provided
                var defaultImagePath = Path.Combine("wwwroot", "Common", "grey-image.jpg");

                if (File.Exists(defaultImagePath))
                {
                    return await File.ReadAllBytesAsync(defaultImagePath);
                }

                throw new FileNotFoundException("Default image not found at " + defaultImagePath);
            }


            try
            {
                using (var httpClient = new HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(coverUrl);

                    return imageBytes;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading book cover image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves a list of books from the user's library with a specific status.
        /// </summary>
        /// <param name="userId">The ID of the user whose library is to be retrieved.</param>
        /// <param name="status">The status of the books (e.g., "WantToRead", "CurrentlyReading", etc.).</param>
        /// <returns>A list of "UserBookViewModel" representing the user's books with the specified status.</returns>
        public async Task<List<UserBookViewModel>> GetUserLibraryBooksAsync(string userId, string status)
        {
            // Query for user books with the specified status
            var userBooks = await _context.UserBooks
                .Where(b => b.UserID == userId && b.Status == status)
                .ToListAsync();

            // Convert UserBooks to UserBookViewModel
            var userBookViewModels = userBooks.Select(book => new UserBookViewModel
            {
                UserBookID = book.UserBookID,
                Title = book.Title,
                Author = book.Author,
                Length = book.Length,
                Status = book.Status,
                CoverImageBase64 = book.Picture != null ? Convert.ToBase64String(book.Picture) : null,
                CurrentPage = book.CurrentPage,
            }).ToList();

            return userBookViewModels;
        }

        /// <summary>
        /// Retrieves a list of journal entries for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose journal entries are to be retrieved.</param>
        /// <returns>A list of "BookJournalViewModel" representing the user's journal entries.</returns>
        public async Task<List<BookJournalViewModel>> GetUserJournalAsync(string userId)
        {
            var journalEntries = await _context.BookJournals
        .Where(j => j.UserBook.UserID == userId)
        .Include(j => j.UserBook)
        .ToListAsync();

            var viewModel = journalEntries.Select(journal => new BookJournalViewModel
            {
                JournalID = journal.JournalID,
                UserBook = journal.UserBook,
                StartDate = journal.StartDate,
                EndDate = journal.EndDate,
                Rating = journal.OverallRating,
                OverallImpression = journal.OverallImpression,
                Insights = journal.Insights,
                AuthorsAim = journal.AuthorsAim,
                Recommendation = journal.Recommendation,
                AdditionalNotes = journal.AdditionalNotes
            }).ToList();

            return viewModel;
        }

        /// <summary>
        /// Searches for book journal entries based on a query.
        /// </summary>
        /// <param name="userId">The ID of the user whose journal entries are to be searched.</param>
        /// <param name="query">The search query to match against book titles or authors.</param>
        /// <returns>A list of "BookJournalViewModel" matching the search criteria.</returns>
        public async Task<List<BookJournalViewModel>> SearchBookJournalAsync(string userId, string query)
        {
            var userBooks = await _context.UserBooks
        .Where(ub => ub.UserID == userId && ub.Title.Contains(query) || ub.Author.Contains(query))
        .ToListAsync();

            var bookIds = userBooks.Select(ub => ub.UserBookID).ToList();
            var journalEntries = await _context.BookJournals
                .Where(j => bookIds.Contains(j.UserBookID))
                .ToListAsync();

            var viewModel = journalEntries.Select(journal => new BookJournalViewModel
            {
                JournalID = journal.JournalID,
                UserBook = journal.UserBook,
                StartDate = journal.StartDate,
                EndDate = journal.EndDate,
                OverallImpression = journal.OverallImpression,
                Insights = journal.Insights,
                AuthorsAim = journal.AuthorsAim,
                Recommendation = journal.Recommendation,
                AdditionalNotes = journal.AdditionalNotes
            }).ToList();

            return viewModel;
        }

        /// <summary>
        /// Changes the status of a book in the user's library (e.g., "CurrentlyReading", "Finished", etc.).
        /// </summary>
        /// <param name="userId">The ID of the user whose book status is being changed.</param>
        /// <param name="userBookId">The ID of the book whose status is being changed.</param>
        /// <param name="status">The new status to set for the book (e.g., "CurrentlyReading", "Finished", etc.).</param>
        /// <returns>True if the status was successfully changed; otherwise, false.</returns>
        public async Task<bool> ChangeStatus(string userId, int userBookId, string status)
        {
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            if (userBook == null)
            {
                return false;
            }

            if (userBook != null)
            {
                if (status == "CurrentlyReading")
                {
                    userBook.StartDate = DateTime.UtcNow;
                }

                userBook.Status = status;
                await _context.SaveChangesAsync();
            }
            return true;
        }

        /// <summary>
        /// Updates the current page of a book in the user's library.
        /// </summary>
        /// <param name="userId">The ID of the user whose book progress is being updated.</param>
        /// <param name="userBookId">The ID of the book whose progress is being updated.</param>
        /// <param name="currentPage">The current page number the user is on in the book.</param>
        public async Task UpdateProgressAsync(string userId, int userBookId, int currentPage)
        {
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            if (userBook != null)
            {
                userBook.CurrentPage = currentPage;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Removes a book from the user's library and deletes its associated journal entry if it exists.
        /// </summary>
        /// <param name="userId">The ID of the user whose book is being removed.</param>
        /// <param name="userBookId">The ID of the book to be removed.</param>
        /// <returns>True if the book was successfully removed from the library; otherwise, false.</returns>
        public async Task<bool> RemoveBookAsync(string userId, int userBookId)
        {
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            if (userBook != null)
            {
                var userBookInLibrary = await _context.BookJournals
                .Where(b => b.UserBookID == userBookId)
                .FirstOrDefaultAsync();

                if (userBookInLibrary != null)
                {
                    _context.BookJournals.Remove(userBookInLibrary);

                }

                _context.UserBooks.Remove(userBook);
                await _context.SaveChangesAsync();
            }

            
            var userBookAfterDeleting = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            var libraryBookAfterDeleting = await _context.BookJournals
                .Where(b => b.UserBookID == userBookId)
                .FirstOrDefaultAsync();

            if (userBookAfterDeleting == null && libraryBookAfterDeleting == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Marks a book as finished, updates its status, and sets the current page to the total length of the book.
        /// </summary>
        /// <param name="userId">The ID of the user who finished reading the book.</param>
        /// <param name="userBookId">The ID of the book that was finished.</param>
        public async Task FinishBookAsync(string userId, int userBookId)
        {
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            if (userBook != null)
            {
                userBook.Status = "Finished";
                userBook.CurrentPage = userBook.Length;
                userBook.EndDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Marks a book as "Did Not Finish" (DNF) and records the end date.
        /// </summary>
        /// <param name="userId">The ID of the user who did not finish the book.</param>
        /// <param name="userBookId">The ID of the book that was marked as DNF.</param>
        public async Task MarkAsDNFAsync(string userId, int userBookId)
        {
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            if (userBook != null)
            {
                userBook.Status = "Dnf";
                userBook.EndDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Saves the edited journal entry for a book, updating various fields based on the provided model.
        /// </summary>
        /// <param name="journal">The journal entry to be updated.</param>
        /// <param name="model">The model containing the updated journal data.</param>
        /// <returns>A list of updated book journal entries for the user.</returns>
        public async Task<List<BookJournalViewModel>> SaveEditedJournalAsync(BookJournal journal, EditJournalViewModel model)
        {
            if(model.StartDate > model.EndDate)
            {
                throw new Exception("Start date cannot be after end date!");
            }

            journal.StartDate = model.StartDate?.ToUniversalTime();
            journal.EndDate = model.EndDate?.ToUniversalTime();
            journal.OverallRating = model.OverallRating;
            journal.OverallImpression = string.IsNullOrEmpty(model.OverallImpression) ? string.Empty : model.OverallImpression;
            journal.Insights = string.IsNullOrEmpty(model.Insights) ? string.Empty : model.Insights;
            journal.AuthorsAim = string.IsNullOrEmpty(model.AuthorsAim) ? string.Empty : model.AuthorsAim;
            journal.Recommendation = string.IsNullOrEmpty(model.Recommendation) ? string.Empty : model.Recommendation;
            journal.AdditionalNotes = string.IsNullOrEmpty(model.AdditionalNotes) ? string.Empty : model.AdditionalNotes;

            await _context.SaveChangesAsync();

            var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
            var updatedJournalEntries = await _context.BookJournals
                .Where(j => j.UserBook.UserID == userId)
                .Include(j => j.UserBook)
                .ToListAsync();

            var viewModel = updatedJournalEntries.Select(journal => new BookJournalViewModel
            {
                JournalID = journal.JournalID,
                UserBook = journal.UserBook,
                StartDate = journal.StartDate,
                EndDate = journal.EndDate,
                Rating = journal.OverallRating,
                OverallImpression = journal.OverallImpression,
                Insights = journal.Insights,
                AuthorsAim = journal.AuthorsAim,
                Recommendation = journal.Recommendation,
                AdditionalNotes = journal.AdditionalNotes
            }).ToList();

            return viewModel;
        }

        public async Task AddToJournalAsync(int userBookId, UserBook userBook)
        {
            var isUserBookExisting = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId)
                .FirstOrDefaultAsync();

            if(isUserBookExisting != null)
            {
                var journalEntry = new BookJournal
                {
                    UserBookID = userBookId,
                    StartDate = userBook.StartDate,
                    EndDate = userBook.EndDate,
                    OverallRating = null,
                    OverallImpression = "",
                    Insights = "",
                    AuthorsAim = "",
                    Recommendation = "",
                    AdditionalNotes = "",
                };

                _context.BookJournals.Add(journalEntry);
                await _context.SaveChangesAsync();
            }
        }
    }
}
