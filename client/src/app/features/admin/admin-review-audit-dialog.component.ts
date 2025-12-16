import { CommonModule, DatePipe } from '@angular/common';
import { Component, Inject, OnInit, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';
import { ReviewAudit } from '../../shared/models/review';
import { ReviewService } from '../../core/services/review.service';
import { MatIconModule } from '@angular/material/icon';

interface AuditDialogData {
  reviewId: number;
  productName: string;
}

@Component({
  selector: 'app-admin-review-audit-dialog',
  standalone: true,
  imports: [CommonModule, DatePipe, MatDialogModule, MatListModule, MatIconModule],
  templateUrl: './admin-review-audit-dialog.component.html',
  styleUrl: './admin-review-audit-dialog.component.scss'
})
export class AdminReviewAuditDialogComponent implements OnInit {
  audits: ReviewAudit[] = [];
  loading = true;
  private reviewService = inject(ReviewService);

  constructor(@Inject(MAT_DIALOG_DATA) public data: AuditDialogData) {}

  ngOnInit(): void {
    this.reviewService.getAuditTrail(this.data.reviewId).subscribe({
      next: audits => {
        this.audits = audits;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  actionLabel(action: number) {
    switch (action) {
      case 0: return 'Created';
      case 1: return 'Updated';
      case 2: return 'Deleted';
      case 3: return 'Hidden';
      case 4: return 'Published';
      case 5: return 'Responded';
      default: return 'Action';
    }
  }
}
