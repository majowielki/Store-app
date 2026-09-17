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

/// <summary>
/// Accounts as the admin panel sees them. The demo administrator sees the panel but not
/// personal data; true-admin sees real values.
/// </summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = Policies.Admin)]
public class AdminUsersController : ControllerBase
{
    private const string AnonymizedUserEmail = "anonymized-user-email";
    private const string AnonymizedUserName = "anonymized-user-name";
    private const string AnonymizedFirstName = "anonymized-first-name";
    private const string AnonymizedLastName = "anonymized-last-name";
    private const string AnonymizedDeliveryAddress = "anonymized-delivery-address";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthService _authService;

    public AdminUsersController(UserManager<ApplicationUser> userManager, IAuthService authService)
    {
        _userManager = userManager;
        _authService = authService;
    }

    private bool Anonymize => User.IsDemoAdmin();

    private string Mask(string? value, string placeholder) => Anonymize ? placeholder : value ?? string.Empty;

    /// <summary>Accounts, newest first, filtered by a fragment of the e-mail or name and by activity.</summary>
    [HttpGet]
    public async Task<PagedResponse<AdminUserResponse>> GetUsers(
        [FromQuery] PagedQuery paging,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        paging = paging.Normalized();
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
        var page = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync();

        var users = page.Select(u => new AdminUserResponse
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

        return new PagedResponse<AdminUserResponse>(users, totalCount, paging);
    }

    /// <summary>One account; 404 when the id is unknown.</summary>
    [HttpGet("{userId}")]
    public async Task<UserResponse> GetUser(string userId)
    {
        var user = await _authService.GetUserAsync(userId);
        if (Anonymize)
        {
            user.Email = AnonymizedUserEmail;
            user.UserName = AnonymizedUserName;
            user.FirstName = AnonymizedFirstName;
            user.LastName = AnonymizedLastName;
            user.DisplayName = AnonymizedUserName;
            user.SimpleAddress = AnonymizedDeliveryAddress;
        }

        return user;
    }
}
