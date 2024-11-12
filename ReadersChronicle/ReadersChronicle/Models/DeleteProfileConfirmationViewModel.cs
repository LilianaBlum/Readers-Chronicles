using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Models
{
    public class DeleteProfileConfirmationViewModel
    {
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
