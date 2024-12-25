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

        public async Task<IActionResult> Index()
        {
            var isUserAdmin = await _bookService.isUserAdmin();
            var searchBookViewModel = new SearchBookViewModel
            {
                isAdmin = isUserAdmin
            };
            return View(searchBookViewModel);
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

            var result = await _bookService.AddToLibrary(bookApiID, title, author, coverUrl, pageCount);

            if (!result)
            {
                return Json(new { success = false, message = "Book already added to your library." });
            }

            return Json(new { success = true, message = "Book added to your library!" });
        }

        public async Task<IActionResult> UserLibrary(string status = "CurrentlyReading")
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["SelectedStatus"] = status;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userBookViewModels = await _bookService.GetUserLibraryBooksAsync(userId, status);

            return View(userBookViewModels);
        }

        public async Task<IActionResult> BookJournal()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var viewModel = await _bookService.GetUserJournalAsync(userId);

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
            var result = await _bookService.ChangeStatus(userId, userBookId, newStatus);

            if (!result)
            {
                return Json(new { success = false, message = "Book not found!" });
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
            
            await _bookService.UpdateProgressAsync(userId, userBookId, currentPage);

            return RedirectToAction("UserLibrary", new { status = "CurrentlyReading" });
        }


        [HttpPost]
        public async Task<IActionResult> RemoveBook(int userBookId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _bookService.RemoveBookAsync(userId, userBookId);

            if (!result)
            {
                return Json(new { success = false, message = "Something went wrong!" });
            }

            return RedirectToAction("UserLibrary");
        }

        [HttpPost]
        public async Task<IActionResult> FinishBook(int userBookId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _bookService.FinishBookAsync(userId, userBookId);

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
            await _bookService.MarkAsDNFAsync(userId, userBookId);

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

            await _bookService.AddToJournalAsync(userBookId, userBook);

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
                    var viewModel = _bookService.SaveEditedJournalAsync(journal, model);

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
