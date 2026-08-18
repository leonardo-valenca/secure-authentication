import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AuthenticatedUser, LoginResponse } from './models';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const user: AuthenticatedUser = {
    id: '11111111-1111-1111-1111-111111111111',
    email: 'user@example.com',
  };

  const completedLogin: LoginResponse = { requiresTwoFactor: false, user };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('starts unauthenticated', () => {
    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('login sets the current user when the response completes the login', () => {
    service.login('user@example.com', 'StrongPass1').subscribe();

    const req = httpMock.expectOne('/api/authentication/login');
    expect(req.request.method).toBe('POST');
    req.flush(completedLogin);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()).toEqual(user);
  });

  it('login does not set the current user when the response requires a second factor', () => {
    service.login('user@example.com', 'StrongPass1').subscribe();

    httpMock
      .expectOne('/api/authentication/login')
      .flush({ requiresTwoFactor: true, user: null } satisfies LoginResponse);

    expect(service.isAuthenticated()).toBe(false);
  });

  it('completeTwoFactorLogin sets the current user on success', () => {
    service.completeTwoFactorLogin('123456').subscribe();

    const req = httpMock.expectOne('/api/authentication/2fa/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ code: '123456' });
    req.flush(user);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()).toEqual(user);
  });

  it('register does not change authentication state', () => {
    service.register('user@example.com', 'StrongPass1').subscribe();

    httpMock.expectOne('/api/authentication/register').flush(user);

    expect(service.isAuthenticated()).toBe(false);
  });

  it('bootstrap sets the current user when /me succeeds', () => {
    service.bootstrap().subscribe();

    httpMock.expectOne('/api/authentication/me').flush(user);

    expect(service.currentUser()).toEqual(user);
  });

  it('bootstrap clears user state and resolves to null instead of throwing when /me fails', () => {
    let resolvedValue: AuthenticatedUser | null | undefined;
    service.bootstrap().subscribe((value) => (resolvedValue = value));

    httpMock
      .expectOne('/api/authentication/me')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(resolvedValue).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('logout clears the current user', () => {
    service.login('user@example.com', 'StrongPass1').subscribe();
    httpMock.expectOne('/api/authentication/login').flush(completedLogin);
    expect(service.isAuthenticated()).toBe(true);

    service.logout().subscribe();
    httpMock.expectOne('/api/authentication/logout').flush(null);

    expect(service.isAuthenticated()).toBe(false);
  });

  it('refreshOnce coalesces concurrent calls into a single HTTP request', () => {
    // Regression test: two requests racing a 401 at the same time used to each call refresh()
    // independently, and the loser would present an already-rotated refresh token, which looks
    // identical to a stolen-token replay and revokes the user's whole session.
    let firstResult: AuthenticatedUser | undefined;
    let secondResult: AuthenticatedUser | undefined;

    service.refreshOnce().subscribe((value) => (firstResult = value));
    service.refreshOnce().subscribe((value) => (secondResult = value));

    // expectOne throws if more than one matching request was made, that failure IS the
    // regression check: it means refreshOnce() didn't actually coalesce the two calls.
    httpMock.expectOne('/api/authentication/refresh').flush(user);

    expect(firstResult).toEqual(user);
    expect(secondResult).toEqual(user);
  });

  it('refreshOnce issues a fresh request after the previous one has settled', () => {
    service.refreshOnce().subscribe();
    httpMock.expectOne('/api/authentication/refresh').flush(user);

    // A second, later call is a new refresh attempt, not a replay of the cached first result.
    service.refreshOnce().subscribe();
    httpMock.expectOne('/api/authentication/refresh').flush(user);
  });

  it('forgotPassword posts the email and does not change authentication state', () => {
    service.forgotPassword('user@example.com').subscribe();

    const req = httpMock.expectOne('/api/authentication/forgot-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@example.com' });
    req.flush(null);

    expect(service.isAuthenticated()).toBe(false);
  });

  it('resetPassword posts the email, token, and new password', () => {
    service.resetPassword('user@example.com', 'reset-token', 'NewStrongPass1').subscribe();

    const req = httpMock.expectOne('/api/authentication/reset-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      email: 'user@example.com',
      token: 'reset-token',
      newPassword: 'NewStrongPass1',
    });
    req.flush(null);
  });

  it('confirmEmail posts the email and token', () => {
    service.confirmEmail('user@example.com', 'confirmation-token').subscribe();

    const req = httpMock.expectOne('/api/authentication/confirm-email');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      email: 'user@example.com',
      token: 'confirmation-token',
    });
    req.flush(null);
  });

  it('resendConfirmationEmail posts the email and does not change authentication state', () => {
    service.resendConfirmationEmail('user@example.com').subscribe();

    const req = httpMock.expectOne('/api/authentication/resend-confirmation');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@example.com' });
    req.flush(null);

    expect(service.isAuthenticated()).toBe(false);
  });

  it('changePassword clears the current user on success', () => {
    service.login('user@example.com', 'StrongPass1').subscribe();
    httpMock.expectOne('/api/authentication/login').flush(completedLogin);
    expect(service.isAuthenticated()).toBe(true);

    service.changePassword('StrongPass1', 'NewStrongPass1').subscribe();

    const req = httpMock.expectOne('/api/authentication/change-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      currentPassword: 'StrongPass1',
      newPassword: 'NewStrongPass1',
    });
    req.flush(null);

    expect(service.isAuthenticated()).toBe(false);
  });

  it('deleteAccount clears the current user on success', () => {
    service.login('user@example.com', 'StrongPass1').subscribe();
    httpMock.expectOne('/api/authentication/login').flush(completedLogin);
    expect(service.isAuthenticated()).toBe(true);

    service.deleteAccount('StrongPass1').subscribe();

    const req = httpMock.expectOne('/api/authentication/delete-account');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ currentPassword: 'StrongPass1' });
    req.flush(null);

    expect(service.isAuthenticated()).toBe(false);
  });

  it('exportAccountData GETs the export endpoint and returns the response body', () => {
    const exportData = { id: user.id, email: user.email, createdAtUtc: '2026-01-01T00:00:00Z' };

    service.exportAccountData().subscribe((result) => {
      expect(result).toEqual(exportData);
    });

    const req = httpMock.expectOne('/api/authentication/me/export');
    expect(req.request.method).toBe('GET');
    req.flush(exportData);
  });

  it('getTwoFactorStatus GETs the status endpoint and returns the response body', () => {
    service.getTwoFactorStatus().subscribe((result) => {
      expect(result).toEqual({ enabled: true });
    });

    const req = httpMock.expectOne('/api/authentication/2fa/status');
    expect(req.request.method).toBe('GET');
    req.flush({ enabled: true });
  });

  it('setupTwoFactor posts to the setup endpoint and returns the response body', () => {
    const setup = { sharedKey: 'SHAREDKEY', authenticatorUri: 'otpauth://totp/...' };

    service.setupTwoFactor().subscribe((result) => {
      expect(result).toEqual(setup);
    });

    const req = httpMock.expectOne('/api/authentication/2fa/setup');
    expect(req.request.method).toBe('POST');
    req.flush(setup);
  });

  it('enableTwoFactor posts the code and returns recovery codes', () => {
    const response = { recoveryCodes: ['code-1', 'code-2'] };

    service.enableTwoFactor('123456').subscribe((result) => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne('/api/authentication/2fa/enable');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ code: '123456' });
    req.flush(response);
  });

  it('disableTwoFactor posts the current password', () => {
    service.disableTwoFactor('StrongPass1').subscribe();

    const req = httpMock.expectOne('/api/authentication/2fa/disable');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ currentPassword: 'StrongPass1' });
    req.flush(null);
  });

  it('regenerateRecoveryCodes posts the current password and returns new codes', () => {
    const response = { recoveryCodes: ['new-code-1', 'new-code-2'] };

    service.regenerateRecoveryCodes('StrongPass1').subscribe((result) => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne('/api/authentication/2fa/recovery-codes/regenerate');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ currentPassword: 'StrongPass1' });
    req.flush(response);
  });
});
