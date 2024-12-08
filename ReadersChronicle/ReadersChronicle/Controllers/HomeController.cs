using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using ReadersChronicle.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace ReadersChronicle.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BookService _bookService;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, BookService bookService, ApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _bookService = bookService;
            _context = applicationDbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> SearchBooks(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return View("Index");
            }

            var books = await _bookService.SearchBooksAsync(query);
            return PartialView("_BookSearchResults", books);
        }

        [HttpPost]
        public async Task<IActionResult> AddToLibrary(string bookApiID, string title, string author, string coverUrl, int pageCount)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingBook = await _context.UserBooks
            .FirstOrDefaultAsync(b => b.UserID == userId && b.BookApiID == bookApiID);

            if (existingBook != null)
            {
                return Json(new { success = false, message = "Book already added to your library." });
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

            return Json(new { success = true, message = "Book added to your library!" });
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

        public async Task<IActionResult> UserLibrary(string status = "CurrentlyReading")
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["SelectedStatus"] = status;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userBooks = await _context.UserBooks
                .Where(b => b.UserID == userId && b.Status == status)
                .ToListAsync();

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

            return View(userBookViewModels);
        }

        public async Task<IActionResult> BookJournal()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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



            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int userBookId, string newStatus)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            if (userBook != null)
            {
                if (newStatus == "CurrentlyReading")
                {
                    userBook.StartDate = DateTime.Now;
                }

                userBook.Status = newStatus;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("UserLibrary", new { status = newStatus });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProgress(int userBookId, int currentPage)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            if (userBook != null)
            {
                userBook.CurrentPage = currentPage;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("UserLibrary", new { status = "CurrentlyReading" });
        }

        [HttpPost]
        public async Task<IActionResult> FinishBook(int userBookId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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

            return RedirectToAction("UserLibrary", new { status = "Finished" });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsDNF(int userBookId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId)
                .FirstOrDefaultAsync();

            if (userBook != null)
            {
                userBook.Status = "Dnf";
                userBook.EndDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("UserLibrary", new { status = "Dnf" });
        }


        [HttpPost]
        public async Task<IActionResult> AddToJournal(int userBookId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "You must be logged in to add a book to your journal." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userBook = await _context.UserBooks
                .Where(b => b.UserBookID == userBookId && b.UserID == userId &&
                            (b.Status == "Finished" || b.Status == "Dnf"))
                .FirstOrDefaultAsync();

            if (userBook == null)
            {
                return Json(new { success = false, message = "Book not found or cannot be added to journal." });
            }

            var existingJournalEntry = await _context.BookJournals
                .FirstOrDefaultAsync(j => j.UserBookID == userBookId);

            if (existingJournalEntry != null)
            {
                return Json(new { success = false, message = "This book is already in your journal." });
            }

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

            return Json(new { success = true, message = "The book has been successfully added to your journal." });
        }

        public async Task<IActionResult> EditJournal(int id)
        {
            var journal = await _context.BookJournals
                .Where(j => j.JournalID == id)
                .Select(j => new EditJournalViewModel
                {
                    JournalID = j.JournalID,
                    UserBookID = j.UserBookID,
                    Title = j.UserBook.Title,
                    Author = j.UserBook.Author,
                    StartDate = j.StartDate,
                    EndDate = j.EndDate,
                    OverallRating = j.OverallRating,
                    OverallImpression = j.OverallImpression,
                    Insights = j.Insights,
                    AuthorsAim = j.AuthorsAim,
                    Recommendation = j.Recommendation,
                    AdditionalNotes = j.AdditionalNotes
                })
                .FirstOrDefaultAsync();

            if (journal == null)
            {
                return NotFound();
            }

            return Json(journal);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEditedJournal(EditJournalViewModel model)
        {
            try
            {
                var journal = await _context.BookJournals.FindAsync(model.JournalID);
                if (journal != null)
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

                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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

                    return Json(new { success = true, updatedJournalEntries = viewModel });
                }
                else
                {
                    return Json(new { success = false, message = "Journal not found." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
