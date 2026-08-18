import { FormControl, FormGroup } from '@angular/forms';

import {
  passwordsDifferentValidator,
  passwordsMatchValidator,
  strongPasswordValidator,
} from './validators';

describe('strongPasswordValidator', () => {
  it('allows an empty value (required is a separate validator)', () => {
    expect(strongPasswordValidator(new FormControl(''))).toBeNull();
  });

  it('rejects a password shorter than 8 characters', () => {
    expect(strongPasswordValidator(new FormControl('Ab1'))).toEqual({ weakPassword: true });
  });

  it('rejects a password longer than 128 characters (mirrors RegisterCommandValidator)', () => {
    const tooLong = 'Aa1'.repeat(60);
    expect(strongPasswordValidator(new FormControl(tooLong))).toEqual({ weakPassword: true });
  });

  it('rejects a password with no uppercase letter', () => {
    expect(strongPasswordValidator(new FormControl('lowercase1'))).toEqual({ weakPassword: true });
  });

  it('rejects a password with no lowercase letter', () => {
    expect(strongPasswordValidator(new FormControl('UPPERCASE1'))).toEqual({ weakPassword: true });
  });

  it('rejects a password with no digit', () => {
    expect(strongPasswordValidator(new FormControl('NoDigitsHere'))).toEqual({
      weakPassword: true,
    });
  });

  it('rejects a password with no non-alphanumeric character', () => {
    expect(strongPasswordValidator(new FormControl('NoSymbolHere1'))).toEqual({
      weakPassword: true,
    });
  });

  it('accepts a password meeting every rule', () => {
    expect(strongPasswordValidator(new FormControl('StrongPass1!'))).toBeNull();
  });
});

describe('passwordsMatchValidator', () => {
  function buildGroup(password: string, confirmPassword: string): FormGroup {
    return new FormGroup({
      password: new FormControl(password),
      confirmPassword: new FormControl(confirmPassword),
    });
  }

  const validator = passwordsMatchValidator('password', 'confirmPassword');

  it('returns an error when the two fields differ', () => {
    expect(validator(buildGroup('StrongPass1', 'Different1'))).toEqual({ passwordsMismatch: true });
  });

  it('returns null when the two fields match', () => {
    expect(validator(buildGroup('StrongPass1', 'StrongPass1'))).toBeNull();
  });
});

describe('passwordsDifferentValidator', () => {
  function buildGroup(currentPassword: string, newPassword: string): FormGroup {
    return new FormGroup({
      currentPassword: new FormControl(currentPassword),
      newPassword: new FormControl(newPassword),
    });
  }

  const validator = passwordsDifferentValidator('currentPassword', 'newPassword');

  it('returns an error when the new password matches the current one', () => {
    expect(validator(buildGroup('StrongPass1', 'StrongPass1'))).toEqual({ samePassword: true });
  });

  it('returns null when the new password differs', () => {
    expect(validator(buildGroup('StrongPass1', 'Different1'))).toBeNull();
  });

  it('returns null while either field is still empty', () => {
    expect(validator(buildGroup('', ''))).toBeNull();
  });
});
