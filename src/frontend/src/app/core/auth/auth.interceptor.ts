import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';

import { AuthService } from './auth.service';

// Auth endpoints are excluded from the silent-refresh-and-retry flow: a 401 from /login or
// /register is a real credential failure, and retrying /refresh itself would just loop forever.
const REFRESH_EXEMPT_PATHS = [
  '/api/authentication/login',
  '/api/authentication/register',
  '/api/authentication/refresh',
  '/api/authentication/logout',
];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const credentialedReq = req.clone({ withCredentials: true });

  if (REFRESH_EXEMPT_PATHS.some((path) => req.url.includes(path))) {
    return next(credentialedReq);
  }

  return next(credentialedReq).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        return authService.refreshOnce().pipe(switchMap(() => next(credentialedReq)));
      }

      return throwError(() => error);
    }),
  );
};
