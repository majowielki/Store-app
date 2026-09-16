using System.Text.Json;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// What a typed client uses to read another service's responses: the web defaults
    /// (camelCase, case-insensitive, numbers may arrive as strings) plus enums by name.
    /// </summary>
    public static readonly JsonSerializerOptions Web = CreateWeb();

    private static JsonSerializerOptions CreateWeb()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
