using System.ComponentModel.DataAnnotations;

namespace WebApiCrawler.Models
{
    public class WebConfigurationDto
    {
        public string UrlPage { get; set; }
        public string ImgTag { get; set; }
        public string Img { get; set; }
        public string StorageExtension { get; set; }

        public string StorageFolder { get; set; }

        public string SearchBox { get; set; }
        public string ResultType { get; set; }
        public string DisplayType { get; set; }


        public Guid StoreId { get; set; }

    }
}
