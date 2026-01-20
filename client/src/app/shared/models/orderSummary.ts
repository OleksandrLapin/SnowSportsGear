export interface OrderSummary {
  id: number;
  buyerEmail: string;
  orderDate: string;
  status: string;
  total: number;
  totalItems?: number;
  previewItems?: OrderSummaryItem[];
}

export interface OrderSummaryItem {
  productId: number;
  productName: string;
  pictureUrl: string;
  price: number;
  quantity: number;
  size: string;
}
