import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { AccountService } from '../../../core/services/account.service';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-confirm-email-change',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, RouterLink],
  templateUrl: './confirm-email-change.component.html'
})
export class ConfirmEmailChangeComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private accountService = inject(AccountService);
  private snack = inject(SnackbarService);

  processing = true;

  ngOnInit(): void {
    const userId = this.route.snapshot.queryParamMap.get('userId');
    const newEmail = this.route.snapshot.queryParamMap.get('newEmail');
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!userId || !newEmail || !token) {
      this.processing = false;
      this.snack.error('Invalid confirmation link');
      return;
    }

    this.accountService.confirmEmailChange({userId, newEmail, token}).subscribe({
      next: () => {
        this.processing = false;
        this.snack.success('Email updated');
      },
      error: err => {
        this.processing = false;
        this.snack.error(err?.error || 'Unable to confirm email change');
      }
    });
  }
}
