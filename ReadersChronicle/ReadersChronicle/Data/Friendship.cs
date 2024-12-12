using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadersChronicle.Data
{
    public class Friendship
    {
        public int FriendshipID { get; set; }

        [ForeignKey(nameof(User1))]
        public string User1ID { get; set; }
        public User User1 { get; set; }

        [ForeignKey(nameof(User2))]
        public string User2ID { get; set; }
        public User User2 { get; set; }
    }
}
