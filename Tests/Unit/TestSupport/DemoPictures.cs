using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Pictures;

namespace Store.Tests.Unit.TestSupport;

/// <summary>The pictures container of the local stack (Azurite), which the demo data is built against in the tests.</summary>
public static class DemoPictures
{
    public const string BaseUrl = "http://localhost:10000/devstoreaccount1/product-images/";

    public static PictureLinks Links { get; } = new(Options.Create(new PictureOptions { BaseUrl = BaseUrl }));
}
