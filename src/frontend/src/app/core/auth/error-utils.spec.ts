import { HttpErrorResponse } from '@angular/common/http';

import { extractErrorMessage } from './error-utils';

describe('extractErrorMessage', () => {
  it('extracts the first validation error message from a ValidationProblem response', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { 'User.InvalidCredentials': ['Email or password is incorrect.'] } },
    });

    expect(extractErrorMessage(error)).toBe('Email or password is incorrect.');
  });

  it('falls back to the default message when the error body has no recognizable shape', () => {
    const error = new HttpErrorResponse({ status: 500, error: {} });

    expect(extractErrorMessage(error)).toBe('Something went wrong. Please try again.');
  });

  it('uses a custom fallback message when provided', () => {
    const error = new HttpErrorResponse({ status: 400, error: {} });

    expect(extractErrorMessage(error, 'Custom fallback.')).toBe('Custom fallback.');
  });

  it('falls back to the default message for non-HTTP errors', () => {
    expect(extractErrorMessage(new Error('boom'))).toBe('Something went wrong. Please try again.');
  });
});
