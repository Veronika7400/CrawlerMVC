namespace CrawlerMVC.Models.EmailModels
{
    public class Subscriber
    {

        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid NotificationTypeId { get; set; } 
        public Guid SubscriptionTargetId { get; set; } 
        public string? SubscriptionWord { get; set; }
        public bool Active { get; set; }

    }
}
