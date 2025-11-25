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
    MatLabel
  ],
  templateUrl: './product-item.component.html',
  styleUrl: './product-item.component.scss'
})
export class ProductItemComponent {
  @Input() product?: Product;
  cartService = inject(CartService);
  sizes = ['XS', 'S', 'M', 'L', 'XL'];
  selectedSize: string | null = null;

  addToCart(event: Event) {
    event.stopPropagation();
    if (!this.product || !this.selectedSize) return;
    if (this.product.quantityInStock <= 0) return;
    this.cartService.addItemToCart(this.product, 1, this.selectedSize);
  }
}
