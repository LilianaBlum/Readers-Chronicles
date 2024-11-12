using Microsoft.AspNetCore.Mvc;
using ReadersChronicle.Models;
using ReadersChronicle.Services;
using System.Diagnostics;

namespace ReadersChronicle.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BookService _bookService;

        public HomeController(ILogger<HomeController> logger, BookService bookService)
        {
            _logger = logger;
            _bookService = bookService;
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
    }
}
