using System.Text.Json;
using System.Text.Json.Serialization;

namespace Store.BuildingBlocks.Api;

/// <summary>
/// A request field that distinguishes "not sent" from "sent as null". A partial update can
/// then clear a value (<c>"salePrice": null</c>) as well as leave it alone (property absent),
/// which a plain nullable property cannot express - both arrive as null.
/// </summary>
/// <typeparam name="T">Value type; use a nullable type (<c>decimal?</c>) when null is a legal value.</typeparam>
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly record struct Optional<T>
{
    private Optional(T? value)
    {
        Value = value;
        IsSet = true;
    }

    /// <summary>True when the property was present in the request, even with a null value.</summary>
    public bool IsSet { get; }

    /// <summary>The value sent; meaningful only when <see cref="IsSet"/> is true.</summary>
    public T? Value { get; }

    /// <summary>A field the client did not send.</summary>
    public static Optional<T> Unset => default;

    /// <summary>A field the client sent, possibly as null.</summary>
    public static Optional<T> Of(T? value) => new(value);

    public static implicit operator Optional<T>(T? value) => Of(value);

    public override string ToString() => IsSet ? Value?.ToString() ?? "null" : "(unset)";
}

/// <summary>
/// Reads and writes <see cref="Optional{T}"/> as the bare value. Absent properties keep the
/// struct default (unset); System.Text.Json calls the converter for null tokens because
/// <see cref="JsonConverter{T}.HandleNull"/> is true.
/// </summary>
public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(typeof(OptionalJsonConverter<>).MakeGenericType(valueType))!;
    }

    private sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
    {
        public override bool HandleNull => true;

        public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return Optional<T>.Of(default);
            }

            return Optional<T>.Of(JsonSerializer.Deserialize<T>(ref reader, options));
        }

        public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
        {
            if (!value.IsSet || value.Value is null)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}
