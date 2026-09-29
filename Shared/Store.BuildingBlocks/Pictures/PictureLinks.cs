using Microsoft.Extensions.Options;

namespace Store.BuildingBlocks.Pictures;

/// <summary>The addresses of the shop's pictures in the container of <see cref="PictureOptions.BaseUrl"/>.</summary>
public sealed class PictureLinks
{
    private readonly string _container;

    public PictureLinks(IOptions<PictureOptions> options)
    {
        _container = options.Value.BaseUrl.TrimEnd('/');
    }

    /// <summary>The address of a picture by its file name ("Maker-Modenza.webp").</summary>
    public string Of(string fileName) => $"{_container}/{fileName}";
}
