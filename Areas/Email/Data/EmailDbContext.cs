using CrawlerMVC.Models.EmailModels;
using Microsoft.EntityFrameworkCore;

namespace CrawlerMVC.Areas.Email.Data
{
    public class EmailDbContext : DbContext
    {

        public EmailDbContext(DbContextOptions<EmailDbContext> options) : base(options)
        {

        }

        public DbSet<Subscriber> Subscribers { get; set; }
        public DbSet<NotificationType> NotificationTypes { get; set; }
        public DbSet<SubscriptionTarget> SubscriptionTargets { get; set; }
    }
}
