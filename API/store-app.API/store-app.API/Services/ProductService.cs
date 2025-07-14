using AutoMapper;
using Microsoft.EntityFrameworkCore;
using store_app.API.Data;
using store_app.API.Interfaces;
using store_app.API.Models.Dto;

namespace store_app.API.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public ProductService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await _db.Products.ToListAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            return product == null ? null : _mapper.Map<ProductDto>(product);
        }
    }
}
