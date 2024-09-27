using System.Net.Mail;
using System.Net;
using HtmlAgilityPack;
using NLog;
using WebApiCrawler.SearchModels;
using System.Text;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using CrawlerMVC.Areas.Email.Data;
using CrawlerMVC.Data;
using CrawlerMVC.Models.EmailModels;

namespace CrawlerMVC.Services
{
    public class EmailSender : INotificationSender
    {
        private static readonly NLog.ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _configuration;
        private readonly EmailDbContext _context;
        private readonly IdentityContext _contextIdentity;
        private readonly IServiceScopeFactory _scopeFactory;

        private string _smtpServer; 
        private int _smtpPort;
        private string _smtpUser;
        private string _smtpPass;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailSender"/> class.
        /// </summary>
        /// <param name="conf">Configuration settings.</param>
        /// <param name="context">Database context for email-related data.</param>
        /// <param name="identityContext">Database context for identity-related data.</param>
        /// <param name="serviceProvider">Factory for creating scoped services.</param>
        public EmailSender(IConfiguration conf, EmailDbContext context, IdentityContext identityContext, IServiceScopeFactory serviceProvider)
        {
            _configuration = conf;
            _context = context;
            _contextIdentity = identityContext;
            _scopeFactory = serviceProvider;
            DefineSmtp();
        }
        /// <summary>
        /// Configures SMTP settings from configuration.
        /// </summary>
        private void DefineSmtp()
        {
            try
            {
                _smtpServer = _configuration["smtpServer"];
                if (string.IsNullOrEmpty(_smtpServer))
                {
                    _logger.Warn("SMTP server configuration is missing.");
                    return;
                }

                if (!int.TryParse(_configuration["smtpPort"], out _smtpPort))
                {
                    _logger.Warn("SMTP port configuration is not a valid integer.");
                    return;
                }

                _smtpUser = _configuration["smtpUser"];
                if (string.IsNullOrEmpty(_smtpUser))
                {
                    _logger.Warn("SMTP user configuration is missing.");
                    return;
                }

                _smtpPass = _configuration["smtpPass"];
                if (string.IsNullOrEmpty(_smtpPass))
                {
                    _logger.Warn("SMTP password configuration is missing.");
                    return;
                }

                _logger.Info("SMTP configuration is successfully defined.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error occurred during SMTP configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Notifies subscribers with the latest products based on the specified frequency.
        /// </summary>
        /// <param name="frequency">The frequency of notifications (e.g., daily, weekly).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task NotifySubscribers(string frequency)
        {
            string subject = char.ToUpper(frequency[0]) + frequency.Substring(1) + " Price List";

            using var smtpClient = await CreateSmtpClient();

            var activeSubscribers = await GetActiveEmailSubscribers();
            var subscriptionTargets = await GetSubscriptionTargets(frequency);
      
            List<Subscriber> allStoreSubscribers = await GetSubscribersByTarget(activeSubscribers, subscriptionTargets, "AllStores");
            List<Subscriber> specificStoreSubscribers = await GetSubscribersByTarget(activeSubscribers, subscriptionTargets, "SpecificStore");
           
            await ProcessSubscribers(allStoreSubscribers, subscriptionTargets, smtpClient, subject, true);
            await ProcessSubscribers(specificStoreSubscribers, subscriptionTargets, smtpClient, subject, false);

        }

        /// <summary>
        /// Creates and configures an SMTP client.
        /// </summary>
        /// <returns>An <see cref="SmtpClient"/> instance configured for sending emails.</returns>
        private async Task<SmtpClient> CreateSmtpClient()
        {
            var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_smtpUser, _smtpPass)
            };
            return smtpClient;
        }

        /// <summary>
        /// Retrieves active mail subscribers from the database.
        /// </summary>
        /// <returns>A list of active subscribers.</returns>
        private async Task<List<Subscriber>> GetActiveEmailSubscribers()
        {
            var emailNotificationTypeId = await _context.NotificationTypes
               .Where(nt => nt.NotificationName.ToLower().Trim() == "email")
               .Select(nt => nt.NotificationTypeId)
               .FirstOrDefaultAsync();

            return await _context.Subscribers
                .Where(s => s.Active && s.NotificationTypeId == emailNotificationTypeId)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves subscription targets based on the specified frequency.
        /// </summary>
        /// <param name="frequency">The frequency for filtering subscription targets.</param>
        /// <returns>A list of subscription targets matching the frequency.</returns>
        private async Task<List<SubscriptionTarget>> GetSubscriptionTargets(string frequency)
        {
          return await _context.SubscriptionTargets
                             .Where(st => st.Frequency == frequency)
                             .ToListAsync();
        }

        /// <summary>
        /// Filters subscribers based on subscription target.
        /// </summary>
        /// <param name="subscribers">List of subscribers.</param>
        /// <param name="targets">List of subscription targets.</param>
        /// <param name="targetName">Name of the target to filter by.</param>
        /// <returns>A list of subscribers matching the specified target name.</returns>
        private async Task<List<Subscriber>> GetSubscribersByTarget(List<Subscriber> subscribers, List<SubscriptionTarget> targets, string targetName)
        {
            return subscribers.Where(s =>
                targets.Any(t => t.SubscriptionTargetId == s.SubscriptionTargetId && t.TargetName == targetName)).ToList();
        }

        /// <summary>
        /// Processes and sends emails to subscribers based on whether they are subscribed to all stores or specific stores.
        /// </summary>
        /// <param name="subscribers">List of subscribers to process.</param>
        /// <param name="subscriptionTargets">List of subscription targets.</param>
        /// <param name="smtpClient">SMTP client for sending emails.</param>
        /// <param name="subject">Subject line for the email.</param>
        /// <param name="isAllStores">Indicates if the subscribers are subscribed to all stores.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessSubscribers(List<Subscriber> subscribers, List<SubscriptionTarget> subscriptionTargets, SmtpClient smtpClient, string subject, bool isAllStores)
        {
            if (isAllStores)
            {
                var allStoreGroups = subscribers.GroupBy(s => s.SubscriptionWord);
                foreach (var group in allStoreGroups)
                {
                    var searchWord = group.Key;
                    var products = await GetProductsAllStores(searchWord);

                    if (products != null && products.Any())
                    {
                        var htmlBody = await GenerateHtmlBody(products, searchWord);
                        await SendEmailsToSubscribers(group, htmlBody, smtpClient, subject);
                    }
                }
            }
            else
            {
                var specificStoreGroups = subscribers.GroupBy(s => new { s.SubscriptionWord, s.SubscriptionTargetId });

                foreach (var group in specificStoreGroups)
                {
                    var searchWord = group.Key.SubscriptionWord;
                    var storeId = subscriptionTargets.First(t => t.SubscriptionTargetId == group.Key.SubscriptionTargetId).StoreId;
                    var products = await GetProductsSpecificStore(searchWord, storeId);

                    if (products != null && products.Any())
                    {
                        var htmlBody = await GenerateHtmlBody(products, searchWord);
                        await SendEmailsToSubscribers(group, htmlBody, smtpClient, subject);
                    }
                }
            }
        }

        /// <summary>
        /// Sends emails to a list of subscribers with the provided HTML body content.
        /// </summary>
        /// <param name="subscribers">The subscribers to send emails to.</param>
        /// <param name="htmlBody">The HTML body content of the email.</param>
        /// <param name="smtpClient">SMTP client for sending emails.</param>
        /// <param name="subject">Subject line for the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SendEmailsToSubscribers(IEnumerable<Subscriber> subscribers, string htmlBody,SmtpClient smtpClient, string subject)
        {
            foreach (var subscriber in subscribers)
            {
                var email = await GetUserEmail(subscriber.UserId.ToString());
                if (!string.IsNullOrEmpty(email))
                {
                    var personalizedHtmlBody = htmlBody.Replace("{{unsubscribe_url}}", _configuration["UnsubscribeUrl"] + email);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_smtpUser, "Crawler"),
                        Subject = subject,
                        Body = personalizedHtmlBody,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    try
                    {
                        await smtpClient.SendMailAsync(mailMessage);
                        _logger.Info($"E-mail sent to {email}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Failed to send e-mail to {email}. Error: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the email address of a user by their user ID.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The email address of the user.</returns>
        private async Task<string> GetUserEmail(string userId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                return user?.Email;
            }
        }

        /// <summary>
        /// Retrieves products for all stores based on a search word.
        /// </summary>
        /// <param name="searchWord">The search word for filtering products.</param>
        /// <returns>A list of products with the lowest prices from all stores.</returns>
        private async Task<List<LowestPriceResult>> GetProductsAllStores(string searchWord)
        {
            using (var httpClient = new HttpClient())
            {
                if (string.IsNullOrEmpty(_configuration["BaseAdress"]) || string.IsNullOrEmpty(_configuration["GetLowestPriceInEachStoreWithoutPagination"]))
                {
                    _logger.Warn($"Configuration settings are not valid.");
                    return null;
                }

                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                string apiUrl = $"{_configuration["GetLowestPriceInEachStoreWithoutPagination"]}?searchWord={searchWord}";

                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrEmpty(jsonResponse))
                        {
                            _logger.Warn("jsonResponse is null or empty.");
                            return null;
                        }

                        var products = JsonConvert.DeserializeObject<List<WebApiCrawler.SearchModels.LowestPriceResult>>(jsonResponse);
                        products = products.OrderBy(product => product.PriceValue).ToList();
                        return products;
                    }
                    else
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        _logger.Warn($"jsonResponse is not successful. Status: {response.StatusCode}, Error: {errorResponse}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Internal server error.");
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieves products for a specific store based on a search word and store ID.
        /// </summary>
        /// <param name="searchWord">The search word for filtering products.</param>
        /// <param name="guid">The store ID.</param>
        /// <returns>A list of products with the lowest prices from the specified store.</returns>
        private async Task<List<LowestPriceResult>> GetProductsSpecificStore(string searchWord, string guid)
        {
            using (var httpClient = new HttpClient())
            {
                if (string.IsNullOrEmpty(_configuration["BaseAdress"]) || string.IsNullOrEmpty(_configuration["GetLowestPriceSpecificStoreWithoutPagination"]))
                {
                    _logger.Warn($"Configuration settings are not valid.");
                    return null;
                }

                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                string apiUrl = $"{_configuration["GetLowestPriceSpecificStoreWithoutPagination"]}?searchWord={searchWord}&guid={guid}";

                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrEmpty(jsonResponse))
                        {
                            _logger.Warn("jsonResponse is null or empty.");
                            return null;
                        }

                        var products = JsonConvert.DeserializeObject<List<WebApiCrawler.SearchModels.LowestPriceResult>>(jsonResponse);
                        products = products.OrderBy(product => product.PriceValue).ToList();
                        return products;
                    }
                    else
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        _logger.Warn($"jsonResponse is not successful. Status: {response.StatusCode}, Error: {errorResponse}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Internal server error.");
                    return null;
                }
            }
        }

        /// <summary>
        /// Generates the HTML body for the email based on the products and search word.
        /// </summary>
        /// <param name="products">List of products to include in the email.</param>
        /// <param name="searchWord">The search word used in the email subject and body.</param>
        /// <returns>The HTML body as a string.</returns>
        private async Task<string> GenerateHtmlBody(List<LowestPriceResult> products, string searchWord)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<style>");
            sb.AppendLine(@"
                   body {
                        padding: 40px 20px;
                        font-family: 'Montserrat', sans-serif;
                    }
                    .products {
                        display: flex;
                        flex-wrap: wrap;
                        gap: 20px;
                        justify-content: center;
                    }
                    .col {
                        flex: calc(33.33% - 20px);
                        max-width: 300px;
                        margin-bottom: 10px;
                        background-color: #fff;
                        border: 1px solid #ccc;
                        border-radius: 20px;
                        overflow: hidden;
                        display: flex;
                        flex-direction: column;
                        transition: margin 0.3s;
                        flex-basis: 100%;
                    }
                    .col:hover {
                        margin-top: -10px;
                        margin-bottom: 20px;
                    }
                    .card {
                        background-color: #fff;
                        border: 1px solid #ccc;
                        border-radius: 20px;
                        overflow: hidden;
                        transition: margin 0.3s;
                    }
                    .product-info {
                        padding: 10px;
                    }
                    .product-image-container {
                        position: relative;
                        flex-grow: 1;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                    }
                    .product-image-container img {
                        width: 300px;
                        height: 300px;
                        max-width: 100%;
                        max-height: 100%;
                        object-fit: contain;
                        border-radius: 20px 20px 0 0;
                    }
                    .product-details {
                        flex-grow: 1;
                        background-color: rgba(255, 255, 255, 0.8);
                        border-top: 1px solid #ccc;
                        border-radius: 0 0 20px 20px;
                        padding: 10px;
                    }
                    .product-details h5,
                    .product-details p {
                        margin: 0;
                    }
                    .product-name {
                        font-weight: bold;
                        font-size: 16px;
                    }
                    .view-page {
                        position: absolute;
                        top: 50%;
                        left: 50%;
                        transform: translate(-50%, -50%);
                        color: white;
                        padding: 5px 10px;
                        opacity: 0;
                        transition: opacity 0.3s;
                        border-radius: 10px;
                        background-color: rgba(0, 0, 0, 0.5);
                        text-align: center;
                        font-size: 16px;
                    }
                    .product-image-container:hover .view-page {
                        opacity: 1;
                    }
                    .price {
                        text-align: right;
                        font-weight: bold;
                        font-size: 22px;
                        margin-top: auto;
                    }
                    .productName {
                        max-width: 300px;
                        white-space: nowrap;
                        overflow: hidden;
                        text-overflow: ellipsis;
                    }
                    h1 {
                        font-size: 60px;
                        text-align: center;
                        margin-bottom: 20px;
                        font-family: 'Montserrat';
                        text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.5);
                        padding-top: 40px;
                        padding-bottom: 40px;
                    }
                    h5 {
                        font-family: 'Montserrat';
                        font-weight: bold;
                        font-size: 22px;
                    }
                    p {
                        font-family: 'Montserrat';
                    }
                ");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<h1>Cheapest Products for word " + searchWord + "</h1>");
            sb.AppendLine("<section class='products' id='productsContainer'>");

            if (products != null && products.Any())
            {
                foreach (var product in products)
                {
                    sb.AppendLine("<div class='col' onclick=\"window.location.href='" + product.Url + "'\">");
                    sb.AppendLine("<div class='card shadow-sm'>");
                    sb.AppendLine("<div class='product-info'>");
                    sb.AppendLine("<div class='product-image-container'>");
                    sb.AppendLine("<img src='" + product.ImageUrl + "' aria-label='Placeholder: Product Image' alt='Product Image'>");
                    sb.AppendLine("</div>");
                    sb.AppendLine("<div class='product-details'>");
                    sb.AppendLine("<h5>" + product.StoreName + "</h5>");
                    sb.AppendLine("<p class='productName'>" + product.ProductName + "</p>");
                    sb.AppendLine("<p class='price'>" + product.PriceValue + " €</p>");
                    sb.AppendLine("</div>");
                    sb.AppendLine("</div>");
                    sb.AppendLine("<div class='view-page'>View Page</div>");
                    sb.AppendLine("</div>");
                    sb.AppendLine("</div>");
                }
            }
            else
            {
                sb.AppendLine("<p>No products found.</p>");
            }

            sb.AppendLine("</section>");
            sb.AppendLine("<div class='unsubscribe'>");
            sb.AppendLine("<p>If you no longer wish to receive these emails, please <a href='{{unsubscribe_url}}'>unsubscribe here</a>.</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return await Task.FromResult(sb.ToString());
        }
    }
}
