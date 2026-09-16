using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.BuildingBlocks.Api;
using Store.BuildingBlocks.Authorization;
using Store.Contracts.Authorization;
using Store.IdentityService.DTOs.Responses;
using Store.IdentityService.Models;
using Store.IdentityService.Services;

namespace Store.IdentityService.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.Admin)]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;
    private readonly IAuthService _authService;

    // Anonymized value constants
    private const string AnonymizedUserEmail = "anonymized-user-email";
    private const string AnonymizedUserName = "anonymized-user-name";
    private const string AnonymizedFirstName = "anonymized-first-name";
    private const string AnonymizedLastName = "anonymized-last-name";
    private const string AnonymizedDeliveryAddress = "anonymized-delivery-address";

    public AdminController(
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger,
        IAuthService authService)
    {
        _userManager = userManager;
        _logger = logger;
        _authService = authService;
    }

    /// <summary>
    /// The demo administrator sees the panel but not personal data; true-admin sees real values.
    /// </summary>
    private bool Anonymize => User.IsDemoAdmin();

    private string Mask(string? value, string placeholder) => Anonymize ? placeholder : value ?? string.Empty;

    [HttpGet("users")]
    public async Task<ActionResult> GetUsersForAdmin(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        try
        {
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(u =>
                    EF.Functions.ILike(u.Email!, pattern) ||
                    EF.Functions.ILike(u.FirstName ?? string.Empty, pattern) ||
                    EF.Functions.ILike(u.LastName ?? string.Empty, pattern));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var page1 = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var users = page1.Select(u => new AdminUserResponse
            {
                Id = u.Id,
                Email = Mask(u.Email, AnonymizedUserEmail),
                UserName = Mask(u.UserName, AnonymizedUserName),
                FirstName = Mask(u.FirstName, AnonymizedFirstName),
                LastName = Mask(u.LastName, AnonymizedLastName),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                LastLoginAt = u.LastLoginAt
            }).ToList();

            var response = new PaginatedResponse<AdminUserResponse>
            {
                Items = users,
                TotalCount = totalCount,
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

    /// <summary>
    /// One user's profile for the admin panel. 404 when the id is unknown; the demo
    /// administrator gets masked personal data.
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetUserForAdmin(string userId)
    {
        var result = await _authService.GetUserAsync(userId);
        if (!result.IsSuccess || result.Data is null)
        {
            return NotFound(result);
        }

        if (Anonymize)
        {
            var user = result.Data;
            user.Email = AnonymizedUserEmail;
            user.UserName = AnonymizedUserName;
            user.FirstName = AnonymizedFirstName;
            user.LastName = AnonymizedLastName;
            user.DisplayName = AnonymizedUserName;
            user.SimpleAddress = AnonymizedDeliveryAddress;
        }

        return Ok(result);
    }
}
