using System.Text.Json;

namespace Store.BuildingBlocks.Serialization;

/// <summary>
/// Shared, cached <see cref="JsonSerializerOptions"/> instances. Options are expensive to build
/// and are meant to be reused (CA1869); every service serialises with the same camelCase rules.
/// </summary>
public static class StoreJson
{
    /// <summary>camelCase property names, compact output.</summary>
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>camelCase property names, indented output (health check pages, development responses).</summary>
    public static readonly JsonSerializerOptions CamelCaseIndented = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>Case-insensitive deserialisation of responses produced by other services.</summary>
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
