import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { extractErrorMessage } from '../../core/auth/error-utils';

type ConfirmEmailState = 'confirming' | 'success' | 'error';

@Component({
  selector: 'app-confirm-email',
  imports: [RouterLink],
  templateUrl: './confirm-email.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './confirm-email.scss',
})
export class ConfirmEmail implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  // Carried as query params on the emailed confirmation link, not typed by the user.
  protected readonly email = this.route.snapshot.queryParamMap.get('email');
  private readonly token = this.route.snapshot.queryParamMap.get('token');

  protected readonly linkInvalid = !this.email || !this.token;

  protected readonly state = signal<ConfirmEmailState>('confirming');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly resending = signal(false);
  protected readonly resent = signal(false);

  ngOnInit(): void {
    if (this.linkInvalid) {
      return;
    }

    this.authService.confirmEmail(this.email!, this.token!).subscribe({
      next: () => this.state.set('success'),
      error: (error: unknown) => {
        this.state.set('error');
        this.errorMessage.set(
          extractErrorMessage(error, 'This confirmation link is invalid or has expired.'),
        );
      },
    });
  }

  protected resend(): void {
    if (!this.email) {
      return;
    }

    this.resending.set(true);

    // The backend never reveals whether the resend actually found an account, resolve the same
    // way either way rather than surfacing a network-error branch the user can't act on.
    this.authService
      .resendConfirmationEmail(this.email)
      .pipe(
        catchError(() => of(void 0)),
        finalize(() => this.resending.set(false)),
      )
      .subscribe(() => this.resent.set(true));
  }
}
