using System.Net;
using Microsoft.AspNetCore.Mvc;
using store_app.API.Data;
using store_app.API.Models;

namespace store_app.API.Controllers
{
    [Route("api/Checkout")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;

        public CheckoutController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetCheckouts()
        {
            _response.Result = _db.Checkouts;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet("{id:int}", Name = "GetCheckout")]
        public async Task<IActionResult> GetCheckout(int id)
        {
            if (id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            Checkout checkout = _db.Checkouts.FirstOrDefault(u => u.Id == id);
            if (checkout == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                return NotFound(_response);
            }
            _response.Result = checkout;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }
    }
}