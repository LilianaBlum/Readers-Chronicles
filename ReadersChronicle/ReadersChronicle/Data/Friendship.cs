using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadersChronicle.Data
{
    public class Friendship
    {
        [Key]
        public int FriendshipID { get; set; }

        [ForeignKey("User1")]
        public string UserID1 { get; set; }

        [ForeignKey("User2")]
        public string UserID2 { get; set; }

        public string FriendshipStatus {  get; set; }

        public virtual User User1 { get; set; }
        public virtual User User2 {  get; set; }
    }
}
