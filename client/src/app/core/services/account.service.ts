import { computed, inject, Injectable, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Address, User } from '../../shared/models/user';
import { map, switchMap, tap } from 'rxjs';
import { SignalrService } from './signalr.service';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);
  private signalrService = inject(SignalrService);
  currentUser = signal<User | null>(null);
  isAdmin = computed(() => {
    const roles = this.currentUser()?.roles;
    if (this.currentUser()?.isAdmin) return true;
    return Array.isArray(roles) ? roles.includes('Admin') : roles === 'Admin';
  });

  login(values: {email: string; password: string}) {
    return this.http.post<LoginResult>(this.baseUrl + 'account/login', values);
  }

  verifyTwoFactor(payload: {email: string; code: string}) {
    return this.http.post<LoginResult>(this.baseUrl + 'account/verify-2fa', payload);
  }

  completeLogin() {
    this.signalrService.createHubConnection();
    return this.getUserInfo();
  }

  register(values: any) {
    return this.http.post<LoginResult>(this.baseUrl + 'account/register', values);
  }

  getUserInfo() {
    return this.http.get<User>(this.baseUrl + 'account/user-info').pipe(
      map(user => {
        this.currentUser.set(user);
        return user;
      })
    )
  }

  logout() {
    return this.http.post(this.baseUrl + 'account/logout', {}).pipe(
      tap(() => {
        this.signalrService.stopHubConnection();
        this.currentUser.set(null);
      })
    )
  }

  updateAddress(address: Address) {
    return this.http.post(this.baseUrl + 'account/address', address).pipe(
      tap(() => {
        this.currentUser.update(user => {
          if (user) user.address = address;
          return user;
        })
      })
    )
  }

  getAuthState() {
    return this.http.get<{isAuthenticated: boolean}>(this.baseUrl + 'account/auth-status');
  }

  updateProfile(payload: {firstName: string; lastName: string}) {
    return this.http.post<User>(this.baseUrl + 'account/profile', payload).pipe(
      tap(user => this.currentUser.set(user))
    );
  }

  changePassword(payload: {currentPassword: string; newPassword: string}) {
    return this.http.post(this.baseUrl + 'account/change-password', payload);
  }

  resendConfirmation(email: string) {
    return this.http.post(this.baseUrl + 'account/resend-confirmation', {email});
  }

  confirmEmail(payload: {email: string; token: string}) {
    return this.http.post(this.baseUrl + 'account/confirm-email', payload);
  }

  forgotPassword(email: string) {
    return this.http.post(this.baseUrl + 'account/forgot-password', {email});
  }

  resetPassword(payload: {email: string; token: string; newPassword: string}) {
    return this.http.post(this.baseUrl + 'account/password-reset', payload);
  }

  requestPasswordReset() {
    return this.http.post(this.baseUrl + 'account/request-password-reset', {});
  }

  requestEmailChange(newEmail: string) {
    return this.http.post(this.baseUrl + 'account/request-email-change', {newEmail});
  }

  confirmEmailChange(payload: {userId: string; newEmail: string; token: string}) {
    return this.http.post(this.baseUrl + 'account/confirm-email-change', payload);
  }

  toggleTwoFactor(enabled: boolean) {
    return this.http.post<{twoFactorEnabled: boolean}>(this.baseUrl + 'account/toggle-2fa?enabled=' + enabled, {});
  }

  requestAccountDeletion() {
    return this.http.post(this.baseUrl + 'account/request-deletion', {});
  }

  requestDataExport() {
    return this.http.post(this.baseUrl + 'account/request-data-export', {});
  }
}

export type LoginResult = {
  success: boolean;
  requiresTwoFactor: boolean;
  requiresEmailConfirmation: boolean;
  message?: string;
};
