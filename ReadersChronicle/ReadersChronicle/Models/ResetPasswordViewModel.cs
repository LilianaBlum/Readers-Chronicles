using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Models
{
    public class ResetPasswordViewModel
    {
        public string UserNameOrEmail { get; set; }

        [Required]
        public string SecurityAnswer { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
