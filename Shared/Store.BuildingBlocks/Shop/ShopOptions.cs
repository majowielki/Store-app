using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Store.BuildingBlocks.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Store.BuildingBlocks.Shop;

/// <summary>
/// The <c>Shop</c> section: the public address of the shop's own pages (the UI), for the links
/// that leave the API - e-mails and sitemaps. Not the API's address: the UI proxies that.
/// </summary>
public sealed class ShopOptions
{
    public const string SectionName = "Shop";

    /// <summary>"https://shop.example", with or without the trailing slash.</summary>
    [Required]
    [Url]
    public string Url { get; init; } = string.Empty;
}

public static class ShopRegistration
{
    /// <summary>Binds and validates <see cref="ShopOptions"/> and registers <see cref="ShopLinks"/>.</summary>
    public static IServiceCollection AddShopLinks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStoreOptions<ShopOptions>(configuration, ShopOptions.SectionName);
        services.AddSingleton<ShopLinks>();
        return services;
    }
}
