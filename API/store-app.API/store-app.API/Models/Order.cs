namespace store_app.API.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public List<CartItem> CartItems { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Name { get; set; }
        public int NumItemsInCart { get; set; }
        public string OrderTotal { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
