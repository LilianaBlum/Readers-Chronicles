using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Models
{
    public class UserBookViewModel
    {
        public int UserBookID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int Length { get; set; }
        public string Status { get; set; }
        public string CoverImageBase64 { get; set; }
    }
}
