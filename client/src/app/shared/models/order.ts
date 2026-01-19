export interface Order {
    id: number
    orderDate: string
    buyerEmail: string
    shippingAddress: ShippingAddress
    deliveryMethod: string
    shippingPrice: number
    paymentSummary: PaymentSummary
    orderItems: OrderItem[]
    subtotal: number
    discount?: number
    status: string
    total: number
    paymentIntentId: string
    statusUpdatedAt?: string | null
    trackingNumber?: string | null
    trackingUrl?: string | null
    cancelledBy?: string | null
    cancelledReason?: string | null
    deliveryUpdateDetails?: string | null
  }
  
  export interface ShippingAddress {
    name: string
    line1: string
    line2?: string
    city: string
    state: string
    postalCode: string
    country: string
  }
  
  export interface PaymentSummary {
    last4: number
    brand: string
    expMonth: number
    expYear: number
  }
  
export interface OrderItem {
    productId: number
    productName: string
    pictureUrl: string
    price: number
    quantity: number
    size: string
    reviewId?: number | null
    reviewRating?: number | null
    reviewDate?: string | null
    canReview?: boolean
  }
  
  export interface OrderToCreate {
    cartId: string;
    deliveryMethodId: number;
    shippingAddress: ShippingAddress;
    paymentSummary: PaymentSummary;
    discount?: number;
  }
