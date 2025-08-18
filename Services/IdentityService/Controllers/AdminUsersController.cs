using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Store.IdentityService.Models;
using Store.IdentityService.DTOs.Responses;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Store.IdentityService.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminAccess")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<object>>> GetUsersForAdmin(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                Id = u.Id,
                Email = "anonymized-user-email",
                UserName = "anonymized-user-name",
                FirstName = "anonymized-first-name",
                LastName = "anonymized-last-name",
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = users,
            TotalCount = await _userManager.Users.CountAsync(),
            Page = page,
            PageSize = pageSize
        });
    }

    // DTO for paginated order response
    private class PaginatedOrderResponse
    {
        public List<AdminOrderResponse> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    [HttpGet("orders")]
    public async Task<ActionResult<IEnumerable<object>>> GetOrdersForAdmin(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:5006";
        var client = _httpClientFactory.CreateClient();

        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", auth.ToString());
        }

        var url = $"{orderServiceUrl.TrimEnd('/')}/api/orders?page={page}&pageSize={pageSize}";
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        // Read paginated order response from OrderService
        var paginated = await response.Content.ReadFromJsonAsync<PaginatedOrderResponse>();
        var orders = paginated?.Orders ?? new List<AdminOrderResponse>();

        // Anonymize userId, userEmail, deliveryAddress, and customer name
        var sanitizedOrders = orders.Select(o => new AdminOrderResponse
        {
            Id = o.Id,
            UserId = "anonymized-user-id",
            UserEmail = "anonymized-user-email",
            DeliveryAddress = "anonymized-delivery-address",
            CustomerName = "anonymized-customer-name",
            TotalItems = o.TotalItems,
            OrderTotal = o.OrderTotal,
            CreatedAt = o.CreatedAt,
            Notes = o.Notes
        });

        return Ok(new
        {
            Items = sanitizedOrders,
            TotalCount = paginated?.TotalCount ?? 0,
            Page = paginated?.Page ?? page,
            PageSize = paginated?.PageSize ?? pageSize
        });
    }

    [HttpGet("orders/{id}")]
    public async Task<ActionResult<object>> GetOrderDetailForAdmin([FromRoute] int id)
    {
        var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:5006";
        var client = _httpClientFactory.CreateClient();

        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", auth.ToString());
        }

        var url = $"{orderServiceUrl.TrimEnd('/')}/api/orders/{id}";
        var response = await client.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        response.EnsureSuccessStatusCode();

        // Read as dynamic to access all fields, including OrderItems
        var order = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>();
        if (order == null)
            return NotFound();

        // Build sanitized order with all fields, including order items
        var sanitizedOrder = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
        sanitizedOrder["id"] = order["id"]?.GetValue<int>() ?? 0;
        sanitizedOrder["userId"] = "anonymized-user-id";
        sanitizedOrder["userEmail"] = "anonymized-user-email";
        sanitizedOrder["deliveryAddress"] = "anonymized-delivery-address";
        sanitizedOrder["customerName"] = "anonymized-customer-name";
        sanitizedOrder["totalItems"] = order["totalItems"]?.GetValue<int>() ?? 0;
        sanitizedOrder["orderTotal"] = order["orderTotal"]?.GetValue<decimal>() ?? 0;
        sanitizedOrder["createdAt"] = order["createdAt"]?.GetValue<DateTime>() ?? default;
        sanitizedOrder["notes"] = order["notes"]?.GetValue<string>();
        sanitizedOrder["orderItems"] = order["orderItems"];

        return Ok(sanitizedOrder);
    }

    [HttpGet("users/{userId}/orders")]
    public async Task<ActionResult<IEnumerable<object>>> GetUserOrders(
        [FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:5006";
        var client = _httpClientFactory.CreateClient();

        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", auth.ToString());
        }

        var url = $"{orderServiceUrl.TrimEnd('/')}/api/orders/by-user/{userId}?page={page}&pageSize={pageSize}";
        var response = await client.GetAsync(url);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        response.EnsureSuccessStatusCode();

        var paginated = await response.Content.ReadFromJsonAsync<PaginatedOrderResponse>();
        var orders = paginated?.Orders ?? new List<AdminOrderResponse>();

        var sanitizedOrders = orders.Select(o => new AdminOrderResponse
        {
            Id = o.Id,
            UserId = userId, // Preserve the userId for filtering
            UserEmail = "anonymized-user-email",
            DeliveryAddress = "anonymized-delivery-address",
            CustomerName = "anonymized-customer-name",
            TotalItems = o.TotalItems,
            OrderTotal = o.OrderTotal,
            CreatedAt = o.CreatedAt,
            Notes = o.Notes
        });

        return Ok(new
        {
            Items = sanitizedOrders,
            TotalCount = paginated?.TotalCount ?? 0,
            Page = paginated?.Page ?? page,
            PageSize = paginated?.PageSize ?? pageSize
        });
    }
}
