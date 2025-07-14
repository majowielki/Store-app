namespace store_app.API.Models
{
    public class Params
    {
        public string Search { get; set; }
        public string Category { get; set; }
        public string Company { get; set; }
        public string Order { get; set; }
        public string Price { get; set; }
        public string Shipping { get; set; }
        public int? Page { get; set; }
    }
}
