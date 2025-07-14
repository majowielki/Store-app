using System.Net;
using Microsoft.AspNetCore.Mvc;
using store_app.API.Interfaces;
using store_app.API.Models;
using store_app.API.Models.Dto;
using store_app.API.Utility;

namespace store_app.API.Controllers
{
    [Route("api/Product")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ApiResponse _response;

        public ProductController(IProductService productService)
        {
            _productService = productService;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _productService.GetAllAsync();
            _response.Result = products;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet("{id:int}", Name = "GetProduct")]
        public async Task<IActionResult> GetProduct(int id)
        {
            if (id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                return NotFound(_response);
            }
            _response.Result = product;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet("response")]
        public async Task<IActionResult> GetProductsResponse()
        {
            var products = await _productService.GetAllAsync();

            var response = new ProductsResponseDto
            {
                Data = products,
                Meta = new ProductsMeta
                {
                    Pagination = new Pagination
                    {
                        Page = 1,
                        PageSize = products.Count(),
                        PageCount = 1,
                        Total = products.Count()
                    },
                    Categories = Enum.GetValues<Category>().Cast<Category>().Select(c => c.ToString()).ToList(),
                    Companies = Enum.GetValues<Company>().Cast<Company>().Select(c => c.ToString()).ToList()
                }
            };

            _response.Result = response;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }
    }
}
