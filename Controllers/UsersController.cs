using CrawlerMVC.Areas.Identity.Data;
using CrawlerMVC.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using WebApiCrawler.ShopModels.SearchModels;

namespace CrawlerMVC.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private IConfiguration _configuration;
        private readonly NLog.ILogger _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(IConfiguration configuration, NLog.ILogger logger, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Retrieves a list of all users with optional filtering and pagination
        /// </summary>
        /// <param name="filter">Filter to apply to the user list</param>
        /// <param name="page">The page number for pagination</param>
        /// <param name="pageSize">The number of users per page</param>
        /// <returns>
        /// Returns a JSON result containing the list of users
        /// </returns>
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllUsersList(string filter = "", int page = 1, int pageSize = 10)
        {
            using (var httpClient = new HttpClient())
            {
                if (string.IsNullOrEmpty(_configuration["BaseAdress"]) || string.IsNullOrEmpty(_configuration["GetAllUsers"]))
                {
                    _logger.Warn("Configuration settings are not valid.");
                    return BadRequest("Configuration settings are not valid.");
                }

                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);

                string apiUrl = $"{_configuration["GetAllUsers"]}?filter={filter}&page={page}&pageSize={pageSize}";

                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrEmpty(jsonResponse))
                        {
                            _logger.Warn("jsonResponse is null.");
                            return NotFound("No users found.");
                        }

                        List<UserModel> users = JsonConvert.DeserializeObject<List<UserModel>>(jsonResponse);
                        return Json(users);
                    }
                    else
                    {
                        _logger.Warn("Response is not successful.");
                        return StatusCode((int)response.StatusCode, "Error fetching users.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"{ex.Message}");
                    return StatusCode(500, "Internal server error.");
                }
            }
        }

        /// <summary>
        /// Retrieves a list of all users with optional filtering and pagination and displays it in a view
        /// </summary>
        /// <param name="filter">Filter to apply to the user list</param>
        /// <param name="page">The page number for pagination</param>
        /// <param name="pageSize">The number of users per page</param>
        /// <returns>
        /// Returns a view containing the list of users
        /// </returns>
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetAllUsers(string filter = "", int page = 1, int pageSize = 10)
        {
            using (var httpClient = new HttpClient())
            {
                if (string.IsNullOrEmpty(_configuration["BaseAdress"]) || string.IsNullOrEmpty(_configuration["GetAllUsers"]))
                {
                    _logger.Warn("Configuration settings are not valid.");
                    return View();
                }

                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);

                string apiUrl = $"{_configuration["GetAllUsers"]}?filter={filter}&page={page}&pageSize={pageSize}";

                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrEmpty(jsonResponse))
                        {
                            _logger.Warn("jsonResponse is null.");
                            return View();
                        }

                        List<UserModel> users = JsonConvert.DeserializeObject<List<UserModel>>(jsonResponse);
                        return View(users);
                    }
                    else
                    {
                        _logger.Warn("Response is not successful.");
                        return View();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"{ex.Message}");
                    return View();
                }
            }
        }

        /// <summary>
        /// Deletes a user based on the provided user ID
        /// </summary>
        /// <param name="id">The ID of the user to be deleted</param>
        /// <returns>
        /// Returns an Ok result if the user is deleted successfully, otherwise returns a BadRequest
        /// </returns>
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            using (var httpClient = new HttpClient())
            {
                if (!_configuration["BaseAdress"].Any() || !_configuration["DeleteUser"].Any())
                {
                    _logger.Warn($"Configuration settings are not valid.");
                    return BadRequest();
                }

                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                string apiUrl = _configuration["DeleteUser"] + id;
                try
                {
                    HttpResponseMessage response = await httpClient.DeleteAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        if (jsonResponse == null)
                        {
                            _logger.Warn("jsonResponse is null.");
                            return BadRequest();
                        }
                        return Ok();
                    }
                    else
                    {
                        _logger.Warn("jsonResponse is not successful.");
                        return BadRequest();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"{ex.Message}.");
                    return BadRequest();
                }
            }
        }

        /// <summary>
        /// Retrieves a list of roles
        /// </summary>
        /// <returns>
        /// Returns an Ok result containing the list of roles if successful, otherwise returns a BadRequest
        /// </returns>
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetRoles()
        {
            using (var httpClient = new HttpClient())
            {
                if (!_configuration["BaseAdress"].Any() || !_configuration["GetRoles"].Any())
                {
                    _logger.Warn($"Configuration settings are not valid.");
                    return BadRequest();
                }

                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                string apiUrl = _configuration["GetRoles"];
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        if (jsonResponse == null)
                        {
                            _logger.Warn("jsonResponse is null.");
                            return BadRequest();
                        }
                        var roles = JsonConvert.DeserializeObject<List<string>>(jsonResponse);
                        return Ok(roles);
                    }
                    else
                    {
                        _logger.Warn("jsonResponse is not successful.");
                        return BadRequest();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"{ex.Message}.");
                    return BadRequest();
                }
            }
        }

        /// <summary>
        /// Updates user details
        /// </summary>
        /// <param name="user">The user object containing updated details</param>
        /// <returns>
        /// Returns an Ok result if the update is successful, otherwise returns a BadRequest
        /// </returns>
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateUser([FromBody] IdentityModel user)
        {
            using (var httpClient = new HttpClient())
            {
                if (string.IsNullOrEmpty(_configuration["BaseAdress"]) || string.IsNullOrEmpty(_configuration["UpdateUser"]))
                {
                    _logger.Warn("Configuration settings are not valid.");
                    return BadRequest();
                }

                httpClient.BaseAddress = new Uri(_configuration["BaseAdress"]);
                string apiUrl = _configuration["UpdateUser"];
                user.avatar = "";
                try
                {
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(apiUrl, jsonContent);
                    _logger.Warn(response.StatusCode + " " + response.Content);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrEmpty(jsonResponse))
                        {
                            _logger.Warn("jsonResponse is null.");
                            return BadRequest();
                        }
                        return Ok();
                    }
                    else
                    {
                        _logger.Warn("jsonResponse is not successful.");
                        return BadRequest();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"{ex.Message}.");
                    return BadRequest();
                }
            }
        }

    }
}