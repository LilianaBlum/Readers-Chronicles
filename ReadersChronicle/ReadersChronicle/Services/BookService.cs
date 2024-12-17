using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using System.Security.Claims;

namespace ReadersChronicle.Services
{
    public class BookService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public BookService(HttpClient httpClient, ApplicationDbContext context, UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<BookViewModel>> SearchBooksAsync(string query)
        {
            var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
            var requestUri = $"https://www.googleapis.com/books/v1/volumes?q={query}";

            var response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            var bookIds = json["items"]
        .Select(item => (string)item["id"])
        .ToList();

            var books = json["items"]
                .Select(item => new BookViewModel
                {
                    Title = (string)item["volumeInfo"]["title"],
                    Author = string.Join(", ", item["volumeInfo"]["authors"] ?? new JArray()),
                    BookId = (string)item["id"],
                    PageCount = (int?)item["volumeInfo"]["pageCount"] ?? 0,
                    CoverUrl = (string)item["volumeInfo"]["imageLinks"]?["thumbnail"],
                })
            .ToList();

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

        public async Task<byte[]> GetBookCoverImage(string coverUrl)
        {
            if (string.IsNullOrEmpty(coverUrl))
            {
                return null;
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
                OverallImpression = journal.OverallImpression,
                Insights = journal.Insights,
                AuthorsAim = journal.AuthorsAim,
                Recommendation = journal.Recommendation,
                AdditionalNotes = journal.AdditionalNotes
            }).ToList();

            return viewModel;
        }

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
                    userBook.StartDate = DateTime.Now;
                }

                userBook.Status = status;
                await _context.SaveChangesAsync();
            }
            return true;
        }

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

        public async Task FinishBookAsync(string userId, int userBookId)
        {
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            if (userBook != null)
            {
                userBook.Status = "Finished";
                userBook.CurrentPage = userBook.Length;
                userBook.EndDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAsDNFAsync(string userId, int userBookId)
        {
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            if (userBook != null)
            {
                userBook.Status = "Dnf";
                userBook.EndDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<BookJournalViewModel>> SaveEditedJournalAsync(BookJournal journal, EditJournalViewModel model)
        {
            journal.StartDate = model.StartDate;
            journal.EndDate = model.EndDate;
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
