using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadersChronicle.Data
{
    public class Profile
    {
        [Key]
        public int ProfileID { get; set; }
        public string UserID { get; set; }

        public string Bio {  get; set; }
        public string ImageMimeType { get; set; }
        public byte[] ImageData { get; set; }

        public virtual User User {  get; set; }
    }
}
