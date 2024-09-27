using CrawlerMVC.Areas.Email.Data;
using CrawlerMVC.Areas.Identity.Data;
using CrawlerMVC.Data;
using CrawlerMVC.Models.EmailModels;
using CrawlerMVC.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CrawlerMVC.Controllers
{
    public class SubscriberController : Controller
    {
        private readonly INotificationSender _emailSender;
        private readonly NLog.ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly EmailDbContext _context;
        private SubscriberService subscriberService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriberController"/> class.
        /// </summary>
        /// <param name="conf">Configuration settings.</param>
        /// <param name="logger">Logger for recording information and errors.</param>
        /// <param name="emailSender">Service for sending notifications.</param>
        /// <param name="context">Database context for email-related data.</param>
        /// <param name="identityContext">Database context for identity-related data.</param>
        /// <param name="serviceProvider">Factory for creating scoped services.</param>
        public SubscriberController(IConfiguration conf, NLog.ILogger logger, INotificationSender emailSender, EmailDbContext context, IdentityContext identityContext, IServiceScopeFactory serviceProvider)
        {
            _emailSender = emailSender;
            _logger = logger;
            _configuration = conf;
            _context = context;
            subscriberService = new SubscriberService(context, identityContext, serviceProvider);
        }

        /// <summary>
        /// Adds a subscriber to the notification system.
        /// </summary>
        /// <param name="frequency">The frequency of notifications.</param>
        /// <param name="searchWord">The keyword for filtering notifications.</param>
        /// <param name="notificationTypeName">The type of notification.</param>
        /// <param name="subscriptionTargetName">The target of the subscription.</param>
        /// <param name="storeId">Optional store identifier for subscription context.</param>
        /// <returns>A JSON result indicating success or failure of the operation.</returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddSubscriber(string frequency, string searchWord, string notificationTypeName, string subscriptionTargetName, string storeId = "")
        {
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await subscriberService.AddSubscriberToDatabase(frequency, searchWord, notificationTypeName, subscriptionTargetName, userId, storeId);

                if (result.Success)
                {
                    return Json(new { success = true });
                }
                else if (result.Denied)
                {
                    return Json(new { denied = true });
                }
                else
                {
                    return Json(new { error = true });
                }
            }
        }

        /// <summary>
        /// Unsubscribes a user from notifications.
        /// </summary>
        /// <param name="email">The email address of the user to unsubscribe.</param>
        /// <returns>A view displaying the result of the unsubscribe operation.</returns>
        public async Task<IActionResult> Unsubscribe(string email)
        {
            var message = await subscriberService.UnsubscribeUser(email); 
            return View("Unsubscribe", model: message); 
        }
    }
}
