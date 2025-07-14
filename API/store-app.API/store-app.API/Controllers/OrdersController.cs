using System.Net;
using Microsoft.AspNetCore.Mvc;
using store_app.API.Interfaces;
using store_app.API.Models;
using store_app.API.Models.Dto;
using store_app.API.Utility;

namespace store_app.API.Controllers
{
    [Route("api/Orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ApiResponse _response;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _orderService.GetAllAsync();
            _response.Result = orders;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet("{id:int}", Name = "GetOrder")]
        public async Task<IActionResult> GetOrder(int id)
        {
            if (id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                return NotFound(_response);
            }
            _response.Result = order;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }
    }
}