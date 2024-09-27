using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebApiCrawler.Models
{
    public class ProductDto 
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid StoreId { get; set; }
    }
}
