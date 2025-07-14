using System.ComponentModel.DataAnnotations;

namespace store_app.API.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public List<CartItem> CartItems { get; set; } = new();
        public int NumItemsInCart { get; set; }
        public decimal CartTotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal OrderTotal { get; set; }

        public void AddItem(Product product, int amount)
        {
            ArgumentNullException.ThrowIfNull(product, nameof(product));
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

            var existingItem = FindItem(product.Id);

            if (existingItem == null)
            {
                CartItems.Add(new CartItem
                {
                    ProductID = product.Id,
                    Product = product,
                    Title = product.Title,
                    Price = product.Price,
                    Image = product.Image,
                    Company = product.Company.ToString(),
                    ProductColor = product.Colors.FirstOrDefault() ?? "Default",
                    Amount = amount,
                    CartId = Id,
                    Cart = this
                });
            }
            else
            {
                existingItem.Amount += amount;
            }

            UpdateCartTotals();
        }

        public void RemoveItem(int productId, int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

            if (CartItems == null || !CartItems.Any())
                return;

            var item = FindItem(productId);
            if (item == null)
                return;

            item.Amount -= amount;

            if (item.Amount <= 0)
                CartItems.Remove(item);

            UpdateCartTotals();
        }

        private CartItem? FindItem(int productId)
        {
            return CartItems?.FirstOrDefault(item => item.ProductID == productId);
        }

        private void UpdateCartTotals()
        {
            NumItemsInCart = CartItems?.Sum(item => item.Amount) ?? 0;
            CartTotal = CartItems?.Sum(item => item.Price * item.Amount) ?? 0;
            OrderTotal = CartTotal + Shipping + Tax;
        }
    }
}