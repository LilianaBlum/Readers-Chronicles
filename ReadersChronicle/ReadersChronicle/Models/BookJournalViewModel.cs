using ReadersChronicle.Data;

namespace ReadersChronicle.Models
{
    public class BookJournalViewModel
    {
        public int JournalID { get; set; }
        public UserBook UserBook { get; set; }  // Reference to UserBook

        public string Title => UserBook?.Title;
        public string Author => UserBook?.Author;
        public string Status => UserBook?.Status;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? Rating { get; set; }
        public string? OverallImpression { get; set; }
        public string Insights { get; set; }
        public string AuthorsAim { get; set; }
        public string Recommendation { get; set; }
        public string AdditionalNotes { get; set; }

        public string CoverImageBase64 => UserBook?.Picture != null
            ? Convert.ToBase64String(UserBook.Picture)
            : null;
    }
}
