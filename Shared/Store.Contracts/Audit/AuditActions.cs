namespace Store.Contracts.Audit;

/// <summary>
/// What an entry of the audit trail says happened (<see cref="V1.AuditEvent.Action"/>): one name
/// per business action, in capitals, the entity first. The audit service stores them as they are
/// and the admin panel filters by them, so a name, once used, stays.
/// </summary>
public static class AuditActions
{
    public const string UserAddressUpdated = "USER_ADDRESS_UPDATED";

    public const string ProductCreated = "PRODUCT_CREATED";
    public const string ProductUpdated = "PRODUCT_UPDATED";
    public const string ProductDeleted = "PRODUCT_DELETED";
    public const string ProductStockUpdated = "PRODUCT_STOCK_UPDATED";

    public const string CartItemAdded = "CART_ITEM_ADDED";
    public const string CartItemUpdated = "CART_ITEM_UPDATED";
    public const string CartItemRemoved = "CART_ITEM_REMOVED";
    public const string CartCleared = "CART_CLEARED";

    public const string OrderPlaced = "ORDER_PLACED";
    public const string OrderStatusChanged = "ORDER_STATUS_CHANGED";

    public const string DiscountCodeCreated = "DISCOUNT_CODE_CREATED";
    public const string DiscountCodeUpdated = "DISCOUNT_CODE_UPDATED";
    public const string DiscountCodeDeleted = "DISCOUNT_CODE_DELETED";

    public const string PaymentSucceeded = "PAYMENT_SUCCEEDED";
    public const string PaymentRefunded = "PAYMENT_REFUNDED";

    public const string ReviewSubmitted = "REVIEW_SUBMITTED";
    public const string ReviewReported = "REVIEW_REPORTED";
    public const string ReviewApproved = "REVIEW_APPROVED";
    public const string ReviewRejected = "REVIEW_REJECTED";

    /// <summary>"MAKER_CREATED" for the content kind "Maker"; the content service records its entries this way.</summary>
    public static string Created(string entityName) => Of(entityName, "CREATED");

    public static string Updated(string entityName) => Of(entityName, "UPDATED");

    public static string Deleted(string entityName) => Of(entityName, "DELETED");

    private static string Of(string entityName, string change) => $"{entityName.ToUpperInvariant()}_{change}";
}
