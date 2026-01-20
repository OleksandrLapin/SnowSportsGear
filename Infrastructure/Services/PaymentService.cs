using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ICartService cartService;
    private readonly IUnitOfWork unit;
    private readonly IProductRepository productRepository;

    public PaymentService(IConfiguration config, ICartService cartService,
        IUnitOfWork unit, IProductRepository productRepository)
    {
        this.cartService = cartService;
        this.unit = unit;
        this.productRepository = productRepository;
        StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];
    }

    public async Task<ShoppingCart?> CreateOrUpdatePaymentIntent(string cartId)
    {
        var cart = await cartService.GetCartAsync(cartId)
            ?? throw new Exception("Cart unavailable");

        var shippingPrice = await GetShippingPriceAsync(cart) ?? 0;

        await ValidateCartItemsInCartAsync(cart);

        var subtotal = CalculateSubtotal(cart);

        if (cart.Coupon != null)
        {
            subtotal = await ApplyDiscountAsync(cart.Coupon, subtotal);
        }

        var total = subtotal + shippingPrice;

        await CreateUpdatePaymentIntentAsync(cart, total);

        await cartService.SetCartAsync(cart);

        return cart;
    }

    public async Task<string> RefundPayment(string paymentIntentId)
    {
        var refundOptions = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId
        };

        var refundService = new RefundService();
        var result = await refundService.CreateAsync(refundOptions);

        return result.Status;
    }

    private async Task CreateUpdatePaymentIntentAsync(ShoppingCart cart, long total)
    {
        var service = new PaymentIntentService();

        if (string.IsNullOrEmpty(cart.PaymentIntentId))
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = total,
                Currency = "usd",
                PaymentMethodTypes = ["card"]
            };
            var intent = await service.CreateAsync(options);
            cart.PaymentIntentId = intent.Id;
            cart.ClientSecret = intent.ClientSecret;
        }
        else
        {
            var intent = await service.GetAsync(cart.PaymentIntentId);
            if (CanUpdateAmount(intent.Status))
            {
                var options = new PaymentIntentUpdateOptions
                {
                    Amount = total
                };
                intent = await service.UpdateAsync(cart.PaymentIntentId, options);
                cart.ClientSecret = intent.ClientSecret;
            }
            else
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = total,
                    Currency = "usd",
                    PaymentMethodTypes = ["card"]
                };
                intent = await service.CreateAsync(options);
                cart.PaymentIntentId = intent.Id;
                cart.ClientSecret = intent.ClientSecret;
            }
        }
    }

    private static bool CanUpdateAmount(string? status)
    {
        return status is "requires_payment_method" or "requires_confirmation" or "requires_action";
    }

    private async Task<long> ApplyDiscountAsync(AppCoupon appCoupon, long amount)
    {
        var couponService = new Stripe.CouponService();

        var coupon = await couponService.GetAsync(appCoupon.CouponId);

        if (coupon.AmountOff.HasValue)
        {
            amount -= (long)coupon.AmountOff * 100;
        }

        if (coupon.PercentOff.HasValue)
        {
            var discount = amount * (coupon.PercentOff.Value / 100);
            amount -= (long)discount;
        }

        return amount;
    }

    private long CalculateSubtotal(ShoppingCart cart)
    {
        var itemTotal = cart.Items.Sum(x => x.Quantity * x.Price * 100);
        return (long)itemTotal;
    }

    private async Task ValidateCartItemsInCartAsync(ShoppingCart cart)
    {
        foreach (var item in cart.Items)
        {
            var productItem = await productRepository.GetProductWithVariantsAsync(item.ProductId) 
                ?? throw new Exception("Problem getting product in cart");

            var hasSale = productItem.SalePrice.HasValue
                && productItem.SalePrice.Value > 0
                && productItem.SalePrice.Value < productItem.Price;
            var priceToUse = hasSale ? productItem.SalePrice!.Value : productItem.Price;

            item.OriginalPrice = productItem.Price;
            item.SalePrice = hasSale ? productItem.SalePrice : null;
            item.LowestPrice = productItem.LowestPrice;

            if (item.Price != priceToUse)
            {
                item.Price = priceToUse;
            }

            var variant = productItem.Variants.FirstOrDefault(v => v.Size == item.Size);
            if (variant == null || variant.QuantityInStock <= 0)
            {
                throw new Exception($"Size {item.Size} is out of stock for {productItem.Name}");
            }

            item.MaxQuantity = variant.QuantityInStock;

            if (item.Quantity > variant.QuantityInStock)
            {
                throw new Exception($"Only {variant.QuantityInStock} left in stock for {productItem.Name} size {item.Size}");
            }
        }
    }

    private async Task<long?> GetShippingPriceAsync(ShoppingCart cart)
    {
        if (cart.DeliveryMethodId.HasValue)
        {
            var deliveryMethod = await unit.Repository<DeliveryMethod>()
                .GetByIdAsync((int)cart.DeliveryMethodId)
                    ?? throw new Exception("Problem with delivery method");

            return (long)deliveryMethod.Price * 100;
        }

        return null;
    }
}
