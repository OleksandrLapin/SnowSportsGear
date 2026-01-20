export const ORDER_STATUS_LABELS: Record<string, string> = {
  Pending: 'Pending',
  PaymentReceived: 'Payment received',
  PaymentFailed: 'Payment failed',
  PaymentMismatch: 'Payment mismatch',
  Refunded: 'Refunded',
  Processing: 'Processing',
  Packed: 'Packed',
  Shipped: 'In transit',
  Delivered: 'Delivered',
  Cancelled: 'Cancelled'
};

export const ORDER_STATUS_BADGE_CLASSES: Record<string, string> = {
  Pending: 'bg-amber-100 text-amber-700',
  PaymentReceived: 'bg-emerald-100 text-emerald-700',
  PaymentFailed: 'bg-red-100 text-red-700',
  PaymentMismatch: 'bg-orange-100 text-orange-700',
  Refunded: 'bg-slate-100 text-slate-700',
  Processing: 'bg-blue-100 text-blue-700',
  Packed: 'bg-indigo-100 text-indigo-700',
  Shipped: 'bg-sky-100 text-sky-700',
  Delivered: 'bg-green-100 text-green-700',
  Cancelled: 'bg-red-100 text-red-700'
};
