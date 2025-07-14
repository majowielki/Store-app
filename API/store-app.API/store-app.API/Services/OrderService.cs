using AutoMapper;
using Microsoft.EntityFrameworkCore;
using store_app.API.Data;
using store_app.API.Interfaces;
using store_app.API.Models.Dto;

namespace store_app.API.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public OrderService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<IEnumerable<OrderDto>> GetAllAsync()
        {
            var orders = await _db.Orders.Include(o => o.CartItems).ToListAsync();
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<OrderDto?> GetByIdAsync(int id)
        {
            var order = await _db.Orders.Include(o => o.CartItems).FirstOrDefaultAsync(o => o.Id == id);
            return order == null ? null : _mapper.Map<OrderDto>(order);
        }
    }
}
