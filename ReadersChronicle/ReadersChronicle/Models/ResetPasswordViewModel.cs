﻿using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Models
{
    public class ResetPasswordViewModel
    {
        public string UserNameOrEmail { get; set; }

        [Required]
        public string SecurityAnswer { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#]).{8,}$", ErrorMessage = "Password must contain at least one uppercase, one lowercase, one digit, and one special character.")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
