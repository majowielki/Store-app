using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Store.Contracts.Authorization;
using Store.IdentityService.Models;
using System.Security.Cryptography;

namespace Store.IdentityService.Seeding;

/// <summary>
/// The roles and the accounts every environment starts with: the true administrator (only
/// with a configured password) and, when <see cref="DemoOptions.Enabled"/>, the two demo
/// accounts. Runs after the migrations, from <c>--migrate</c> or the Development start-up.
/// </summary>
public sealed class IdentitySeeder
{
    private const string DemoAddress = "123 Demo Street, Demo City, DC 12345";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly DemoOptions _demo;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        IHostEnvironment environment,
        IOptions<DemoOptions> demo,
        ILogger<IdentitySeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _environment = environment;
        _demo = demo.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedTrueAdminAsync();

        if (_demo.Enabled)
        {
            await SeedAccountAsync(_demo.AdminEmail, _demo.AdminPassword, "Demo", "Administrator", Roles.DemoAdmin);
            await SeedAccountAsync(_demo.UserEmail, _demo.UserPassword, "Demo", "User", Roles.User);
        }
        else
        {
            _logger.LogInformation("Demo accounts are disabled ({Section}:Enabled=false); none seeded", DemoOptions.SectionName);
        }
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in new[] { Roles.TrueAdmin, Roles.DemoAdmin, Roles.User })
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
                _logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    private async Task SeedTrueAdminAsync()
    {
        var email = _configuration["TrueAdmin:Email"] ?? "trueadmin@store.com";
        var password = _configuration["TrueAdmin:Password"];

        if (await _userManager.FindByEmailAsync(email) != null)
        {
            _logger.LogInformation("True Admin already exists: {Email}", email);
            return;
        }

        // The password is never generated and never logged. Without one the account is not
        // created: production refuses to start, other environments skip the seed.
        if (string.IsNullOrEmpty(password))
        {
            const string hint = "Set TrueAdmin:Password (TrueAdmin__Password) to create the true admin account.";
            if (_environment.IsProduction())
            {
                throw new InvalidOperationException("True admin password is not configured. " + hint);
            }

            _logger.LogWarning("True Admin not created: no password configured. {Hint}", hint);
            return;
        }

        await CreateAsync(email, password, "True", "Administrator", Roles.TrueAdmin, address: null);
    }

    /// <summary>A demo account; without a configured password it gets one nobody knows - the demo endpoints do not need it.</summary>
    private async Task SeedAccountAsync(string email, string? password, string firstName, string lastName, string role)
    {
        if (await _userManager.FindByEmailAsync(email) != null)
        {
            _logger.LogInformation("{Role} account already exists: {Email}", role, email);
            return;
        }

        await CreateAsync(email, password ?? RandomPassword(), firstName, lastName, role, DemoAddress);
    }

    private async Task CreateAsync(string email, string password, string firstName, string lastName, string role, string? address)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            SimpleAddress = address,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seeding the {role} account {email} failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await _userManager.AddToRoleAsync(user, role);
        _logger.LogInformation("{Role} account created: {Email}", role, email);
    }

    /// <summary>Meets the password policy by construction: letters of both cases, digits and a symbol.</summary>
    private static string RandomPassword()
        => "Aa1!" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
}
