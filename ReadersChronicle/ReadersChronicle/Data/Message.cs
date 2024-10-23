using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadersChronicle.Data
{
    public class Message
    {
        [Key]
        public int MessageID { get; set; }

        [ForeignKey("Sender")]
        public int SenderID { get; set; } // Foreign key for the sender
        [ForeignKey("Receiver")]
        public int ReceiverID { get; set; } // Foreign key for the receiver

        [Required]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsRead { get; set; }

        public User Sender { get; set; } // The user who sent the message
        public User Receiver { get; set; } // The user who received the message
    }
}
