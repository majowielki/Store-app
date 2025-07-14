using AutoMapper;
using store_app.API.Models;
using store_app.API.Models.Dto;

namespace store_app.API.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Cart, CartDto>();
            CreateMap<CartItem, CartItemDto>();
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()))
                .ForMember(dest => dest.Company, opt => opt.MapFrom(src => src.Company.ToString()));
            CreateMap<Order, OrderDto>();
        }
    }
}
