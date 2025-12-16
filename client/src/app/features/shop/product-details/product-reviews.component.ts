import { CommonModule, DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog } from '@angular/material/dialog';
import { Review, ReviewEligibility, ReviewParams } from '../../../shared/models/review';
import { Pagination } from '../../../shared/models/pagination';
import { ReviewService } from '../../../core/services/review.service';
import { AccountService } from '../../../core/services/account.service';
import { DialogService } from '../../../core/services/dialog.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { ReviewFormDialogComponent } from '../../../shared/components/reviews/review-form-dialog/review-form-dialog.component';
import { StarRatingComponent } from '../../../shared/components/rating/star-rating.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-product-reviews',
  standalone: true,
  imports: [
    CommonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatDividerModule,
    StarRatingComponent,
    EmptyStateComponent,
    DatePipe
  ],
  templateUrl: './product-reviews.component.html',
  styleUrl: './product-reviews.component.scss'
})
export class ProductReviewsComponent implements OnChanges {
  @Input() productId!: number;
  @Input() productName = '';
  @Input() ratingAverage = 0;
  @Input() ratingCount = 0;
  @Output() ratingChanged = new EventEmitter<void>();

  private reviewService = inject(ReviewService);
  private accountService = inject(AccountService);
  private dialogService = inject(DialogService);
  private snackbar = inject(SnackbarService);
  private dialog = inject(MatDialog);

  reviews?: Pagination<Review>;
  eligibility?: ReviewEligibility;
  userReview?: Review;
  reviewParams = new ReviewParams();
  loading = false;
  sortOptions = [
    { label: 'Newest first', value: 'newest' },
    { label: 'Oldest first', value: 'oldest' },
    { label: 'Rating: High-Low', value: 'ratingDesc' },
    { label: 'Rating: Low-High', value: 'ratingAsc' }
  ];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['productId'] && this.productId) {
      this.reviewParams.pageNumber = 1;
      this.loadReviews();
      this.loadUserContext();
    }
  }

  get totalReviews() {
    return this.reviews?.count ?? this.ratingCount ?? 0;
  }

  get canAddReview() {
    return this.isLoggedIn && this.eligibility?.canReview;
  }

  get hasUserReview() {
    return !!this.userReview;
  }

  get isLoggedIn() {
    return !!this.accountService.currentUser();
  }

  loadReviews() {
    if (!this.productId) return;
    this.loading = true;
    this.reviewService.getProductReviews(this.productId, this.reviewParams).subscribe({
      next: (response) => {
        this.reviews = response;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackbar.error('Unable to load reviews');
      }
    });
  }

  loadUserContext() {
    if (!this.accountService.currentUser()) {
      this.eligibility = { canReview: false, alreadyReviewed: false };
      this.userReview = undefined;
      return;
    }

    this.reviewService.getEligibility(this.productId).subscribe({
      next: (eligibility) => (this.eligibility = eligibility),
      error: () => (this.eligibility = { canReview: false, alreadyReviewed: false })
    });

    this.reviewService.getMyReview(this.productId).subscribe({
      next: (review) => (this.userReview = review || undefined),
      error: () => (this.userReview = undefined)
    });
  }

  onSortChange(sort: ReviewParams['sort']) {
    this.reviewParams.sort = sort;
    this.reviewParams.pageNumber = 1;
    this.loadReviews();
  }

  onPageChange(event: PageEvent) {
    this.reviewParams.pageNumber = event.pageIndex + 1;
    this.reviewParams.pageSize = event.pageSize;
    this.loadReviews();
  }

  openReviewDialog(review?: Review) {
    const dialogRef = this.dialog.open(ReviewFormDialogComponent, {
      width: '520px',
      data: {
        productId: this.productId,
        productName: this.productName,
        review
      }
    });

    dialogRef.afterClosed().subscribe({
      next: (result?: Review) => {
        if (result) {
          this.userReview = result;
          this.eligibility = { canReview: false, alreadyReviewed: true };
          this.reviewParams.pageNumber = 1;
          this.loadReviews();
          this.ratingChanged.emit();
        }
      }
    });
  }

  async deleteReview(review: Review) {
    const confirmed = await this.dialogService.confirm(
      'Delete review',
      'Are you sure you want to remove this review?'
    );
    if (!confirmed) return;

    this.reviewService.deleteReview(review.productId, review.id).subscribe({
      next: () => {
        this.snackbar.success('Review deleted');
        if (this.userReview?.id === review.id) {
          this.userReview = undefined;
          this.eligibility = { canReview: true, alreadyReviewed: false };
        }
        this.reviewParams.pageNumber = 1;
        this.loadReviews();
        this.ratingChanged.emit();
      },
      error: () => this.snackbar.error('Unable to delete review')
    });
  }
}
