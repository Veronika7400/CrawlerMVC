using System.ComponentModel.DataAnnotations;

namespace WebApiCrawler.Models
{
    public class Image
    {
        [Key]
        public Guid Id { get; set; }
        public string Path { get; set; }
        public Guid ProductId { get; set; }
    }
}
