namespace store_app.API.Models.Dto
{
    public class ProductsResponseDto
    {
        public IEnumerable<ProductDto> Data { get; set; }
        public ProductsMeta Meta { get; set; }
    }
}
