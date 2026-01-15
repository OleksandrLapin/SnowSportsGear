import { Injectable, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class NavigationService {
  private router = inject(Router);
  private history: string[] = [];

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => {
        const url = event.urlAfterRedirects;
        const last = this.history[this.history.length - 1];
        if (last !== url) {
          this.history.push(url);
        }
      });
  }

  back(): void {
    if (this.history.length > 1) {
      this.history.pop();
      const target = this.history[this.history.length - 1];
      if (target) {
        this.router.navigateByUrl(target);
        return;
      }
    }

    this.router.navigateByUrl('/');
  }

  canGoBack(): boolean {
    return this.history.length > 1;
  }
}
