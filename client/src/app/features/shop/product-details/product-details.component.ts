import { Component, inject, OnInit } from '@angular/core';
import { ShopService } from '../../../core/services/shop.service';
import { ActivatedRoute } from '@angular/router';
import { Product } from '../../../shared/models/product';
import { CurrencyPipe, NgClass } from '@angular/common';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatDivider } from '@angular/material/divider';
import { CartService } from '../../../core/services/cart.service';
import { FormsModule } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { StarRatingComponent } from '../../../shared/components/rating/star-rating.component';
import { ProductReviewsComponent } from './product-reviews.component';

@Component({
  selector: 'app-product-details',
  standalone: true,
  imports: [
    CurrencyPipe,
    MatButton,
    MatIcon,
    MatFormField,
    MatInput,
    MatLabel,
    MatDivider,
    FormsModule,
    MatSelectModule,
    NgClass,
    StarRatingComponent,
    ProductReviewsComponent
  ],
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.scss'
})
export class ProductDetailsComponent implements OnInit {
  private shopService = inject(ShopService);
  private activatedRoute = inject(ActivatedRoute);
  private cartService = inject(CartService);
  private snackbar = inject(SnackbarService);
  product?: Product;
  quantityInCart = 0;
  quantity = 1;
  selectedSize: string | null = null;
  private shouldScrollToReviews = false;

  ngOnInit(): void {
    this.activatedRoute.fragment.subscribe(fragment => {
      this.shouldScrollToReviews = fragment === 'reviews';
    });
    this.loadProduct();
  }

  loadProduct() {
    const id = this.activatedRoute.snapshot.paramMap.get('id');
    if (!id) return;
    this.shopService.getProduct(+id).subscribe({
      next: product => {
        this.product = product;
        this.selectedSize = this.firstAvailableSize();
        this.updateQuantityInCart();
        if (this.shouldScrollToReviews) {
          setTimeout(() => this.scrollToReviews(), 100);
          this.shouldScrollToReviews = false;
        }
      },
      error: error => console.log(error)
    })
  }

  updateCart() {
    if (!this.product || !this.selectedSize) return;
    const variantQty = this.getVariantQuantity(this.selectedSize);
    if (variantQty <= 0) {
      this.snackbar.error(`Size ${this.selectedSize} is out of stock`);
      return;
    }
    if (this.quantity > variantQty) {
      this.snackbar.error(`Only ${variantQty} left in stock`);
      this.quantity = variantQty;
      return;
    }
    try {
      if (this.quantity > this.quantityInCart) {
        const itemsToAdd = this.quantity - this.quantityInCart;
        this.cartService.addItemToCart(this.product, itemsToAdd, this.selectedSize);
        this.quantityInCart += itemsToAdd;
      } else {
        const itemsToRemove = this.quantityInCart - this.quantity;
        this.cartService.removeItemFromCart(this.product.id, itemsToRemove, this.selectedSize);
        this.quantityInCart -= itemsToRemove;
      }
    } catch (error: any) {
      this.snackbar.error(error?.message || 'Not enough stock');
      this.updateQuantityInCart();
    }
  }

  updateQuantityInCart() {
    const matchingItems = this.cartService.cart()?.items.filter(x => x.productId === this.product?.id) || [];
    const currentItem = this.selectedSize
      ? matchingItems.find(x => x.size === this.selectedSize)
      : matchingItems[0];

    if (currentItem) {
      this.selectedSize = currentItem.size;
      this.quantityInCart = currentItem.quantity;
    } else {
      this.quantityInCart = 0;
    }
    this.quantity = this.quantityInCart || 1;
  }

  refreshProductRating() {
    if (!this.product) return;
    this.shopService.getProduct(this.product.id).subscribe({
      next: updated => {
        if (!this.product) {
          this.product = updated;
          this.selectedSize = this.firstAvailableSize();
          this.updateQuantityInCart();
          return;
        }
        this.product.ratingAverage = updated.ratingAverage;
        this.product.ratingCount = updated.ratingCount;
      },
      error: error => console.error(error)
    })
  }

  scrollToReviews() {
    const el = document.getElementById('reviews-section');
    if (el) {
      el.scrollIntoView({behavior: 'smooth', block: 'start'});
    }
  }

  getButtonText() {
    return this.quantityInCart > 0 ? 'Update cart' : 'Add to cart'
  }

  get stockWarning(): string | null {
    if (!this.product || !this.selectedSize) return null;
    const qty = this.getVariantQuantity(this.selectedSize);
    if (qty <= 0) return `Size ${this.selectedSize} out of stock`;
    if (qty < 5) return `Only ${qty} left for size ${this.selectedSize}`;
    return null;
  }

  get availableSizes(): string[] {
    if (!this.product) return [];
    return this.product.variants.map(v => v.size);
  }

  private firstAvailableSize(): string | null {
    const anyAvailable = this.product?.variants.find(v => v.quantityInStock > 0)?.size;
    return anyAvailable ?? this.availableSizes[0] ?? null;
  }

  getVariantQuantity(size: string): number {
    if (!this.product) return 0;
    const variant = this.product.variants.find(v => v.size === size);
    return variant?.quantityInStock ?? 0;
  }
}
