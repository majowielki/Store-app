using store_app.API.Utility;

namespace store_app.API.Models
{
    public class ProductsMeta
    {
        public Pagination Pagination { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Companies { get; set; }
    }
}
