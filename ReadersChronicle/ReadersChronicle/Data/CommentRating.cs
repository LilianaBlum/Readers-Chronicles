namespace ReadersChronicle.Data
{
    public class CommentRating
    {
        public int Id { get; set; }
        public int CommentId { get; set; }
        public string UserId { get; set; }

        public virtual Comment Comment { get; set; }
        public virtual User User { get; set; }
    }
}
