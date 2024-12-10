using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Models
{
    public class EditArticleViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; }
    }
}
