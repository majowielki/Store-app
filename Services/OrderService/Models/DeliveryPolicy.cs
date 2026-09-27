using System.ComponentModel.DataAnnotations;

namespace Store.OrderService.Models;

/// <summary>Delivery times, bound from the <c>Delivery</c> section; the defaults are the store's current rules.</summary>
public sealed class DeliveryOptions
{
    public const string SectionName = "Delivery";

    /// <summary>An order placed at or after this hour (warehouse time) counts as placed on the next business day.</summary>
    [Range(0, 24)]
    public int CutoffHour { get; init; } = 14;

    /// <summary>Business days in transit after the parcel leaves: the earliest and the latest.</summary>
    [Range(0, 30)]
    public int TransitDaysMin { get; init; } = 2;

    [Range(0, 30)]
    public int TransitDaysMax { get; init; } = 4;
}

/// <summary>The dates a parcel should arrive between, both included.</summary>
public readonly record struct DeliveryWindow(DateOnly From, DateOnly To);

/// <summary>
/// When an order placed now arrives. It is packed on the business day it is placed (or the next
/// one, after the cut-off hour or at the weekend), leaves the warehouse the business day after
/// that and is in transit for a few business days. Business days are Monday to Friday; public
/// holidays are not counted.
/// </summary>
public static class DeliveryPolicy
{
    /// <summary>
    /// The warehouse's clock: central European time, summer time from the last Sunday of March
    /// to the last Sunday of October. Built in rather than looked up, because the service runs
    /// with invariant globalization (no IANA names on Windows) and in an image without tzdata.
    /// </summary>
    public static readonly TimeZoneInfo WarehouseTime = TimeZoneInfo.CreateCustomTimeZone(
        "Central European Time",
        TimeSpan.FromHours(1),
        "Central European Time",
        "Central European Time",
        "Central European Summer Time",
        [
            TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                DateTime.MinValue.Date,
                DateTime.MaxValue.Date,
                TimeSpan.FromHours(1),
                TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday),
                TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday))
        ]);

    public static DeliveryWindow Estimate(DateTimeOffset now, DeliveryOptions options)
    {
        var local = TimeZoneInfo.ConvertTime(now, WarehouseTime);
        var placed = DateOnly.FromDateTime(local.DateTime);
        if (!IsBusinessDay(placed) || local.Hour >= options.CutoffHour)
        {
            placed = AddBusinessDays(placed, 1);
        }

        var shipped = AddBusinessDays(placed, 1);
        return new DeliveryWindow(AddBusinessDays(shipped, options.TransitDaysMin), AddBusinessDays(shipped, options.TransitDaysMax));
    }

    private static bool IsBusinessDay(DateOnly day) => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    /// <summary>The business day <paramref name="days"/> business days after <paramref name="day"/>, which need not be one itself.</summary>
    private static DateOnly AddBusinessDays(DateOnly day, int days)
    {
        while (days > 0)
        {
            day = day.AddDays(1);
            if (IsBusinessDay(day)) days--;
        }
        return day;
    }
}
