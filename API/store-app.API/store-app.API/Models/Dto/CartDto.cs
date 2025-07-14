namespace store_app.API.Models.Dto
{
    public class CartDto
    {
        public List<CartItemDto> CartItems { get; set; } = new();
        public int NumItemsInCart { get; set; }
        public decimal CartTotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal OrderTotal { get; set; }
    }
}
