import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { TwoFactorLogin } from './two-factor-login';

describe('TwoFactorLogin', () => {
  let component: TwoFactorLogin;
  let fixture: ComponentFixture<TwoFactorLogin>;
  let authService: { completeTwoFactorLogin: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(() => {
    authService = { completeTwoFactorLogin: vi.fn() };

    TestBed.configureTestingModule({
      imports: [TwoFactorLogin],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    });

    fixture = TestBed.createComponent(TwoFactorLogin);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  });

  it('does not submit an empty code', () => {
    component['form'].setValue({ code: '' });

    component['submit']();

    expect(authService.completeTwoFactorLogin).not.toHaveBeenCalled();
  });

  it('completes the login and navigates to the dashboard on a correct code', () => {
    authService.completeTwoFactorLogin.mockReturnValue(of({ id: '1', email: 'user@example.com' }));
    component['form'].setValue({ code: '123456' });

    component['submit']();

    expect(authService.completeTwoFactorLogin).toHaveBeenCalledWith('123456');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('shows a fallback error message for an incorrect or expired code', () => {
    authService.completeTwoFactorLogin.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400 })),
    );
    component['form'].setValue({ code: '000000' });

    component['submit']();

    expect(component['errorMessage']()).toBe('That code is incorrect or has expired.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});
