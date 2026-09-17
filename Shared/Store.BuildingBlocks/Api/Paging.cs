namespace Store.BuildingBlocks.Api;

/// <summary>
/// Page and page size as they arrive in the query string. Values outside the allowed range
/// are brought into it rather than rejected: a client asking for a page that is too large
/// gets the largest page there is.
/// </summary>
public sealed class PagedQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>Page ≥ 1 and 1 ≤ PageSize ≤ <paramref name="maxPageSize"/>.</summary>
    public PagedQuery Normalized(int defaultPageSize = DefaultPageSize, int maxPageSize = MaxPageSize) => new()
    {
        Page = Math.Max(Page, 1),
        PageSize = PageSize < 1 ? defaultPageSize : Math.Min(PageSize, maxPageSize)
    };

    /// <summary>Rows to skip for this page.</summary>
    public int Skip => (Page - 1) * PageSize;
}

/// <summary>One page of a listing, the same shape for every resource.</summary>
public sealed class PagedResponse<T>
{
    public PagedResponse()
    {
    }

    public PagedResponse(IReadOnlyList<T> items, int totalCount, PagedQuery query)
    {
        Items = items;
        TotalCount = totalCount;
        Page = query.Page;
        PageSize = query.PageSize;
    }

    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>Rows in the whole listing, not on this page.</summary>
    public int TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasNextPage => Page < TotalPages;

    public bool HasPreviousPage => Page > 1;
}
