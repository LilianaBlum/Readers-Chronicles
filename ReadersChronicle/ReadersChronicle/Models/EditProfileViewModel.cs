using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ReadersChronicle.Models
{
    public class EditProfileViewModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Bio { get; set; }
    }
}
