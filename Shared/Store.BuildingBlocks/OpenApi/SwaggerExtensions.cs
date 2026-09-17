using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Store.BuildingBlocks.Api;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json;

namespace Store.BuildingBlocks.OpenApi;

/// <summary>
/// One OpenAPI document per service, the way the UI client is generated from it: every
/// non-nullable property is required, enums are their JSON names, errors are the shared
/// <see cref="ProblemDetails"/>, the bearer scheme is a real HTTP scheme and the XML comments
/// of the service and of the shared projects become descriptions.
/// </summary>
public static class SwaggerExtensions
{
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Title of the document</param>
    /// <param name="version">API version; the document name in the Swagger UI and the export</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services, string serviceName, string version = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = serviceName,
                Version = version,
                Description = $"{serviceName} API documentation"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Access token issued by the identity service",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            // A property that cannot be null in C# is always present in the JSON, so the client
            // can rely on it; nullable ones stay optional (nulls are omitted when serialising)
            options.DescribeAllParametersInCamelCase();
            options.SupportNonNullableReferenceTypes();
            options.SchemaFilter<RequiredNonNullableSchemaFilter>();
            options.SchemaFilter<OptionalSchemaFilter>();
            options.OperationFilter<StoreResponsesOperationFilter>();

            // The service's own comments and those of the shared projects it exposes types from
            foreach (var xmlPath in Directory.GetFiles(AppContext.BaseDirectory, "Store.*.xml"))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
        });

        return services;
    }
}

/// <summary>
/// Marks every property whose C# type is neither a nullable value type nor a nullable
/// reference as required. Swashbuckle only does that for reference types, which would leave
/// <c>int</c>, <c>decimal</c> and <c>bool</c> optional in the generated client.
/// </summary>
public sealed class RequiredNonNullableSchemaFilter : ISchemaFilter
{
    private static readonly NullabilityInfoContext Nullability = new();

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties.Count == 0)
        {
            return;
        }

        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!schema.Properties.ContainsKey(name) || IsNullable(property) || IsOptional(property.PropertyType))
            {
                continue;
            }

            schema.Required.Add(name);
        }
    }

    /// <summary>An Optional field may be absent by design.</summary>
    private static bool IsOptional(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>);

    private static bool IsNullable(PropertyInfo property)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
        {
            return true;
        }

        var info = Nullability.Create(property);
        return info.ReadState == NullabilityState.Nullable || info.WriteState == NullabilityState.Nullable;
    }
}

/// <summary>
/// <see cref="Optional{T}"/> is the bare value on the wire (absent, null or the value), so its
/// schema is the schema of the value, nullable - not an object with IsSet and Value.
/// </summary>
public sealed class OptionalSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsGenericType || context.Type.GetGenericTypeDefinition() != typeof(Optional<>))
        {
            return;
        }

        var valueType = context.Type.GetGenericArguments()[0];
        var value = context.SchemaGenerator.GenerateSchema(Nullable.GetUnderlyingType(valueType) ?? valueType, context.SchemaRepository);

        schema.Type = value.Type;
        schema.Format = value.Format;
        schema.Reference = value.Reference;
        schema.Items = value.Items;
        schema.Enum = value.Enum;
        schema.Properties.Clear();
        schema.Required.Clear();
        schema.AdditionalPropertiesAllowed = value.AdditionalPropertiesAllowed;
        schema.Nullable = true;
    }
}

/// <summary>
/// The API speaks JSON: bodies are documented as application/json only (the formatters would
/// also offer text/plain and text/json). Every operation can fail, and every failure is a
/// <see cref="ProblemDetails"/>: declared once as the default response, with the statuses
/// the operation's authorisation implies.
/// </summary>
public sealed class StoreResponsesOperationFilter : IOperationFilter
{
    private const string Json = "application/json";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content.ContainsKey(Json) == true)
        {
            operation.RequestBody.Content = new Dictionary<string, OpenApiMediaType> { [Json] = operation.RequestBody.Content[Json] };
        }
        foreach (var response in operation.Responses.Values.Where(r => r.Content.ContainsKey(Json)))
        {
            response.Content = new Dictionary<string, OpenApiMediaType> { [Json] = response.Content[Json] };
        }

        var problem = new OpenApiMediaType
        {
            Schema = context.SchemaGenerator.GenerateSchema(typeof(StoreProblemDetails), context.SchemaRepository)
        };

        operation.Responses.TryAdd("default", new OpenApiResponse
        {
            Description = "Error, as an RFC 9457 problem (application/problem+json)",
            Content = { ["application/problem+json"] = problem }
        });

        var requiresAuthentication = context.MethodInfo.GetCustomAttributes(true)
            .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? Array.Empty<object>())
            .Any(a => a is Microsoft.AspNetCore.Authorization.AuthorizeAttribute);
        var allowsAnonymous = context.MethodInfo.GetCustomAttributes(true)
            .Any(a => a is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute);

        if (requiresAuthentication && !allowsAnonymous)
        {
            // The bearer requirement is declared per operation, so anonymous endpoints stay anonymous in the document
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
            operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(), new OpenApiResponse { Description = "No valid access token" });
            operation.Responses.TryAdd(StatusCodes.Status403Forbidden.ToString(), new OpenApiResponse { Description = "The signed-in user may not do this" });
        }
    }
}

/// <summary>
/// The error response as this store fills it, for the document only: the RFC 9457 members
/// plus the trace id every problem carries and the field messages of a validation problem.
/// </summary>
public sealed class StoreProblemDetails : ProblemDetails
{
    /// <summary>W3C trace id of the request, to find it in the logs.</summary>
    public string? TraceId { get; set; }

    /// <summary>Messages per field of a 422 problem.</summary>
    public IDictionary<string, string[]>? Errors { get; set; }
}
