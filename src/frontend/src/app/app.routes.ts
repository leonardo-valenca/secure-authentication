import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'login', loadComponent: () => import('./features/login/login').then((m) => m.Login) },
  {
    // Not authGuard-protected: at this point the user has only a short-lived mfa_challenge
    // cookie, not a real session, so authGuard's isAuthenticated() check would just bounce them
    // straight back to /login.
    path: 'login/two-factor',
    loadComponent: () =>
      import('./features/two-factor-login/two-factor-login').then((m) => m.TwoFactorLogin),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/register/register').then((m) => m.Register),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/forgot-password/forgot-password').then((m) => m.ForgotPassword),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/reset-password/reset-password').then((m) => m.ResetPassword),
  },
  {
    path: 'confirm-email',
    loadComponent: () =>
      import('./features/confirm-email/confirm-email').then((m) => m.ConfirmEmail),
  },
  {
    path: 'resend-confirmation',
    loadComponent: () =>
      import('./features/resend-confirmation/resend-confirmation').then(
        (m) => m.ResendConfirmation,
      ),
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
    canActivate: [authGuard],
  },
  {
    path: 'change-password',
    loadComponent: () =>
      import('./features/change-password/change-password').then((m) => m.ChangePassword),
    canActivate: [authGuard],
  },
  {
    path: 'delete-account',
    loadComponent: () =>
      import('./features/delete-account/delete-account').then((m) => m.DeleteAccount),
    canActivate: [authGuard],
  },
  {
    path: 'two-factor-settings',
    loadComponent: () =>
      import('./features/two-factor-settings/two-factor-settings').then((m) => m.TwoFactorSettings),
    canActivate: [authGuard],
  },
  { path: '**', redirectTo: 'dashboard' },
];
