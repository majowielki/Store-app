using Store.Contracts.Catalog;
using Store.ProductService.Models;

namespace Store.ProductService.DTOs.Responses;

/// <summary>
/// A colour products are sold in, as the shop draws its swatch: the key products, carts and orders
/// store, the name to show, the family the colour filter counts it under, and one or two parts.
/// </summary>
/// <param name="Key">What a product's colors list ("natural-oak")</param>
/// <param name="Name">What the shop shows ("Natural oak")</param>
/// <param name="Family">The colour filter value it counts under</param>
/// <param name="Swatch">One part, or two for a product of two colours or materials, the main one first</param>
public sealed record FinishResponse(string Key, string Name, Color Family, IReadOnlyList<SwatchPartResponse> Swatch)
{
    public static FinishResponse From(Finish finish) => new(
        finish.Key,
        finish.Name,
        finish.Family,
        finish.Swatch.Select(part => new SwatchPartResponse(part.Color, part.Texture)).ToList());
}

/// <summary>One part of a swatch.</summary>
/// <param name="Color">Its colour (#rrggbb); with a texture, the texture's average, to show while the picture loads</param>
/// <param name="Texture">The wood, weave, stone or metal it shows, when it is one</param>
public sealed record SwatchPartResponse(string Color, SwatchTexture? Texture);
