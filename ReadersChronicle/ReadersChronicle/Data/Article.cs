namespace ReadersChronicle.Data
{
    public class Article
    {
        public int Id { get; set; }
        public string UserId {  get; set; }
        public DateTime TimeCreated {  get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte[] Picture { get; set; }

        public string PictureMimeType { get; set; }
        public virtual ICollection<ArticleRating> ArticleRatings { get; set; }
        public virtual User User { get; set; }
    }
}
