using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadersChronicle.Data
{
    public class Profile
    {
        [Key]
        public int ProfileID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }

        public string Bio {  get; set; }
        
        public string ProfilePicture { get; set; }

        public virtual User User {  get; set; }
    }
}
