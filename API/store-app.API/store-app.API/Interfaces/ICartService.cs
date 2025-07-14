using store_app.API.Models;
using store_app.API.Models.Dto;

namespace store_app.API.Interfaces
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(int userId);
        Task<CartDto> AddItemAsync(int userId, int productId, int amount);
        Task<CartDto> RemoveItemAsync(int userId, int productId, int amount);
    }
}
