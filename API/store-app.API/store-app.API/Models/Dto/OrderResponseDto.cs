namespace store_app.API.Models.Dto
{
    public class OrderResponseDto
    {
        public IEnumerable<OrderDto> Data { get; set; }
        public OrdersMeta Meta { get; set; }
    }
}
