using Core.Constants;
using Core.Entities.Notifications;

namespace Core.Helpers;

public static class NotificationTemplateDefaults
{
    private const string CodeSpan = "<span style=\"display:inline-block; margin:12px 0; padding:10px 14px; font-family:'Courier New', monospace; font-size:18px; letter-spacing:2px; color:#4c1d95; background:#f3e8ff; border:1px dashed #c4b5fd; border-radius:10px;\">{{Code}}</span>";

    public static IReadOnlyList<NotificationTemplate> GetDefaults()
    {
        var now = DateTime.UtcNow;
        return new List<NotificationTemplate>
        {
            new()
            {
                Key = NotificationTemplateKeys.AccountEmailConfirmation,
                Category = NotificationCategory.AccountSecurity,
                Subject = "Confirm your email for {{StoreName}}",
                Headline = "Verify your email",
                Body = "Use the confirmation code below to verify your email address:<br>" + CodeSpan + "<br>This code expires in {{CodeExpiry}}.",
                CtaLabel = "Confirm email",
                CtaUrl = "{{ConfirmUrl}}",
                Footer = "If you did not create an account, you can ignore this email.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AccountWelcome,
                Category = NotificationCategory.AccountSecurity,
                Subject = "Welcome to {{StoreName}}",
                Headline = "Welcome, {{CustomerName}}",
                Body = "Thanks for joining {{StoreName}}. Your account is ready and you can start shopping right away.",
                CtaLabel = "Shop now",
                CtaUrl = "{{AppUrl}}/shop",
                Footer = "Need help? Contact us at {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AccountPasswordReset,
                Category = NotificationCategory.AccountSecurity,
                Subject = "Reset your {{StoreName}} password",
                Headline = "Password reset request",
                Body = "We received a request to reset your password. Use this code:<br>" + CodeSpan + "<br>or reset using the link below. This code expires in {{CodeExpiry}}.",
                CtaLabel = "Reset password",
                CtaUrl = "{{ResetUrl}}",
                Footer = "If you did not request a reset, you can ignore this email.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AccountPasswordChanged,
                Category = NotificationCategory.AccountSecurity,
                Subject = "Your {{StoreName}} password was changed",
                Headline = "Password updated",
                Body = "Your password was updated successfully. If this was not you, please reset your password immediately.",
                CtaLabel = "Reset password",
                CtaUrl = "{{ResetUrl}}",
                Footer = "If you need help, contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AccountEmailChange,
                Category = NotificationCategory.AccountSecurity,
                Subject = "Confirm your new email",
                Headline = "Confirm email change",
                Body = "Use the code below to confirm your new email address:<br>" + CodeSpan + "<br>This code expires in {{CodeExpiry}}.",
                CtaLabel = "Confirm email",
                CtaUrl = "{{ConfirmUrl}}",
                Footer = "If you did not request this change, contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AccountSuspiciousActivity,
                Category = NotificationCategory.AccountSecurity,
                Subject = "Suspicious activity detected",
                Headline = "We noticed unusual sign-in attempts",
                Body = "There were {{FailedAttempts}} failed sign-in attempts on your account. If this was not you, reset your password now.",
                CtaLabel = "Secure my account",
                CtaUrl = "{{ResetUrl}}",
                Footer = "For help, contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AccountTwoFactorCode,
                Category = NotificationCategory.AccountSecurity,
                Subject = "Your {{StoreName}} login code",
                Headline = "Two-factor authentication",
                Body = "Use this code to complete your sign-in:<br>" + CodeSpan + "<br>This code expires in {{CodeExpiry}}.",
                Footer = "If you did not attempt to sign in, contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AccountDeletionRequest,
                Category = NotificationCategory.AccountSecurity,
                Subject = "Account deletion request received",
                Headline = "We received your request",
                Body = "Your request to delete your account was received. If this was not you, contact support immediately.",
                CtaLabel = "Contact support",
                CtaUrl = "{{AppUrl}}/support",
                Footer = "We will process the request within {{ProcessingTime}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AccountDataExportRequest,
                Category = NotificationCategory.AccountSecurity,
                Subject = "Data export request received",
                Headline = "Your data export request is in progress",
                Body = "We are preparing your data export. You will receive another email when it is ready.",
                Footer = "If this was not you, contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Key = NotificationTemplateKeys.OrderCreated,
                Category = NotificationCategory.OrdersPayments,
                Subject = "Order {{OrderNumber}} confirmed",
                Headline = "Thanks for your order",
                Body = "We received your order and will start processing it soon.<div style=\"margin:16px 0;\">{{OrderSummary}}</div><div style=\"margin:8px 0;\">Ship to: {{ShippingAddress}}</div><div style=\"margin:8px 0;\">Payment: {{PaymentSummary}}</div>",
                CtaLabel = "View order",
                CtaUrl = "{{OrderUrl}}",
                Footer = "Questions? Contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.OrderPaymentReceived,
                Category = NotificationCategory.OrdersPayments,
                Subject = "Payment received for order {{OrderNumber}}",
                Headline = "Payment received",
                Body = "We received your payment for order {{OrderNumber}}. We will keep you updated on the next steps.",
                CtaLabel = "View order",
                CtaUrl = "{{OrderUrl}}",
                Footer = "Thank you for shopping with {{StoreName}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.OrderPaymentDeclined,
                Category = NotificationCategory.OrdersPayments,
                Subject = "Payment declined for order {{OrderNumber}}",
                Headline = "Payment declined",
                Body = "Your payment could not be processed. Please update your payment method and try again.",
                CtaLabel = "Retry payment",
                CtaUrl = "{{PaymentUrl}}",
                Footer = "If you need help, contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.OrderPaymentPending,
                Category = NotificationCategory.OrdersPayments,
                Subject = "Payment pending for order {{OrderNumber}}",
                Headline = "Payment pending",
                Body = "Your payment is pending. We will notify you once it is confirmed.",
                CtaLabel = "View order",
                CtaUrl = "{{OrderUrl}}",
                Footer = "If you have questions, contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.OrderPaymentRetry,
                Category = NotificationCategory.OrdersPayments,
                Subject = "Action required for order {{OrderNumber}}",
                Headline = "Payment failed",
                Body = "We could not complete the payment for your order. Please retry using a valid payment method.",
                CtaLabel = "Retry payment",
                CtaUrl = "{{PaymentUrl}}",
                Footer = "If you need help, contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.OrderInvoiceAvailable,
                Category = NotificationCategory.OrdersPayments,
                Subject = "Invoice ready for order {{OrderNumber}}",
                Headline = "Invoice available",
                Body = "Your invoice is ready. You can download it using the link below.",
                CtaLabel = "Download invoice",
                CtaUrl = "{{InvoiceUrl}}",
                Footer = "Thank you for shopping with {{StoreName}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.OrderStatusUpdated,
                Category = NotificationCategory.OrdersPayments,
                Subject = "Order {{OrderNumber}} status updated",
                Headline = "Order status update",
                Body = "Your order status changed to: <strong>{{OrderStatus}}</strong>.",
                CtaLabel = "View order",
                CtaUrl = "{{OrderUrl}}",
                Footer = "We will keep you updated as your order moves forward.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.OrderCancelled,
                Category = NotificationCategory.OrdersPayments,
                Subject = "Order {{OrderNumber}} cancelled",
                Headline = "Order cancelled",
                Body = "Your order was cancelled by {{CancelledBy}}. Reason: {{CancelReason}}.",
                CtaLabel = "View order",
                CtaUrl = "{{OrderUrl}}",
                Footer = "If you have questions, contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Key = NotificationTemplateKeys.DeliveryHandedOff,
                Category = NotificationCategory.Delivery,
                Subject = "Your order {{OrderNumber}} is with the carrier",
                Headline = "Shipped and on the way",
                Body = "Your order has been handed off to the carrier. Tracking number: {{TrackingNumber}}.",
                CtaLabel = "Track shipment",
                CtaUrl = "{{TrackingUrl}}",
                Footer = "Delivery times may vary.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.DeliveryUpdated,
                Category = NotificationCategory.Delivery,
                Subject = "Delivery updated for order {{OrderNumber}}",
                Headline = "Delivery update",
                Body = "Your delivery details have been updated to: {{DeliveryDetails}}.",
                CtaLabel = "View order",
                CtaUrl = "{{OrderUrl}}",
                Footer = "If this was not requested by you, contact {{SupportEmail}}.",
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Key = NotificationTemplateKeys.ReturnRequestCreated,
                Category = NotificationCategory.Returns,
                Subject = "Return request created for order {{OrderNumber}}",
                Headline = "Return request received",
                Body = "We received your return request for order {{OrderNumber}}. Reason: {{ReturnReason}}.",
                CtaLabel = "View return",
                CtaUrl = "{{ReturnUrl}}",
                Footer = "We will contact you with next steps.",
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Key = NotificationTemplateKeys.AdminNewOrder,
                Category = NotificationCategory.AdminOrders,
                Subject = "New order {{OrderNumber}}",
                Headline = "New order placed",
                Body = "Order {{OrderNumber}} was placed by {{CustomerEmail}} for {{OrderTotal}}.",
                CtaLabel = "View order",
                CtaUrl = "{{AdminOrderUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AdminOrderPaid,
                Category = NotificationCategory.AdminOrders,
                Subject = "Order {{OrderNumber}} paid",
                Headline = "Payment received",
                Body = "Payment received for order {{OrderNumber}} ({{OrderTotal}}).",
                CtaLabel = "View order",
                CtaUrl = "{{AdminOrderUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AdminOrderCancelled,
                Category = NotificationCategory.AdminOrders,
                Subject = "Order {{OrderNumber}} cancelled",
                Headline = "Order cancelled",
                Body = "Order {{OrderNumber}} was cancelled by {{CancelledBy}}. Reason: {{CancelReason}}.",
                CtaLabel = "View order",
                CtaUrl = "{{AdminOrderUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AdminOrderManualReview,
                Category = NotificationCategory.AdminOrders,
                Subject = "Order {{OrderNumber}} requires review",
                Headline = "Manual review required",
                Body = "Order {{OrderNumber}} was flagged for manual review. Reason: {{ReviewReason}}.",
                CtaLabel = "Review order",
                CtaUrl = "{{AdminOrderUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Key = NotificationTemplateKeys.AdminInventoryLow,
                Category = NotificationCategory.AdminInventory,
                Subject = "Low stock: {{ProductName}}",
                Headline = "Low stock alert",
                Body = "Variant {{Variant}} for {{ProductName}} is low on stock ({{Quantity}} left).",
                CtaLabel = "View product",
                CtaUrl = "{{AdminProductUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AdminInventorySyncFailed,
                Category = NotificationCategory.AdminInventory,
                Subject = "Inventory sync failed",
                Headline = "Inventory sync error",
                Body = "Inventory sync failed for {{SystemName}}. Error: {{ErrorMessage}}.",
                CtaLabel = "Review inventory",
                CtaUrl = "{{AdminInventoryUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AdminShippingLabelFailed,
                Category = NotificationCategory.AdminInventory,
                Subject = "Shipping label failed for order {{OrderNumber}}",
                Headline = "Shipping label error",
                Body = "We could not create a shipping label for order {{OrderNumber}}. Error: {{ErrorMessage}}.",
                CtaLabel = "View order",
                CtaUrl = "{{AdminOrderUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Key = NotificationTemplateKeys.AdminReturnReceived,
                Category = NotificationCategory.AdminInventory,
                Subject = "Return received for order {{OrderNumber}}",
                Headline = "Return received",
                Body = "Return received for order {{OrderNumber}}. Items: {{ReturnItems}}.",
                CtaLabel = "View return",
                CtaUrl = "{{AdminReturnUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AdminReturnExpected,
                Category = NotificationCategory.AdminInventory,
                Subject = "Return expected for order {{OrderNumber}}",
                Headline = "Return expected",
                Body = "A return is expected for order {{OrderNumber}}. ETA: {{ReturnEta}}.",
                CtaLabel = "View return",
                CtaUrl = "{{AdminReturnUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Key = NotificationTemplateKeys.AdminPaymentFailed,
                Category = NotificationCategory.AdminPayments,
                Subject = "Payment failed for order {{OrderNumber}}",
                Headline = "Payment failure",
                Body = "Payment failed for order {{OrderNumber}}. Reason: {{ErrorMessage}}.",
                CtaLabel = "View order",
                CtaUrl = "{{AdminOrderUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AdminRefundSuccess,
                Category = NotificationCategory.AdminPayments,
                Subject = "Refund completed for order {{OrderNumber}}",
                Headline = "Refund completed",
                Body = "Refund processed for order {{OrderNumber}} ({{OrderTotal}}).",
                CtaLabel = "View order",
                CtaUrl = "{{AdminOrderUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AdminRefundFailed,
                Category = NotificationCategory.AdminPayments,
                Subject = "Refund failed for order {{OrderNumber}}",
                Headline = "Refund failed",
                Body = "Refund failed for order {{OrderNumber}}. Error: {{ErrorMessage}}.",
                CtaLabel = "View order",
                CtaUrl = "{{AdminOrderUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AdminFeeMismatch,
                Category = NotificationCategory.AdminPayments,
                Subject = "Payment mismatch for order {{OrderNumber}}",
                Headline = "Payment mismatch",
                Body = "Order {{OrderNumber}} has a payment mismatch. Expected {{ExpectedAmount}}, received {{ActualAmount}}.",
                CtaLabel = "Review order",
                CtaUrl = "{{AdminOrderUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },

            new()
            {
                Key = NotificationTemplateKeys.AdminNewReview,
                Category = NotificationCategory.AdminCustomers,
                Subject = "New review on {{ProductName}}",
                Headline = "New customer review",
                Body = "{{CustomerEmail}} left a {{Rating}} star review for {{ProductName}}.",
                CtaLabel = "Review feedback",
                CtaUrl = "{{AdminReviewUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Key = NotificationTemplateKeys.AdminAccountRequest,
                Category = NotificationCategory.AdminCustomers,
                Subject = "Account request: {{RequestType}}",
                Headline = "Customer account request",
                Body = "{{CustomerEmail}} submitted a request: {{RequestType}}.",
                CtaLabel = "Open admin",
                CtaUrl = "{{AdminDashboardUrl}}",
                Footer = "Store admin notification.",
                CreatedAt = now,
                UpdatedAt = now
            }
        };
    }
}
