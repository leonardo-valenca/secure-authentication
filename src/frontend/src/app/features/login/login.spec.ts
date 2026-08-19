import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { LoginResponse } from '../../core/auth/models';
import { Login } from './login';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;
  let authService: { login: ReturnType<typeof vi.fn> };
  let router: Router;

  function setup() {
    authService = { login: vi.fn() };

    TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    });

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  }

  function fillForm(email: string, password: string) {
    component['form'].setValue({ email, password });
  }

  beforeEach(() => setup());

  it('does not submit an invalid form', () => {
    fillForm('', '');

    component['submit']();

    expect(authService.login).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('navigates to the dashboard on a completed login', () => {
    const response: LoginResponse = {
      requiresTwoFactor: false,
      user: { id: '1', email: 'user@example.com' },
    };
    authService.login.mockReturnValue(of(response));
    fillForm('user@example.com', 'StrongPass1!');

    component['submit']();

    expect(authService.login).toHaveBeenCalledWith('user@example.com', 'StrongPass1!');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/dashboard');
    expect(component['submitting']()).toBe(false);
  });

  it('navigates to the 2FA challenge when the login requires a second factor', () => {
    const response: LoginResponse = { requiresTwoFactor: true, user: null };
    authService.login.mockReturnValue(of(response));
    fillForm('user@example.com', 'StrongPass1!');

    component['submit']();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/login/two-factor');
  });

  it('shows a fallback error message and no resend link for a generic login failure', () => {
    authService.login.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: {} })),
    );
    fillForm('user@example.com', 'wrong-password');

    component['submit']();

    expect(component['errorMessage']()).toBe('Email or password is incorrect.');
    expect(component['emailNotConfirmed']()).toBe(false);
    expect(component['submitting']()).toBe(false);
  });

  it('flags emailNotConfirmed and surfaces the resend link for a User.EmailNotConfirmed error', () => {
    authService.login.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              errors: {
                'User.EmailNotConfirmed': ['Please confirm your email address before logging in.'],
              },
            },
          }),
      ),
    );
    fillForm('user@example.com', 'StrongPass1!');

    component['submit']();

    expect(component['emailNotConfirmed']()).toBe(true);
    expect(component['errorMessage']()).toBe(
      'Please confirm your email address before logging in.',
    );

    fixture.detectChanges();
    const resendLink = (fixture.nativeElement as HTMLElement).querySelector(
      'a[href*="resend-confirmation"]',
    );
    expect(resendLink).not.toBeNull();
  });
});
