import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AccountService } from '../../../core/services/account.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { CommonModule } from '@angular/common';
import { MatDividerModule } from '@angular/material/divider';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDividerModule,
    MatSlideToggleModule
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private accountService = inject(AccountService);
  private snackbar = inject(SnackbarService);

  profileForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
  });

  passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
  });

  emailForm = this.fb.group({
    newEmail: ['', [Validators.required, Validators.email]]
  });

  twoFactorEnabled = false;

  ngOnInit(): void {
    const user = this.accountService.currentUser();
    if (user) {
      this.profileForm.patchValue({
        firstName: user.firstName,
        lastName: user.lastName
      });
      this.twoFactorEnabled = !!user.twoFactorEnabled;
    } else {
      this.accountService.getUserInfo().subscribe({
        next: u => {
          this.profileForm.patchValue({ firstName: u.firstName, lastName: u.lastName });
          this.twoFactorEnabled = !!u.twoFactorEnabled;
        }
      });
    }
  }

  saveProfile() {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    this.accountService.updateProfile(this.profileForm.value as any).subscribe({
      next: () => this.snackbar.success('Profile updated'),
      error: () => this.snackbar.error('Unable to update profile')
    });
  }

  changePassword() {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    this.accountService.changePassword(this.passwordForm.value as any).subscribe({
      next: () => {
        this.snackbar.success('Password changed');
        this.passwordForm.reset();
      },
      error: (err) => {
        const message = err?.error || 'Unable to change password';
        this.snackbar.error(message);
      }
    });
  }

  requestPasswordReset() {
    this.accountService.requestPasswordReset().subscribe({
      next: () => this.snackbar.success('Password reset email sent'),
      error: err => this.snackbar.error(err?.error || 'Unable to send reset email')
    });
  }

  requestEmailChange() {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }

    const newEmail = this.emailForm.value.newEmail ?? '';
    this.accountService.requestEmailChange(newEmail).subscribe({
      next: () => {
        this.snackbar.success('Email change confirmation sent');
        this.emailForm.reset();
      },
      error: err => this.snackbar.error(err?.error || 'Unable to request email change')
    });
  }

  toggleTwoFactor(event: {checked: boolean}) {
    const enabled = event.checked;
    this.accountService.toggleTwoFactor(enabled).subscribe({
      next: () => {
        this.twoFactorEnabled = enabled;
        this.snackbar.success(enabled ? 'Two-factor enabled' : 'Two-factor disabled');
      },
      error: err => {
        this.snackbar.error(err?.error || 'Unable to update two-factor settings');
      }
    });
  }

  requestDataExport() {
    this.accountService.requestDataExport().subscribe({
      next: () => this.snackbar.success('Data export request sent'),
      error: err => this.snackbar.error(err?.error || 'Unable to request data export')
    });
  }

  requestDeletion() {
    this.accountService.requestAccountDeletion().subscribe({
      next: () => this.snackbar.success('Deletion request sent'),
      error: err => this.snackbar.error(err?.error || 'Unable to request deletion')
    });
  }
}
