import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { getErrorMessage } from '../../../core/utils/http-error';
import { switchMap } from 'rxjs';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    RouterLink
  ],
  templateUrl: './confirm-email.component.html'
})
export class ConfirmEmailComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private accountService = inject(AccountService);
  private snack = inject(SnackbarService);
  emailLocked = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    token: ['', Validators.required]
  });

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email');
    const token = this.route.snapshot.queryParamMap.get('token');
    if (email) {
      this.form.patchValue({email});
      this.emailLocked = true;
    }
    if (token) {
      this.form.patchValue({token});
    }
  }

  confirm() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.value as {email: string; token: string};
    this.accountService.confirmEmail(payload).pipe(
      switchMap(() => this.accountService.completeLogin())
    ).subscribe({
      next: () => {
        this.snack.success('Email confirmed. You are now logged in.');
        this.router.navigateByUrl('/shop');
      },
      error: err => {
        this.snack.error(getErrorMessage(err, 'Unable to confirm email'));
      }
    });
  }

  resend() {
    const email = this.form.value.email ?? '';
    if (!email) {
      this.snack.error('Enter an email address first');
      return;
    }
    this.accountService.resendConfirmation(email).subscribe({
      next: () => this.snack.success('Confirmation email resent'),
      error: err => this.snack.error(getErrorMessage(err, 'Unable to resend confirmation'))
    });
  }
}
