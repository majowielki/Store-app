using System.ComponentModel.DataAnnotations;

namespace Store.OrderService.Saga;

/// <summary>How long a customer has to pay and how often the deadline is checked. Section "OrderSaga".</summary>
public sealed class OrderSagaOptions
{
    public const string SectionName = "OrderSaga";

    /// <summary>Minutes between the stock being reserved and the order being cancelled unpaid.</summary>
    [Range(1, 24 * 60)]
    public int PaymentWindowMinutes { get; init; } = 15;

    /// <summary>Seconds between two looks for orders past their deadline.</summary>
    [Range(1, 3600)]
    public int DeadlineCheckSeconds { get; init; } = 30;

    public TimeSpan PaymentWindow => TimeSpan.FromMinutes(PaymentWindowMinutes);
}
