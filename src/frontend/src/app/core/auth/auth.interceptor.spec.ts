import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AuthenticatedUser } from './models';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;

  const user: AuthenticatedUser = { id: '1', email: 'user@example.com' };

  function setup(authServiceStub: Partial<AuthService>) {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceStub },
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  }

  afterEach(() => {
    httpMock.verify();
  });

  it('attaches withCredentials to every request', () => {
    setup({ refreshOnce: () => of(user) });

    httpClient.get('/api/authentication/me').subscribe();

    const req = httpMock.expectOne('/api/authentication/me');
    expect(req.request.withCredentials).toBe(true);
    req.flush(user);
  });

  it('does not attempt a refresh when an exempt auth endpoint itself returns 401', () => {
    const refreshOnce = vi.fn(() => of(user));
    setup({ refreshOnce });

    httpClient.post('/api/authentication/login', {}).subscribe({ error: () => undefined });

    httpMock
      .expectOne('/api/authentication/login')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(refreshOnce).not.toHaveBeenCalled();
  });

  it('retries the original request after a successful silent refresh on 401', () => {
    setup({ refreshOnce: () => of(user) });

    let result: AuthenticatedUser | undefined;
    httpClient
      .get<AuthenticatedUser>('/api/authentication/me')
      .subscribe((value) => (result = value));

    httpMock
      .expectOne('/api/authentication/me')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    // The interceptor should retry against the same URL after refreshing.
    httpMock.expectOne('/api/authentication/me').flush(user);

    expect(result).toEqual(user);
  });

  it('propagates the original error when the refresh attempt itself fails', () => {
    setup({ refreshOnce: () => throwError(() => new Error('refresh failed')) });

    let caught: unknown;
    httpClient
      .get('/api/authentication/me')
      .subscribe({ error: (error: unknown) => (caught = error) });

    httpMock
      .expectOne('/api/authentication/me')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(caught).toBeInstanceOf(Error);
  });
});
