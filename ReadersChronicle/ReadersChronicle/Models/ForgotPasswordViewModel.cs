using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        public string UserNameOrEmail { get; set; }
    }
}
