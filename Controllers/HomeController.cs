using CrawlerMVC.Areas.Email.Data;
using CrawlerMVC.Models;
using CrawlerMVC.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace CrawlerMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly NLog.ILogger _logger;
        private IConfiguration configuration;

        public HomeController(IConfiguration conf, NLog.ILogger logger)
        {
            _logger = logger;
            this.configuration = conf;
        }

        /// <summary>
        /// Retrieves all stores and displays them on the Index page
        /// </summary>
        /// <returns>
        /// Returns the Index view
        /// </returns>
        public async Task<IActionResult> Index()
        {
            using (var httpClient = new HttpClient())
            {
                if (!configuration["BaseAdress"].Any() || !configuration["GetAllStores"].Any())
                {
                    _logger.Warn($"Configuration settings are not valid.");
                    return View();
                }

                httpClient.BaseAddress = new Uri(configuration["BaseAdress"]);
                string apiUrl = configuration["GetAllStores"];
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        if (jsonResponse == null)
                        {
                            _logger.Warn("jsonResponse is null.");
                            return View();
                        }
                        var stores = JsonConvert.DeserializeObject<List<WebApiCrawler.Models.WebStore>>(jsonResponse);
                        stores = stores.OrderBy(store => store.Name).ToList();
                        return View(stores);
                    }
                    else
                    {
                        _logger.Warn($"API call was not successful. Status code: {response.StatusCode}. Reason: {response.ReasonPhrase}");
                        return View();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"{ex.Message}.");
                    return View();
                }
            }
        }

        /// <summary>
        /// Displays the Privacy page
        /// </summary>
        /// <returns>
        /// Returns the Privacy view
        /// </returns>
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
