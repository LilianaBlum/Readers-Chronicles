namespace ReadersChronicle.Models
{
    public class ArticleDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte[] Picture { get; set; }
        public string PictureMimeType { get; set; }
        public string UserName { get; set; }
        public DateTime TimeCreated { get; set; }
    }

}
