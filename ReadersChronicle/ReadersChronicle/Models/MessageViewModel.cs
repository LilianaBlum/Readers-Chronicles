namespace ReadersChronicle.Models
{
    public class MessageViewModel
    {
        public string SenderUserId { get; set; }
        public string ReceiverUserId { get; set; }
        public string Text { get; set; }
        public DateTime SentAt { get; set; }
        public string SenderUserName { get; set; }
    }
}
