import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../services/account.service';
import { map, of, switchMap } from 'rxjs';

export const authGuard: CanActivateFn = (route, state) => {
  const accountService = inject(AccountService);
  const router = inject(Router);

  if (accountService.currentUser()) {
    return of(true);
  } else {
    return accountService.getAuthState().pipe(
      switchMap(auth => {
        if (auth.isAuthenticated) {
          return accountService.getUserInfo().pipe(map(() => true));
        }
        router.navigate(['/account/login'], {queryParams: {returnUrl: state.url}});
        return of(false);
      })
    )
  }
};
