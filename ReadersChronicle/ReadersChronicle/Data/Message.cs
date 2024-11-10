using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadersChronicle.Data
{
    public class Message
    {
        [Key]
        public int MessageID { get; set; }

        [ForeignKey("Sender")]
        public string SenderID { get; set; } // Foreign key for the sender
        [ForeignKey("Receiver")]
        public string ReceiverID { get; set; } // Foreign key for the receiver

        [Required]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsRead { get; set; }

        public virtual User Sender { get; set; } // The user who sent the message
        public virtual User Receiver { get; set; } // The user who received the message
    }
}
