import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { DeleteAccount } from './delete-account';

describe('DeleteAccount', () => {
  let component: DeleteAccount;
  let fixture: ComponentFixture<DeleteAccount>;
  let authService: { deleteAccount: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(() => {
    authService = { deleteAccount: vi.fn() };

    TestBed.configureTestingModule({
      imports: [DeleteAccount],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    });

    fixture = TestBed.createComponent(DeleteAccount);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  });

  it('does not submit with an empty password', () => {
    component['form'].setValue({ currentPassword: '' });

    component['submit']();

    expect(authService.deleteAccount).not.toHaveBeenCalled();
  });

  it('deletes the account and navigates to /login on success', () => {
    authService.deleteAccount.mockReturnValue(of(void 0));
    component['form'].setValue({ currentPassword: 'StrongPass1!' });

    component['submit']();

    expect(authService.deleteAccount).toHaveBeenCalledWith('StrongPass1!');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('shows an error message when the password is wrong and does not navigate away', () => {
    authService.deleteAccount.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400 })),
    );
    component['form'].setValue({ currentPassword: 'wrong-password' });

    component['submit']();

    expect(component['errorMessage']()).toBe('Something went wrong. Please try again.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});
