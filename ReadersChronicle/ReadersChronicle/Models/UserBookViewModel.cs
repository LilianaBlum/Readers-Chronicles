using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Models
{
    public class UserBookViewModel
    {
        public int UserBookID { get; set; }

        public int BookApiID { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Author { get; set; }

        public byte[] Picture { get; set; }

        public string PictureMimeType { get; set; }

        [Required]
        public int Length { get; set; }

        public string Status { get; set; } = "WantToRead"; // Want to read, Currently reading, Finished, DNF

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int CurrentPage { get; set; }

        public string StatusDisplay => Status switch
        {
            "WantToRead" => "Want to Read",
            "CurrentlyReading" => "Currently Reading",
            "Finished" => "Finished",
            "DNF" => "Did Not Finish",
            _ => "Unknown"
        };
    }
}
