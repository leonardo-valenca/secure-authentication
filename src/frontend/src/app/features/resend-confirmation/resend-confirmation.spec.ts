import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { ResendConfirmation } from './resend-confirmation';

describe('ResendConfirmation', () => {
  let component: ResendConfirmation;
  let fixture: ComponentFixture<ResendConfirmation>;
  let authService: { resendConfirmationEmail: ReturnType<typeof vi.fn> };

  function create(queryParams: Record<string, string> = {}) {
    authService = { resendConfirmationEmail: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ResendConfirmation],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } },
        },
      ],
    });

    fixture = TestBed.createComponent(ResendConfirmation);
    component = fixture.componentInstance;
  }

  it('prefills the email from the query param, when present', () => {
    create({ email: 'user@example.com' });

    expect(component['form'].controls.email.value).toBe('user@example.com');
  });

  it('starts with an empty email when no query param is present', () => {
    create();

    expect(component['form'].controls.email.value).toBe('');
  });

  it('does not submit an invalid email', () => {
    create();
    component['form'].setValue({ email: 'not-an-email' });

    component['submit']();

    expect(authService.resendConfirmationEmail).not.toHaveBeenCalled();
    expect(component['form'].touched).toBe(true);
  });

  it('shows the same success state whether or not the account exists, matching the backend', () => {
    create();
    authService.resendConfirmationEmail.mockReturnValue(of(void 0));
    component['form'].setValue({ email: 'user@example.com' });

    component['submit']();

    expect(authService.resendConfirmationEmail).toHaveBeenCalledWith('user@example.com');
    expect(component['success']()).toBe(true);
    expect(component['submitting']()).toBe(false);
  });

  it('shows an error message on an actual delivery/transport failure', () => {
    create();
    authService.resendConfirmationEmail.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    component['form'].setValue({ email: 'user@example.com' });

    component['submit']();

    expect(component['errorMessage']()).toBe('Something went wrong. Please try again.');
    expect(component['success']()).toBe(false);
  });
});
