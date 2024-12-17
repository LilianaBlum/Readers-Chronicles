using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ReadersChronicle.Models
{
    public class RegisterViewModel
    {
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9_]{4,15}$", ErrorMessage = "Username must be 4-15 characters long and can only contain letters, numbers, and underscores.")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(40, ErrorMessage="Email cannot be longer than 40 symbols")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|yahoo\.com|outlook\.com)$", ErrorMessage = "Only Gmail, Yahoo, and Outlook emails are allowed.")]
        public string Email { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#]).{8,}$", ErrorMessage = "Password must contain at least one uppercase, one lowercase, one digit, and one special character.")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string SecurityQuestion {  get; set; }
        [Required]
        public string SecurityAnswer { get; set; }
    }
}