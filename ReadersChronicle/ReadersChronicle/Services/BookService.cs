using Newtonsoft.Json.Linq;
using ReadersChronicle.Models;

namespace ReadersChronicle.Services
{
    public class BookService
    {
        private readonly HttpClient _httpClient;

        public BookService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<BookViewModel>> SearchBooksAsync(string query)
        {
            var requestUri = $"https://www.googleapis.com/books/v1/volumes?q={query}";

            var response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            var books = json["items"]
                .Select(item => new BookViewModel
                {
                    Title = (string)item["volumeInfo"]["title"],
                    Author = string.Join(", ", item["volumeInfo"]["authors"] ?? new JArray()),
                    BookId = (string)item["id"],
                    PageCount = (int?)item["volumeInfo"]["pageCount"] ?? 0,
                    CoverUrl = (string)item["volumeInfo"]["imageLinks"]?["thumbnail"]
                })
                .ToList();

            return books;
        }
    }
}
