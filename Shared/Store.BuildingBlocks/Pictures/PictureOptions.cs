using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Store.BuildingBlocks.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Store.BuildingBlocks.Pictures;

/// <summary>
/// The <c>Pictures</c> section: the public address of the blob container the shop's pictures are
/// published to (<c>Blobs/</c> in the repository), which the demo data points its products and
/// editorial pages at. Azurite's container locally, the pictures account's in Azure.
/// </summary>
public sealed class PictureOptions
{
    public const string SectionName = "Pictures";

    /// <summary>"https://storepx123.blob.core.windows.net/product-images/", with or without the trailing slash.</summary>
    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;
}

public static class PictureRegistration
{
    /// <summary>Binds and validates <see cref="PictureOptions"/> and registers <see cref="PictureLinks"/>.</summary>
    public static IServiceCollection AddPictureLinks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStoreOptions<PictureOptions>(configuration, PictureOptions.SectionName);
        services.AddSingleton<PictureLinks>();
        return services;
    }
}
