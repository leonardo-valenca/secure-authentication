import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { ResetPassword } from './reset-password';

describe('ResetPassword', () => {
  let component: ResetPassword;
  let fixture: ComponentFixture<ResetPassword>;
  let authService: { resetPassword: ReturnType<typeof vi.fn> };
  let router: Router;

  function create(queryParams: Record<string, string>) {
    authService = { resetPassword: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ResetPassword],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } },
        },
      ],
    });

    fixture = TestBed.createComponent(ResetPassword);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  }

  it('flags the link as invalid and never submits when params are missing', () => {
    create({});
    component['form'].setValue({ newPassword: 'StrongPass1!', confirmPassword: 'StrongPass1!' });

    component['submit']();

    expect(component['linkInvalid']).toBe(true);
    expect(authService.resetPassword).not.toHaveBeenCalled();
  });

  it('does not submit an invalid form', () => {
    create({ email: 'user@example.com', token: 'a-token' });
    component['form'].setValue({ newPassword: 'weak', confirmPassword: 'weak' });

    component['submit']();

    expect(authService.resetPassword).not.toHaveBeenCalled();
  });

  it('does not submit when the passwords do not match', () => {
    create({ email: 'user@example.com', token: 'a-token' });
    component['form'].setValue({
      newPassword: 'StrongPass1!',
      confirmPassword: 'Different1!',
    });

    component['submit']();

    expect(authService.resetPassword).not.toHaveBeenCalled();
  });

  it('resets the password, shows success, and redirects to /login shortly after', () => {
    vi.useFakeTimers();
    create({ email: 'user@example.com', token: 'a-token' });
    authService.resetPassword.mockReturnValue(of(void 0));
    component['form'].setValue({
      newPassword: 'StrongPass1!',
      confirmPassword: 'StrongPass1!',
    });

    component['submit']();

    expect(authService.resetPassword).toHaveBeenCalledWith(
      'user@example.com',
      'a-token',
      'StrongPass1!',
    );
    expect(component['success']()).toBe(true);
    expect(router.navigateByUrl).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1200);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');

    vi.useRealTimers();
  });

  it('shows a fallback error message when the reset link is invalid or expired', () => {
    create({ email: 'user@example.com', token: 'expired-token' });
    authService.resetPassword.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400 })),
    );
    component['form'].setValue({
      newPassword: 'StrongPass1!',
      confirmPassword: 'StrongPass1!',
    });

    component['submit']();

    expect(component['errorMessage']()).toBe('This reset link is invalid or has expired.');
    expect(component['success']()).toBe(false);
  });
});
