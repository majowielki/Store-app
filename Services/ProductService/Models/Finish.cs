using Store.Contracts.Catalog;

namespace Store.ProductService.Models;

/// <summary>
/// The surfaces a swatch shows beside its colour - a wood, a weave, a stone, a metal. Each is a
/// crop of a product picture (Scripts/swatches.json) the UI draws the swatch with; the API writes
/// them in camelCase.
/// </summary>
public enum SwatchTexture
{
    NaturalOak,
    LightOak,
    HoneyOak,
    Walnut,
    Teak,
    Pine,
    Birch,
    Beech,
    Bamboo,
    Acacia,
    OliveWood,
    ReclaimedWood,
    Rubberwood,
    Rattan,
    Cane,
    RattanWeave,
    GreyRattan,
    Seagrass,
    Jute,
    Travertine,
    Marble,
    Terracotta,
    SpeckledStoneware,
    GreyStoneware,
    Stone,
    WhiteGlaze,
    Brass,
    BlackSteel
}

/// <summary>One part of a swatch: its colour (#rrggbb), and the surface it shows when it has one.</summary>
/// <param name="Color">The colour, taken from the product pictures; with a texture, the texture's average</param>
/// <param name="Texture">The surface, for a wood, a weave, a stone or a metal</param>
public sealed record SwatchPart(string Color, SwatchTexture? Texture = null);

/// <summary>
/// A colour a product is sold in, the way its pictures show it: a name for the customer, the colour
/// family the shop's colour filter groups it under, and the swatch - one part, or two for a product
/// of two colours or materials (cushions in cream and rust, oak on black steel).
/// </summary>
/// <param name="Key">What products, carts and orders store ("natural-oak")</param>
/// <param name="Name">What the shop shows ("Natural oak")</param>
/// <param name="Family">The colour filter value it counts under</param>
/// <param name="Swatch">One or two parts, the main one first</param>
public sealed record Finish(string Key, string Name, Color Family, IReadOnlyList<SwatchPart> Swatch);
