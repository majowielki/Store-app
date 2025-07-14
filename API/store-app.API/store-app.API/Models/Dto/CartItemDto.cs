namespace store_app.API.Models.Dto
{
    public class CartItemDto
    {
        public int ProductID { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public int Amount { get; set; }
        public string ProductColor { get; set; }
        public string Image { get; set; }
        public string Company { get; set; }
    }
}
