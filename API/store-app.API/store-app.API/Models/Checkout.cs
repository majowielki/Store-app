using System.ComponentModel.DataAnnotations;

namespace store_app.API.Models
{
    public class Checkout
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal ChargeTotal { get; set; }
        public string OrderTotal { get; set; }
        public List<CartItem> CartItems { get; set; }
        public int NumItemsInCart { get; set; }
    }
}
