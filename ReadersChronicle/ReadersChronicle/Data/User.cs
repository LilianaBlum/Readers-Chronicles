using System.ComponentModel.DataAnnotations;

namespace ReadersChronicle.Data
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(25)]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email {  get; set; }

        [Required]
        [StringLength(30)]
        public string Password { get; set; }

        [Required]
        public DateTime JoinDate { get; set; }

        [Required]
        public string UserType { get; set; }

        public virtual Profile Profile { get; set; }
        public ICollection<Friendship> Friendships1 { get; set; } // For friendships initiated by this user
        public ICollection<Friendship> Friendships2 { get; set; } // For friendships received by this user
        public ICollection<Message> SentMessages { get; set; } // Messages sent by the user
        public ICollection<Message> ReceivedMessages { get; set; } // Messages received by the user
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<UserBook> UserBooks { get; set; }
    }
}
