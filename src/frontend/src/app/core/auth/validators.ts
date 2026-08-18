import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

// Mirrors Identity's PasswordOptions on the backend (Infrastructure/DependencyInjection.cs), so
// users see the same rule client-side instead of round-tripping to the server to discover a weak
// password was rejected. Keep these two in sync by hand, nothing enforces it automatically.
export const strongPasswordValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const value = control.value as string;
  if (!value) {
    return null;
  }

  const isValid =
    value.length >= 8 &&
    value.length <= 128 &&
    /[A-Z]/.test(value) &&
    /[a-z]/.test(value) &&
    /[0-9]/.test(value) &&
    /[^A-Za-z0-9]/.test(value);

  return isValid ? null : { weakPassword: true };
};

export function passwordsMatchValidator(passwordField: string, confirmField: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get(passwordField)?.value;
    const confirmPassword = group.get(confirmField)?.value;

    return password === confirmPassword ? null : { passwordsMismatch: true };
  };
}

// Mirrors ChangePasswordCommandValidator's NotEqual rule on the backend.
export function passwordsDifferentValidator(currentField: string, newField: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const currentPassword = group.get(currentField)?.value;
    const newPassword = group.get(newField)?.value;

    return currentPassword && newPassword && currentPassword === newPassword
      ? { samePassword: true }
      : null;
  };
}
