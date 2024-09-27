using HtmlAgilityPack;
using System.Xml;

namespace WebApiCrawler.SearchModels
{
    public class LowestPriceResult
    {
        public string StoreName { get; set; }
        public string Url { get; set; }
        public string ProductName { get; set; }
        public double PriceValue { get; set; }
        public string ImageUrl {  get; set; }

    }
}
