using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using store_app.API.Data;
using store_app.API.Interfaces;
using store_app.API.Models;
using store_app.API.Models.Dto;

namespace store_app.API.Controllers
{
    [Route("api/Cart")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<ActionResult<CartDto>> GetCart()
        {
            var cart = await _cartService.GetCartAsync(GetUserIdFromRequest());
            return Ok(cart);
        }

        [HttpPost]
        public async Task<ActionResult<CartDto>> AddItemToCart(int productId, int amount)
        {
            var cart = await _cartService.AddItemAsync(GetUserIdFromRequest(), productId, amount);
            return Ok(cart);
        }

        [HttpDelete]
        public async Task<ActionResult<CartDto>> RemoveCartItem(int productId, int amount)
        {
            var cart = await _cartService.RemoveItemAsync(GetUserIdFromRequest(), productId, amount);
            return Ok(cart);
        }

        private int GetUserIdFromRequest()
        {
            // Replace this with your logic to retrieve the user ID or cart identifier
            return 1; // Example: hardcoded user ID
        }
    }
}