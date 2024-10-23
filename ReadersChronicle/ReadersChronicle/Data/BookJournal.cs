using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Data
{
    public class BookJournal
    {
        [Key]
        public int JournalID { get; set; }

        [ForeignKey("UserBook")]
        public int UserBookID { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public double? OverallRating { get; set; }

        public string OverallImpression { get; set; }

        public string Insights { get; set; }

        public string AuthorsAim { get; set; }

        public string Recommendation { get; set; }

        public string AdditionalNotes { get; set; }

        public virtual UserBook UserBook { get; set; }
    }

}
