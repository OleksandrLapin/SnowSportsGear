import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatCard } from '@angular/material/card';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { AccountService } from '../../../core/services/account.service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { getErrorMessage } from '../../../core/utils/http-error';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCard,
    MatFormField,
    MatInput,
    MatLabel,
    MatButton,
    RouterLink
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private snack = inject(SnackbarService);
  returnUrl = '/shop';
  step: 'login' | 'twoFactor' = 'login';
  pendingEmail = '';

  constructor() {
    const url = this.activatedRoute.snapshot.queryParams['returnUrl'];
    if (url) this.returnUrl = url;
  }

  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  twoFactorForm = this.fb.group({
    code: ['', Validators.required]
  });

  onSubmit() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    const payload = this.loginForm.value as {email: string; password: string};
    this.accountService.login(payload).subscribe({
      next: result => {
        if (result.requiresTwoFactor) {
          this.pendingEmail = payload.email;
          this.step = 'twoFactor';
          this.snack.success('Verification code sent to your email');
          return;
        }

        if (result.success) {
          this.accountService.completeLogin().subscribe(() => {
            this.router.navigateByUrl(this.returnUrl);
          });
        }
      },
      error: err => {
        const needsConfirmation = err?.status === 403 && err?.error?.requiresEmailConfirmation;
        if (needsConfirmation) {
          this.pendingEmail = payload.email;
          this.snack.success('Confirmation code sent. Enter it to continue.');
          this.router.navigate(['/account/confirm-email'], {queryParams: {email: payload.email}});
          return;
        }
        this.snack.error(getErrorMessage(err, 'Login failed'));
      }
    });
  }

  verifyCode() {
    if (this.twoFactorForm.invalid || !this.pendingEmail) {
      this.twoFactorForm.markAllAsTouched();
      return;
    }

    const payload = {email: this.pendingEmail, code: this.twoFactorForm.value.code ?? ''};
    this.accountService.verifyTwoFactor(payload).subscribe({
      next: result => {
        if (!result.success) {
          this.snack.error(result.message || 'Invalid code');
          return;
        }

        this.accountService.completeLogin().subscribe(() => {
          this.router.navigateByUrl(this.returnUrl);
        });
      },
      error: err => {
        this.snack.error(getErrorMessage(err, 'Invalid code'));
      }
    });
  }

  resendConfirmation() {
    if (!this.pendingEmail) return;
    this.accountService.resendConfirmation(this.pendingEmail).subscribe({
      next: () => this.snack.success('Confirmation email resent'),
      error: err => this.snack.error(getErrorMessage(err, 'Unable to resend confirmation'))
    });
  }

  backToLogin() {
    this.step = 'login';
  }

}
