using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Data
{
    public class User : IdentityUser
    {
        [Required]
        public DateTime JoinDate { get; set; }

        [Required]
        public string UserType { get; set; } = "user";

        public bool IsOnline { get; set; }
        [Required]
        public string SecurityQuestion { get; set; }
        [Required]
        public string SecurityAnswerHash { get; set; }

        public virtual Profile Profile { get; set; }
        public ICollection<Friendship> Friendships1 { get; set; } // For friendships initiated by this user
        public ICollection<Friendship> Friendships2 { get; set; } // For friendships received by this user
        public ICollection<Message> SentMessages { get; set; } // Messages sent by the user
        public ICollection<Message> ReceivedMessages { get; set; } // Messages received by the user
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<UserBook> UserBooks { get; set; }
    }
}
