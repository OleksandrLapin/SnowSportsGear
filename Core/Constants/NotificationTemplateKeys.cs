namespace Core.Constants;

public static class NotificationTemplateKeys
{
    // Account & security
    public const string AccountEmailConfirmation = "account.email-confirmation";
    public const string AccountWelcome = "account.welcome";
    public const string AccountPasswordReset = "account.password-reset";
    public const string AccountPasswordChanged = "account.password-changed";
    public const string AccountEmailChange = "account.email-change";
    public const string AccountSuspiciousActivity = "account.suspicious-activity";
    public const string AccountTwoFactorCode = "account.two-factor-code";
    public const string AccountDeletionRequest = "account.deletion-request";
    public const string AccountDataExportRequest = "account.data-export-request";

    // Orders & payments
    public const string OrderCreated = "order.created";
    public const string OrderPaymentReceived = "order.payment-received";
    public const string OrderPaymentDeclined = "order.payment-declined";
    public const string OrderPaymentPending = "order.payment-pending";
    public const string OrderPaymentRetry = "order.payment-retry";
    public const string OrderInvoiceAvailable = "order.invoice-available";
    public const string OrderStatusUpdated = "order.status-updated";
    public const string OrderCancelled = "order.cancelled";

    // Delivery
    public const string DeliveryHandedOff = "delivery.handed-off";
    public const string DeliveryUpdated = "delivery.updated";

    // Returns
    public const string ReturnRequestCreated = "returns.request-created";

    // Admin notifications
    public const string AdminNewOrder = "admin.order.new";
    public const string AdminOrderPaid = "admin.order.paid";
    public const string AdminOrderCancelled = "admin.order.cancelled";
    public const string AdminOrderManualReview = "admin.order.manual-review";
    public const string AdminInventoryLow = "admin.inventory.low";
    public const string AdminInventorySyncFailed = "admin.inventory.sync-failed";
    public const string AdminShippingLabelFailed = "admin.shipping.label-failed";
    public const string AdminReturnReceived = "admin.returns.received";
    public const string AdminReturnExpected = "admin.returns.expected";
    public const string AdminPaymentFailed = "admin.payments.failed";
    public const string AdminRefundSuccess = "admin.payments.refund-success";
    public const string AdminRefundFailed = "admin.payments.refund-failed";
    public const string AdminFeeMismatch = "admin.payments.fee-mismatch";
    public const string AdminNewReview = "admin.reviews.new";
    public const string AdminAccountRequest = "admin.account.request";
}
