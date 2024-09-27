

namespace WebApiCrawler.SearchModels
{
    public interface IProductDto
    {
         string ProductName { get; set; }
         double PriceValue { get; }
        string Link {  get; }
    }
}
