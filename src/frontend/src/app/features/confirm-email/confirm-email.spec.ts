import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { ConfirmEmail } from './confirm-email';

describe('ConfirmEmail', () => {
  let component: ConfirmEmail;
  let fixture: ComponentFixture<ConfirmEmail>;
  let authService: {
    confirmEmail: ReturnType<typeof vi.fn>;
    resendConfirmationEmail: ReturnType<typeof vi.fn>;
  };

  function create(queryParams: Record<string, string>) {
    authService = { confirmEmail: vi.fn(), resendConfirmationEmail: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ConfirmEmail],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } },
        },
      ],
    });

    fixture = TestBed.createComponent(ConfirmEmail);
    component = fixture.componentInstance;
  }

  it('flags the link as invalid and never calls confirmEmail when params are missing', () => {
    create({});
    fixture.detectChanges();

    expect(component['linkInvalid']).toBe(true);
    expect(authService.confirmEmail).not.toHaveBeenCalled();
    expect(component['state']()).toBe('confirming');
  });

  it('confirms the email and shows success when the link is valid', () => {
    create({ email: 'user@example.com', token: 'a-token' });
    authService.confirmEmail.mockReturnValue(of(void 0));

    fixture.detectChanges();

    expect(authService.confirmEmail).toHaveBeenCalledWith('user@example.com', 'a-token');
    expect(component['state']()).toBe('success');
  });

  it('shows an error state when confirmation fails', () => {
    create({ email: 'user@example.com', token: 'expired-token' });
    authService.confirmEmail.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400 })),
    );

    fixture.detectChanges();

    expect(component['state']()).toBe('error');
    expect(component['errorMessage']()).toBe('This confirmation link is invalid or has expired.');
  });

  it('resend() sends the email from the link and flips to resent, even on failure', () => {
    create({ email: 'user@example.com', token: 'expired-token' });
    authService.confirmEmail.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400 })),
    );
    authService.resendConfirmationEmail.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    fixture.detectChanges();

    component['resend']();

    expect(authService.resendConfirmationEmail).toHaveBeenCalledWith('user@example.com');
    expect(component['resent']()).toBe(true);
    expect(component['resending']()).toBe(false);
  });
});
