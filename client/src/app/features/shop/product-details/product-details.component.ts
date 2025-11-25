import { Component, inject, OnInit } from '@angular/core';
import { ShopService } from '../../../core/services/shop.service';
import { ActivatedRoute } from '@angular/router';
import { Product } from '../../../shared/models/product';
import { CurrencyPipe } from '@angular/common';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatDivider } from '@angular/material/divider';
import { CartService } from '../../../core/services/cart.service';
import { FormsModule } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { SnackbarService } from '../../../core/services/snackbar.service';

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
    MatSelectModule
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
  sizes = ['XS', 'S', 'M', 'L', 'XL'];
  selectedSize: string | null = null;

  ngOnInit(): void {
    this.loadProduct();
  }

  loadProduct() {
    const id = this.activatedRoute.snapshot.paramMap.get('id');
    if (!id) return;
    this.shopService.getProduct(+id).subscribe({
      next: product => {
        this.product = product;
        this.updateQuantityInCart();
      },
      error: error => console.log(error)
    })
  }

  updateCart() {
    if (!this.product) return;
    if (this.product.quantityInStock <= 0) {
      this.snackbar.error('Out of stock');
      return;
    }
    if (this.quantity > this.product.quantityInStock) {
      this.snackbar.error(`Only ${this.product.quantityInStock} left in stock`);
      this.quantity = this.product.quantityInStock;
      return;
    }
    if (!this.selectedSize) {
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

  getButtonText() {
    return this.quantityInCart > 0 ? 'Update cart' : 'Add to cart'
  }

  get stockWarning(): string | null {
    if (!this.product) return null;
    if (this.product.quantityInStock <= 0) return 'Out of stock';
    if (this.product.quantityInStock < 5) return `Only ${this.product.quantityInStock} left`;
    return null;
  }
}
