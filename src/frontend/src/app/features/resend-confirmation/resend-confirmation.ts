import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { extractErrorMessage } from '../../core/auth/error-utils';

@Component({
  selector: 'app-resend-confirmation',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './resend-confirmation.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './resend-confirmation.scss',
})
export class ResendConfirmation {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  // Prefilled when arriving from register/login, still editable, not trusted for anything.
  protected readonly form = this.fb.nonNullable.group({
    email: [
      this.route.snapshot.queryParamMap.get('email') ?? '',
      [Validators.required, Validators.email],
    ],
  });

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly success = signal(false);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const { email } = this.form.getRawValue();

    this.authService.resendConfirmationEmail(email).subscribe({
      // Always shows the same success state, matching the backend's refusal to reveal
      // whether the email is registered or already confirmed.
      next: () => {
        this.submitting.set(false);
        this.success.set(true);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(extractErrorMessage(error));
      },
    });
  }
}
