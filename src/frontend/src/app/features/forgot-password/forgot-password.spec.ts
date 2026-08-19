import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { ForgotPassword } from './forgot-password';

describe('ForgotPassword', () => {
  let component: ForgotPassword;
  let fixture: ComponentFixture<ForgotPassword>;
  let authService: { forgotPassword: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authService = { forgotPassword: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ForgotPassword],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    });

    fixture = TestBed.createComponent(ForgotPassword);
    component = fixture.componentInstance;
  });

  it('does not submit an invalid email', () => {
    component['form'].setValue({ email: 'not-an-email' });

    component['submit']();

    expect(authService.forgotPassword).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('shows the same success state whether or not the account exists, matching the backend', () => {
    authService.forgotPassword.mockReturnValue(of(void 0));
    component['form'].setValue({ email: 'user@example.com' });

    component['submit']();

    expect(authService.forgotPassword).toHaveBeenCalledWith('user@example.com');
    expect(component['success']()).toBe(true);
    expect(component['submitting']()).toBe(false);
  });

  it('shows an error message on an actual transport failure', () => {
    authService.forgotPassword.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    component['form'].setValue({ email: 'user@example.com' });

    component['submit']();

    expect(component['errorMessage']()).toBe('Something went wrong. Please try again.');
    expect(component['success']()).toBe(false);
  });
});
