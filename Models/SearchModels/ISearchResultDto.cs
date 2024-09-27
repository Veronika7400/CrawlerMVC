using WebApiCrawler.Models;

namespace WebApiCrawler.SearchModels
{
    public interface ISearchResultDto
    {
        List<IProductDto> Products { get; set; }
    }
}
