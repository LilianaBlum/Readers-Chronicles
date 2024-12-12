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

        public BookService(HttpClient httpClient,  ApplicationDbContext context, UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
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
    }
}
