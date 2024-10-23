using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Data
{
    public class UserBook
    {
        [Key]
        public int UserBookID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }

        [ForeignKey("Book")]
        public int BookID { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Author { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Folder { get; set; } // "Want to Read", "Currently Reading", "Read", "DNF"

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int CurrentPage { get; set; }

        public string Status { get; set; } // Not started, in progress, finished, dnf

        public double? OverallRating { get; set; }

        public virtual User User { get; set; }
        public virtual BookJournal BookJournal { get; set; }
    }

}
