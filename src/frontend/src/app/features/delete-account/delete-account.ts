import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { extractErrorMessage } from '../../core/auth/error-utils';

@Component({
  selector: 'app-delete-account',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './delete-account.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './delete-account.scss',
})
export class DeleteAccount {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
  });

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const { currentPassword } = this.form.getRawValue();

    this.authService.deleteAccount(currentPassword).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigateByUrl('/login');
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(extractErrorMessage(error));
      },
    });
  }
}
