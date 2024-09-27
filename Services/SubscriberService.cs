using CrawlerMVC.Areas.Email.Data;
using CrawlerMVC.Data;
using CrawlerMVC.Models.EmailModels;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace CrawlerMVC.Services
{
    public class SubscriberService
    {
        private static readonly NLog.ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly EmailDbContext _context;
        private readonly IdentityContext _contextIdentity;
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriberService"/> class.
        /// </summary>
        /// <param name="context">The database context for email-related data.</param>
        /// <param name="identityContext">The database context for identity-related data.</param>
        /// <param name="serviceProvider">Factory for creating service scopes.</param>
        public SubscriberService(EmailDbContext context, IdentityContext identityContext, IServiceScopeFactory serviceProvider)
        {
            _context = context;
            _contextIdentity = identityContext;
            _scopeFactory = serviceProvider;
        }

        /// <summary>
        /// Adds a subscriber to the database or updates an existing subscription.
        /// </summary>
        /// <param name="frequency">The frequency of the subscription (e.g., daily, weekly).</param>
        /// <param name="searchWord">The search word for the subscription.</param>
        /// <param name="notificationTypeName">The name of the notification type.</param>
        /// <param name="subscriptionTargetName">The name of the subscription target.</param>
        /// <param name="userId">The ID of the user subscribing.</param>
        /// <param name="storeId">The ID of the store (optional).</param>
        /// <returns>A tuple indicating whether the operation was successful and if the subscription was denied.</returns>
        public async Task<(bool Success, bool Denied)> AddSubscriberToDatabase(string frequency, string searchWord, string notificationTypeName, string subscriptionTargetName, string userId, string storeId = "")
        {
            var notificationTypeId = await GetNotificationTypeId(notificationTypeName);

            if (notificationTypeId == null)
            {
                _logger.Warn("NotificationTypeId is null for NotificationTypeName: {NotificationTypeName}", notificationTypeName);
                return (false, false);
            }

            var subscriptionTargetId = await GetSubscriptionTargetId(subscriptionTargetName, storeId, frequency);

            if (subscriptionTargetId == null)
            {
                _logger.Info("SubscriptionTargetId is null, creating new subscription target for SubscriptionTargetName: {SubscriptionTargetName}", subscriptionTargetName);
                subscriptionTargetId = await CreateSubscriptionTarget(subscriptionTargetName, storeId, frequency);
                if (await AddNewSubscriber(notificationTypeId, subscriptionTargetId, searchWord, userId))
                {
                    _logger.Info("Successfully added new subscriber");
                    return (true, false);
                }
                else
                {
                    _logger.Error("Failed to add new subscriber");
                    return (false, false);
                }
            }

            _logger.Info("Searching for existing subscriber");
            Subscriber subscriber = await FindSubscriber(notificationTypeId, subscriptionTargetId, searchWord, userId);
            if (subscriber != null)
            {
                if (subscriber.Active)
                {
                    _logger.Info("Subscriber is already active");
                    return (false, true);
                }
                else
                {
                    _logger.Info("Subscriber found but inactive, updating subscription");
                    if (await UpdateSubsription(subscriber))
                    {
                        _logger.Info("Successfully updated subscription");
                        return (true, false);
                    }
                    else
                    {
                        _logger.Error("Failed to update subscription");
                        return (false, false);
                    }
                }
            }
            else
            {
                _logger.Info("No existing subscriber found, adding new subscriber");
                await AddNewSubscriber(notificationTypeId, subscriptionTargetId, searchWord, userId);
                return (true, false);
            }
        }

        /// <summary>
        /// Updates an existing subscriber's subscription status.
        /// </summary>
        /// <param name="subscriber">The subscriber to update.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        private async Task<bool> UpdateSubsription(Subscriber subscriber)
        {
            try
            {
                _logger.Info("Updating subscription for SubscriberId: {SubscriberId}", subscriber.Id);
                var existingSubscriber = await _context.Subscribers.FirstOrDefaultAsync(s => s.Id == subscriber.Id);

                if (existingSubscriber == null)
                {
                    _logger.Warn("Subscriber not found: {SubscriberId}", subscriber.Id);
                    return false;
                }

                existingSubscriber.Active = subscriber.Active;

                _context.Subscribers.Update(existingSubscriber);
                await _context.SaveChangesAsync();

                _logger.Info("Successfully updated subscription for SubscriberId: {SubscriberId}", subscriber.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating subscription for subscriber: {SubscriberId}", subscriber.Id);
                return false;
            }
        }

        /// <summary>
        /// Finds an existing subscriber based on the provided criteria.
        /// </summary>
        /// <param name="notificationTypeId">The notification type ID.</param>
        /// <param name="subscriptionTargetId">The subscription target ID.</param>
        /// <param name="searchWord">The search word for the subscription.</param>
        /// <param name="userId">The ID of the user subscribing.</param>
        /// <returns>The subscriber if found; otherwise, null.</returns>
        private async Task<Subscriber> FindSubscriber(string notificationTypeId, string subscriptionTargetId, string searchWord, string? userId)
        {
            try
            {
                _logger.Info("Finding subscriber with NotificationTypeId: {NotificationTypeId}, SubscriptionTargetId: {SubscriptionTargetId}, SearchWord: {SearchWord}, UserId: {UserId}", notificationTypeId, subscriptionTargetId, searchWord, userId);
                var subscriber = await _context.Subscribers.FirstOrDefaultAsync(s =>
                    s.NotificationTypeId.ToString() == notificationTypeId &&
                    s.SubscriptionTargetId.ToString() == subscriptionTargetId &&
                    s.SubscriptionWord == searchWord &&
                    s.UserId.ToString() == userId);

                return subscriber;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error finding subscriber with NotificationTypeId: {NotificationTypeId}, SubscriptionTargetId: {SubscriptionTargetId}, SearchWord: {SearchWord}, UserId: {UserId}",
                    notificationTypeId, subscriptionTargetId, searchWord, userId);
                return null;
            }
        }

        /// <summary>
        /// Adds a new subscriber to the database.
        /// </summary>
        /// <param name="notificationTypeId">The notification type ID.</param>
        /// <param name="subscriptionTargetId">The subscription target ID.</param>
        /// <param name="searchWord">The search word for the subscription.</param>
        /// <param name="userId">The ID of the user subscribing.</param>
        /// <returns>True if the addition was successful; otherwise, false.</returns>
        private async Task<bool> AddNewSubscriber(string notificationTypeId, string subscriptionTargetId, string searchWord, string? userId)
        {
            try
            {
                _logger.Info("Adding new subscriber with NotificationTypeId: {NotificationTypeId}, SubscriptionTargetId: {SubscriptionTargetId}, SearchWord: {SearchWord}, UserId: {UserId}", notificationTypeId, subscriptionTargetId, searchWord, userId);
                var newSubscriber = new Subscriber
                {
                    Id = Guid.NewGuid(),
                    UserId = new Guid(userId),
                    NotificationTypeId = new Guid(notificationTypeId),
                    SubscriptionTargetId = new Guid(subscriptionTargetId),
                    SubscriptionWord = searchWord,
                    Active = true
                };

                await _context.Subscribers.AddAsync(newSubscriber);
                await _context.SaveChangesAsync();

                _logger.Info("Successfully added new subscriber with Id: {SubscriberId}", newSubscriber.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error adding new subscriber with NotificationTypeId: {NotificationTypeId}, SubscriptionTargetId: {SubscriptionTargetId}, SearchWord: {SearchWord}, UserId: {UserId}",
                    notificationTypeId, subscriptionTargetId, searchWord, userId);
                return false;
            }
        }

        /// <summary>
        /// Creates a new subscription target in the database.
        /// </summary>
        /// <param name="subscriptionTargetName">The name of the subscription target.</param>
        /// <param name="storeId">The ID of the store.</param>
        /// <param name="frequency">The frequency of the subscription.</param>
        /// <returns>The ID of the created subscription target.</returns>
        private async Task<string> CreateSubscriptionTarget(string subscriptionTargetName, string storeId, string frequency)
        {
            try
            {
                _logger.Info("Creating subscription target with TargetName: {SubscriptionTargetName}, StoreId: {StoreId}, Frequency: {Frequency}", subscriptionTargetName, storeId, frequency);
                var newSubscriptionTarget = new SubscriptionTarget
                {
                    SubscriptionTargetId = Guid.NewGuid(),
                    TargetName = subscriptionTargetName,
                    StoreId = storeId,
                    Frequency = frequency
                };

                _context.SubscriptionTargets.Add(newSubscriptionTarget);
                await _context.SaveChangesAsync();

                _logger.Info("Created new subscription target with ID: {SubscriptionTargetId}", newSubscriptionTarget.SubscriptionTargetId);

                return newSubscriptionTarget.SubscriptionTargetId.ToString();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating subscription target with TargetName: {SubscriptionTargetName}, StoreId: {StoreId}, Frequency: {Frequency}", subscriptionTargetName, storeId, frequency);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the ID of an existing subscription target.
        /// </summary>
        /// <param name="subscriptionTargetName">The name of the subscription target.</param>
        /// <param name="storeId">The ID of the store.</param>
        /// <param name="frequency">The frequency of the subscription.</param>
        /// <returns>The ID of the subscription target if found; otherwise, null.</returns>
        private async Task<string> GetSubscriptionTargetId(string subscriptionTargetName, string storeId, string frequency)
        {
            try
            {
                _logger.Info("Getting subscription target ID for TargetName: {SubscriptionTargetName}, StoreId: {StoreId}, Frequency: {Frequency}", subscriptionTargetName, storeId, frequency);
                var subscriptionTarget = await _context.SubscriptionTargets
                    .Where(c => c.TargetName == subscriptionTargetName && c.StoreId == storeId && c.Frequency.ToLower() == frequency.ToLower())
                    .FirstOrDefaultAsync();

                if (subscriptionTarget == null)
                {
                    _logger.Warn("Subscription target not found for TargetName: {SubscriptionTargetName}, StoreId: {StoreId}, Frequency: {Frequency}", subscriptionTargetName, storeId, frequency);
                    return null;
                }

                return subscriptionTarget.SubscriptionTargetId.ToString();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting subscription target ID for TargetName: {SubscriptionTargetName}, StoreId: {StoreId}, Frequency: {Frequency}", subscriptionTargetName, storeId, frequency);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the ID of an existing notification type.
        /// </summary>
        /// <param name="notificationTypeName">The name of the notification type.</param>
        /// <returns>The ID of the notification type if found; otherwise, null.</returns>
        private async Task<string> GetNotificationTypeId(string notificationTypeName)
        {
            try
            {
                _logger.Info("Getting notification type ID for NotificationTypeName: {NotificationTypeName}", notificationTypeName);
                var notificationType = await _context.NotificationTypes
                    .Where(c => c.NotificationName == notificationTypeName)
                    .FirstOrDefaultAsync();

                if (notificationType == null)
                {
                    _logger.Warn("Notification type not found: {NotificationTypeName}", notificationTypeName);
                    return null;
                }

                return notificationType.NotificationTypeId.ToString();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting notification type ID for NotificationTypeName: {NotificationTypeName}", notificationTypeName);
                throw;
            }
        }

        /// <summary>
        /// Unsubscribes a user by email address.
        /// </summary>
        /// <param name="email">The email address of the user to unsubscribe.</param>
        /// <returns>A message indicating the result of the unsubscribe operation.</returns>

        public async Task<string> UnsubscribeUser(string email)
        {
            string userId = await FindUserId(email);
            if (userId == null)
            {
                _logger.Warn("Error while unsubscribing. User Id not found"); 
                return "Error while unsubscribing.";
            }
            var subscriber = await _context.Subscribers.FirstOrDefaultAsync(s => s.UserId.ToString() == userId);
            if (subscriber == null)
            {
                _logger.Warn("Error while unsubscribing. Subscriber not found");
                return "Error while unsubscribing.";
            }

            subscriber.Active = false;
            await _context.SaveChangesAsync();

            return "You have been unsubscribed.";
        }

        /// <summary>
        /// Finds the user ID associated with a given email address.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>The user ID if found; otherwise, null.</returns>
        private async Task<string> FindUserId(string email)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
                var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
                return user?.Id;
            }
        }
    }
}
