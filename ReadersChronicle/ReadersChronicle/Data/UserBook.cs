using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Data
{
    public class UserBook
    {
        [Key]
        public int UserBookID { get; set; }

        [ForeignKey("User")]
        public string UserID { get; set; }

        public string BookApiID { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Author { get; set; }

        public byte[] Picture { get; set; }

        public string PictureMimeType { get; set; }

        [Required]
        public int Length { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int CurrentPage { get; set; }

        public string Status { get; set; } = "WantToRead"; // Want to read, Currently reading, Finished, DNF (did not finish)


        public virtual User User { get; set; }
        //public virtual BookJournal BookJournal { get; set; }
    }

}
