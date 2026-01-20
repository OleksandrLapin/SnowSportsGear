import { Component, inject, OnInit } from '@angular/core';
import { OrderService } from '../../core/services/order.service';
import { OrderSummary } from '../../shared/models/orderSummary';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ORDER_STATUS_BADGE_CLASSES, ORDER_STATUS_LABELS } from '../../shared/utils/order-status';

@Component({
  selector: 'app-order',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    CurrencyPipe
  ],
  templateUrl: './order.component.html',
  styleUrl: './order.component.scss'
})
export class OrderComponent implements OnInit {
  private orderService = inject(OrderService);
  orders: OrderSummary[] = [];
  statusLabels = ORDER_STATUS_LABELS;
  statusClasses = ORDER_STATUS_BADGE_CLASSES;

  ngOnInit(): void {
    this.orderService.getOrdersForUser().subscribe({
      next: orders => this.orders = orders
    })
  }
}
