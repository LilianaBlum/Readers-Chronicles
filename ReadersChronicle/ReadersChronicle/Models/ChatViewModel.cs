using ReadersChronicle.Data;

namespace ReadersChronicle.Models
{
    public class ChatViewModel
    {
        public string CurrentUserId { get; set; }
        public string ReceiverUserId { get; set; }
        public string ReceiverUserName { get; set; }
        public List<MessageViewModel> Messages { get; set; }
    }

}
