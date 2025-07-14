using Microsoft.EntityFrameworkCore;
using store_app.API.Data;
using store_app.API.Interfaces;
using store_app.API.Models;
using store_app.API.Models.Dto;
using AutoMapper;

namespace store_app.API.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CartService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<CartDto> GetCartAsync(int userId)
        {
            var cart = await _db.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.Id == userId);

            return cart != null ? _mapper.Map<CartDto>(cart) : new CartDto();
        }

        public async Task<CartDto> AddItemAsync(int userId, int productId, int amount)
        {
            var cart = await _db.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.Id == userId);

            if (cart == null)
            {
                cart = new Cart { Id = userId };
                _db.Carts.Add(cart);
            }

            var product = await _db.Products.FindAsync(productId);
            if (product == null) throw new ArgumentException("Product not found");

            var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductID == productId);
            if (existingItem == null)
            {
                cart.CartItems.Add(new CartItem
                {
                    ProductID = product.Id,
                    Product = product,
                    Title = product.Title,
                    Price = product.Price,
                    Image = product.Image,
                    Company = product.Company.ToString(),
                    ProductColor = product.Colors.FirstOrDefault() ?? "Default",
                    Amount = amount,
                    CartId = cart.Id,
                    Cart = cart
                });
            }
            else
            {
                existingItem.Amount += amount;
            }

            UpdateCartTotals(cart);

            await _db.SaveChangesAsync();
            return _mapper.Map<CartDto>(cart);
        }

        public async Task<CartDto> RemoveItemAsync(int userId, int productId, int amount)
        {
            var cart = await _db.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.Id == userId);

            if (cart == null) throw new ArgumentException("Cart not found");

            var item = cart.CartItems.FirstOrDefault(i => i.ProductID == productId);
            if (item == null) return _mapper.Map<CartDto>(cart);

            item.Amount -= amount;
            if (item.Amount <= 0)
                cart.CartItems.Remove(item);

            UpdateCartTotals(cart);

            await _db.SaveChangesAsync();
            return _mapper.Map<CartDto>(cart);
        }

        private void UpdateCartTotals(Cart cart)
        {
            cart.NumItemsInCart = cart.CartItems.Sum(i => i.Amount);
            cart.CartTotal = cart.CartItems.Sum(i => i.Price * i.Amount);
            cart.OrderTotal = cart.CartTotal + cart.Shipping + cart.Tax;
        }
    }
}
