using Microsoft.Extensions.Options;
using System.Globalization;

namespace Store.NotificationService.Mail;

/// <summary>The pages of the shop the e-mails lead to, under <see cref="MailOptions.ShopUrl"/>; the same routes as the UI's.</summary>
public sealed class ShopLinks
{
    private readonly string _shop;

    public ShopLinks(IOptions<MailOptions> options)
    {
        _shop = options.Value.ShopUrl.TrimEnd('/');
    }

    /// <summary>The customer's order page.</summary>
    public string Order(int orderId) => $"{_shop}/orders/{Id(orderId)}";

    /// <summary>The payment page of an order still waiting for its payment.</summary>
    public string PayOrder(int orderId) => $"{Order(orderId)}/pay";

    /// <summary>A product's page.</summary>
    public string Product(int productId) => $"{_shop}/products/{Id(productId)}";

    private static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);
}
