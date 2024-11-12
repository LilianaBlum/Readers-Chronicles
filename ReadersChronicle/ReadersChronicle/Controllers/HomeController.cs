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
                return null; // Return null if no cover URL is provided
            }

            try
            {
                // Initialize HttpClient to download the image
                using (var httpClient = new HttpClient())
                {
                    // Get the image content as a byte array
                    var imageBytes = await httpClient.GetByteArrayAsync(coverUrl);

                    return imageBytes;
                }
            }
            catch (Exception ex)
            {
                // Log error if the image could not be retrieved (you can use a logging library)
                Console.WriteLine($"Error downloading book cover image: {ex.Message}");
                return null; // Return null if an error occurred
            }
        }

        public async Task<IActionResult> UserLibrary()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userBooks = await _context.UserBooks
                .Where(b => b.UserID == userId)
                .ToListAsync();

            // Map UserBook to UserBookViewModel
            var userBookViewModels = userBooks.Select(book => new UserBookViewModel
            {
                Title = book.Title,
                Author = book.Author,
                Length = book.Length,
                Status = book.Status,
                CoverImageBase64 = book.Picture != null ? Convert.ToBase64String(book.Picture) : null  // Convert image to base64
            }).ToList();

            return View(userBookViewModels);
        }
    }
}
