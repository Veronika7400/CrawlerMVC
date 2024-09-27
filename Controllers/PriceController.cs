using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NLog.Filters;
using System;
using System.Net.Http;
using WebApiCrawler.Models;

namespace CrawlerMVC.Controllers
{
    public class PriceController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly NLog.ILogger _logger;

        public PriceController(IConfiguration conf, NLog.ILogger logger)
        {
            _logger = logger;
            _configuration = conf;
        }

        /// <summary>
        /// Displays the All Stores Search Page page
        /// </summary>
        /// <returns>
        /// Returns the SearchPageAllStores view
        /// </returns>
        public IActionResult SearchPageAllStores()
        {
            return View();
        }

        /// <summary>
        /// Retrieves all stores and displays them on the Specific Store Search Page  page
        /// </summary>
        /// <returns>
        /// Returns the SearchPageSpecificStore view
        /// </returns>
        public async Task<IActionResult> SearchPageSpecificStore()
        {
            using (var httpClient = new HttpClient())
            {
                if (!_configuration["BaseAdress"].Any() || !_configuration["GetAllStores"].Any())
                {
                    _logger.Warn($"Configuration settings are not valid.");
                    return View();
                }

                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                string apiUrl = _configuration["GetAllStores"];
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
                        _logger.Warn("jsonResponse is not successful.");
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
        /// Retrieves a list of cheapest product from all stores based on the provided search word and displays them on the Product List All Stores page
        /// </summary>
        /// <param name="searchWord">The search word used to query the products</param>
        /// <returns>
        /// Returns the ProductListAllStores view/// </returns>
        public async Task<IActionResult> ProductListAllStores(string searchWord, int page = 1, int pageSize = 10)
        {
            using (var httpClient = new HttpClient())
            {
                if(!_configuration["BaseAdress"].Any() || !_configuration["GetLowestPriceInEachStore"].Any())
                {
                    _logger.Warn($"Configuration settings are not valid.");
                    return View(); 
                }
                ViewData["SearchWord"] = searchWord;
                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                string apiUrl = $"{_configuration["GetLowestPriceInEachStore"]}?searchWord={searchWord}&page={page}&pageSize={pageSize}";
             
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
                        var products = JsonConvert.DeserializeObject<List<WebApiCrawler.SearchModels.LowestPriceResult>>(jsonResponse);
                        products = products.OrderBy(product => product.PriceValue).ToList();
                        return View(products);
                    }
                    else
                    {
                        _logger.Warn("jsonResponse is not successful.");
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
        /// Retrieves a list of cheapest products from all stores based on the provided search word and returns them as a JSON response
        /// </summary>
        /// <param name="searchWord">The search word used to query the products</param>
        /// <returns>Returns a JSON response with the list of cheapest products</returns>
        public async Task<IActionResult> ProductListAllStoresReturnList(string searchWord, int page = 1, int pageSize = 10)
        {
            using (var httpClient = new HttpClient())
            {
                if (!_configuration["BaseAdress"].Any() || !_configuration["GetLowestPriceInEachStore"].Any())
                {
                    _logger.Warn($"Configuration settings are not valid.");
                    return BadRequest("Configuration settings are not valid.");
                }
                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                string apiUrl = $"{_configuration["GetLowestPriceInEachStore"]}?searchWord={searchWord}&page={page}&pageSize={pageSize}";
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                   
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        if (jsonResponse == null)
                        {
                            _logger.Warn("jsonResponse is null.");
                            return NotFound("No products found.");
                        }
                        var products = JsonConvert.DeserializeObject<List<WebApiCrawler.SearchModels.LowestPriceResult>>(jsonResponse);
                        products = products.OrderBy(product => product.PriceValue).ToList();
                        return Json(products);
                    }
                    else
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        _logger.Warn($"jsonResponse is not successful. Status: {response.StatusCode}, Error: {errorResponse}");
                        return StatusCode((int)response.StatusCode, errorResponse);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Internal server error.");
                    return StatusCode(500, "Internal server error.");
                }
            }
        }



        /// <summary>
        /// Retrieves a list of products from a specific store and displays them on the Product List Specific Store page
        /// </summary>
        /// <param name="store">The ID of the specific store</param>
        /// <param name="searchWord">The search word used to query the products</param>
        /// <returns>
        ///  Returns the ProductListSpecificStore view
        ///  </returns>
        public async Task<IActionResult> ProductListSpecificStore(string store, string searchWord, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrEmpty(store) || string.IsNullOrEmpty(searchWord))
            {
                _logger.Warn("Invalid store or search word.");
                return View();
            }

            WebConfiguration conf = await GetStoreConf(store);
            if (conf == null)
            {
                _logger.Warn($"No configuration found for store: {store}");
                return View();
            }
            
            try
            {
                using (var httpClient = new HttpClient())
                {
                    if (!_configuration["BaseAdress"].Any() || !_configuration["GetPricesFromStore"].Any())
                    {
                        _logger.Warn($"Configuration settings are not valid.");
                        return View();
                    }
                    ViewData["SearchWord"] = searchWord;
                    ViewData["StoreId"] = store;
                    httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                    string apiUrl = $"{_configuration["GetPricesFromStore"]}?searchWord={searchWord}&guid={store}&page={page}&pageSize={pageSize}";

                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        if (jsonResponse == null)
                        {
                            _logger.Warn("jsonResponse is null.");
                            return View();
                        }
                        var products = JsonConvert.DeserializeObject<List<WebApiCrawler.SearchModels.LowestPriceResult>>(jsonResponse);
                        products = products.OrderBy(product => product.PriceValue).ToList();
                        return View(products);
                    }
                    else
                    {
                        _logger.Warn($"Response was not successful. Status code: {response.StatusCode}");
                        return View();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred: {ex.Message}");
                return View();
            }
        }

        /// <summary>
        /// Retrieves a list of products from a specific store based on the provided search word and returns them as a JSON response
        /// </summary>
        /// <param name="store">The ID of the specific store</param>
        /// <param name="searchWord">The search word used to query the products</param>
        /// <returns>Returns a JSON response with the list of products</returns>
        public async Task<IActionResult> ProductListSpecificStoreReturnList(string store, string searchWord, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrEmpty(store) || string.IsNullOrEmpty(searchWord))
            {
                _logger.Warn("Invalid store or search word.");
                return BadRequest("Invalid store or search word.");
            }

            WebConfiguration conf = await GetStoreConf(store);
            if (conf == null)
            {
                _logger.Warn($"No configuration found for store: {store}");
                return BadRequest("No configuration found for store.");
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    if (!_configuration["BaseAdress"].Any() || !_configuration["GetPricesFromStore"].Any())
                    {
                        _logger.Warn($"Configuration settings are not valid.");
                        return BadRequest("Configuration settings are not valid.");
                    }

                    httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                    string apiUrl = $"{_configuration["GetPricesFromStore"]}?searchWord={searchWord}&guid={store}&page={page}&pageSize={pageSize}";

                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        if (jsonResponse == null)
                        {
                            _logger.Warn("jsonResponse is null.");
                            return BadRequest("jsonResponse is null.");
                        }
                        var products = JsonConvert.DeserializeObject<List<WebApiCrawler.SearchModels.LowestPriceResult>>(jsonResponse);
                        products = products.OrderBy(product => product.PriceValue).ToList();
                        return Ok(products);
                    }
                    else
                    {
                        _logger.Warn($"Response was not successful. Status code: {response.StatusCode}");
                        return BadRequest("Response was not successful");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred: {ex.Message}");
                return BadRequest();
            }
        }

        /// <summary>
        /// Retrieves the web configuration for a specific store 
        /// </summary>
        /// <param name="store">The ID of the specific store</param>
        /// <returns>returns a WebConfiguration object representing the configuration
        /// for the specified store</returns>
        private async Task<WebConfiguration> GetStoreConf(string store)
        {
            using (var httpClient = new HttpClient())
            {
                if (!_configuration["BaseAdress"].Any() || !_configuration["GetConfiguration"].Any())
                {
                    _logger.Warn($"Configuration settings are not valid.");
                    return null;
                }
                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                string apiUrl = _configuration["GetConfiguration"]+store;
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        if (jsonResponse == null)
                        {
                            _logger.Warn("jsonResponse is null.");
                            return null;
                        }
                        var conf = JsonConvert.DeserializeObject<WebConfiguration>(jsonResponse);
                        return conf;
                    }
                    else
                    {
                        _logger.Warn($"Configuration for store with ID {store} was not found.");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"{ex.Message}.");
                    return null;
                }
            }
        }
    }
}
