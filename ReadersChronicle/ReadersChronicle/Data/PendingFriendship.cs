using System.ComponentModel.DataAnnotations.Schema;

namespace ReadersChronicle.Data
{
    public class PendingFriendship
    {
        public int FriendshipID { get; set; }

        [ForeignKey("InitiatorUser")]
        public string InitiatorUserID { get; set; }
        public User InitiatorUser { get; set; }

        [ForeignKey("ApprovingUser")]
        public string ApprovingUserID { get; set; }
        public User ApprovingUser { get; set; }
    }
}
