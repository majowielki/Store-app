namespace Store.Contracts.Catalog.V1;

/// <summary>
/// Published by the product service for each customer who asked to be told when a product
/// that had run out can be bought again - one event per subscriber, so one failed e-mail is
/// retried on its own. Consumer: the notification service. Published through the outbox, so it
/// is delivered at least once.
/// </summary>
/// <param name="ProductId">Catalogue product id</param>
/// <param name="ProductTitle">Title</param>
/// <param name="ProductSlug">Address-friendly name, for the link to the product page</param>
/// <param name="ProductImage">Main picture</param>
/// <param name="Price">The price a customer pays now</param>
/// <param name="SubscriberEmail">Where to send the notice</param>
/// <param name="AvailableAt">When the product became available (UTC)</param>
public sealed record ProductBackInStock(
    int ProductId,
    string ProductTitle,
    string ProductSlug,
    string ProductImage,
    decimal Price,
    string SubscriberEmail,
    DateTime AvailableAt);
