using Store.Contracts.Catalog.V1;
using System.Text;
using static Store.NotificationService.Mail.MailFormats;
using static Store.NotificationService.Mail.MailHtml;

namespace Store.NotificationService.Mail.Templates;

/// <summary>A sold-out product the visitor waited for can be bought again; they are told once.</summary>
public sealed class BackInStockMail : IMailTemplate<ProductBackInStock>
{
    public const string Kind = "back-in-stock";

    private const string OnlyOnce = "You asked us to tell you when it was back. We will not write about it again.";

    private readonly ShopLinks _links;

    public BackInStockMail(ShopLinks links)
    {
        _links = links;
    }

    public Email Render(ProductBackInStock product)
    {
        var link = _links.Product(product.ProductId);
        var html = new StringBuilder()
            .Append(Image(product.ProductImage, product.ProductTitle))
            .Append(Paragraph($"Good news: <strong>{Encode(product.ProductTitle)}</strong> is back in stock, at {Money(product.Price)}."))
            .Append(Paragraph("It sold out once already - order soon if you would like one."))
            .Append(Button("Shop it now", link))
            .Append(Paragraph(Note(OnlyOnce)))
            .ToString();
        var text = $"Good news: {product.ProductTitle} is back in stock, at {Money(product.Price)}.\n\nIt sold out once already - order soon if you would like one.\n\nShop it now: {link}\n\n{OnlyOnce}\n";
        return new Email(product.SubscriberEmail, $"{product.ProductTitle} is back in stock", Layout("It is back", html), text, Kind);
    }
}
