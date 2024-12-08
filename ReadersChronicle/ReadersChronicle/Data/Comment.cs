namespace ReadersChronicle.Data
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int ArticleId { get; set; }
        public string UserId { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Article Article { get; set; }
        public virtual User User { get; set; }
    }
}
