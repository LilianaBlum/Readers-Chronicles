using System.ComponentModel.DataAnnotations.Schema;

namespace ReadersChronicle.Data
{
    public class ArticleRating
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public string UserId { get; set; }

        [ForeignKey("ArticleId")]
        public virtual Article Article { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
