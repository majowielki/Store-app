using Microsoft.AspNetCore.Authorization;
using Store.Shared.Authorization;
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
[Authorize(Policy = Policies.Admin)]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    // Anonymized value constants
    private const string AnonymizedUserId = "anonymized-user-id";
    private const string AnonymizedUserEmail = "anonymized-user-email";
    private const string AnonymizedUserName = "anonymized-user-name";
    private const string AnonymizedFirstName = "anonymized-first-name";
    private const string AnonymizedLastName = "anonymized-last-name";
    private const string AnonymizedDeliveryAddress = "anonymized-delivery-address";
    private const string AnonymizedCustomerName = "anonymized-customer-name";

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

    // Helper method to create HttpClient with Authorization header if present
    private HttpClient CreateAuthorizedClient()
    {
        var client = _httpClientFactory.CreateClient();
        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", auth.ToString());
        }
        return client;
    }

    [HttpGet("users")]
    public async Task<ActionResult> GetUsersForAdmin(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserResponse
                {
                    Id = u.Id,
                    Email = AnonymizedUserEmail,
                    UserName = AnonymizedUserName,
                    FirstName = AnonymizedFirstName,
                    LastName = AnonymizedLastName,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();

            var response = new PaginatedResponse<AdminUserResponse>
            {
                Items = users,
                TotalCount = await _userManager.Users.CountAsync(),
                Page = page,
                PageSize = pageSize
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetUsersForAdmin");
            return StatusCode(500, "An unexpected error occurred while fetching users.");
        }
    }

    [HttpGet("orders")]
    public async Task<ActionResult> GetOrdersForAdmin(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:5006";
            var client = CreateAuthorizedClient();

            var url = $"{orderServiceUrl.TrimEnd('/')}/api/orders?page={page}&pageSize={pageSize}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Use local DTOs for deserialization (no namespace prefix)
            var apiResponse = await response.Content.ReadFromJsonAsync<OrderServiceApiResponse>();
            var orderList = apiResponse?.Data;
            var orders = orderList?.Orders ?? new List<OrderServiceOrderResponse>();

            var sanitizedOrders = orders.Select(o => new AdminOrderResponse
            {
                Id = o.Id,
                UserId = AnonymizedUserId,
                UserEmail = AnonymizedUserEmail,
                DeliveryAddress = AnonymizedDeliveryAddress,
                CustomerName = AnonymizedCustomerName,
                TotalItems = o.TotalItems,
                OrderTotal = o.OrderTotal,
                CreatedAt = o.CreatedAt,
                Notes = o.Notes,
                OrderItems = o.OrderItems.Select(oi => new DTOs.Responses.OrderServiceOrderItemResponse
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductTitle = oi.ProductTitle,
                    ProductImage = oi.ProductImage,
                    Price = oi.Price,
                    Quantity = oi.Quantity,
                    Color = oi.Color,
                    Company = oi.Company,
                    LineTotal = oi.LineTotal,
                    DeliveryCost = oi.DeliveryCost,
                    OrderDiscount = oi.OrderDiscount
                }).ToList()
            }).ToList();

            var result = new PaginatedResponse<AdminOrderResponse>
            {
                Items = sanitizedOrders,
                TotalCount = orderList?.TotalCount ?? 0,
                Page = orderList?.Page ?? page,
                PageSize = orderList?.PageSize ?? pageSize
            };
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching orders from OrderService");
            return StatusCode(502, "Failed to fetch orders from OrderService.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetOrdersForAdmin");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("orders/{id}")]
    public async Task<ActionResult> GetOrderDetailForAdmin([FromRoute] int id)
    {
        try
        {
            var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:5006";
            var client = CreateAuthorizedClient();

            var url = $"{orderServiceUrl.TrimEnd('/')}/api/orders/{id}";
            var response = await client.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            response.EnsureSuccessStatusCode();

            // Use local DTOs for deserialization
            var apiResponse = await response.Content.ReadFromJsonAsync<OrderServiceApiResponseSingle>();
            var order = apiResponse?.Data;
            if (order == null)
                return NotFound();

            var sanitizedOrder = new {
                id = order.Id,
                userId = AnonymizedUserId,
                userEmail = AnonymizedUserEmail,
                deliveryAddress = AnonymizedDeliveryAddress,
                customerName = AnonymizedCustomerName,
                totalItems = order.TotalItems,
                orderTotal = order.OrderTotal,
                createdAt = order.CreatedAt,
                notes = order.Notes,
                orderItems = order.OrderItems // You may want to further anonymize orderItems if needed
            };

            return Ok(sanitizedOrder);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching order detail from OrderService");
            return StatusCode(502, "Failed to fetch order detail from OrderService.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetOrderDetailForAdmin");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("users/{userId}/orders")]
    public async Task<ActionResult> GetUserOrders(
        [FromRoute] string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:5006";
            var client = CreateAuthorizedClient();

            var url = $"{orderServiceUrl.TrimEnd('/')}/api/orders/by-user/{userId}?page={page}&pageSize={pageSize}";
            var response = await client.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            response.EnsureSuccessStatusCode();

            // Use local DTOs for deserialization (no namespace prefix)
            var apiResponse = await response.Content.ReadFromJsonAsync<OrderServiceApiResponse>();
            var orderList = apiResponse?.Data;
            var orders = orderList?.Orders ?? new List<OrderServiceOrderResponse>();

            var sanitizedOrders = orders.Select(o => new AdminOrderResponse
            {
                Id = o.Id,
                UserId = userId, // Preserve the userId for filtering
                UserEmail = AnonymizedUserEmail,
                DeliveryAddress = AnonymizedDeliveryAddress,
                CustomerName = AnonymizedCustomerName,
                TotalItems = o.TotalItems,
                OrderTotal = o.OrderTotal,
                CreatedAt = o.CreatedAt,
                Notes = o.Notes,
                OrderItems = o.OrderItems.Select(oi => new DTOs.Responses.OrderServiceOrderItemResponse
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductTitle = oi.ProductTitle,
                    ProductImage = oi.ProductImage,
                    Price = oi.Price,
                    Quantity = oi.Quantity,
                    Color = oi.Color,
                    Company = oi.Company,
                    LineTotal = oi.LineTotal,
                    DeliveryCost = oi.DeliveryCost,
                    OrderDiscount = oi.OrderDiscount
                }).ToList()
            }).ToList();

            var result = new PaginatedResponse<AdminOrderResponse>
            {
                Items = sanitizedOrders,
                TotalCount = orderList?.TotalCount ?? 0,
                Page = orderList?.Page ?? page,
                PageSize = orderList?.PageSize ?? pageSize
            };
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching user orders from OrderService");
            return StatusCode(502, "Failed to fetch user orders from OrderService.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetUserOrders");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}

// DTOs for deserialization of OrderService response
public class OrderServiceApiResponse
{
    public bool IsSuccess { get; set; }
    public OrderServiceOrderListResponse? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public int StatusCode { get; set; }
}

public class OrderServiceOrderListResponse
{
    public List<OrderServiceOrderResponse> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class OrderServiceOrderResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal OrderTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
    public List<OrderServiceOrderItemResponse> OrderItems { get; set; } = new();
}

public class OrderServiceOrderItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public decimal LineTotal { get; set; }
    public decimal? DeliveryCost { get; set; }
    public decimal? OrderDiscount { get; set; }
}

// DTO for single order response
public class OrderServiceApiResponseSingle
{
    public bool IsSuccess { get; set; }
    public OrderServiceOrderResponse? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public int StatusCode { get; set; }
}
