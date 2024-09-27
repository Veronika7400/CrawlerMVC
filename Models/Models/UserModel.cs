namespace CrawlerMVC.Models.Models
{
    public class UserModel
    {
        public string id { get; set; }
        public string fullname { get; set; }
        public string email { get; set; }
        public string username { get; set; }
        public string role { get; set; }
        public string avatar { get; set; }
        public bool active { get; set; }
    }
}
