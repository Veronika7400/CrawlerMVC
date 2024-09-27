using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebApiCrawler.Models
{
    public class Product
    {
        [Key]
        public Guid Id  { get; set; }

        [Required]
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid StoreId { get; set; }
  
    }
}
