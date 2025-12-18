export type Product = {
    id: number;
    name: string;
    description: string;
    price: number;
    pictureUrl?: string;
    type: string;
    brand: string;
    quantityInStock: number;
    variants: ProductVariant[];
    ratingAverage?: number;
    ratingCount?: number;
    salePrice?: number | null;
    lowestPrice?: number | null;
    color?: string | null;
}

export type ProductVariant = {
    size: string;
    quantityInStock: number;
}
