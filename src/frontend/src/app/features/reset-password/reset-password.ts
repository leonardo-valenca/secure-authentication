import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { extractErrorMessage } from '../../core/auth/error-utils';
import { passwordsMatchValidator, strongPasswordValidator } from '../../core/auth/validators';

@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './reset-password.scss',
})
export class ResetPassword {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  // Carried as query params on the emailed reset link, not typed by the user.
  private readonly email = this.route.snapshot.queryParamMap.get('email');
  private readonly token = this.route.snapshot.queryParamMap.get('token');

  protected readonly linkInvalid = !this.email || !this.token;

  protected readonly form = this.fb.nonNullable.group(
    {
      newPassword: ['', [Validators.required, strongPasswordValidator]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatchValidator('newPassword', 'confirmPassword') },
  );

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly success = signal(false);

  protected submit(): void {
    if (this.linkInvalid || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const { newPassword } = this.form.getRawValue();

    this.authService.resetPassword(this.email!, this.token!, newPassword).subscribe({
      next: () => {
        this.submitting.set(false);
        this.success.set(true);
        setTimeout(() => this.router.navigateByUrl('/login'), 1200);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(
          extractErrorMessage(error, 'This reset link is invalid or has expired.'),
        );
      },
    });
  }
}
