import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { TwoFactorSettings } from './two-factor-settings';

vi.mock('qrcode', () => ({
  toDataURL: vi.fn().mockResolvedValue('data:image/png;base64,mock'),
}));

describe('TwoFactorSettings', () => {
  let component: TwoFactorSettings;
  let fixture: ComponentFixture<TwoFactorSettings>;
  let authService: {
    getTwoFactorStatus: ReturnType<typeof vi.fn>;
    setupTwoFactor: ReturnType<typeof vi.fn>;
    enableTwoFactor: ReturnType<typeof vi.fn>;
    disableTwoFactor: ReturnType<typeof vi.fn>;
    regenerateRecoveryCodes: ReturnType<typeof vi.fn>;
  };

  const setupResponse = { sharedKey: 'SHAREDKEY', authenticatorUri: 'otpauth://totp/...' };
  const recoveryCodesResponse = { recoveryCodes: ['code-1', 'code-2'] };

  function create(enabled: boolean) {
    authService = {
      getTwoFactorStatus: vi.fn(() => of({ enabled })),
      setupTwoFactor: vi.fn(),
      enableTwoFactor: vi.fn(),
      disableTwoFactor: vi.fn(),
      regenerateRecoveryCodes: vi.fn(),
    };

    TestBed.configureTestingModule({
      imports: [TwoFactorSettings],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    });

    fixture = TestBed.createComponent(TwoFactorSettings);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('shows the disabled view when 2FA is off', () => {
    create(false);

    expect(component['view']()).toBe('disabled');
  });

  it('shows the enabled view when 2FA is on', () => {
    create(true);

    expect(component['view']()).toBe('enabled');
  });

  it('falls back to the disabled view if the status check fails', () => {
    create(false);
    authService.getTwoFactorStatus.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    component.ngOnInit();

    expect(component['view']()).toBe('disabled');
  });

  describe('setup flow', () => {
    beforeEach(() => create(false));

    it('startSetup loads the shared key, QR code, and moves to the setup view', async () => {
      authService.setupTwoFactor.mockReturnValue(of(setupResponse));

      component['startSetup']();
      await Promise.resolve();
      await Promise.resolve();

      expect(component['view']()).toBe('setup');
      expect(component['sharedKey']()).toBe('SHAREDKEY');
      expect(component['qrCodeDataUrl']()).toBe('data:image/png;base64,mock');
    });

    it('startSetup surfaces an error without changing view', () => {
      authService.setupTwoFactor.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 500 })),
      );

      component['startSetup']();

      expect(component['errorMessage']()).toBe('Something went wrong. Please try again.');
      expect(component['view']()).toBe('disabled');
    });

    it('does not confirm setup with an invalid code', () => {
      component['setupForm'].setValue({ code: '' });

      component['confirmSetup']();

      expect(authService.enableTwoFactor).not.toHaveBeenCalled();
    });

    it('confirmSetup shows recovery codes and moves to the recovery-codes view on success', () => {
      authService.enableTwoFactor.mockReturnValue(of(recoveryCodesResponse));
      component['setupForm'].setValue({ code: '123456' });

      component['confirmSetup']();

      expect(authService.enableTwoFactor).toHaveBeenCalledWith('123456');
      expect(component['recoveryCodes']()).toEqual(['code-1', 'code-2']);
      expect(component['view']()).toBe('recovery-codes');
    });

    it('confirmSetup surfaces a wrong-code error', () => {
      authService.enableTwoFactor.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 400 })),
      );
      component['setupForm'].setValue({ code: '000000' });

      component['confirmSetup']();

      expect(component['errorMessage']()).toBe('That code is incorrect.');
    });

    it('acknowledgeRecoveryCodes clears the codes and moves to the enabled view', () => {
      component['recoveryCodes'].set(['code-1']);
      component['view'].set('recovery-codes');

      component['acknowledgeRecoveryCodes']();

      expect(component['recoveryCodes']()).toEqual([]);
      expect(component['view']()).toBe('enabled');
    });

    it('cancelSetup returns to the disabled view and clears the error', () => {
      component['errorMessage'].set('boom');
      component['view'].set('setup');

      component['cancelSetup']();

      expect(component['view']()).toBe('disabled');
      expect(component['errorMessage']()).toBeNull();
    });
  });

  describe('disable flow', () => {
    beforeEach(() => create(true));

    it('startDisable moves to the disable view', () => {
      component['startDisable']();

      expect(component['view']()).toBe('disable');
    });

    it('does not disable with an empty password', () => {
      component['startDisable']();
      component['passwordForm'].setValue({ currentPassword: '' });

      component['confirmDisable']();

      expect(authService.disableTwoFactor).not.toHaveBeenCalled();
    });

    it('confirmDisable moves to the disabled view on success', () => {
      authService.disableTwoFactor.mockReturnValue(of(void 0));
      component['startDisable']();
      component['passwordForm'].setValue({ currentPassword: 'StrongPass1!' });

      component['confirmDisable']();

      expect(authService.disableTwoFactor).toHaveBeenCalledWith('StrongPass1!');
      expect(component['view']()).toBe('disabled');
    });

    it('confirmDisable surfaces an error and stays put', () => {
      authService.disableTwoFactor.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 400 })),
      );
      component['startDisable']();
      component['passwordForm'].setValue({ currentPassword: 'wrong-password' });

      component['confirmDisable']();

      expect(component['errorMessage']()).toBe('Something went wrong. Please try again.');
      expect(component['view']()).toBe('disable');
    });

    it('cancelPasswordPrompt returns to the enabled view and clears the error', () => {
      component['errorMessage'].set('boom');
      component['view'].set('disable');

      component['cancelPasswordPrompt']();

      expect(component['view']()).toBe('enabled');
      expect(component['errorMessage']()).toBeNull();
    });
  });

  describe('regenerate flow', () => {
    beforeEach(() => create(true));

    it('startRegenerate moves to the regenerate view', () => {
      component['startRegenerate']();

      expect(component['view']()).toBe('regenerate');
    });

    it('does not regenerate with an empty password', () => {
      component['startRegenerate']();
      component['passwordForm'].setValue({ currentPassword: '' });

      component['confirmRegenerate']();

      expect(authService.regenerateRecoveryCodes).not.toHaveBeenCalled();
    });

    it('confirmRegenerate shows the new codes and moves to the recovery-codes view', () => {
      authService.regenerateRecoveryCodes.mockReturnValue(
        of({ recoveryCodes: ['new-1', 'new-2'] }),
      );
      component['startRegenerate']();
      component['passwordForm'].setValue({ currentPassword: 'StrongPass1!' });

      component['confirmRegenerate']();

      expect(authService.regenerateRecoveryCodes).toHaveBeenCalledWith('StrongPass1!');
      expect(component['recoveryCodes']()).toEqual(['new-1', 'new-2']);
      expect(component['view']()).toBe('recovery-codes');
    });

    it('confirmRegenerate surfaces an error and stays put', () => {
      authService.regenerateRecoveryCodes.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 400 })),
      );
      component['startRegenerate']();
      component['passwordForm'].setValue({ currentPassword: 'wrong-password' });

      component['confirmRegenerate']();

      expect(component['errorMessage']()).toBe('Something went wrong. Please try again.');
      expect(component['view']()).toBe('regenerate');
    });
  });
});
