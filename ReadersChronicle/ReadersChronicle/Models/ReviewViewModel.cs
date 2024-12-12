namespace ReadersChronicle.Models
{
    public class ReviewViewModel
    {
        public int ReviewID { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; }
    }
}
