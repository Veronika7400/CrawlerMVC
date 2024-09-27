using System.Runtime.CompilerServices;

namespace CrawlerMVC.Services
{
    public interface INotificationSender
    {
       async Task NotifySubscribers(string frequency) { }
    }

}
