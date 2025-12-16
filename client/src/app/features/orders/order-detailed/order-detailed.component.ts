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
    StarRatingComponent
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
  order?: Order;
  buttonText = this.accountService.isAdmin() ? 'Return to admin' : 'Return to orders'

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
      next: order => this.order = order,
      error: () => this.order = undefined
    })
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
