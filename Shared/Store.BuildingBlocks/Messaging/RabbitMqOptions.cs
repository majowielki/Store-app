using System.ComponentModel.DataAnnotations;

namespace Store.BuildingBlocks.Messaging;

/// <summary>
/// Broker connection, bound from the <c>RabbitMQ</c> section and validated on start. The
/// password has no default: use user-secrets locally and <c>RabbitMQ__Password</c> in containers.
/// </summary>
public sealed class RabbitMqOptions : IValidatableObject
{
    [Range(1, 60)] public int OutboxQueryDelaySeconds { get; init; } = 1;
    [Range(1, 10080)] public int DuplicateDetectionMinutes { get; init; } = 30;
    [Range(1, 1024)] public ushort PrefetchCount { get; init; } = 16;
    [Range(0, 10)] public int RetryCount { get; init; } = 3;
    [Range(1, 300)] public int RetryMinSeconds { get; init; } = 1;
    [Range(1, 3600)] public int RetryMaxSeconds { get; init; } = 30;
    [Range(1, 300)] public int RetryDeltaSeconds { get; init; } = 2;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RetryMinSeconds > RetryMaxSeconds)
            yield return new ValidationResult("RetryMinSeconds must not exceed RetryMaxSeconds.", [nameof(RetryMinSeconds), nameof(RetryMaxSeconds)]);
    }

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
