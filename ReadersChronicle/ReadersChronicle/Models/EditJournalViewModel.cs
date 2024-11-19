namespace ReadersChronicle.Models
{
    public class EditJournalViewModel
    {
        public int JournalID { get; set; }
        public int UserBookID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? OverallRating { get; set; }
        public string OverallImpression { get; set; }
        public string Insights { get; set; }
        public string AuthorsAim { get; set; }
        public string Recommendation { get; set; }
        public string AdditionalNotes { get; set; }
    }
}
