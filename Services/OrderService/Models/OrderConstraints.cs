namespace Store.OrderService.Models;

/// <summary>One place for the limits the database schema and the request validators must agree on.</summary>
public static class OrderConstraints
{
    public const int EmailMaxLength = 256;
    public const int CustomerNameMinLength = 2;
    public const int CustomerNameMaxLength = 100;
    public const int DeliveryAddressMaxLength = 300;
    public const int NotesMaxLength = 500;
    public const int DiscountReasonMaxLength = 50;
    public const int ProductTitleMaxLength = 200;
    public const int CompanyMaxLength = 100;
    public const int ColorMaxLength = 50;

    /// <summary>The longest name of an enum stored by name (a status, a kind of discount).</summary>
    public const int EnumMaxLength = 20;

    /// <summary>A SHA-256 hash in hex.</summary>
    public const int RequestHashLength = 64;

    /// <summary>The longest name of a state of the order saga.</summary>
    public const int SagaStateMaxLength = 64;
}
