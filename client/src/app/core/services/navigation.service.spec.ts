import { TestBed } from '@angular/core/testing';
import { Event as RouterEvent, NavigationEnd, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { NavigationService } from './navigation.service';

describe('NavigationService', () => {
  let routerEvents$: Subject<RouterEvent>;
  let routerSpy: jasmine.SpyObj<Router>;
  let service: NavigationService;

  beforeEach(() => {
    routerEvents$ = new Subject<RouterEvent>();
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigateByUrl'], {
      events: routerEvents$.asObservable()
    });

    TestBed.configureTestingModule({
      providers: [
        NavigationService,
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(NavigationService);
  });

  it('navigates to the previous route when history exists', () => {
    routerEvents$.next(new NavigationEnd(1, '/shop', '/shop'));
    routerEvents$.next(new NavigationEnd(2, '/shop/1', '/shop/1'));

    service.back();

    expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('/shop');
  });

  it('falls back to home when there is no history', () => {
    service.back();

    expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('reports when back navigation is available', () => {
    routerEvents$.next(new NavigationEnd(1, '/', '/'));
    routerEvents$.next(new NavigationEnd(2, '/cart', '/cart'));

    expect(service.canGoBack()).toBeTrue();
  });
});
