import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { Register } from './register';

describe('Register', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;
  let authService: { register: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authService = { register: vi.fn() };

    TestBed.configureTestingModule({
      imports: [Register],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    });

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
  });

  function fillForm(email: string, password: string, confirmPassword: string) {
    component['form'].setValue({ email, password, confirmPassword });
  }

  it('does not submit an invalid form', () => {
    fillForm('', '', '');

    component['submit']();

    expect(authService.register).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('does not submit when the passwords do not match', () => {
    fillForm('user@example.com', 'StrongPass1!', 'Different1!');

    component['submit']();

    expect(authService.register).not.toHaveBeenCalled();
  });

  it('shows a success state with a resend link prefilled with the registered email', () => {
    authService.register.mockReturnValue(
      of({ id: '1', email: 'user@example.com' } as never),
    );
    fillForm('user@example.com', 'StrongPass1!', 'StrongPass1!');

    component['submit']();

    expect(authService.register).toHaveBeenCalledWith('user@example.com', 'StrongPass1!');
    expect(component['success']()).toBe(true);
    expect(component['submitting']()).toBe(false);

    fixture.detectChanges();
    const resendLink = (fixture.nativeElement as HTMLElement).querySelector(
      'a[href*="resend-confirmation"]',
    );
    expect(resendLink?.getAttribute('href')).toContain('email=user@example.com');
  });

  it('shows an error message when registration fails, e.g. the email is already registered', () => {
    authService.register.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { errors: { 'User.EmailAlreadyInUse': ['This email is already registered.'] } },
          }),
      ),
    );
    fillForm('user@example.com', 'StrongPass1!', 'StrongPass1!');

    component['submit']();

    expect(component['errorMessage']()).toBe('This email is already registered.');
    expect(component['success']()).toBe(false);
    expect(component['submitting']()).toBe(false);
  });
});
