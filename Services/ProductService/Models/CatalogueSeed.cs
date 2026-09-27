namespace Store.ProductService.Models;

/// <summary>
/// The version of the demo catalogue a database has been brought to - a single row. The seeder
/// compares it with <c>DemoCatalogue.Version</c> to know whether products seeded earlier still
/// need the changes of a newer version; each version is applied once, so what an administrator
/// changes afterwards stays as it is.
/// </summary>
public class CatalogueSeed
{
    /// <summary>The one row there is.</summary>
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public int Version { get; set; }

    public DateTime AppliedAt { get; set; }
}
