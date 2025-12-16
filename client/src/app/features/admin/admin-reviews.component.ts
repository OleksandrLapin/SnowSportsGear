import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { ReviewAdmin, AdminReviewParams } from '../../shared/models/review';
import { Pagination } from '../../shared/models/pagination';
import { ReviewService } from '../../core/services/review.service';
import { DialogService } from '../../core/services/dialog.service';
import { SnackbarService } from '../../core/services/snackbar.service';
import { AdminService } from '../../core/services/admin.service';
import { StarRatingComponent } from '../../shared/components/rating/star-rating.component';
import { AdminReviewEditDialogComponent } from './admin-review-edit-dialog.component';
import { AdminReviewReplyDialogComponent } from './admin-review-reply-dialog.component';
import { AdminReviewAuditDialogComponent } from './admin-review-audit-dialog.component';

@Component({
  selector: 'app-admin-reviews',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatCardModule,
    MatDividerModule,
    MatDialogModule,
    MatTooltipModule,
    StarRatingComponent,
    DatePipe
  ],
  templateUrl: './admin-reviews.component.html',
  styleUrl: './admin-reviews.component.scss'
})
export class AdminReviewsComponent implements OnInit {
  private reviewService = inject(ReviewService);
  private dialogService = inject(DialogService);
  private snackbar = inject(SnackbarService);
  private fb = inject(FormBuilder);
  private adminService = inject(AdminService);
  private dialog = inject(MatDialog);

  reviews?: Pagination<ReviewAdmin>;
  params = new AdminReviewParams();
  loading = false;
  brands: string[] = [];
  types: string[] = [];
  statusOptions = ['all', 'published', 'hidden'];
  sortOptions = [
    { label: 'Newest', value: 'newest' },
    { label: 'Oldest', value: 'oldest' },
    { label: 'Rating: High-Low', value: 'ratingDesc' },
    { label: 'Rating: Low-High', value: 'ratingAsc' }
  ];

  filters = this.fb.group({
    search: [''],
    status: ['all'],
    brand: [''],
    type: [''],
    productId: [''],
    minRating: [''],
    maxRating: [''],
    userEmail: [''],
    from: [''],
    to: ['']
  });

  ngOnInit(): void {
    this.loadFilters();
    this.loadReviews();
  }

  loadFilters() {
    this.adminService.getBrands()?.subscribe({
      next: brands => this.brands = brands
    });
    this.adminService.getTypes()?.subscribe({
      next: types => this.types = types
    });
  }

  loadReviews() {
    this.loading = true;
    this.reviewService.getAdminReviews(this.params).subscribe({
      next: response => {
        this.reviews = response;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.snackbar.error('Unable to load reviews');
      }
    });
  }

  applyFilters() {
    const f = this.filters.value;
    this.params.pageNumber = 1;
    this.params.search = f.search ?? '';
    this.params.status = f.status ?? 'all';
    this.params.brand = f.brand ?? '';
    this.params.type = f.type ?? '';
    this.params.productId = f.productId ? Number(f.productId) : undefined;
    this.params.minRating = f.minRating ? Number(f.minRating) : undefined;
    this.params.maxRating = f.maxRating ? Number(f.maxRating) : undefined;
    this.params.userEmail = f.userEmail ?? '';
    this.params.from = f.from ?? '';
    this.params.to = f.to ?? '';
    this.loadReviews();
  }

  resetFilters() {
    this.filters.reset({
      search: '',
      status: 'all',
      brand: '',
      type: '',
      productId: '',
      minRating: '',
      maxRating: '',
      userEmail: '',
      from: '',
      to: ''
    });
    this.params = new AdminReviewParams();
    this.loadReviews();
  }

  onSortChange(sort: AdminReviewParams['sort']) {
    this.params.sort = sort;
    this.params.pageNumber = 1;
    this.loadReviews();
  }

  onPageChange(event: PageEvent) {
    this.params.pageNumber = event.pageIndex + 1;
    this.params.pageSize = event.pageSize;
    this.loadReviews();
  }

  async deleteReview(review: ReviewAdmin) {
    const confirmed = await this.dialogService.confirm(
      'Delete review',
      `Delete review from ${review.author}? This cannot be undone.`
    );
    if (!confirmed) return;

    this.reviewService.deleteAdminReview(review.id).subscribe({
      next: () => {
        this.snackbar.success('Review deleted');
        this.loadReviews();
      },
      error: () => this.snackbar.error('Unable to delete review')
    });
  }

  toggleVisibility(review: ReviewAdmin) {
    this.reviewService.updateVisibility(review.id, { isHidden: !review.isHidden }).subscribe({
      next: updated => this.replaceReview(updated),
      error: () => this.snackbar.error('Unable to update visibility')
    });
  }

  openEdit(review: ReviewAdmin) {
    const dialogRef = this.dialog.open(AdminReviewEditDialogComponent, {
      width: '560px',
      data: { review }
    });

    dialogRef.afterClosed().subscribe({
      next: (updated?: ReviewAdmin) => {
        if (updated) {
          this.replaceReview(updated);
        }
      }
    });
  }

  openReply(review: ReviewAdmin) {
    const dialogRef = this.dialog.open(AdminReviewReplyDialogComponent, {
      width: '560px',
      data: { review }
    });

    dialogRef.afterClosed().subscribe({
      next: (updated?: ReviewAdmin) => {
        if (updated) {
          this.replaceReview(updated);
        }
      }
    });
  }

  openAudit(review: ReviewAdmin) {
    this.dialog.open(AdminReviewAuditDialogComponent, {
      width: '600px',
      data: { reviewId: review.id, productName: review.productName }
    });
  }

  private replaceReview(updated: ReviewAdmin) {
    if (!this.reviews?.data) return;
    this.reviews.data = this.reviews.data.map(r => r.id === updated.id ? updated : r);
    this.snackbar.success('Review updated');
  }
}
