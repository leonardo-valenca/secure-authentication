import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { ChangePassword } from './change-password';

describe('ChangePassword', () => {
  let component: ChangePassword;
  let fixture: ComponentFixture<ChangePassword>;
  let authService: { changePassword: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(() => {
    authService = { changePassword: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ChangePassword],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    });

    fixture = TestBed.createComponent(ChangePassword);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  });

  it('does not submit an invalid form', () => {
    component['form'].setValue({
      currentPassword: '',
      newPassword: '',
      confirmNewPassword: '',
    });

    component['submit']();

    expect(authService.changePassword).not.toHaveBeenCalled();
  });

  it('does not submit when the new password matches the current one', () => {
    component['form'].setValue({
      currentPassword: 'StrongPass1!',
      newPassword: 'StrongPass1!',
      confirmNewPassword: 'StrongPass1!',
    });

    component['submit']();

    expect(authService.changePassword).not.toHaveBeenCalled();
  });

  it('does not submit when the new password confirmation does not match', () => {
    component['form'].setValue({
      currentPassword: 'StrongPass1!',
      newPassword: 'NewStrongPass1!',
      confirmNewPassword: 'Different1!',
    });

    component['submit']();

    expect(authService.changePassword).not.toHaveBeenCalled();
  });

  it('changes the password and navigates to /login, since every session was just revoked', () => {
    authService.changePassword.mockReturnValue(of(void 0));
    component['form'].setValue({
      currentPassword: 'StrongPass1!',
      newPassword: 'NewStrongPass1!',
      confirmNewPassword: 'NewStrongPass1!',
    });

    component['submit']();

    expect(authService.changePassword).toHaveBeenCalledWith('StrongPass1!', 'NewStrongPass1!');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('shows an error message when the current password is wrong', () => {
    authService.changePassword.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400 })),
    );
    component['form'].setValue({
      currentPassword: 'wrong-password',
      newPassword: 'NewStrongPass1!',
      confirmNewPassword: 'NewStrongPass1!',
    });

    component['submit']();

    expect(component['errorMessage']()).toBe('Something went wrong. Please try again.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});
