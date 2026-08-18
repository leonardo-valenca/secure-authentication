import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, of, shareReplay, tap } from 'rxjs';

import {
  AccountDataExport,
  AuthenticatedUser,
  LoginResponse,
  RecoveryCodesResponse,
  TwoFactorSetup,
} from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly currentUserSignal = signal<AuthenticatedUser | null>(null);

  private refreshInProgress$: Observable<AuthenticatedUser> | null = null;

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);

  /** Primes the double-submit CSRF cookie. Must run before any state-changing request. */
  primeCsrfToken(): Observable<void> {
    return this.http.get<void>('/api/csrf-token');
  }

  /** Restores session state from the access-token cookie, if any, on app start. */
  bootstrap(): Observable<AuthenticatedUser | null> {
    return this.http.get<AuthenticatedUser>('/api/authentication/me').pipe(
      tap((user) => this.currentUserSignal.set(user)),
      catchError(() => {
        this.currentUserSignal.set(null);
        return of(null);
      }),
    );
  }

  register(email: string, password: string): Observable<AuthenticatedUser> {
    return this.http.post<AuthenticatedUser>('/api/authentication/register', { email, password });
  }

  /** user is only set locally when the response actually completes a login, a 2FA challenge leaves the session unauthenticated until completeTwoFactorLogin succeeds. */
  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/authentication/login', { email, password }).pipe(
      tap((response) => {
        if (!response.requiresTwoFactor && response.user) {
          this.currentUserSignal.set(response.user);
        }
      }),
    );
  }

  /** Completes a login that login() left pending on a second factor, the server reads which pending login from an HttpOnly cookie, not from anything this call sends. */
  completeTwoFactorLogin(code: string): Observable<AuthenticatedUser> {
    return this.http
      .post<AuthenticatedUser>('/api/authentication/2fa/login', { code })
      .pipe(tap((user) => this.currentUserSignal.set(user)));
  }

  refresh(): Observable<AuthenticatedUser> {
    return this.http
      .post<AuthenticatedUser>('/api/authentication/refresh', {})
      .pipe(tap((user) => this.currentUserSignal.set(user)));
  }

  /**
   * Coalesces concurrent refresh attempts into a single in-flight request. Refresh tokens rotate
   * on every use, so two requests independently calling refresh() around the same moment would
   * race: whichever loses the race presents an already-rotated token, which looks identical to a
   * stolen-token replay and revokes the user's entire session. This is what the auth interceptor
   * calls on a 401, which is exactly where concurrent requests can pile up.
   */
  refreshOnce(): Observable<AuthenticatedUser> {
    this.refreshInProgress$ ??= this.refresh().pipe(
      finalize(() => {
        this.refreshInProgress$ = null;
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    return this.refreshInProgress$;
  }

  logout(): Observable<void> {
    return this.http
      .post<void>('/api/authentication/logout', {})
      .pipe(tap(() => this.currentUserSignal.set(null)));
  }

  /** Always resolves, whether or not the email is registered, the backend never leaks that. */
  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>('/api/authentication/forgot-password', { email });
  }

  confirmEmail(email: string, token: string): Observable<void> {
    return this.http.post<void>('/api/authentication/confirm-email', { email, token });
  }

  /** Always resolves, whether or not the email is registered or already confirmed. */
  resendConfirmationEmail(email: string): Observable<void> {
    return this.http.post<void>('/api/authentication/resend-confirmation', { email });
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<void> {
    return this.http.post<void>('/api/authentication/reset-password', {
      email,
      token,
      newPassword,
    });
  }

  /** The backend revokes every session on success, including this one, clear local state to match. */
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http
      .post<void>('/api/authentication/change-password', { currentPassword, newPassword })
      .pipe(tap(() => this.currentUserSignal.set(null)));
  }

  /** Everything this app persists about the current account, for a self-service data export. */
  exportAccountData(): Observable<AccountDataExport> {
    return this.http.get<AccountDataExport>('/api/authentication/me/export');
  }

  /** Irreversible, the backend deletes the account outright, cascading every session with it. */
  deleteAccount(currentPassword: string): Observable<void> {
    return this.http
      .post<void>('/api/authentication/delete-account', { currentPassword })
      .pipe(tap(() => this.currentUserSignal.set(null)));
  }

  /** Live status, not cached client-side state, safe to call every time a settings page loads. */
  getTwoFactorStatus(): Observable<{ enabled: boolean }> {
    return this.http.get<{ enabled: boolean }>('/api/authentication/2fa/status');
  }

  /** Doesn't enable anything yet, just issues (or re-returns) the key an authenticator app scans, ready for enableTwoFactor to confirm. */
  setupTwoFactor(): Observable<TwoFactorSetup> {
    return this.http.post<TwoFactorSetup>('/api/authentication/2fa/setup', {});
  }

  /** Turns 2FA on once the code proves the authenticator app was set up with the right key, returns recovery codes shown to the user exactly once. */
  enableTwoFactor(code: string): Observable<RecoveryCodesResponse> {
    return this.http.post<RecoveryCodesResponse>('/api/authentication/2fa/enable', { code });
  }

  /** Also resets the authenticator key server-side, a later re-enable starts from a fresh QR code. */
  disableTwoFactor(currentPassword: string): Observable<void> {
    return this.http.post<void>('/api/authentication/2fa/disable', { currentPassword });
  }

  /** Invalidates every previously issued recovery code in favor of a fresh set. */
  regenerateRecoveryCodes(currentPassword: string): Observable<RecoveryCodesResponse> {
    return this.http.post<RecoveryCodesResponse>(
      '/api/authentication/2fa/recovery-codes/regenerate',
      {
        currentPassword,
      },
    );
  }
}
