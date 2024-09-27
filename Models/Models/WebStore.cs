using System.ComponentModel.DataAnnotations;

namespace WebApiCrawler.Models
{
    public class WebStore
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
