import { Component, inject, Input } from '@angular/core';
import { Product } from '../../../shared/models/product';
import { MatCard, MatCardActions, MatCardContent } from '@angular/material/card';
import { MatIcon } from '@angular/material/icon';
import { CurrencyPipe } from '@angular/common';
import { MatButton } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';
import { MatSelectModule } from '@angular/material/select';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-product-item',
  standalone: true,
  imports: [
    MatCard,
    MatCardContent,
    MatCardActions,
    MatIcon,
    CurrencyPipe,
    MatButton,
    RouterLink,
    MatSelectModule,
    FormsModule,
    MatFormField,
    MatLabel,
    NgClass
  ],
  templateUrl: './product-item.component.html',
  styleUrl: './product-item.component.scss'
})
export class ProductItemComponent {
  @Input() product?: Product;
  cartService = inject(CartService);
  selectedSize: string | null = null;

  addToCart(event: Event) {
    event.stopPropagation();
    if (!this.product || !this.selectedSize) return;
    const qty = this.getVariantQuantity(this.selectedSize);
    if (qty <= 0) return;
    this.cartService.addItemToCart(this.product, 1, this.selectedSize);
  }

  get availableSizes(): string[] {
    if (!this.product) return [];
    return this.product.variants.map(v => v.size);
  }

  getVariantQuantity(size: string): number {
    if (!this.product) return 0;
    return this.product.variants.find(v => v.size === size)?.quantityInStock ?? 0;
  }

  isSelectedSizeAvailable(): boolean {
    return this.selectedSize ? this.isSizeAvailable(this.selectedSize) : false;
  }

  isSizeAvailable(size: string): boolean {
    return this.getVariantQuantity(size) > 0;
  }
}
