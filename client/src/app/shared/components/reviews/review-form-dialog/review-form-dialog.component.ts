import { Component, Inject, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';
import { Review, ReviewPayload } from '../../../../shared/models/review';
import { ReviewService } from '../../../../core/services/review.service';
import { SnackbarService } from '../../../../core/services/snackbar.service';
import { StarRatingComponent } from '../../rating/star-rating.component';

export interface ReviewDialogData {
  productId: number;
  productName: string;
  review?: Review;
  orderId?: number;
}

@Component({
  selector: 'app-review-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    ReactiveFormsModule,
    StarRatingComponent
  ],
  templateUrl: './review-form-dialog.component.html',
  styleUrl: './review-form-dialog.component.scss'
})
export class ReviewFormDialogComponent {
  dialogRef = inject(MatDialogRef<ReviewFormDialogComponent, Review>);
  private fb = inject(FormBuilder);
  private reviewService = inject(ReviewService);
  private snackbar = inject(SnackbarService);

  constructor(@Inject(MAT_DIALOG_DATA) public data: ReviewDialogData) {}

  submitting = false;

  form = this.fb.group({
    rating: [this.data.review?.rating ?? 0, [Validators.required, Validators.min(1), Validators.max(5)]],
    title: [this.data.review?.title ?? '', [Validators.maxLength(150)]],
    content: [this.data.review?.content ?? '', [Validators.maxLength(1000)]]
  });

  setRating(value: number) {
    this.form.get('rating')?.setValue(value);
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: ReviewPayload = {
      rating: this.form.value.rating ?? 0,
      title: this.form.value.title ?? null,
      content: this.form.value.content ?? null,
      orderId: this.data.orderId ?? null
    };

    const request$ = this.data.review
      ? this.reviewService.updateReview(this.data.productId, this.data.review.id, payload)
      : this.reviewService.createReview(this.data.productId, payload);

    this.submitting = true;

    request$.subscribe({
      next: (review) => {
        this.snackbar.success(this.data.review ? 'Review updated' : 'Review added');
        this.dialogRef.close(review);
      },
      error: (err) => {
        this.submitting = false;
        this.snackbar.error(err?.error || 'Unable to save review');
      }
    });
  }
}

