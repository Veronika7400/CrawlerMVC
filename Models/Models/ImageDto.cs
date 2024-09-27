using System.ComponentModel.DataAnnotations;

namespace WebApiCrawler.Models
{
    public class ImageDto
    {
        public string Path { get; set; }
        public Guid ProductId { get; set; }
    }
}
