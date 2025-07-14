namespace store_app.API.Models.Dto
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Featured { get; set; }
        public string Image { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool Shipping { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<string> Colors { get; set; } = new();
    }
}
