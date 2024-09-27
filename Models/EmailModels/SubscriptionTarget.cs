namespace CrawlerMVC.Models.EmailModels
{
    public class SubscriptionTarget
    {
        public Guid SubscriptionTargetId { get; set; }
        public string TargetName { get; set; }

        public string Frequency { get; set; }
        public string? StoreId { get; set; }
    }
}
