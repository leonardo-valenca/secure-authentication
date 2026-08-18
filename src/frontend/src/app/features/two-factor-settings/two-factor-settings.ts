import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { toDataURL } from 'qrcode';

import { AuthService } from '../../core/auth/auth.service';
import { extractErrorMessage } from '../../core/auth/error-utils';

type View =
  'loading' | 'disabled' | 'setup' | 'recovery-codes' | 'enabled' | 'disable' | 'regenerate';

@Component({
  selector: 'app-two-factor-settings',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './two-factor-settings.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './two-factor-settings.scss',
})
export class TwoFactorSettings implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  protected readonly view = signal<View>('loading');
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly sharedKey = signal<string | null>(null);
  protected readonly qrCodeDataUrl = signal<string | null>(null);
  protected readonly recoveryCodes = signal<string[]>([]);

  protected readonly setupForm = this.fb.nonNullable.group({
    code: ['', [Validators.required]],
  });

  protected readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
  });

  ngOnInit(): void {
    this.authService.getTwoFactorStatus().subscribe({
      next: ({ enabled }) => this.view.set(enabled ? 'enabled' : 'disabled'),
      error: () => this.view.set('disabled'),
    });
  }

  protected startSetup(): void {
    this.errorMessage.set(null);
    this.setupForm.reset();

    this.authService.setupTwoFactor().subscribe({
      next: ({ sharedKey, authenticatorUri }) => {
        this.sharedKey.set(sharedKey);
        void toDataURL(authenticatorUri).then((dataUrl) => this.qrCodeDataUrl.set(dataUrl));
        this.view.set('setup');
      },
      error: (error: unknown) => this.errorMessage.set(extractErrorMessage(error)),
    });
  }

  protected confirmSetup(): void {
    if (this.setupForm.invalid) {
      this.setupForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.authService.enableTwoFactor(this.setupForm.getRawValue().code).subscribe({
      next: ({ recoveryCodes }) => {
        this.submitting.set(false);
        this.recoveryCodes.set(recoveryCodes);
        this.view.set('recovery-codes');
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(extractErrorMessage(error, 'That code is incorrect.'));
      },
    });
  }

  /** The recovery codes screen is a one-time notice, not a route - this is just "I've saved them", not a server call. */
  protected acknowledgeRecoveryCodes(): void {
    this.recoveryCodes.set([]);
    this.view.set('enabled');
  }

  protected startDisable(): void {
    this.errorMessage.set(null);
    this.passwordForm.reset();
    this.view.set('disable');
  }

  protected confirmDisable(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.authService.disableTwoFactor(this.passwordForm.getRawValue().currentPassword).subscribe({
      next: () => {
        this.submitting.set(false);
        this.view.set('disabled');
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(extractErrorMessage(error));
      },
    });
  }

  protected startRegenerate(): void {
    this.errorMessage.set(null);
    this.passwordForm.reset();
    this.view.set('regenerate');
  }

  protected confirmRegenerate(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.authService
      .regenerateRecoveryCodes(this.passwordForm.getRawValue().currentPassword)
      .subscribe({
        next: ({ recoveryCodes }) => {
          this.submitting.set(false);
          this.recoveryCodes.set(recoveryCodes);
          this.view.set('recovery-codes');
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.errorMessage.set(extractErrorMessage(error));
        },
      });
  }

  protected cancelSetup(): void {
    this.errorMessage.set(null);
    this.view.set('disabled');
  }

  protected cancelPasswordPrompt(): void {
    this.errorMessage.set(null);
    this.view.set('enabled');
  }
}
