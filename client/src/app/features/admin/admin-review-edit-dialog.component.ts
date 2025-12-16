import { CommonModule } from '@angular/common';
import { Component, Inject, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { ReviewAdmin } from '../../shared/models/review';
import { ReviewService } from '../../core/services/review.service';
import { SnackbarService } from '../../core/services/snackbar.service';
import { StarRatingComponent } from '../../shared/components/rating/star-rating.component';

interface AdminReviewDialogData {
  review: ReviewAdmin;
}

@Component({
  selector: 'app-admin-review-edit-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSlideToggleModule,
    ReactiveFormsModule,
    StarRatingComponent
  ],
  templateUrl: './admin-review-edit-dialog.component.html',
  styleUrl: './admin-review-edit-dialog.component.scss'
})
export class AdminReviewEditDialogComponent {
  dialogRef = inject(MatDialogRef<AdminReviewEditDialogComponent, ReviewAdmin>);
  private fb = inject(FormBuilder);
  private reviewService = inject(ReviewService);
  private snackbar = inject(SnackbarService);

  constructor(@Inject(MAT_DIALOG_DATA) public data: AdminReviewDialogData) {}

  submitting = false;

  form = this.fb.group({
    rating: [this.data.review.rating, [Validators.required, Validators.min(1), Validators.max(5)]],
    title: [this.data.review.title ?? '', [Validators.maxLength(150)]],
    content: [this.data.review.content ?? '', [Validators.maxLength(1000)]],
    isHidden: [this.data.review.isHidden]
  });

  setRating(value: number) {
    this.form.get('rating')?.setValue(value);
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;

    this.reviewService.updateAdminReview(this.data.review.id, {
      rating: this.form.value.rating ?? this.data.review.rating,
      title: this.form.value.title ?? null,
      content: this.form.value.content ?? null,
      isHidden: this.form.value.isHidden ?? false
    }).subscribe({
      next: updated => {
        this.snackbar.success('Review updated');
        this.dialogRef.close(updated);
      },
      error: err => {
        this.submitting = false;
        this.snackbar.error(err?.error || 'Unable to update review');
      }
    });
  }
}

