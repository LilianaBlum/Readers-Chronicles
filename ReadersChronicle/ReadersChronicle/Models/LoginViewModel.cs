using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Models
{
    public class LoginViewModel
    {
        [Display(Name = "Username")]
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
}