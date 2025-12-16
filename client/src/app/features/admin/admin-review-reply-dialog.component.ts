import { CommonModule } from '@angular/common';
import { Component, Inject, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ReviewAdmin } from '../../shared/models/review';
import { ReviewService } from '../../core/services/review.service';
import { SnackbarService } from '../../core/services/snackbar.service';

interface ReplyDialogData {
  review: ReviewAdmin;
}

@Component({
  selector: 'app-admin-review-reply-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    ReactiveFormsModule
  ],
  templateUrl: './admin-review-reply-dialog.component.html',
  styleUrl: './admin-review-reply-dialog.component.scss'
})
export class AdminReviewReplyDialogComponent {
  dialogRef = inject(MatDialogRef<AdminReviewReplyDialogComponent, ReviewAdmin>);
  private fb = inject(FormBuilder);
  private reviewService = inject(ReviewService);
  private snackbar = inject(SnackbarService);

  constructor(@Inject(MAT_DIALOG_DATA) public data: ReplyDialogData) {}

  submitting = false;

  form = this.fb.group({
    response: [this.data.review.adminResponse ?? '', [Validators.maxLength(1000)]]
  });

  submit() {
    this.submitting = true;
    this.reviewService.replyToReview(this.data.review.id, {
      response: this.form.value.response ?? null
    }).subscribe({
      next: updated => {
        this.snackbar.success('Reply saved');
        this.dialogRef.close(updated);
      },
      error: err => {
        this.submitting = false;
        this.snackbar.error(err?.error || 'Unable to save reply');
      }
    });
  }
}

