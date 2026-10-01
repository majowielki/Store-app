using System.ComponentModel.DataAnnotations;

namespace Store.AuditLogService.Models;

/// <summary>
/// A step a visitor took towards buying, sent by the shop's pages for the purchase funnel (ADR 020):
/// what happened, to which product and when. Nothing about who - no account, no address, no cookie.
/// </summary>
public class ShopEvent : IHasLongId
{
    public const int KindMaxLength = 20;
    public long Id { get; set; }

    public ShopEventKind Kind { get; set; }

    public int ProductId { get; set; }

    public DateTime OccurredAt { get; set; }
}

/// <summary>Stored by name.</summary>
public enum ShopEventKind
{
    /// <summary>A product's page or its quick view was opened.</summary>
    ProductViewed,

    /// <summary>A product went into the bag.</summary>
    AddedToBag
}

/// <summary>Body of POST /api/v1/shop-events.</summary>
public sealed class RecordShopEventRequest
{
    [Required]
    public ShopEventKind? Kind { get; set; }

    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }
}

/// <summary>The stages of the purchase funnel, in the order a visitor goes through them.</summary>
public enum FunnelStage
{
    ProductViewed,
    AddedToBag,
    OrderPlaced
}

/// <summary>How many times each stage was reached since the start of the window.</summary>
public sealed class FunnelResponse
{
    /// <summary>The start of the window: midnight (UTC) the given number of days ago, where the dashboard's order statistics start too.</summary>
    public DateTime Since { get; set; }

    public int Days { get; set; }

    /// <summary>Every stage, in order.</summary>
    public List<FunnelStageCount> Stages { get; set; } = [];
}

public sealed class FunnelStageCount
{
    public FunnelStage Stage { get; set; }

    public int Count { get; set; }
}
