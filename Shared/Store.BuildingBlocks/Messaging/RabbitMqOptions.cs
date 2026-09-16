using System.ComponentModel.DataAnnotations;

namespace Store.BuildingBlocks.Messaging;

/// <summary>
/// Broker connection, bound from the <c>RabbitMQ</c> section and validated on start. The
/// password has no default: use user-secrets locally and <c>RabbitMQ__Password</c> in containers.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    [Required(AllowEmptyStrings = false)]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public ushort Port { get; init; } = 5672;

    [Required(AllowEmptyStrings = false)]
    public string VirtualHost { get; init; } = "/";

    [Required(AllowEmptyStrings = false)]
    public string Username { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Password { get; init; } = string.Empty;
}
