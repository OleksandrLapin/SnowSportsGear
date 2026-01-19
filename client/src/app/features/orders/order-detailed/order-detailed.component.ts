import { Component, inject, OnInit } from '@angular/core';
import { OrderService } from '../../../core/services/order.service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Order } from '../../../shared/models/order';
import { MatCardModule } from '@angular/material/card';
import { MatButton } from '@angular/material/button';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { AddressPipe } from "../../../shared/pipes/address.pipe";
import { PaymentCardPipe } from "../../../shared/pipes/payment-card.pipe";
import { AccountService } from '../../../core/services/account.service';
import { AdminService } from '../../../core/services/admin.service';
import { ReviewService } from '../../../core/services/review.service';
import { MatDialog } from '@angular/material/dialog';
import { MatDialogModule } from '@angular/material/dialog';
import { ReviewFormDialogComponent } from '../../../shared/components/reviews/review-form-dialog/review-form-dialog.component';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { StarRatingComponent } from '../../../shared/components/rating/star-rating.component';
import { Review } from '../../../shared/models/review';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-order-detailed',
  standalone: true,
  imports: [
    MatCardModule,
    MatButton,
    DatePipe,
    CurrencyPipe,
    AddressPipe,
    PaymentCardPipe,
    RouterLink,
    MatDialogModule,
    StarRatingComponent,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule
],
  templateUrl: './order-detailed.component.html',
  styleUrl: './order-detailed.component.scss'
})
export class OrderDetailedComponent implements OnInit {
  private orderService = inject(OrderService);
  private activatedRoute = inject(ActivatedRoute);
  private accountService = inject(AccountService);
  private adminService = inject(AdminService);
  private router = inject(Router);
  private reviewService = inject(ReviewService);
  private dialog = inject(MatDialog);
  private snackbar = inject(SnackbarService);
  private fb = inject(FormBuilder);
  order?: Order;
  buttonText = this.accountService.isAdmin() ? 'Return to admin' : 'Return to orders';
  adminStatusOptions = [
    'Pending',
    'PaymentReceived',
    'PaymentFailed',
    'PaymentMismatch',
    'Processing',
    'Packed',
    'Shipped',
    'Delivered',
    'Cancelled',
    'Refunded'
  ];

  statusForm = this.fb.group({
    status: ['', Validators.required],
    cancelReason: [''],
    trackingNumber: [''],
    trackingUrl: [''],
    deliveryDetails: ['']
  });

  cancelForm = this.fb.group({
    reason: ['']
  });

  ngOnInit(): void {
    this.loadOrder();
  }

  onReturnClick() {
    this.accountService.isAdmin() 
      ? this.router.navigateByUrl('/admin')
      : this.router.navigateByUrl('/orders')
  }

  loadOrder() {
    const id = this.activatedRoute.snapshot.paramMap.get('id');
    if (!id) return;

    const loadOrderData = this.accountService.isAdmin()
      ? this.adminService.getOrder(+id)
      : this.orderService.getOrderDetailed(+id);

    loadOrderData.subscribe({
      next: order => {
        this.order = order;
        if (this.accountService.isAdmin()) {
          this.statusForm.patchValue({
            status: order.status,
            cancelReason: order.cancelledReason ?? '',
            trackingNumber: order.trackingNumber ?? '',
            trackingUrl: order.trackingUrl ?? '',
            deliveryDetails: order.deliveryUpdateDetails ?? ''
          }, { emitEvent: false });
        }
      },
      error: () => this.order = undefined
    })
  }

  updateStatus() {
    if (!this.order) return;
    if (this.statusForm.invalid) {
      this.statusForm.markAllAsTouched();
      return;
    }

    const raw = this.statusForm.value;
    const payload = {
      status: raw.status ?? '',
      cancelReason: raw.cancelReason?.toString().trim() || undefined,
      trackingNumber: raw.trackingNumber?.toString().trim() || undefined,
      trackingUrl: raw.trackingUrl?.toString().trim() || undefined,
      deliveryDetails: raw.deliveryDetails?.toString().trim() || undefined
    };

    this.adminService.updateOrderStatus(this.order.id, payload).subscribe({
      next: () => {
        this.snackbar.success('Order status updated');
        this.loadOrder();
      },
      error: err => this.snackbar.error(err?.error || 'Unable to update order status')
    });
  }

  cancelOrder() {
    if (!this.order) return;
    const reason = this.cancelForm.value.reason?.toString().trim() || undefined;
    this.orderService.cancelOrder(this.order.id, reason).subscribe({
      next: order => {
        this.order = order;
        this.snackbar.success('Order cancelled');
      },
      error: err => this.snackbar.error(err?.error || 'Unable to cancel order')
    });
  }

  get canCancelOrder(): boolean {
    if (!this.order || this.accountService.isAdmin()) return false;
    return !['Cancelled', 'Shipped', 'Delivered', 'Refunded'].includes(this.order.status);
  }

  get isAdmin(): boolean {
    return this.accountService.isAdmin();
  }

  openReviewDialog(item: Order['orderItems'][number]) {
    const existing$ = item.reviewId
      ? this.reviewService.getMyReview(item.productId)
      : null;

    if (existing$) {
      existing$.subscribe({
        next: review => this.launchReviewDialog(item, review),
        error: () => this.launchReviewDialog(item)
      });
    } else {
      this.launchReviewDialog(item);
    }
  }

  private launchReviewDialog(item: Order['orderItems'][number], review?: Review) {
    const dialogRef = this.dialog.open(ReviewFormDialogComponent, {
      width: '520px',
      data: {
        productId: item.productId,
        productName: item.productName,
        orderId: this.order?.id,
        review
      }
    });

    dialogRef.afterClosed().subscribe({
      next: (result?: Review) => {
        if (result) {
          this.snackbar.success(review ? 'Review updated' : 'Review added');
          this.loadOrder();
        }
      }
    });
  }
}
