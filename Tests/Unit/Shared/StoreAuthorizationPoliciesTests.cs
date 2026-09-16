using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Store.BuildingBlocks.Authorization;
using Store.Contracts.Authorization;
using System.Security.Claims;
using Xunit;

namespace Store.Tests.Unit.Shared;

/// <summary>
/// One set of policies, evaluated against the roles IdentityService
/// issues. The JWT bearer handler maps the "role" claim to <see cref="ClaimTypes.Role"/>, which
/// is what the principals below carry.
/// </summary>
public class StoreAuthorizationPoliciesTests
{
    private static readonly IAuthorizationService Authorization = BuildAuthorizationService();

    private static IAuthorizationService BuildAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStoreAuthorization();
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal PrincipalWithRoles(params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user-1") };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test", ClaimTypes.Name, ClaimTypes.Role));
    }

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    private static async Task<bool> Allowed(ClaimsPrincipal principal, string policy)
        => (await Authorization.AuthorizeAsync(principal, resource: null, policy)).Succeeded;

    [Theory]
    [InlineData(Roles.User, true)]
    [InlineData(Roles.DemoAdmin, true)]
    [InlineData(Roles.TrueAdmin, true)]
    public async Task User_policy_admits_every_store_role(string role, bool expected)
    {
        Assert.Equal(expected, await Allowed(PrincipalWithRoles(role), Policies.User));
    }

    [Theory]
    [InlineData(Roles.User, false)]
    [InlineData(Roles.DemoAdmin, true)]
    [InlineData(Roles.TrueAdmin, true)]
    public async Task Admin_policy_admits_both_admin_roles_only(string role, bool expected)
    {
        Assert.Equal(expected, await Allowed(PrincipalWithRoles(role), Policies.Admin));
    }

    [Theory]
    [InlineData(Roles.User, false)]
    [InlineData(Roles.DemoAdmin, false)]
    [InlineData(Roles.TrueAdmin, true)]
    public async Task AdminWrite_policy_admits_true_admin_only(string role, bool expected)
    {
        Assert.Equal(expected, await Allowed(PrincipalWithRoles(role), Policies.AdminWrite));
    }

    [Theory]
    [InlineData(Policies.User)]
    [InlineData(Policies.Admin)]
    [InlineData(Policies.AdminWrite)]
    public async Task Anonymous_and_unknown_roles_are_rejected_everywhere(string policy)
    {
        Assert.False(await Allowed(Anonymous(), policy));
        Assert.False(await Allowed(PrincipalWithRoles("admin"), policy));
    }

    [Fact]
    public async Task Role_order_in_the_token_does_not_matter()
    {
        // ProductsController.IsDemoAdmin() used to look at the first role claim only
        var demoAdminListedSecond = PrincipalWithRoles(Roles.User, Roles.DemoAdmin);

        Assert.True(await Allowed(demoAdminListedSecond, Policies.Admin));
        Assert.False(await Allowed(demoAdminListedSecond, Policies.AdminWrite));
        Assert.True(demoAdminListedSecond.IsDemoAdmin());
        Assert.True(demoAdminListedSecond.IsStoreAdmin());
    }
}
